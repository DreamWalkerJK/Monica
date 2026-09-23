namespace Monica.EventBus.Abstractions;

/// <summary>
/// Defines the contract for the local, in-process event bus.
/// Application publishing gateways are scoped so durable local events can join the current
/// writable operation. Long-lived services must resolve one from a scope for each operation.
/// </summary>
public interface ILocalEventBus : IEventBus
{
    
}
