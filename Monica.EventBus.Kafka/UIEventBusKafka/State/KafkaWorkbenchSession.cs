using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Monica.EventBus.Kafka.Facades;
using Monica.EventBus.Kafka.Localization;
using Monica.Modules;

namespace Monica.EventBus.Kafka.UIEventBusKafka.State;

/// <summary>Creates a routed workbench's independently owned sampling session.</summary>
/// <remarks>
/// The factory is circuit-scoped, but every created session owns its sampler. A composing component
/// must reject new work, cancel and drain its admitted operations and native dialogs, then dispose
/// the session. Retiring one route never stops a newer route's sampler.
/// </remarks>
public sealed class KafkaWorkbenchSessionFactory(
    KafkaConsoleFacade facade,
    IOptions<ModuleEventBusKafkaUIOption> options,
    IStringLocalizer<EventBusKafkaResource> localizer)
{
    /// <summary>Creates one caller-owned workbench session with the configured initial sampling defaults.</summary>
    public KafkaWorkbenchSession Create()
    {
        var polling = new KafkaPerformancePollingState(facade, options);
        return new KafkaWorkbenchSession(new EventBusKafkaPageState(facade, polling, localizer), polling);
    }
}

/// <summary>Owns state and periodic sampling for one routed Kafka workbench instance.</summary>
/// <remarks>
/// Evidence is detached from the rendering component. An exact requested direct cluster is probed
/// during initialization; externally managed or unavailable identities remain selected without
/// granting broker administration. Selection-free initialization retains the previous explicit-test default.
/// </remarks>
public sealed class KafkaWorkbenchSession : IAsyncDisposable
{
    internal KafkaWorkbenchSession(EventBusKafkaPageState state, KafkaPerformancePollingState polling)
    {
        State = state;
        PerformancePolling = polling;
    }

    /// <summary>Gets detached workbench evidence and operational transitions.</summary>
    public EventBusKafkaPageState State { get; }

    /// <summary>Gets the session-owned sampler used for render notifications.</summary>
    public KafkaPerformancePollingState PerformancePolling { get; }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => PerformancePolling.DisposeAsync();
}
