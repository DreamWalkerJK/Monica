using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monica.Core.Modularity.Extensions;
using Monica.DependencyInjection.Abstractions;
using Monica.Modules;
using Monica.Repository.Persistence.Services;
using Monica.Repository.Snowflake.Abstractions;
using Monica.Repository.UnitOfWork.Abstractions;
using Monica.Repository.UnitOfWork.Models;
using Monica.Testing.Hosting;
using Test.Monica.Repository.Persistence;
using Xunit;

namespace Test.Monica.Repository.UnitOfWork;

public sealed class SharedTransactionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunAsync_WhenContextsShareConnection_ShouldUseOneAtomicTransaction(bool fail)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        using var host = CreateHost(connection, connection);
        await using (var setup = host.Services.CreateAsyncScope())
            await setup.ServiceProvider.GetRequiredService<TestRepositoryDbContext>().Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        await using (var operation = host.Services.CreateAsyncScope())
        {
            var manager = operation.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            var execute = () => manager.RunAsync(async () =>
            {
                var first = operation.ServiceProvider.GetRequiredService<TestRepositoryDbContext>();
                var second = operation.ServiceProvider.GetRequiredService<SecondDbContext>();
                first.Add(new HardDeleteRow { Title = "first" });
                second.Add(new HardDeleteRow { Title = "second" });
                await manager.Current!.FlushAsync(TestContext.Current.CancellationToken);
                if (fail) throw new ApplicationException();
            }, new UnitOfWorkScopeOptions([typeof(TestRepositoryDbContext), typeof(SecondDbContext)]),
                TestContext.Current.CancellationToken);
            if (fail) await Assert.ThrowsAsync<ApplicationException>(execute);
            else await execute();
        }
        await using var verify = host.Services.CreateAsyncScope();
        Assert.Equal(fail ? 0 : 2, await verify.ServiceProvider.GetRequiredService<TestRepositoryDbContext>()
            .HardDeleteRows.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RunAsync_WhenConnectionsAreIndependent_ShouldRejectBeforeHandler()
    {
        await using var first = new SqliteConnection("Data Source=:memory:");
        await using var second = new SqliteConnection("Data Source=:memory:");
        using var host = CreateHost(first, second);
        await using var scope = host.Services.CreateAsyncScope();
        var invoked = false;
        await Assert.ThrowsAsync<NotSupportedException>(() => scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>()
            .RunAsync(() => { invoked = true; return Task.CompletedTask; },
                new UnitOfWorkScopeOptions([typeof(TestRepositoryDbContext), typeof(SecondDbContext)]),
                TestContext.Current.CancellationToken));
        Assert.False(invoked);
    }

    [Fact]
    public async Task RunAsync_WhenStoresAreAmbiguous_ShouldRequireSelection()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        using var host = CreateHost(connection, connection);
        await using var scope = host.Services.CreateAsyncScope();
        await Assert.ThrowsAsync<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>()
            .RunAsync(() => Task.CompletedTask, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RunAsync_WhenIndependentStoreIsExplicitlySelected_ShouldCommitOnlyThatStore()
    {
        await using var first = new SqliteConnection("Data Source=:memory:");
        await using var second = new SqliteConnection("Data Source=:memory:");
        await first.OpenAsync(TestContext.Current.CancellationToken);
        await second.OpenAsync(TestContext.Current.CancellationToken);
        using var host = CreateHost(first, second, DbContextProviderType.Default);
        await using (var setup = host.Services.CreateAsyncScope())
        {
            await setup.ServiceProvider.GetRequiredService<TestRepositoryDbContext>().Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            await setup.ServiceProvider.GetRequiredService<SecondDbContext>().Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }
        await using (var operation = host.Services.CreateAsyncScope())
        {
            var db = operation.ServiceProvider.GetRequiredService<SecondDbContext>();
            await operation.ServiceProvider.GetRequiredService<IUnitOfWorkManager>().RunAsync(() =>
            {
                db.Add(new HardDeleteRow { Title = "independent" });
                return Task.CompletedTask;
            }, new UnitOfWorkScopeOptions([typeof(SecondDbContext)]), TestContext.Current.CancellationToken);
        }
        await using var verify = host.Services.CreateAsyncScope();
        Assert.Empty(await verify.ServiceProvider.GetRequiredService<TestRepositoryDbContext>().HardDeleteRows.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Single(await verify.ServiceProvider.GetRequiredService<SecondDbContext>().HardDeleteRows.ToListAsync(TestContext.Current.CancellationToken));
    }

    private static IHost CreateHost(SqliteConnection first, SqliteConnection second,
        DbContextProviderType secondParticipation = DbContextProviderType.UnitOfWork)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddMonica(monica => monica.AddRepository()
            .AddRepositoryDbContext<TestRepositoryDbContext>((_, db) => db.UseSqlite(first))
            .AddRepositoryDbContext<SecondDbContext>((_, db) => db.UseSqlite(second), secondParticipation));
        builder.Services.AddMonicaTestSeams();
        builder.Services.AddSingleton<ISnowflakeIdGenerator, SequentialTestIdGenerator>();
        return builder.Build();
    }

    public sealed class SecondDbContext(DbContextOptions<SecondDbContext> options, ICachedServiceProvider services)
        : RepositoryDbContext<SecondDbContext>(options, services)
    {
        public DbSet<HardDeleteRow> HardDeleteRows => Set<HardDeleteRow>();
    }
}
