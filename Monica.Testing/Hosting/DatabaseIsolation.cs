using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Monica.Repository.Persistence.Services;

namespace Monica.Testing.Hosting;

/// <summary>Configures database options while retaining production context, repository and transaction registrations.</summary>
public static class TestDatabaseServiceCollectionExtensions
{
    /// <summary>
    /// Uses one named SQLite memory database per scenario host and context type. A host-owned keeper connection
    /// preserves it while each scope obtains its own context/connection. Requires production AddRepositoryDbContext.
    /// </summary>
    public static IServiceCollection UseTestDatabase<TDbContext>(this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder<TDbContext>>? configureOptions = null)
        where TDbContext : RepositoryDbContext<TDbContext>
    {
        RequireProductionContext<TDbContext>(services);
        services.AddSingleton<ScenarioSqliteDatabase<TDbContext>>();
        services.AddScoped<ITestDatabaseInitializer, TestDatabaseInitializer<TDbContext>>();
        services.RemoveAll<DbContextOptions<TDbContext>>();
        services.AddScoped(sp =>
        {
            var options = new DbContextOptionsBuilder<TDbContext>()
                .UseSqlite(sp.GetRequiredService<ScenarioSqliteDatabase<TDbContext>>().ConnectionString)
                .UseApplicationServiceProvider(sp);
            configureOptions?.Invoke(sp, options);
            return options.Options;
        });
        return services;
    }

    /// <summary>
    /// Replaces provider options only. The caller owns schema setup, data isolation and cleanup.
    /// Use the real provider for provider-specific SQL, transaction and sharding guarantees.
    /// </summary>
    public static IServiceCollection UseRealTestDatabase<TDbContext>(
        this IServiceCollection services, Action<IServiceProvider, DbContextOptionsBuilder<TDbContext>> configureProvider)
        where TDbContext : RepositoryDbContext<TDbContext>
    {
        ArgumentNullException.ThrowIfNull(configureProvider);
        RequireProductionContext<TDbContext>(services);
        services.RemoveAll<DbContextOptions<TDbContext>>();
        services.AddScoped(sp =>
        {
            var options = new DbContextOptionsBuilder<TDbContext>().UseApplicationServiceProvider(sp);
            configureProvider(sp, options);
            return options.Options;
        });
        return services;
    }

    private static void RequireProductionContext<TDbContext>(IServiceCollection services)
    {
        if (!services.Any(x => x.ServiceType == typeof(TDbContext)))
            throw new InvalidOperationException($"Register the production Repository DbContext '{typeof(TDbContext)}' before replacing its provider options.");
    }
}

internal sealed class ScenarioSqliteDatabase<TDbContext> : IDisposable
{
    private readonly SqliteConnection _keeper;

    public ScenarioSqliteDatabase()
    {
        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = $"monica-{Guid.NewGuid():N}", Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared, Pooling = false
        }.ToString();
        _keeper = new SqliteConnection(ConnectionString);
        _keeper.Open();
    }

    public string ConnectionString { get; }
    public void Dispose() => _keeper.Dispose();
}

internal interface ITestDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}

internal sealed class TestDatabaseInitializer<TDbContext>(TDbContext context) : ITestDatabaseInitializer
    where TDbContext : DbContext
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
        => await context.Database.EnsureCreatedAsync(cancellationToken);
}
