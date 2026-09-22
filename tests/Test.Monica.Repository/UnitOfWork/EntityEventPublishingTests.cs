using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monica.Authority.Identity.Abstractions;
using Monica.Core;
using Monica.Core.Modularity.Extensions;
using Monica.EventBus.Abstractions;
using Monica.EventBus.Services;
using Monica.EventBus.Services.Support;
using Monica.Modules;
using Monica.Repository.Entity.Abstractions;
using Monica.Repository.Persistence.Abstractions;
using Monica.Repository.Snowflake.Abstractions;
using Monica.Repository.UnitOfWork.Abstractions;
using Monica.Repository.UnitOfWork.Models;
using Monica.Testing.Doubles;
using Test.Monica.Repository.Persistence;
using Xunit;

namespace Test.Monica.Repository.UnitOfWork;

/// <summary>
/// Verifies entity event semantics are consistent with and without an ambient unit of work: outside a unit
/// of work a successful save publishes its events immediately; inside one events defer until commit.
/// </summary>
public sealed class EntityEventPublishingTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public EntityEventPublishingTests()
    {
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenNoUnitOfWork_ShouldPublishEntityEventsRightAfterSave()
    {
        using var composition = await CreateCompositionAsync();

        var row = new SoftDeleteAuditRow { Title = "evented" };
        composition.Context.Add(row);
        await composition.Context.SaveChangesAsync(composition.CancellationToken);

        composition.EventBus.Recorded<EntityCreatedEventData<SoftDeleteAuditRow>>()
            .Should().ContainSingle();

        composition.Context.Remove(row);
        await composition.Context.SaveChangesAsync(composition.CancellationToken);

        composition.EventBus.Recorded<EntityDeletedEventData<SoftDeleteAuditRow>>()
            .Should().ContainSingle();
    }

    [Fact]
    public async Task RunAsync_WhenInsideUnitOfWork_ShouldPublishEntityEventsAfterCommit()
    {
        using var composition = await CreateCompositionAsync();

        await composition.Manager.RunAsync(
            async () =>
            {
                var repository = composition.Scope.ServiceProvider.GetRequiredService<IRepository<SoftDeleteAuditRow>>();
                await repository.InsertAsync(new SoftDeleteAuditRow { Title = "deferred" }, composition.CancellationToken);

                // Events staged inside the unit of work must not be visible before it commits.
                composition.EventBus.Recorded<EntityCreatedEventData<SoftDeleteAuditRow>>()
                    .Should().BeEmpty();
            },
            cancellationToken: composition.CancellationToken);

        composition.EventBus.Recorded<EntityCreatedEventData<SoftDeleteAuditRow>>()
            .Should().ContainSingle();
    }

    private async Task<Composition> CreateCompositionAsync()
    {
        var builder = Host.CreateApplicationBuilder();
        var services = builder.Services;
        services.AddLogging();
        services.AddSingleton<ICurrentUser>(new TestCurrentUser("event-tester", "event-tester"));
        services.AddSingleton<ISnowflakeIdGenerator>(new SequentialTestIdGenerator());
        services.AddSingleton<IEventHandlerInvoker, EventHandlerInvoker>();
        services.AddSingleton<IEventSubscriptionRegistry, EventSubscriptionRegistry>();
        services.AddSingleton<RecordingEventBus>();
        services.AddSingleton<ILocalEventBus>(sp => sp.GetRequiredService<RecordingEventBus>());
        services.AddSingleton<IDistributedEventBus>(sp => sp.GetRequiredService<RecordingEventBus>());
        services.AddOptions<DistributedEntityEventOptions>()
            .Configure(options => options.AddLocalEntityEvent<SoftDeleteAuditRow>());

        builder.AddMonica(monica =>
        {
            monica.AddUnitOfWork(options => options.EnableEntityEvent = true);
            monica.AddRepository()
                .AddRepositoryDbContext<TestRepositoryDbContext>(
                    (_, db) => db.UseSqlite(_connection),
                    DbContextProviderType.UnitOfWork);
        });

        var host = builder.Build();
        var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestRepositoryDbContext>();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        return new Composition(
            host,
            scope,
            context,
            scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>(),
            scope.ServiceProvider.GetRequiredService<RecordingEventBus>(),
            TestContext.Current.CancellationToken);
    }

    private sealed record Composition(
        IHost Host,
        IServiceScope Scope,
        TestRepositoryDbContext Context,
        IUnitOfWorkManager Manager,
        RecordingEventBus EventBus,
        CancellationToken CancellationToken) : IDisposable
    {
        public void Dispose()
        {
            Scope.Dispose();
            Host.Dispose();
        }
    }
}
