using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Monica.Repository.Persistence.Abstractions;
using Monica.Repository.Persistence.Services;
using Monica.Repository.UnitOfWork.Abstractions;
using Monica.Repository.UnitOfWork.Models;

namespace Monica.Testing.Hosting;

/// <summary>
/// Represents one dependency-injection scope owned by a <see cref="MonicaTestApplication"/>.
/// </summary>
/// <remarks>
/// <para>
/// The scope mirrors the runtime shapes a production host offers: <see cref="Resolve{T}"/> for direct resolution,
/// <see cref="InvokeAsync(Func{MonicaTestScope,Task},UnitOfWorkScopeOptions?)"/> for request-shaped execution inside an
/// ambient unit of work, and <see cref="SeedAsync"/> for seeding entity graphs that carry the repository
/// persistence concepts.
/// </para>
/// <para>
/// <see cref="InvokeAsync(Func{MonicaTestScope,Task},UnitOfWorkScopeOptions?)"/> runs the action the way the request pipeline runs handlers: a unit of work wraps the
/// action, writes made through test-scope DbContexts are saved when the action succeeds, and the unit of work
/// commits (flushing buffered entity events) or rolls back on failure. Write-scenario tests need no manual
/// context initialization or explicit saves.
/// </para>
/// </remarks>
public sealed class MonicaTestScope : IAsyncDisposable
{
    private readonly AsyncServiceScope _scope;
    private bool _disposed;

    internal MonicaTestScope(AsyncServiceScope scope, CancellationToken cancellationToken)
    {
        _scope = scope;
        CancellationToken = cancellationToken;
        ServiceProvider = scope.ServiceProvider;
    }

