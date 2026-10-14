namespace Monica.JobScheduler.UI.UIJobScheduler.Abstractions;

/// <summary>
/// One service-registry observation from an optional infrastructure-integration provider. The built-in
/// ServiceDiscovery provider includes all registered services; it does not verify a scheduler role, scheduler
/// scope membership, owner correlation, or replica capacity. The legacy type name does not establish those facts.
/// </summary>
public sealed record JobSchedulerWorkerInstanceView
{
    /// <summary>Gets the registry's stable service identifier, for example the AppId.</summary>
    public required string AppId { get; init; }

    /// <summary>Gets the display name of the registered service.</summary>
    public required string AppName { get; init; }

    /// <summary>Gets the project name reported by the registry; no scheduler-owner correlation is verified.</summary>
    public required string ProjectName { get; init; }

    /// <summary>Gets the registry's unique instance identifier for one running process.</summary>
    public required string InstanceId { get; init; }

    /// <summary>Gets whether the registry elected this instance as its service leader.</summary>
    public required bool IsLeader { get; init; }

    /// <summary>Gets when the instance first registered.</summary>
    public required DateTimeOffset RegisteredAtUtc { get; init; }

    /// <summary>Gets the instance's most recent heartbeat time.</summary>
    public required DateTimeOffset LastHeartbeatUtc { get; init; }

    /// <summary>Gets the client-side liveness classification derived from heartbeat freshness.</summary>
    public required JobSchedulerWorkerInstanceStatus Status { get; init; }

    /// <summary>Gets the network addresses the instance listens on, when reported. May be empty.</summary>
    public IReadOnlyList<string> ListeningAddresses { get; init; } = [];
}

/// <summary>
/// Classifies worker-instance liveness from heartbeat freshness. Offline is a client-side derivation; the
/// registry itself removes expired instances from its durable view.
/// </summary>
public enum JobSchedulerWorkerInstanceStatus
{
    /// <summary>The heartbeat age is at or below 1.5 heartbeat periods in the built-in provider.</summary>
    Online,

    /// <summary>The heartbeat missed the healthy boundary but has not crossed the registration TTL.</summary>
    Unhealthy,

    /// <summary>The heartbeat crossed the registration TTL; the instance is presumed offline.</summary>
    Offline
}

/// <summary>
/// Optionally enriches the JobScheduler runtime view with separately sourced registry observations. The
/// runtime page resolves this from the service provider and degrades to a setup hint when absent,
/// so hosts that do not adopt the integration keep the rest of the runtime tab.
/// </summary>
public interface IJobSchedulerWorkerInsightProvider
{
    /// <summary>
    /// Loads the backing registry's observations. The built-in provider flattens all service instances and
    /// orders them by exact project name then instance identifier. Classifications are producer-owned; the
    /// consumer accepts the list with its own observation time and does not infer a classification instant.
    /// </summary>
    /// <param name="cancellationToken">Cancels the registry read.</param>
    /// <returns>The registry's instance list; empty when nothing has registered yet.</returns>
    Task<IReadOnlyList<JobSchedulerWorkerInstanceView>> GetWorkerInstancesAsync(
        CancellationToken cancellationToken = default);
}
