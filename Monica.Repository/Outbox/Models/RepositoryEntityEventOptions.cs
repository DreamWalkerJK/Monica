using Monica.Repository.Persistence.Models;

namespace Monica.Repository.Outbox.Models;

/// <summary>Maps committed entity changes to ordinary EventBus events after generated values are available.</summary>
public sealed class RepositoryEntityEventOptions
{
    private readonly HashSet<(Type Entity, Type Event)> _registered = [];
    internal Dictionary<Type, List<EntityEventProjection>> Projections { get; } = [];
    internal HashSet<Type> AsyncEntityTypes { get; } = [];

    /// <summary>
    /// Registers a synchronous projection evaluated after the business row's first flush, inside its transaction.
    /// Return null to omit an event. The event must declare EventBus Outbox and stable event-name metadata.
    /// </summary>
    public void RegisterEntity<TEntity, TEvent>(Func<IServiceProvider, PersistenceChange, TEvent?> project)
        where TEntity : class where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!_registered.Add((typeof(TEntity), typeof(TEvent))))
            throw new InvalidOperationException($"Duplicate entity-event projection for '{typeof(TEntity)}' and '{typeof(TEvent)}'.");
        if (!Projections.TryGetValue(typeof(TEntity), out var projections))
            Projections.Add(typeof(TEntity), projections = []);
        projections.Add(new EntityEventProjection((services, change) => project(services, change), null));
    }

    /// <summary>Registers a projection that needs only the changed entity.</summary>
    public void RegisterEntity<TEntity, TEvent>(Func<TEntity, TEvent?> project)
        where TEntity : class where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(project);
        RegisterEntity<TEntity, TEvent>((_, change) => project((TEntity)change.Entry.Entity));
    }

    /// <summary>
    /// Registers an asynchronous projection. It may query or stage source-side bookkeeping with
    /// <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry.Context"/> but must not save or publish.
    /// </summary>
    public void RegisterEntity<TEntity, TEvent>(
        Func<IServiceProvider, PersistenceChange, CancellationToken, ValueTask<TEvent?>> project)
        where TEntity : class where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!_registered.Add((typeof(TEntity), typeof(TEvent))))
            throw new InvalidOperationException($"Duplicate entity-event projection for '{typeof(TEntity)}' and '{typeof(TEvent)}'.");
        if (!Projections.TryGetValue(typeof(TEntity), out var projections))
            Projections.Add(typeof(TEntity), projections = []);
        AsyncEntityTypes.Add(typeof(TEntity));
        projections.Add(new EntityEventProjection(null,
            async (services, change, token) => await project(services, change, token)));
    }
}

internal sealed record EntityEventProjection(
    Func<IServiceProvider, PersistenceChange, object?>? Sync,
    Func<IServiceProvider, PersistenceChange, CancellationToken, ValueTask<object?>>? Async);
