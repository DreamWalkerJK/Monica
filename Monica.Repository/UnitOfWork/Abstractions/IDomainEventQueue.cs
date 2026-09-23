namespace Monica.Repository.UnitOfWork.Abstractions;

/// <summary>Stages explicit domain effects to run in the current operation before commit.</summary>
public interface IDomainEventQueue
{
    /// <summary>Queues an event. Its handlers resolve from this operation's scope and must avoid external side effects.</summary>
    void Enqueue<TEvent>(TEvent domainEvent) where TEvent : class;
}

/// <summary>Handles an exact domain-event type within the publisher's DI scope and transaction.</summary>
public interface IDomainEventHandler<in TEvent> where TEvent : class
{
    /// <summary>Applies transactional domain effects; exceptions abort the operation.</summary>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
