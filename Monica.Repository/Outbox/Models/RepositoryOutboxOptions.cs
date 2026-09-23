namespace Monica.Repository.Outbox.Models;

/// <summary>Controls durable delivery for one repository context. Event contracts come from EventBus metadata.</summary>
public sealed class RepositoryOutboxOptions
{
    /// <summary>Claim duration, five minutes by default. Configure above the transport's normal send timeout.</summary>
    public TimeSpan DeliveryLease { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Maximum due rows each store scans per worker pass; defaults to 100.</summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>Delay between worker passes; defaults to five seconds.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Enables the host-owned delivery worker; disable only when another host drains this store or in deterministic tests.</summary>
    public bool EnableWorker { get; set; } = true;

    /// <summary>Initial delay after a failed send; defaults to one second.</summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Maximum backoff for a pending message; defaults to five minutes.</summary>
    public TimeSpan RetryMaxDelay { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Delivered rows older than this are pruned; defaults to seven days. Pending rows are retained.</summary>
    public TimeSpan DeliveredRetention { get; set; } = TimeSpan.FromDays(7);
}
