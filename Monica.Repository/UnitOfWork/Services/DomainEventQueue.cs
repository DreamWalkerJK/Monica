using Microsoft.Extensions.DependencyInjection;
using Monica.Repository.UnitOfWork.Abstractions;

namespace Monica.Repository.UnitOfWork.Services;

internal sealed class DomainEventQueue(IServiceProvider services, IUnitOfWorkManager manager) : IDomainEventQueue
{
    private readonly Queue<Func<CancellationToken, Task>> _events = new();
    private bool _closed;

    public void Enqueue<TEvent>(TEvent domainEvent) where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        if (_closed || manager.Current is null)
            throw new InvalidOperationException("Queue domain events only inside an active operation.");
        _events.Enqueue(async token =>
        {
            foreach (var handler in services.GetServices<IDomainEventHandler<TEvent>>())
                await handler.HandleAsync(domainEvent, token);
        });
    }

    public async Task DrainAsync(int maximumEvents, CancellationToken token)
    {
        var count = 0;
        while (_events.TryDequeue(out var handle))
        {
            token.ThrowIfCancellationRequested();
            if (++count > maximumEvents)
                throw new InvalidOperationException("Domain event limit exceeded; a handler cycle may exist.");
            await handle(token);
        }
    }

    public void Close()
    {
        _closed = true;
        _events.Clear();
    }
}
