using Microsoft.Extensions.DependencyInjection;
using Monica.EventBus.Abstractions.Handlers;
using Monica.EventBus.Models;
using Monica.EventBus.Models.Internal;

namespace Monica.EventBus.Services.Support;

internal sealed class EventBusAutoDiscovery
{
    private readonly List<EventHandlerRegistration> _registrations = [];

    public void Collect(Type type)
    {
        if (type is not { IsClass: true, IsAbstract: false } || !typeof(IEventHandler).IsAssignableFrom(type))
        {
            return;
        }

        _registrations.AddRange(EventHandlerRegistration.CreateFromHandlerType(type));
    }

    public IReadOnlyList<EventSubscriptionDescriptor> BuildDescriptors(IServiceScopeFactory serviceScopeFactory)
    {
        using var probeScope = serviceScopeFactory.CreateScope();
        return _registrations
            .Where(static registration => registration.IsAutoRegistered)
            // Type discovery sees handler classes inside shared assemblies that this host references but
            // does not own. Only subscribe handler types this host registered; subscribing a foreign host's
            // handler fails on every delivery and wedges the topic subscription in endless retry.
            .Where(registration => IsOwnedByThisHost(probeScope.ServiceProvider, registration.HandlerType))
            .Select(registration => new EventSubscriptionDescriptor
            {
                ServiceKey = null,
                EventType = registration.EventType,
                TopicName = registration.TopicName,
                HandlerFactory = new IocEventHandlerFactory(serviceScopeFactory, registration.HandlerType),
                Scope = registration.IsDistributed ? EventSubscriptionScope.Distributed : EventSubscriptionScope.Local,
                IsAutoDiscovered = true
            })
            .ToList();
    }

    private static bool IsOwnedByThisHost(IServiceProvider provider, Type handlerType)
    {
        try
        {
            return provider.GetService(handlerType) is not null;
        }
        catch
        {
            // The type is registered but cannot be constructed here; keep the subscription so delivery
            // surfaces the defect instead of silently unsubscribing the owning host's handler.
            return true;
        }
    }
}
