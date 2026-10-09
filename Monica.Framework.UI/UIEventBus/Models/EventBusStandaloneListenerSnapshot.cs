using Monica.EventBus.Models;

namespace Monica.Framework.UI.UIEventBus.Models;

/// <summary>A detached identity of the exact standalone test listener currently controlled by a component lease.</summary>
/// <param name="SubscriptionId">The registry handle retained until successful removal.</param>
/// <param name="TopicName">The exact topic attached to that handle.</param>
public sealed record EventBusStandaloneListenerSnapshot(EventSubscriptionId SubscriptionId, string TopicName);
