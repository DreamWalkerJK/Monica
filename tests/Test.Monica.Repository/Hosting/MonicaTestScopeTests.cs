using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Monica.Authority.Identity.Abstractions;
using Monica.Core.Modularity.Abstractions;
using Monica.Modules;
using Monica.Repository.Entity.Abstractions;
using Monica.Repository.Entity.Services;
using Monica.Repository.Persistence.Abstractions;
using Monica.Repository.Snowflake.Abstractions;
using Monica.Testing.Doubles;
using Monica.Testing.Hosting;
using Test.Monica.Repository.Persistence;
using Xunit;

namespace Test.Monica.Repository.Hosting;

/// <summary>
/// Verifies the request-shaped execution and seeding surface of <see cref="MonicaTestScope"/>.
/// </summary>
public sealed class MonicaTestScopeTests
{
    private const string TesterId = "scope-tester";

    private readonly TestRepositoryApplicationFactory _factory = new();

    [Fact]
    public async Task SeedAsync_WhenRowsSeededAndLaterUpdated_ShouldNotConflictWithTrackedInstances()
    {
        await using var application = await CreateApplicationAsync();
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);

        await scope.SeedAsync(new SoftDeleteAuditRow { Title = "conflict" });

        // Before the tracker-clearing seed, attaching this no-tracking read collided with the seed instance.
        var action = async () =>
        {
            await scope.InvokeAsync(async s =>
            {
                var repository = s.Resolve<IRepository<SoftDeleteAuditRow>>();
                var loaded = await repository.FindAsync(r => r.Title == "conflict", TestContext.Current.CancellationToken);
                loaded.Should().NotBeNull();
                loaded!.Title = "no-conflict";
                await repository.UpdateAsync(loaded, TestContext.Current.CancellationToken);
            });
        };

        await action.Should().NotThrowAsync();

