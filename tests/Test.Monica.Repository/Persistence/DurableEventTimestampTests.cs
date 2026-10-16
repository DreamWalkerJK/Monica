using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Monica.EventBus.Abstractions;
using Monica.EventBus.Models;
using Monica.Repository.Inbox.Models;
using Monica.Repository.Outbox.Models;
using Test.Monica.Repository.Hosting;
using Test.Monica.Repository.UnitOfWork;
using Xunit;

namespace Test.Monica.Repository.Persistence;

public sealed class DurableEventTimestampTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static readonly DateTime _utc = new(2031, 2, 3, 4, 5, 6, DateTimeKind.Utc);

    [Fact]
    public async Task TimestampMapping_WhenCreatingParameters_ShouldPreserveUtcTicksWithoutTimezoneKind()
    {
        await using var app = await new RepositoryScenarioFactory(inbox: true).CreateAsync(cancellationToken: Token);
        await using var scope = app.CreateScope(Token);
        var db = scope.Resolve<TestRepositoryDbContext>();
        (Type Entity, string Property, bool Nullable)[] timestamps =
        [
            (typeof(OutboxMessage), nameof(OutboxMessage.CreatedAtUtc), false),
            (typeof(OutboxMessage), nameof(OutboxMessage.DeliveredAtUtc), true),
            (typeof(OutboxMessage), nameof(OutboxMessage.LeaseUntilUtc), true),
            (typeof(OutboxMessage), nameof(OutboxMessage.NextAttemptAtUtc), true),
            (typeof(InboxReceipt), nameof(InboxReceipt.ReceivedAtUtc), false)
        ];
        var instant = _utc.AddTicks(1234567);

        foreach (var (entityType, propertyName, nullable) in timestamps)
        {
            var property = db.Model.FindEntityType(entityType)!.FindProperty(propertyName)!;
            var mapping = property.GetRelationalTypeMapping();
            using var command = new SqliteCommand();
            var parameter = mapping.CreateParameter(command, "timestamp", instant, nullable);
            var stored = Assert.IsType<DateTime>(parameter.Value);

            Assert.Equal(DateTimeKind.Unspecified, stored.Kind);
            Assert.Equal(instant.Ticks, stored.Ticks);
            Assert.Equal(nullable, property.IsNullable);
            var converter = mapping.Converter;
            Assert.NotNull(converter);
            AssertUtc(Assert.IsType<DateTime>(converter.ConvertFromProvider(stored)), instant);
            if (nullable)
            {
                Assert.Equal(DBNull.Value, mapping.CreateParameter(command, "null_timestamp", null, true).Value);
                Assert.Null(converter.ConvertFromProvider(null));
            }
        }
    }

    [Fact]
    public async Task DurableEvents_WhenPublishedReceivedAndDelivered_ShouldUseCompatibleParametersAndReadUtc()
    {
        var parameters = new TimestampParameterRecorder();
        await using var app = await new RepositoryScenarioFactory(inbox: true, interceptor: parameters,
            configure: services => services.AddSingleton<TimeProvider>(new FixedTimeProvider()))
            .CreateAsync(cancellationToken: Token);

        await app.ExecuteAsync(async scope =>
            await scope.Resolve<IDistributedEventBus>().PublishAsync(new IntegrationNotice("timestamp"),
                cancellationToken: Token), cancellationToken: Token);

        EventMessage incoming;
        await using (var scope = app.CreateScope(Token))
        {
            incoming = scope.Resolve<IEventMessageFactory>().Prepare(typeof(InboxNotice),
                new InboxNotice("timestamp"), EventSubscriptionScope.Distributed);
        }
        await app.Services.GetRequiredService<IEventReceiveDispatcher>().DispatchAsync(incoming, Token);

        await app.VerifyAsync<TestRepositoryDbContext>(async (db, ct) =>
        {
            var row = Assert.Single(await db.Set<OutboxMessage>().ToListAsync(ct));
            AssertUtc(row.CreatedAtUtc, _utc);
            Assert.Null(row.DeliveredAtUtc);
            Assert.Null(row.LeaseUntilUtc);
            Assert.Null(row.NextAttemptAtUtc);
            var receipts = await db.Set<InboxReceipt>().ToListAsync(ct);
            Assert.Equal(2, receipts.Count);
            Assert.All(receipts, receipt => AssertUtc(receipt.ReceivedAtUtc, _utc));
        }, Token);

        Assert.Equal(1, await app.DrainOutboxAsync<TestRepositoryDbContext>(cancellationToken: Token));
        var leaseUntil = _utc.AddMinutes(1);
        var retryAt = _utc.AddMinutes(2);
        await app.VerifyAsync<TestRepositoryDbContext>(async (db, ct) =>
        {
            var row = Assert.Single(await db.Set<OutboxMessage>().ToListAsync(ct));
            AssertUtc(Assert.IsType<DateTime>(row.DeliveredAtUtc), _utc);
            Assert.Null(row.LeaseUntilUtc);
            Assert.Null(row.NextAttemptAtUtc);
            await db.Set<OutboxMessage>().ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.LeaseUntilUtc, leaseUntil)
                .SetProperty(x => x.NextAttemptAtUtc, retryAt), ct);
        }, Token);
        await app.VerifyAsync<TestRepositoryDbContext>(async (db, ct) =>
        {
            var row = Assert.Single(await db.Set<OutboxMessage>().ToListAsync(ct));
            AssertUtc(Assert.IsType<DateTime>(row.LeaseUntilUtc), leaseUntil);
            AssertUtc(Assert.IsType<DateTime>(row.NextAttemptAtUtc), retryAt);
        }, Token);

        Assert.Contains(parameters.Values, value => value.Command.Contains("INSERT INTO \"MonicaOutbox\"", StringComparison.Ordinal));
        Assert.Contains(parameters.Values, value => value.Command.Contains("INSERT INTO \"MonicaInbox\"", StringComparison.Ordinal));
        Assert.Contains(parameters.Values, value => value.Command.Contains("UPDATE \"MonicaOutbox\"", StringComparison.Ordinal));
        Assert.Contains(parameters.Values, value => value.Command.StartsWith("SELECT", StringComparison.Ordinal));
        Assert.All(parameters.Values, value => Assert.Equal(DateTimeKind.Unspecified, value.Value.Kind));
        Assert.Contains(parameters.Values, value => value.Value.Ticks == leaseUntil.Ticks);
        Assert.Contains(parameters.Values, value => value.Value.Ticks == retryAt.Ticks);
    }

    private static void AssertUtc(DateTime actual, DateTime expected)
    {
        Assert.Equal(DateTimeKind.Utc, actual.Kind);
        Assert.Equal(expected.Ticks, actual.Ticks);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(_utc);
    }

    private sealed class TimestampParameterRecorder : DbCommandInterceptor
    {
        public List<(string Command, DateTime Value)> Values { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Record(command);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Record(command);
            return ValueTask.FromResult(result);
        }

        private void Record(DbCommand command)
        {
            if (!command.CommandText.Contains("\"MonicaOutbox\"", StringComparison.Ordinal)
                && !command.CommandText.Contains("\"MonicaInbox\"", StringComparison.Ordinal)) return;
            foreach (DbParameter parameter in command.Parameters)
            {
                if (parameter.Value is DateTime value) Values.Add((command.CommandText, value));
            }
        }
    }
}
