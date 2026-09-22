using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monica.Authority.Identity.Abstractions;
using Monica.Core;
using Monica.Core.Modularity.Extensions;
using Monica.Modules;
using Monica.Repository.Entity.Abstractions;
using Monica.Repository.Persistence.Abstractions;
using Monica.Repository.Snowflake.Abstractions;
using Monica.Repository.UnitOfWork.Abstractions;
using Monica.Testing.Doubles;
using Test.Monica.Repository.Persistence;
using Xunit;

namespace Test.Monica.Repository.UnitOfWork;

/// <summary>
/// Verifies the production unit-of-work topology (adaptive DbContext provider, transactional commit)
/// still initializes contexts and commits writes with full persistence concepts.
/// </summary>
public sealed class UnitOfWorkRepositoryCommitTests : IDisposable
{
    private const string TesterId = "uow-tester";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public UnitOfWorkRepositoryCommitTests()
    {
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task RunAsync_WhenRepositoryWritesInsideUnitOfWork_ShouldCommitWithConcepts()
    {
        using var composition = await CreateCompositionAsync();
        var insertedId = 0L;

        await composition.Manager.RunAsync(
            async () =>
            {
                var repository = composition.Scope.ServiceProvider.GetRequiredService<IRepository<SoftDeleteAuditRow>>();
                var row = await repository.InsertAsync(new SoftDeleteAuditRow { Title = "uow-row" }, composition.CancellationToken);
                insertedId = row.Id;
            },
            cancellationToken: composition.CancellationToken);

        composition.Manager.Current.Should().BeNull();
        insertedId.Should().NotBe(0);

        var committed = await composition.SetupContext.SoftDeleteRows.AsNoTracking()
            .SingleAsync(r => r.Id == insertedId, composition.CancellationToken);
        committed.Title.Should().Be("uow-row");
        committed.CreationTime.Should().NotBe(default);
        committed.CreatorId.Should().Be(TesterId);
    }

    [Fact]
    public async Task RunAsync_WhenSecondUnitOfWorkSoftDeletesCommittedRow_ShouldRewriteToSoftDelete()
    {
        using var composition = await CreateCompositionAsync();
        var insertedId = await InsertInsideUnitOfWorkAsync(composition, "to-delete");

        await composition.Manager.RunAsync(
            async () =>
            {
                var repository = composition.Scope.ServiceProvider.GetRequiredService<IRepository<SoftDeleteAuditRow>>();
                var loaded = await repository.FindAsync(r => r.Title == "to-delete", composition.CancellationToken);
                loaded.Should().NotBeNull();
                await repository.DeleteAsync(loaded!, composition.CancellationToken);
            },
            cancellationToken: composition.CancellationToken);

        var stored = await composition.SetupContext.SoftDeleteRows.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(r => r.Id == insertedId, composition.CancellationToken);
        stored.IsDeleted.Should().BeTrue();
        stored.DeletionTime.Should().NotBeNull();

        var visible = await composition.SetupContext.SoftDeleteRows.AsNoTracking()
            .AnyAsync(r => r.Id == insertedId, composition.CancellationToken);
        visible.Should().BeFalse();
    }

    private async Task<Composition> CreateCompositionAsync()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<ICurrentUser>(new TestCurrentUser(TesterId, "uow-tester"));
        builder.Services.AddSingleton<ISnowflakeIdGenerator>(new SequentialTestIdGenerator());
        builder.AddMonica(monica =>
        {
            monica.AddRepository()
                .AddRepositoryDbContext<TestRepositoryDbContext>(
                    (_, db) => db.UseSqlite(_connection),
                    DbContextProviderType.UnitOfWork);
        });

        var host = builder.Build();
        var scope = host.Services.CreateScope();
        var setupContext = scope.ServiceProvider.GetRequiredService<TestRepositoryDbContext>();
        await setupContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        setupContext.ChangeTracker.Clear();

        return new Composition(
            host,
            scope,
            scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>(),
            setupContext,
            TestContext.Current.CancellationToken);
    }

    private static async Task<long> InsertInsideUnitOfWorkAsync(Composition composition, string title)
    {
        var insertedId = 0L;
        await composition.Manager.RunAsync(
            async () =>
            {
                var repository = composition.Scope.ServiceProvider.GetRequiredService<IRepository<SoftDeleteAuditRow>>();
                var row = await repository.InsertAsync(new SoftDeleteAuditRow { Title = title }, composition.CancellationToken);
                insertedId = row.Id;
            },
            cancellationToken: composition.CancellationToken);
        return insertedId;
    }

    private sealed record Composition(
        IHost Host,
        IServiceScope Scope,
        IUnitOfWorkManager Manager,
        TestRepositoryDbContext SetupContext,
        CancellationToken CancellationToken) : IDisposable
    {
        public void Dispose()
        {
            Scope.Dispose();
            Host.Dispose();
        }
    }
}
