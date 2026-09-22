using System.Text.Json;
using Monica.Repository.Persistence.Models;

namespace Monica.Repository.Outbox.Models;

/// <summary>
/// Explicit versioned contract allowlist for one context's outbox. Configure identically on writers and dispatchers.
/// Register projections that serialize independently of EF navigation graphs.
/// </summary>
public sealed class RepositoryOutboxOptions
{
    private readonly HashSet<(Type Entity, Type Projection)> _entityContracts = [];
    internal Dictionary<Type, OutboxContract> ContractsByType { get; } = [];
    internal Dictionary<string, OutboxContract> ContractsByName { get; } = new(StringComparer.Ordinal);
    internal Dictionary<Type, List<Func<IServiceProvider, PersistenceChange, object?>>> EntityProjections { get; } = [];

    /// <summary>JSON contract configuration. Set before the host starts; do not mutate during execution.</summary>
    public JsonSerializerOptions Json { get; } = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Claim duration, default five minutes. A worker must finish a publish within this window.
    /// Cancellation is passed to transports; consumers must still tolerate redelivery when a transport ignores it.
    /// </summary>
    public TimeSpan DeliveryLease { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Registers a payload and destination under a stable, versioned application name.</summary>
    public void Register<TMessage>(string contract, OutboxDestination destination = OutboxDestination.Distributed)
        where TMessage : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contract);
        if (contract.Length > 200) throw new ArgumentException("Outbox contract names are limited to 200 characters.", nameof(contract));
        if (ContractsByType.ContainsKey(typeof(TMessage)) || ContractsByName.ContainsKey(contract))
            throw new InvalidOperationException($"Duplicate outbox contract '{contract}' or payload '{typeof(TMessage)}'.");
        var registration = new OutboxContract(contract, typeof(TMessage), destination,
            (row, payload) => new OutboxDelivery<TMessage>(row.MessageId, row.Sequence, row.CreatedAtUtc, (TMessage)payload));
        ContractsByType.Add(typeof(TMessage), registration);
        ContractsByName.Add(contract, registration);
    }

    /// <summary>Captures successful row changes using a scalar projection after database-generated values are available.</summary>
    public void RegisterEntity<TEntity, TProjection>(
        string contract, Func<TEntity, TProjection> project, OutboxDestination destination = OutboxDestination.Distributed)
        where TEntity : class
        where TProjection : class
    {
        ArgumentNullException.ThrowIfNull(project);
        RegisterEntity<TEntity, TProjection>(contract, (_, entity) => project(entity), destination);
    }

    /// <summary>
    /// Captures an entity projection with services from the saving context's scope. Return null to skip capture.
    /// Several distinct payload contracts may project one entity, for example synchronization and cache invalidation.
    /// Different entities may share the same payload contract when its name and destination are identical.
    /// The callback must be synchronous and side-effect free; it runs inside the business transaction after generated keys exist.
    /// </summary>
    public void RegisterEntity<TEntity, TProjection>(string contract, Func<IServiceProvider, TEntity, TProjection?> project,
        OutboxDestination destination = OutboxDestination.Distributed)
        where TEntity : class where TProjection : class
    {
        ArgumentNullException.ThrowIfNull(project);
        if (_entityContracts.Contains((typeof(TEntity), typeof(TProjection))))
            throw new InvalidOperationException($"Duplicate entity projection for '{typeof(TEntity)}' and '{typeof(TProjection)}'.");
        if (ContractsByType.TryGetValue(typeof(EntityChange<TProjection>), out var existing))
        {
            if (existing.Name != contract || existing.Destination != destination)
                throw new InvalidOperationException($"Conflicting outbox contract for '{typeof(TProjection)}'.");
        }
        else
        {
            Register<EntityChange<TProjection>>(contract, destination);
        }
        _entityContracts.Add((typeof(TEntity), typeof(TProjection)));
        if (!EntityProjections.TryGetValue(typeof(TEntity), out var projections))
            EntityProjections.Add(typeof(TEntity), projections = []);
        projections.Add((services, change) => project(services, (TEntity)change.Entry.Entity) is { } payload
            ? new EntityChange<TProjection>(change.Kind, payload) : null);
    }
}

internal sealed record OutboxContract(
    string Name, Type PayloadType, OutboxDestination Destination, Func<OutboxMessage, object, object> Wrap);
