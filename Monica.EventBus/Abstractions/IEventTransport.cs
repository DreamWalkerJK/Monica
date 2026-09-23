using Monica.EventBus.Models;

namespace Monica.EventBus.Abstractions;

/// <summary>Sends a prepared distributed event without entering the application publishing gateway.</summary>
public interface IEventTransport
{
    /// <summary>Sends the stored identity, route, and body. Completion means transport acceptance.</summary>
    Task SendAsync(EventMessage message, CancellationToken cancellationToken);
}
