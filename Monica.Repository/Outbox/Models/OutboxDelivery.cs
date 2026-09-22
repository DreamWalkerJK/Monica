using Monica.Repository.Persistence.Models;

namespace Monica.Repository.Outbox.Models;

/// <summary>
/// At-least-once delivery envelope. Consumers must atomically deduplicate MessageId with their own writes.
/// Sequence allows consumers to reject stale deliveries; a lease expiry may cause duplicates.
/// </summary>
public sealed record OutboxDelivery<T>(Guid MessageId, long Sequence, DateTime CreatedAtUtc, T Payload);

/// <summary>Committed storage notification containing an explicit application projection, never a live EF entity.</summary>
public sealed record EntityChange<T>(PersistenceChangeKind Kind, T Entity);

/// <summary>Selects the existing Monica transport used for durable delivery.</summary>
public enum OutboxDestination
{
    /// <summary>In-process notification, replayed after restart until acknowledged.</summary>
    Local,
    /// <summary>Cross-process integration message.</summary>
    Distributed
}
