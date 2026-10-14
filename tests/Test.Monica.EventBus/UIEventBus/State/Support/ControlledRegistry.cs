using Microsoft.Extensions.Logging.Abstractions;
using Monica.EventBus.Abstractions;
using Monica.EventBus.Models;
using Monica.EventBus.Services;
using Xunit;

namespace Test.Monica.EventBus.UIEventBus.State.Support;

/// <summary>Controls registry boundaries while preserving its real identities, lifecycle, and notifications.</summary>
internal sealed class ControlledRegistry : IEventSubscriptionRegistry, IAsyncDisposable
{
    private Action<int>? _readHook;
    private int _readCount;
    public EventSubscriptionRegistry Inner { get; } = new(NullLogger<EventSubscriptionRegistry>.Instance);
    public int ReadCount => Volatile.Read(ref _readCount);
    public Func<EventSubscriptionDescriptor, CancellationToken, Task>? BeforeSubscribe { get; set; }
    public Func<EventSubscriptionId, CancellationToken, Task>? BeforeUnsubscribe { get; set; }
    public List<EventSubscriptionId> UnsubscribeAttempts { get; } = [];

    public void SetReadHook(Action<int>? hook)
    {
        _readHook = hook;
        Interlocked.Exchange(ref _readCount, 0);
    }

    public IQueryable<IEventSubscription> GetAll()
    {
        _readHook?.Invoke(Interlocked.Increment(ref _readCount));
        return Inner.GetAll();
    }

    public IDisposable Subscribe(IObserver<EventSubscriptionChange> observer) => Inner.Subscribe(observer);
    public async Task<IEventSubscription> SubscribeAsync(EventSubscriptionDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        if (BeforeSubscribe is { } hook) await hook(descriptor, cancellationToken);
        return await Inner.SubscribeAsync(descriptor, cancellationToken);
    }
    public Task<IReadOnlyList<IEventSubscription>> SubscribeBatchAsync(IEnumerable<EventSubscriptionDescriptor> descriptors, CancellationToken cancellationToken = default) =>
        Inner.SubscribeBatchAsync(descriptors, cancellationToken);

    public async Task UnsubscribeAsync(EventSubscriptionId subscriptionId, CancellationToken cancellationToken = default)
    {
        UnsubscribeAttempts.Add(subscriptionId);
        if (BeforeUnsubscribe is { } hook) await hook(subscriptionId, cancellationToken);
        await Inner.UnsubscribeAsync(subscriptionId, cancellationToken);
    }

    public Task UnsubscribeBatchAsync(IEnumerable<EventSubscriptionId> subscriptionIds, CancellationToken cancellationToken = default) =>
        Inner.UnsubscribeBatchAsync(subscriptionIds, cancellationToken);
    public Task UnsubscribeWhereAsync(Func<IEventSubscription, bool> predicate, CancellationToken cancellationToken = default) =>
        Inner.UnsubscribeWhereAsync(predicate, cancellationToken);
    public IEventSubscription? GetById(EventSubscriptionId subscriptionId) => Inner.GetById(subscriptionId);
    public IReadOnlyList<IEventSubscription> GetByServiceKey(string? serviceKey) => Inner.GetByServiceKey(serviceKey);
    public IReadOnlyList<IEventSubscription> GetByTopicName(string topicName) => Inner.GetByTopicName(topicName);
    public IReadOnlyList<IEventSubscription> GetByEventType(Type eventType) => Inner.GetByEventType(eventType);
    public IReadOnlyList<IEventSubscription> GetByState(EventSubscriptionState state) => Inner.GetByState(state);
    public IReadOnlyList<IEventSubscription> GetByScope(EventSubscriptionScope scope) => Inner.GetByScope(scope);
    public Task ActivateAsync(EventSubscriptionId subscriptionId, CancellationToken cancellationToken = default) =>
        Inner.ActivateAsync(subscriptionId, cancellationToken);
    public Task DeactivateAsync(EventSubscriptionId subscriptionId, CancellationToken cancellationToken = default) =>
        Inner.DeactivateAsync(subscriptionId, cancellationToken);
    public Task ReactivateAsync(EventSubscriptionId subscriptionId, CancellationToken cancellationToken = default) =>
        Inner.ReactivateAsync(subscriptionId, cancellationToken);
    public Task UnsubscribeByEventTypeAsync(Type eventType, CancellationToken cancellationToken = default) =>
        Inner.UnsubscribeByEventTypeAsync(eventType, cancellationToken);
    public Task UnsubscribeByHandlerTypeAsync(Type handlerType, CancellationToken cancellationToken = default) =>
        Inner.UnsubscribeByHandlerTypeAsync(handlerType, cancellationToken);
    public Task UnsubscribeByServiceKeyAsync(string? serviceKey, CancellationToken cancellationToken = default) =>
        Inner.UnsubscribeByServiceKeyAsync(serviceKey, cancellationToken);

    public async ValueTask DisposeAsync() => await Inner.UnsubscribeBatchAsync(
        Inner.GetAll().Select(subscription => subscription.Id).ToList(), TestContext.Current.CancellationToken);
}
