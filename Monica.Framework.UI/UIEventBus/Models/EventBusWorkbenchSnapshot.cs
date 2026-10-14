using Monica.EventBus.Models;

namespace Monica.Framework.UI.UIEventBus.Models;

/// <summary>
/// A detached EventBus capture whose subscription-derived aggregates refer to the same captured handler set.
/// </summary>
/// <remarks>
/// The workbench state replaces the whole capture after a successful refresh. Treat its compatible view models as
/// read-only; a caller needing an editable or frozen inspector uses <c>EventBusWorkbenchState.CloneSubscription</c>.
/// Membership and topic-error collections are read-only, and metadata is detached from producers and change feeds.
/// The capture is sequential across service reads, not a transaction that freezes the live registry and transports.
/// </remarks>
public sealed class EventBusWorkbenchSnapshot
{
    internal EventBusWorkbenchSnapshot(
        DateTimeOffset capturedAt,
        IReadOnlyList<SubscriptionViewModel> subscriptions,
        IReadOnlyList<EventBusProviderInfo> providers,
        IReadOnlyList<TopicSubscriptionStatus> topicStatuses,
        SubscriptionStatistics statistics)
    {
        CapturedAt = capturedAt;
        Subscriptions = subscriptions;
        Providers = providers;
        TopicStatuses = topicStatuses;
        Statistics = statistics;
    }

    /// <summary>Gets the UTC time at which this complete capture was adopted.</summary>
    public DateTimeOffset CapturedAt { get; }

    /// <summary>Gets visible registered-handler snapshots, excluding hidden row-level diagnostic listeners.</summary>
    public IReadOnlyList<SubscriptionViewModel> Subscriptions { get; }

    /// <summary>Gets discovered bus snapshots with exact key/scope counts derived from <see cref="Subscriptions"/>.</summary>
    public IReadOnlyList<EventBusProviderInfo> Providers { get; }

    /// <summary>
    /// Gets all topic runtime snapshots sampled from the host status store, including topics with no visible handler.
    /// </summary>
    public IReadOnlyList<TopicSubscriptionStatus> TopicStatuses { get; }

    /// <summary>
    /// Gets handler aggregates derived from <see cref="Subscriptions"/>; unhealthy-topic count retains status-store scope.
    /// </summary>
    public SubscriptionStatistics Statistics { get; }
}
