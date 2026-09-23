namespace Monica.EventBus.Abstractions;

/// <summary>
/// Defines the contract for the distributed event bus.
/// Application publishing gateways are scoped. Long-lived services must create a scope for each
/// publishing operation. Provider transports and the subscription registry remain singletons.
/// </summary>
public interface IDistributedEventBus : IEventBus
{
  
}
