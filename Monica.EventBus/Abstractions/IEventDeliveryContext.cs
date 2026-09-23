using Monica.EventBus.Models;

namespace Monica.EventBus.Abstractions;

/// <summary>Exposes delivery metadata only within the current handler invocation scope.</summary>
public interface IEventDeliveryContext
{
    /// <summary>Gets the current delivery, or null when no message is being handled.</summary>
    EventDeliveryMetadata? Current { get; }
}
