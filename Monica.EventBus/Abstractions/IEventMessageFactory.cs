using Monica.EventBus.Models;

namespace Monica.EventBus.Abstractions;

/// <summary>Captures an event as an immutable transport-ready JSON snapshot.</summary>
public interface IEventMessageFactory
{
    /// <summary>
    /// Prepares a message using the declared contract and current host JSON settings.
    /// Durable events require an exact runtime contract type so their saved identity and payload remain stable.
    /// </summary>
    EventMessage Prepare(Type eventType, object eventData, EventSubscriptionScope scope,
        string? topicName = null, string? serviceKey = null);
}
