namespace Monica.Repository.Outbox.Models;

/// <summary>Durable immutable payload with mutable delivery bookkeeping. Retain delivered rows until consumer replay requirements allow cleanup.</summary>
public sealed class OutboxMessage
{
    /// <summary>Database-generated ordering token, also delivered to consumers.</summary>
    public long Sequence { get; set; }
    /// <summary>Stable deduplication identity, preserved across delivery retries.</summary>
    public Guid MessageId { get; set; } = Guid.NewGuid();
    /// <summary>Application-owned versioned contract name; never a CLR assembly name.</summary>
    public string Contract { get; set; } = string.Empty;
    /// <summary>Serialized payload snapshot captured before commit.</summary>
    public string Payload { get; set; } = string.Empty;
    /// <summary>UTC capture time.</summary>
    public DateTime CreatedAtUtc { get; set; }
    /// <summary>UTC acknowledgment time; null means delivery remains pending.</summary>
    public DateTime? DeliveredAtUtc { get; set; }
    /// <summary>Claim identity used for conditional acknowledgment.</summary>
    public Guid? LeaseId { get; set; }
    /// <summary>UTC lease expiration; expired claims may be retried.</summary>
    public DateTime? LeaseUntilUtc { get; set; }
    /// <summary>Number of attempted deliveries, including attempts interrupted by a process crash.</summary>
    public int Attempts { get; set; }
}
