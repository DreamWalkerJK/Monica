using Monica.EventBus.Abstractions;
using Monica.EventBus.Models;

namespace Monica.EventBus.Services;

internal sealed class EventDeliveryContext : IEventDeliveryContext
{
    public EventDeliveryMetadata? Current { get; private set; }

    internal void Set(EventDeliveryMetadata? metadata) => Current = metadata;
}
