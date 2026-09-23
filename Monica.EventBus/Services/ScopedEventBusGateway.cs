using Microsoft.Extensions.DependencyInjection;
using Monica.EventBus.Abstractions;
using Monica.EventBus.Abstractions.Handlers;
using Monica.EventBus.Annotations;
using Monica.EventBus.Models;

namespace Monica.EventBus.Services;

/// <summary>Applies durable publishing policy before delegating ordinary events to a provider.</summary>
public abstract class ScopedEventBusGateway(
    IEventBus provider,
    IEventMessageFactory messageFactory,
    IServiceProvider services,
    EventSubscriptionScope scope,
    string? serviceKey) : IEventBus
{
    public IEventSubscriptionRegistry Subscriptions => provider.Subscriptions;

    public Task PublishAsync<TEvent>(TEvent eventData, string? topicName = null,
        CancellationToken cancellationToken = default) where TEvent : class
        => PublishAsync(typeof(TEvent), eventData, topicName, cancellationToken);

    public async Task PublishAsync(Type eventType, object eventData, string? topicName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        var required = IsDurable(eventType) || eventData is not null && IsDurable(eventData.GetType());
        if (!required)
        {
            await provider.PublishAsync(eventType, eventData!, topicName, cancellationToken);
            return;
        }

        await RequireSink(eventType).StageAsync(() =>
        {
            ValidateExactType(eventType, eventData!);
            EnsureDurableTransport(eventType);
            return [messageFactory.Prepare(eventType, eventData!, scope, topicName, serviceKey)];
        }, cancellationToken);
    }

    public Task BulkPublishAsync<TEvent>(IEnumerable<TEvent> eventDataList, string? topicName = null,
        CancellationToken cancellationToken = default) where TEvent : class
        => BulkPublishAsync(typeof(TEvent), eventDataList is null ? null! : eventDataList.Cast<object>(),
            topicName, cancellationToken);

    public async Task BulkPublishAsync(Type eventType, IEnumerable<object> eventDataList,
        string? topicName = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        if (IsDurable(eventType))
        {
            await RequireSink(eventType).StageAsync(() =>
            {
                ArgumentNullException.ThrowIfNull(eventDataList);
                EnsureDurableTransport(eventType);
                var values = eventDataList.ToArray();
                foreach (var value in values)
                {
                    ValidateExactType(eventType, value);
                }

                // Capture the complete batch before staging its first row.
                return values.Select(value => messageFactory.Prepare(eventType, value, scope,
                    topicName, serviceKey)).ToArray();
            }, cancellationToken);
            return;
        }

        ArgumentNullException.ThrowIfNull(eventDataList);
        var ordinaryValues = eventDataList.ToArray();
        if (ordinaryValues.Any(value => value is not null && IsDurable(value.GetType())))
        {
            await RequireSink(eventType).StageAsync(() =>
            {
                foreach (var value in ordinaryValues)
                {
                    ValidateExactType(eventType, value);
                }

                throw new InvalidOperationException("A durable event cannot be published through a different declared contract.");
            }, cancellationToken);
            return;
        }

        await provider.BulkPublishAsync(eventType, ordinaryValues, topicName, cancellationToken);
    }

    public Task<IEventSubscription> SubscribeAsync<TEvent, THandler>(string? topicName = null)
        where TEvent : class where THandler : IEventHandler
        => provider.SubscribeAsync<TEvent, THandler>(topicName);

    public Task<IEventSubscription> SubscribeAsync<TEvent>(Func<TEvent, CancellationToken, Task> handler,
        string? topicName = null) where TEvent : class
        => provider.SubscribeAsync(handler, topicName);

    private static bool IsDurable(Type eventType)
        => eventType.IsDefined(typeof(OutboxAttribute), inherit: false);

    private ITransactionalEventSink RequireSink(Type eventType)
        => services.GetService<ITransactionalEventSink>()
           ?? throw new InvalidOperationException(
               $"Durable event '{eventType}' requires AddOutbox<TContext>() and an active writable operation.");

    private static void ValidateExactType(Type eventType, object eventData)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(eventData);
        if (eventData.GetType() != eventType)
        {
            throw new ArgumentException(
                $"The event value type '{eventData.GetType()}' must match the exact published contract '{eventType}'.",
                nameof(eventData));
        }
    }

    private void EnsureDurableTransport(Type eventType)
    {
        if (scope == EventSubscriptionScope.Distributed &&
            (serviceKey is null
                ? services.GetService<IEventTransport>()
                : services.GetKeyedService<IEventTransport>(serviceKey)) is null)
        {
            throw new InvalidOperationException(
                $"Durable distributed event '{eventType}' requires a real distributed transport provider.");
        }
    }
}

/// <summary>Scoped application gateway for a singleton local provider.</summary>
public sealed class ScopedLocalEventBusGateway(
    ILocalEventBus provider, IEventMessageFactory factory, IServiceProvider services,
    string? serviceKey = null)
    : ScopedEventBusGateway(provider, factory, services, EventSubscriptionScope.Local, serviceKey),
        ILocalEventBus;

/// <summary>Scoped application gateway for a singleton distributed provider.</summary>
public sealed class ScopedDistributedEventBusGateway(
    IDistributedEventBus provider, IEventMessageFactory factory, IServiceProvider services,
    string? serviceKey = null)
    : ScopedEventBusGateway(provider, factory, services, EventSubscriptionScope.Distributed, serviceKey),
        IDistributedEventBus;
