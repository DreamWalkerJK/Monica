using Microsoft.Extensions.Options;
using Monica.EventBus.Kafka.Facades;
using Monica.EventBus.Kafka.Models;
using Monica.Modules;

namespace Monica.EventBus.Kafka.UIEventBusKafka.State;

/// <summary>
/// Owns one workbench session's Kafka performance sampler.
/// </summary>
/// <remarks>
/// Manual and periodic captures share a serialized capture boundary. Failed metadata captures
/// retain the last valid snapshot with a separate attempt diagnostic. The caller owns disposal;
/// this sampler must not be shared between independently retiring routed workbenches.
/// </remarks>
public sealed class KafkaPerformancePollingState : IAsyncDisposable
{
    /// <summary>Minimum accepted sampling interval, in seconds.</summary>
    public const int MinimumRefreshIntervalSeconds = 1;

    /// <summary>Maximum accepted sampling interval, in seconds.</summary>
    public const int MaximumRefreshIntervalSeconds = 3600;

    private readonly KafkaPollingSession<KafkaPerformanceSnapshot> _session;

    /// <summary>Creates an independently owned performance sampler.</summary>
    /// <param name="facade">Real Kafka console operations.</param>
    /// <param name="uiOptions">Initial sampling settings, which remain local to this session.</param>
    public KafkaPerformancePollingState(KafkaConsoleFacade facade, IOptions<ModuleEventBusKafkaUIOption> uiOptions)
    {
        _session = new KafkaPollingSession<KafkaPerformanceSnapshot>(
            (target, token) => facade.CapturePerformanceAsync(target.ClusterId, token),
            snapshot => snapshot.HasMetadataSnapshot,
            snapshot => snapshot.Message,
            snapshot => snapshot.CapturedAt,
            uiOptions.Value.EnablePerformanceAutoRefresh,
            uiOptions.Value.PerformanceRefreshInterval);
    }

    /// <summary>Gets the latest complete metadata snapshot, including partial optional offset diagnostics.</summary>
    public KafkaPerformanceSnapshot? LatestSnapshot => _session.Snapshot;

    /// <summary>Gets whether periodic sampling is enabled for the session.</summary>
    public bool IsAutomaticRefreshEnabled => _session.IsEnabled;

    /// <summary>Gets the current periodic sampling interval.</summary>
    public TimeSpan RefreshInterval => _session.RefreshInterval;

    /// <summary>Gets the current periodic sampling interval in whole seconds.</summary>
    public int RefreshIntervalSeconds => (int)_session.RefreshInterval.TotalSeconds;

    /// <summary>Gets whether an actual Kafka capture is admitted.</summary>
    public bool IsSampling => _session.IsSampling;

    /// <summary>Gets the timestamp of the most recent valid metadata capture.</summary>
    public DateTimeOffset? LastCapturedAt => _session.LastCapturedAt;

    /// <summary>Gets the timestamp of the most recent completed capture attempt, including failed attempts.</summary>
    public DateTimeOffset? LastAttemptedAt => _session.LastAttemptedAt;

    /// <summary>Gets the latest capture failure or partial optional-offset diagnostic.</summary>
    public string? LastError => _session.LastError;

    /// <summary>Raised when sampler evidence changes; owners must retire and drain their render callbacks.</summary>
    public event Action? StateChanged
    {
        add => _session.StateChanged += value;
        remove => _session.StateChanged -= value;
    }

    /// <summary>Loads detached repository evidence without treating failed default counts as a successful capture.</summary>
    /// <param name="snapshot">Repository evidence, or null when no capture is recorded.</param>
    public void SetSnapshot(KafkaPerformanceSnapshot? snapshot) =>
        _session.SetSnapshot(snapshot, snapshot is null ? null : new KafkaSamplingTarget(snapshot.ClusterId));

    /// <summary>Clears evidence for the currently selected cluster.</summary>
    public void ClearSnapshot() => _session.SetSnapshot(null);

    /// <summary>Activates a cluster after a real successful connection probe.</summary>
    /// <param name="clusterId">Exact cluster identity.</param>
    /// <param name="canSample">Whether direct broker access has been verified.</param>
    /// <param name="cancellationToken">Cancellation token for activation and the initial capture.</param>
    public Task ActivateAsync(string? clusterId, bool canSample, CancellationToken cancellationToken = default) =>
        _session.ActivateAsync(canSample && !string.IsNullOrWhiteSpace(clusterId)
            ? new KafkaSamplingTarget(clusterId)
            : null, cancellationToken);

    /// <summary>Enables or disables periodic sampling, retaining valid evidence.</summary>
    /// <param name="enabled">Whether periodic sampling should run.</param>
    /// <param name="cancellationToken">Cancellation token for the initial enabling capture.</param>
    public Task SetAutomaticRefreshAsync(bool enabled, CancellationToken cancellationToken = default) =>
        _session.SetEnabledAsync(enabled, cancellationToken);

    /// <summary>Changes the periodic interval after draining the previous loop.</summary>
    /// <param name="seconds">Requested seconds, clamped to the documented minimum and maximum.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task SetRefreshIntervalAsync(int seconds, CancellationToken cancellationToken = default) =>
        _session.SetIntervalAsync(seconds, cancellationToken);

    /// <summary>Captures actual Kafka evidence regardless of the periodic setting.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when metadata completed; optional offset diagnostics remain available separately.</returns>
    public Task<bool> CaptureNowAsync(CancellationToken cancellationToken = default) => _session.CaptureAsync(cancellationToken);

    /// <summary>Stops and drains the periodic loop while retaining evidence and settings.</summary>
    public Task DeactivateAsync() => _session.DeactivateAsync();

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _session.DisposeAsync();
}
