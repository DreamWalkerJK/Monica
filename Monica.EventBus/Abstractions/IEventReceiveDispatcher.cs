using Monica.EventBus.Models;

namespace Monica.EventBus.Abstractions;

/// <summary>Dispatches a received transport message through ordinary EventBus handlers.</summary>
public interface IEventReceiveDispatcher
{
    /// <summary>Deserializes a known subscription type and awaits its matching handlers.</summary>
    Task DispatchAsync(EventMessage message, CancellationToken cancellationToken);
}
