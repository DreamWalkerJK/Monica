using Microsoft.Extensions.DependencyInjection;
using Monica.Repository.Persistence.Services;

namespace Monica.Testing.Hosting;

/// <summary>
/// Ordinary DI scope owned by a scenario host. Use application-level ExecuteAsync for write operations
/// and independent SeedAsync/VerifyAsync scopes; this type contains no transaction or save implementation.
/// </summary>
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

    /// <summary>The scope's provider; services must not escape its lifetime.</summary>
    public IServiceProvider ServiceProvider { get; }
    /// <summary>The scenario operation's cancellation token.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>Resolves a production or explicitly replaced service from this scope.</summary>
    public T Resolve<T>() where T : notnull
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>Resolves a runtime-selected service from this scope.</summary>
    public object Resolve(Type type)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return ServiceProvider.GetRequiredService(type);
    }

    /// <summary>Returns the directly registered scoped context, identical to repository injection.</summary>
    public Task<TDbContext> GetDbContextAsync<TDbContext>() where TDbContext : RepositoryDbContext<TDbContext>
        => Task.FromResult(Resolve<TDbContext>());

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await _scope.DisposeAsync();
    }
}