        var context = await scope.GetDbContextAsync<TestRepositoryDbContext>();
        var stored = await context.SoftDeleteRows.AsNoTracking()
            .SingleAsync(r => r.Title == "no-conflict", TestContext.Current.CancellationToken);
        stored.LastModificationTime.Should().NotBeNull();
    }

    [Fact]
    public async Task SeedAsync_WhenAuditSeamIsReal_ShouldStampCreationAuditOnSeeds()
    {
        await using var application = await CreateApplicationAsync();
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);

        var row = new SoftDeleteAuditRow { Title = "audited-seed" };
        await scope.SeedAsync(row);

        row.CreationTime.Should().NotBe(default);
        row.CreatorId.Should().Be(TesterId);
    }

    [Fact]
    public async Task SeedRangeAsync_WhenSeedingEnumerable_ShouldPersistAllRows()
    {
        await using var application = await CreateApplicationAsync();
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);

        await scope.SeedRangeAsync(Enumerable.Range(1, 3).Select(i => new SoftDeleteAuditRow { Title = $"row-{i}" }));
        await scope.SeedAsync(
            new HardDeleteRow { Title = "hard-1" },
            new HardDeleteRow { Title = "hard-2" });

        var context = await scope.GetDbContextAsync<TestRepositoryDbContext>();
        (await context.SoftDeleteRows.AsNoTracking().CountAsync(TestContext.Current.CancellationToken)).Should().Be(3);
        (await context.HardDeleteRows.AsNoTracking().CountAsync(TestContext.Current.CancellationToken)).Should().Be(2);
    }

    [Fact]
    public async Task SeedAsync_WhenPassedCollection_ShouldFlattenAndSeedItsItems()
    {
        await using var application = await CreateApplicationAsync();
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);

        var rows = new[]
        {
            new SoftDeleteAuditRow { Title = "array-1" },
            new SoftDeleteAuditRow { Title = "array-2" }
        };
        await scope.SeedAsync(rows);

        var context = await scope.GetDbContextAsync<TestRepositoryDbContext>();
        var titles = await context.SoftDeleteRows.AsNoTracking()
            .Select(r => r.Title)
            .ToListAsync(TestContext.Current.CancellationToken);
        titles.Should().BeEquivalentTo("array-1", "array-2");
    }

    [Fact]
    public async Task InvokeAsync_WhenRepositoryWrites_ShouldCommitWithConceptsWithoutManualSaves()
    {
        await using var application = await CreateApplicationAsync();
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);

        await scope.SeedAsync(
            new SoftDeleteAuditRow { Title = "to-update" },
            new SoftDeleteAuditRow { Title = "to-delete" });

        await scope.InvokeAsync(async s =>
        {
            var repository = s.Resolve<IRepository<SoftDeleteAuditRow>>();
            var loaded = await repository.FindAsync(r => r.Title == "to-update", TestContext.Current.CancellationToken);
            loaded.Should().NotBeNull();
            loaded!.Title = "updated";
            await repository.UpdateAsync(loaded, TestContext.Current.CancellationToken);

            var doomed = await repository.FindAsync(r => r.Title == "to-delete", TestContext.Current.CancellationToken);
            await repository.DeleteAsync(doomed!, TestContext.Current.CancellationToken);
        });

        var context = await scope.GetDbContextAsync<TestRepositoryDbContext>();
        var updated = await context.SoftDeleteRows.AsNoTracking()
            .SingleAsync(r => r.Title == "updated", TestContext.Current.CancellationToken);
        updated.LastModificationTime.Should().NotBeNull();
        updated.LastModifierId.Should().Be(TesterId);

        var deleted = await context.SoftDeleteRows.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(r => r.Title == "to-delete", TestContext.Current.CancellationToken);
        deleted.IsDeleted.Should().BeTrue();
        deleted.DeletionTime.Should().NotBeNull();
        deleted.DeleterId.Should().Be(TesterId);
    }

    [Fact]
    public async Task InvokeAsync_WhenActionThrows_ShouldPersistNothing()
    {
        await using var application = await CreateApplicationAsync();
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);

        var action = async () => await scope.InvokeAsync(async s =>
        {
            var repository = s.Resolve<IRepository<SoftDeleteAuditRow>>();
            await repository.InsertAsync(new SoftDeleteAuditRow { Title = "rolled-back" });
            throw new InvalidOperationException("scenario failure");
        });

        await action.Should().ThrowAsync<InvalidOperationException>();

        var context = await scope.GetDbContextAsync<TestRepositoryDbContext>();
        (await context.SoftDeleteRows.AsNoTracking().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnitOfWorkModuleMissing_ShouldFailFast()
    {
        await using var application = await new NoUnitOfWorkApplicationFactory().CreateAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);

        var action = async () => await scope.InvokeAsync(_ => Task.CompletedTask);

        (await action.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("UnitOfWork module");
    }

    private Task<MonicaTestApplication> CreateApplicationAsync()
    {
        return _factory.CreateAsync(
            scenario => scenario.With<IAuditPropertySetter>(
                new AuditPropertySetter(new TestCurrentUser(TesterId, "scope-tester"))),
            TestContext.Current.CancellationToken);
    }

    private sealed class TestRepositoryApplicationFactory : MonicaTestApplicationFactory<MonicaTestScopeTests>
    {
        protected override void ConfigureMonica(IMonicaBuilder monica)
        {
            monica.AddRepository()
                .AddRepositoryDbContext<TestRepositoryDbContext>(
                    (_, builder) => builder.UseSqlite("Data Source=:memory:"),
                    DbContextProviderType.UnitOfWork);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddSingleton<ISnowflakeIdGenerator>(new SequentialTestIdGenerator());
            services.UseTestDatabase<TestRepositoryDbContext>();
        }
    }

    /// <summary>
    /// Composition that deliberately omits the UnitOfWork module to verify InvokeAsync fails fast.
    /// </summary>
    private sealed class NoUnitOfWorkApplicationFactory : MonicaTestApplicationFactory<MonicaTestScopeTests>
    {
        protected override void ConfigureMonica(IMonicaBuilder monica)
        {
            monica.AddRepository()
                .AddRepositoryDbContext<TestRepositoryDbContext>(
                    (_, builder) => builder.UseSqlite("Data Source=:memory:"));
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddSingleton<ISnowflakeIdGenerator>(new SequentialTestIdGenerator());
            services.UseTestDatabase<TestRepositoryDbContext>();
        }
    }
}
