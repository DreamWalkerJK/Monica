using Microsoft.EntityFrameworkCore;
using Monica.Core.Execution;
using Monica.Repository.UnitOfWork.Abstractions;
using Monica.Repository.UnitOfWork.Models;
using Monica.Repository.Outbox.Services;
using Monica.Repository.Persistence.Services;
using Monica.Repository.UnitOfWork.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Monica.Core;
using Monica.Core.Modularity.Models;

namespace Monica.Testing.Hosting;

/// <summary>
/// Owns one fully started Monica test host and the module composition bound to that host.
/// </summary>
public sealed class MonicaTestApplication : IAsyncDisposable
{
    private readonly WebApplication _host;
    private bool _disposed;

    internal MonicaTestApplication(WebApplication host)
    {
        _host = host;
        Application = host.Services.GetRequiredService<MonicaApplication>();
        ModuleSnapshots = Array.AsReadOnly(Application.Modules.RuntimeSnapshots.ToArray());
    }

    /// <summary>
    /// Gets the root service provider owned by this scenario host.
    /// </summary>
    public IServiceProvider Services => _host.Services;

    /// <summary>
    /// Gets the Monica composition owned by this scenario host.
    /// </summary>
    public MonicaApplication Application { get; }

    /// <summary>
    /// Gets the immutable module snapshot captured after host startup.
    /// </summary>
    public IReadOnlyList<ModuleRuntimeSnapshot> ModuleSnapshots { get; }

    /// <summary>
    /// Creates a service scope owned by this scenario host.
    /// </summary>
    /// <param name="cancellationToken">The token test operations in the scope should observe.</param>
    /// <returns>A scope that resolves services only from this application's service graph.</returns>
    public MonicaTestScope CreateScope(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new MonicaTestScope(_host.Services.CreateAsyncScope(), cancellationToken);
    }

    /// <summary>
    /// Runs one write operation through the production execution pipeline in a fresh scope.
    /// Resolve the handler inside the callback; nested mediator calls join the same transaction.
    /// </summary>
    public async Task<TResult> ExecuteAsync<TResult>(
        Func<MonicaTestScope, Task<TResult>> action,
        UnitOfWorkScopeOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        await using var scope = CreateScope(cancellationToken);
        _ = scope.Resolve<IUnitOfWorkManager>();
        var descriptor = ExecutionDescriptor.ForMethod<ExecutionUnit, TResult>(
            new ExecutionPoint("Monica.Testing.Operation"), typeof(MonicaTestApplication), null,
            isBusinessOperation: true, transactionMode: ExecutionTransactionMode.Automatic);
        var features = new ExecutionFeatureCollection();
        if (options is not null) features.Set(options);
        return await scope.Resolve<IExecutionPipeline>().ExecuteAsync(
            descriptor, ExecutionUnit.Value, null, () => action(scope), cancellationToken, features);
    }

    /// <summary>Runs a write operation without a return value in a fresh production scope.</summary>
    public Task ExecuteAsync(
        Func<MonicaTestScope, Task> action, UnitOfWorkScopeOptions? options = null, CancellationToken cancellationToken = default)
        => ExecuteAsync(async scope => { await action(scope); return ExecutionUnit.Value; }, options, cancellationToken);

    /// <summary>
    /// Arranges data in a dedicated context scope. Save explicitly inside the callback before returning IDs or DTOs;
    /// no tracked entity should escape into the act phase. One callback can add an entire heterogeneous graph.
    /// </summary>
    public async Task<TResult> SeedAsync<TDbContext, TResult>(
        Func<TDbContext, CancellationToken, Task<TResult>> seed, CancellationToken cancellationToken = default)
        where TDbContext : DbContext
    {
        await using var scope = CreateScope(cancellationToken);
        using var suppressed = scope.Resolve<EntityEventPublicationScope>().Suspend();
        var db = scope.Resolve<TDbContext>();
        var result = await seed(db, cancellationToken);
        if (db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Seed callbacks must save their changes before returning.");
        return result;
    }

    /// <summary>Verifies persisted state using a fresh context without saving or starting a write transaction.</summary>
    public async Task VerifyAsync<TDbContext>(
        Func<TDbContext, CancellationToken, Task> verify, CancellationToken cancellationToken = default)
        where TDbContext : DbContext
    {
        await using var scope = CreateScope(cancellationToken);
        await verify(scope.Resolve<TDbContext>(), cancellationToken);
    }

    /// <summary>Drains committed outbox rows explicitly, allowing tests to assert capture separately from delivery.</summary>
    public Task<int> DrainOutboxAsync<TDbContext>(int maximumMessages = 100, CancellationToken cancellationToken = default)
        where TDbContext : RepositoryDbContext<TDbContext>
        => Services.GetRequiredService<OutboxDispatcher<TDbContext>>().DrainAsync(maximumMessages, cancellationToken);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            await _host.StopAsync();
        }
        finally
        {
            await _host.DisposeAsync();
        }
    }
}
