using Microsoft.Extensions.Options;
using Monica.EventBus.Kafka.Facades;
using Monica.EventBus.Kafka.Models;
using Monica.Modules;

namespace Monica.EventBus.Kafka.UIEventBusKafka.State;

/// <summary>Creates independently owned native-dialog consumer metrics samplers.</summary>
public sealed class KafkaConsumerMetricsPollingStateFactory(
    KafkaConsoleFacade facade,
    IOptions<ModuleEventBusKafkaUIOption> uiOptions)
{
    /// <summary>Creates a sampler whose caller owns cancellation, drainage and disposal.</summary>
    public KafkaConsumerMetricsPollingState Create() => new(facade, uiOptions);
}

/// <summary>Owns live member and partition evidence for one frozen consumer-group inspector.</summary>
public sealed class KafkaConsumerMetricsPollingState : IAsyncDisposable
{
    private readonly KafkaPollingSession<KafkaConsumerMetricsSnapshot> _session;

    /// <summary>Creates a dialog-owned sampler.</summary>
    /// <param name="facade">Real Kafka console operations.</param>
    /// <param name="uiOptions">Initial periodic settings.</param>
    public KafkaConsumerMetricsPollingState(KafkaConsoleFacade facade, IOptions<ModuleEventBusKafkaUIOption> uiOptions)
    {
        _session = new KafkaPollingSession<KafkaConsumerMetricsSnapshot>(
            (target, token) => facade.CaptureConsumerMetricsAsync(target.ClusterId, target.TopicName, target.GroupId, token),
            _ => true,
            snapshot => snapshot.Message,
            snapshot => snapshot.CapturedAt,
            uiOptions.Value.EnablePerformanceAutoRefresh,
            uiOptions.Value.PerformanceRefreshInterval);
    }

    /// <summary>Gets the latest consumer metrics evidence, retaining it on a later failed capture.</summary>
    public KafkaConsumerMetricsSnapshot? Snapshot => _session.Snapshot;

    /// <summary>Gets whether periodic sampling is enabled.</summary>
    public bool IsAutomaticRefreshEnabled => _session.IsEnabled;

    /// <summary>Gets the current sampling interval in whole seconds.</summary>
    public int RefreshIntervalSeconds => (int)_session.RefreshInterval.TotalSeconds;

    /// <summary>Gets whether a broker query is admitted.</summary>
    public bool IsSampling => _session.IsSampling;

    /// <summary>Gets the latest attempt diagnostic.</summary>
    public string? LastError => _session.LastError;

    /// <summary>Gets the timestamp of the latest completed attempt, including failed attempts.</summary>
    public DateTimeOffset? LastAttemptedAt => _session.LastAttemptedAt;

    /// <summary>Raised when evidence changes; owners must retire and drain their render callbacks.</summary>
    public event Action? StateChanged
    {
        add => _session.StateChanged += value;
        remove => _session.StateChanged -= value;
    }

    /// <summary>Activates exact frozen consumer-group and optional topic identities.</summary>
    /// <param name="clusterId">Exact target cluster identifier.</param>
    /// <param name="groupId">Exact target group identifier.</param>
    /// <param name="topicName">Optional exact topic filter.</param>
    /// <param name="cancellationToken">Cancellation token for activation.</param>
    public Task ActivateAsync(string clusterId, string groupId, string? topicName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clusterId);
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
        return _session.ActivateAsync(new KafkaSamplingTarget(clusterId, groupId, topicName), cancellationToken, captureWhenDisabled: true);
    }

    /// <summary>Captures fresh evidence regardless of the periodic setting.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when usable evidence was returned, including partial optional diagnostics.</returns>
    public Task<bool> CaptureNowAsync(CancellationToken cancellationToken = default) => _session.CaptureAsync(cancellationToken);

    /// <summary>Enables or disables periodic refresh after draining the previous loop.</summary>
    /// <param name="enabled">Whether periodic refresh should run.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task SetAutomaticRefreshAsync(bool enabled, CancellationToken cancellationToken = default) =>
        _session.SetEnabledAsync(enabled, cancellationToken);

    /// <summary>Changes the refresh interval after draining the previous loop.</summary>
    /// <param name="seconds">Requested seconds, clamped to the documented sampler limits.</param>
    public Task SetRefreshIntervalAsync(int seconds) => _session.SetIntervalAsync(seconds);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _session.DisposeAsync();
}
