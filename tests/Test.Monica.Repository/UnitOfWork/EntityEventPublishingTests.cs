using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Monica.EventBus.Abstractions;
using Monica.Repository.Outbox.Abstractions;
using Monica.Repository.Outbox.Models;
using Monica.Repository.Persistence.Abstractions;
using Monica.Testing.Doubles;
using NSubstitute;
using Test.Monica.Repository.Hosting;
using Test.Monica.Repository.Persistence;
using Xunit;

namespace Test.Monica.Repository.UnitOfWork;

public sealed class EntityEventPublishingTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SaveChanges_WhenEntityHasScopedProjections_ShouldCaptureDistinctContractsAndAllowSkipping()
    {
        await using var app = await new RepositoryScenarioFactory(
            configure: services => services.AddScoped<ProjectionScope>(),
            configureOutbox: options =>
            {
                options.RegisterEntity<GeneratedKeyRow, ScopedProjection>("tests.scoped.v1",
                    (services, row) => new ScopedProjection(row.Id, services.GetRequiredService<ProjectionScope>().Id), OutboxDestination.Local);
                options.RegisterEntity<GeneratedKeyRow, SkippedProjection>("tests.skipped.v1",
                    (_, _) => null, OutboxDestination.Local);
            }).CreateAsync(cancellationToken: Token);
        Guid scopeId = default;
        await app.ExecuteAsync(scope =>
        {
            scopeId = scope.Resolve<ProjectionScope>().Id;
            scope.Resolve<TestRepositoryDbContext>().Add(new GeneratedKeyRow { Title = "two projections" });
            return Task.CompletedTask;
        }, cancellationToken: Token);
        Assert.Equal(2, await app.DrainOutboxAsync<TestRepositoryDbContext>(cancellationToken: Token));
        var bus = app.Services.GetRequiredService<RecordingEventBus>();
        var generated = Assert.Single(bus.Recorded<OutboxDelivery<EntityChange<GeneratedProjection>>>());
        var scoped = Assert.Single(bus.Recorded<OutboxDelivery<EntityChange<ScopedProjection>>>());
        Assert.Equal(generated.Payload.Entity.Id, scoped.Payload.Entity.Id);
        Assert.Equal(scopeId, scoped.Payload.Entity.ScopeId);
        Assert.Empty(bus.Recorded<OutboxDelivery<EntityChange<SkippedProjection>>>());
    }

    [Fact]
    public async Task Outbox_WhenDifferentEntitiesShareAContract_ShouldCaptureBothWithGeneratedKeys()
    {
        await using var app = await new RepositoryScenarioFactory(configureOutbox: options =>
        {
            options.RegisterEntity<SoftDeleteAuditRow, SharedProjection>("tests.shared.v1",
                row => new SharedProjection(row.Id, row.Title), OutboxDestination.Local);
            options.RegisterEntity<GeneratedKeyRow, SharedProjection>("tests.shared.v1",
                row => new SharedProjection(row.Id, row.Title), OutboxDestination.Local);
        }).CreateAsync(cancellationToken: Token);
        await app.ExecuteAsync(operation =>
        {
            operation.Resolve<TestRepositoryDbContext>().AddRange(
                new SoftDeleteAuditRow { Id = 41, Title = "audited" }, new GeneratedKeyRow { Title = "generated" });
            return Task.CompletedTask;
        }, cancellationToken: Token);

        Assert.Equal(4, await app.DrainOutboxAsync<TestRepositoryDbContext>(cancellationToken: Token));
        var deliveries = app.Services.GetRequiredService<RecordingEventBus>()
            .Recorded<OutboxDelivery<EntityChange<SharedProjection>>>().ToArray();
        Assert.Equal(2, deliveries.Length);
        Assert.All(deliveries, delivery => Assert.True(delivery.Payload.Entity.Id > 0));
        Assert.Equal(new[] { "audited", "generated" }, deliveries.Select(delivery => delivery.Payload.Entity.Title).Order());
    }

    [Fact]
    public void Outbox_ShouldRejectConflictingAndDuplicateEntityContracts()
    {
        var options = new RepositoryOutboxOptions();
        options.RegisterEntity<SoftDeleteAuditRow, SharedProjection>("tests.shared.v1", row => new(row.Id, row.Title));
        Assert.Throws<InvalidOperationException>(() => options.RegisterEntity<SoftDeleteAuditRow, SharedProjection>(
            "tests.shared.v1", row => new(row.Id, row.Title)));
        Assert.Throws<InvalidOperationException>(() => options.RegisterEntity<GeneratedKeyRow, SharedProjection>(
            "tests.different.v1", row => new(row.Id, row.Title)));
        Assert.Throws<InvalidOperationException>(() => options.RegisterEntity<GeneratedKeyRow, SharedProjection>(
            "tests.shared.v1", row => new(row.Id, row.Title), OutboxDestination.Local));
    }

    public sealed record SharedProjection(long Id, string Title);
    private sealed class ProjectionScope { public Guid Id { get; } = Guid.NewGuid(); }
    public sealed record ScopedProjection(int Id, Guid ScopeId);
    public sealed record SkippedProjection(int Id);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveChanges_WhenCapturingOutbox_ShouldPersistGeneratedKeysAndImmutablePayload(bool synchronous)
    {
        await using var app = await new RepositoryScenarioFactory().CreateAsync(cancellationToken: Token);
        int id;
        await using (var scope = app.CreateScope(Token))
        {
            var db = scope.Resolve<TestRepositoryDbContext>();
            var row = new GeneratedKeyRow { Title = "saved snapshot" };
            db.Add(row);
            if (synchronous) db.SaveChanges();
            else await db.SaveChangesAsync(Token);
            id = row.Id;
            Assert.True(id > 0);
            row.Title = "unsaved later mutation";
            Assert.Empty(scope.Resolve<RecordingEventBus>().Events);
        }
        Assert.Equal(1, await app.DrainOutboxAsync<TestRepositoryDbContext>(cancellationToken: Token));
        var delivery = Assert.Single(app.Services.GetRequiredService<RecordingEventBus>()
            .Recorded<OutboxDelivery<EntityChange<GeneratedProjection>>>());
        Assert.Equal(id, delivery.Payload.Entity.Id);
        Assert.Equal("saved snapshot", delivery.Payload.Entity.Title);
        Assert.Equal(0, await app.DrainOutboxAsync<TestRepositoryDbContext>(cancellationToken: Token));
    }

    [Fact]
    public async Task SaveChanges_WhenExternalTransactionRollsBack_ShouldDiscardRowsAndNotifications()
    {
        await using var app = await new RepositoryScenarioFactory().CreateAsync(cancellationToken: Token);
        await using (var scope = app.CreateScope(Token))
        {
            var db = scope.Resolve<TestRepositoryDbContext>();
            await using var transaction = await db.Database.BeginTransactionAsync(Token);
            db.Add(new SoftDeleteAuditRow { Title = "rolled back" });
            await db.SaveChangesAsync(Token);
            Assert.Empty(scope.Resolve<RecordingEventBus>().Events);
            Assert.Equal(1, await db.Set<OutboxMessage>().CountAsync(Token));
            await transaction.RollbackAsync(Token);
        }
        await app.VerifyAsync<TestRepositoryDbContext>(async (db, ct) =>
        {
            Assert.Equal(0, await db.SoftDeleteRows.CountAsync(ct));
            Assert.Equal(0, await db.Set<OutboxMessage>().CountAsync(ct));
        }, Token);
        Assert.Equal(0, await app.DrainOutboxAsync<TestRepositoryDbContext>(cancellationToken: Token));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCommitFails_ShouldRollBackWithoutPublishing()
    {
        var interceptor = new FailCommit();
        await using var app = await new RepositoryScenarioFactory(interceptor: interceptor).CreateAsync(cancellationToken: Token);
        interceptor.Enabled = true;
        await Assert.ThrowsAsync<CommitException>(() => app.ExecuteAsync(scope =>
        {
            scope.Resolve<IRepository<SoftDeleteAuditRow>>().Add(new() { Title = "commit failure" });
            return Task.CompletedTask;
        }, cancellationToken: Token));
        Assert.Empty(app.Services.GetRequiredService<RecordingEventBus>().Events);
        await app.VerifyAsync<TestRepositoryDbContext>(async (db, ct) =>
        {
            Assert.Equal(0, await db.SoftDeleteRows.CountAsync(ct));
            Assert.Equal(0, await db.Set<OutboxMessage>().CountAsync(ct));
        }, Token);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveChanges_WhenOutboxWriteFails_ShouldRollBackBothWrites(bool external)
    {
        await using var app = await new RepositoryScenarioFactory(interceptor: new FailOutboxSave())
            .CreateAsync(cancellationToken: Token);
        await using (var scope = app.CreateScope(Token))
        {
            var db = scope.Resolve<TestRepositoryDbContext>();
            await using var tx = external ? await db.Database.BeginTransactionAsync(Token) : null;
            db.Add(new SoftDeleteAuditRow { Title = "atomic" });
            await Assert.ThrowsAsync<OutboxWriteException>(() => db.SaveChangesAsync(Token));
            if (tx is not null) await tx.CommitAsync(Token);
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(Token));
        }
        await app.VerifyAsync<TestRepositoryDbContext>(async (db, ct) =>
        {
            Assert.Equal(0, await db.SoftDeleteRows.CountAsync(ct));
            Assert.Equal(0, await db.Set<OutboxMessage>().CountAsync(ct));
        }, Token);
    }

    [Fact]
    public async Task DrainAsync_WhenAcknowledgmentFails_ShouldRetryWithStableIdentity()
    {
        var received = new List<OutboxDelivery<IntegrationNotice>>();
        var processed = new HashSet<Guid>();
        var effects = 0;
        var bus = Substitute.For<ILocalEventBus>();
        bus.PublishAsync(Arg.Any<Type>(), Arg.Any<object>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var envelope = (OutboxDelivery<IntegrationNotice>)call[1];
                received.Add(envelope);
                if (processed.Add(envelope.MessageId)) effects++;
                return received.Count == 1 ? Task.FromException(new IOException("lost acknowledgment")) : Task.CompletedTask;
            });
        await using var app = await new RepositoryScenarioFactory().CreateAsync(
            seams => seams.With<ILocalEventBus>(bus), Token);
        Guid messageId = default;
        await app.ExecuteAsync(scope =>
        {
            messageId = scope.Resolve<IOutboxWriter<TestRepositoryDbContext>>().Enqueue(new IntegrationNotice("durable"));
            scope.Resolve<IRepository<HardDeleteRow>>().Add(new() { Title = "already committed" });
            return Task.CompletedTask;
        }, cancellationToken: Token);
        await Assert.ThrowsAsync<IOException>(() => app.DrainOutboxAsync<TestRepositoryDbContext>(cancellationToken: Token));
        await app.VerifyAsync<TestRepositoryDbContext>(async (db, ct) =>
            Assert.Equal(1, await db.HardDeleteRows.CountAsync(ct)), Token);
        Assert.Equal(1, await app.DrainOutboxAsync<TestRepositoryDbContext>(cancellationToken: Token));
        Assert.Equal(2, received.Count);
        Assert.All(received, message => Assert.Equal(messageId, message.MessageId));
        Assert.Equal(received[0].Sequence, received[1].Sequence);
        Assert.Equal(1, effects);
        await app.VerifyAsync<TestRepositoryDbContext>(async (db, ct) =>
        {
            var row = await db.Set<OutboxMessage>().SingleAsync(ct);
            Assert.Equal(2, row.Attempts);
            Assert.NotNull(row.DeliveredAtUtc);
        }, Token);
    }

    [Fact]
    public async Task DrainAsync_WhenOldestMessageIsLeased_ShouldRetainOrdering()
    {
        await using var app = await new RepositoryScenarioFactory().CreateAsync(cancellationToken: Token);
        await app.ExecuteAsync(scope =>
        {
            var writer = scope.Resolve<IOutboxWriter<TestRepositoryDbContext>>();
            writer.Enqueue(new IntegrationNotice("first"));
            writer.Enqueue(new IntegrationNotice("second"));
            return Task.CompletedTask;
        }, cancellationToken: Token);
        await app.VerifyAsync<TestRepositoryDbContext>(async (db, ct) =>
        {
            var first = await db.Set<OutboxMessage>().OrderBy(x => x.Sequence).FirstAsync(ct);
            await db.Set<OutboxMessage>().Where(x => x.Sequence == first.Sequence).ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.LeaseId, Guid.NewGuid())
                    .SetProperty(x => x.LeaseUntilUtc, DateTime.UtcNow.AddMinutes(5)), ct);
        }, Token);
        Assert.Equal(0, await app.DrainOutboxAsync<TestRepositoryDbContext>(cancellationToken: Token));
        Assert.Empty(app.Services.GetRequiredService<RecordingEventBus>().Events);
    }

    private sealed class CommitException : Exception;
    private sealed class OutboxWriteException : Exception;
    private sealed class FailCommit : DbTransactionInterceptor
    {
        public bool Enabled { get; set; }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(
            DbTransaction transaction, TransactionEventData eventData, InterceptionResult result,
            CancellationToken cancellationToken = default)
            => Enabled ? throw new CommitException() : ValueTask.FromResult(result);
    }

    private sealed class FailOutboxSave : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<OutboxMessage>().Any(x => x.State == EntityState.Added))
                throw new OutboxWriteException();
            return ValueTask.FromResult(result);
        }
    }
}