    /// <summary>
    /// Gets the scoped service provider.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets the cancellation token test operations in this scope should observe.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Resolves a required service from this scope.
    /// </summary>
    public T Resolve<T>()
        where T : notnull
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Resolves a required service from this scope.
    /// </summary>
    public object Resolve(Type type)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return ServiceProvider.GetRequiredService(type);
    }

    /// <summary>
    /// Runs one write-scenario action with request-shaped semantics: an ambient unit of work wraps the action,
    /// test-scope DbContexts are saved when the action succeeds, and the unit of work commits or rolls back.
    /// </summary>
    /// <param name="action">The action to execute; resolve services from the scope argument.</param>
    /// <param name="options">Optional unit-of-work scope options; leave <see langword="null"/> for defaults.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the scenario composition does not register the UnitOfWork module.
    /// </exception>
    public async Task InvokeAsync(Func<MonicaTestScope, Task> action, UnitOfWorkScopeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        var manager = ResolveUnitOfWorkManager();
        await manager.RunAsync(
            async () =>
            {
                await action(this);
                await SaveScopeDbContextsAsync();
            },
            options,
            CancellationToken);
    }

    /// <summary>
    /// Runs one write-scenario action with a result, using the same request-shaped semantics as
    /// <see cref="InvokeAsync(Func{MonicaTestScope,Task},UnitOfWorkScopeOptions?)"/>.
    /// </summary>
    /// <typeparam name="TResult">The action result type.</typeparam>
    /// <param name="action">The action to execute; resolve services from the scope argument.</param>
    /// <param name="options">Optional unit-of-work scope options; leave <see langword="null"/> for defaults.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the scenario composition does not register the UnitOfWork module.
    /// </exception>
    public async Task<TResult> InvokeAsync<TResult>(Func<MonicaTestScope, Task<TResult>> action, UnitOfWorkScopeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        var manager = ResolveUnitOfWorkManager();
        return await manager.RunAsync(
            async () =>
            {
                var result = await action(this);
                await SaveScopeDbContextsAsync();
                return result;
            },
            options,
            CancellationToken);
    }

    /// <summary>
    /// Seeds entities into the single repository DbContext registered in this scope and saves them, including
    /// related entities reachable through navigation properties.
    /// </summary>
    /// <param name="entities">
    /// Entities of mixed types forming one graph; an element that is itself a collection (for example an
    /// array or list of entities) is flattened and seeded as its items, so <c>SeedAsync(users)</c> and
    /// <c>SeedAsync(userA, userB)</c> are equivalent.
    /// </param>
    /// <remarks>
    /// The save runs through the repository save pipeline, so seeded rows carry the persistence concepts
    /// (for example creation audit stamping, subject to the registered <c>IAuditPropertySetter</c> seam).
    /// The change tracker is cleared afterwards so later no-tracking reads never collide with leftover tracked
    /// instances. Seed entities that reference each other in one call: after the tracker clears, a later call
    /// would re-attach a shared parent as a new row.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the scenario has no registered repository DbContext or has more than one candidate.
    /// </exception>
    public async Task SeedAsync(params object[] entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var materialized = entities.SelectMany(FlattenSeedElement).ToList();
        if (materialized.Count == 0)
        {
            return;
        }

        var dbContext = ResolveSingleTestDbContext();
        dbContext.AddRange(materialized);
        await dbContext.SaveChangesAsync(CancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    /// <summary>
    /// Seeds a sequence of entities of one type into the single repository DbContext registered in this scope,
    /// with the same persistence and tracker semantics as <see cref="SeedAsync"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the scenario has no registered repository DbContext or has more than one candidate.
    /// </exception>
    public async Task SeedRangeAsync<TEntity>(IEnumerable<TEntity> entities)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entities);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var materialized = entities.Cast<object>().ToList();
        if (materialized.Count == 0)
        {
            return;
        }

        var dbContext = ResolveSingleTestDbContext();
        dbContext.AddRange(materialized);
        await dbContext.SaveChangesAsync(CancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    /// <summary>
    /// Expands collection elements (arrays, lists) passed to <see cref="SeedAsync"/> into their items so a
    /// seeded collection is never mistaken for a single entity.
    /// </summary>
    private static IEnumerable<object> FlattenSeedElement(object entity)
    {
        return entity is string
            ? [entity]
            : entity is IEnumerable<object> sequence
                ? sequence.SelectMany(FlattenSeedElement)
                : [entity];
    }

    /// <summary>
    /// Resolves the repository DbContext registered for this scope.
    /// </summary>
    public async Task<TDbContext> GetDbContextAsync<TDbContext>()
        where TDbContext : RepositoryDbContext<TDbContext>
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var provider = ServiceProvider.GetService<IDbContextProvider<TDbContext>>();
        return provider is null
            ? Resolve<TDbContext>()
            : await provider.GetDbContextAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _scope.DisposeAsync();
    }

    private IUnitOfWorkManager ResolveUnitOfWorkManager()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return ServiceProvider.GetService<IUnitOfWorkManager>()
            ?? throw new InvalidOperationException(
                "MonicaTestScope.InvokeAsync requires the UnitOfWork module (monica.AddUnitOfWork()) in the scenario composition.");
    }

    private async Task SaveScopeDbContextsAsync()
    {
        // Test hosts own their DbContexts at scope level (UseTestDatabase), so the ambient unit of work cannot
        // see them; the invoke wrapper therefore saves them inside the unit-of-work window before it commits.
        foreach (var dbContext in ResolveTestDbContexts())
        {
            if (dbContext.ChangeTracker.HasChanges())
            {
                await dbContext.SaveChangesAsync(CancellationToken);
            }
        }
    }

    private DbContext ResolveSingleTestDbContext()
    {
        var registry = ServiceProvider.GetService<TestDbContextTypeRegistry>();
        if (registry is null || registry.Types.Count == 0)
        {
            throw new InvalidOperationException("No RepositoryDbContext service is registered in this test scope.");
        }

        return registry.Types.Count == 1
            ? (DbContext)Resolve(registry.Types.Single())
            : throw new InvalidOperationException(
                "Multiple RepositoryDbContext services are registered. Resolve the intended DbContext and seed it explicitly.");
    }

    private IEnumerable<DbContext> ResolveTestDbContexts()
    {
        var registry = ServiceProvider.GetService<TestDbContextTypeRegistry>();
        return registry is null
            ? []
            : registry.Types.Select(Resolve).Cast<DbContext>();
    }
}
