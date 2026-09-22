using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using Monica.Core.ObjectMapping.Abstractions;
using Monica.EventBus.Abstractions;
using Monica.Repository.Entity.Abstractions;
using Monica.Repository.Persistence.Extensions;
using Monica.Repository.UnitOfWork.Abstractions;
using Monica.Repository.UnitOfWork.Models;
using Monica.Tool.Extensions;

namespace Monica.Repository.UnitOfWork.Services;


public class AsyncEventBuffer
{
    public List<TransactionEventRecord> Records { get; set; } = [];
    public List<TransactionEventRecord> DistributedEvents { get; } = [];
    public List<TransactionEventRecord> LocalEvents { get; } = [];

    public HashSet<int> DistributedEventsHash { get; } = [];
    public HashSet<int> LocalEventsHash { get; } = [];

    public async Task Flush(ILocalEventBus eventBus, IDistributedEventBus distributedEventBus)
    {
        while (LocalEvents.Count != 0 || DistributedEvents.Count != 0)
        {
            if (LocalEvents.Count != 0)
            {
                var group = LocalEvents.GroupBy(p => p.EventType).ToDictionary(p => p.Key, p => p.Select(r => r.EventData).ToList());
                LocalEvents.Clear();
                LocalEventsHash.Clear();
                foreach (var record in group)
                {
                    await eventBus.BulkPublishAsync(record.Key, record.Value);
                }
            }

            if (DistributedEvents.Count != 0)
            {
                var group = DistributedEvents.GroupBy(p => p.EventType).ToDictionary(p => p.Key, p => p.Select(r => r.EventData).ToList());
                DistributedEvents.Clear();
                DistributedEventsHash.Clear();
                foreach (var record in group)
                {
                    await distributedEventBus.BulkPublishAsync(record.Key, record.Value);
                }
            }
        }
    }
}


/// <summary>
/// A save-scoped entity event buffer used when no ambient unit of work is active.
/// </summary>
/// <remarks>
/// The repository save pipeline opens the scope before staging events and publishes the collected events
/// right after the save commits; disposing the scope restores the previous buffer slot of the async flow.
/// </remarks>
public sealed class DetachedEventBufferScope(AsyncEventBuffer buffer, Action dispose) : IDisposable
{
    private Action? _dispose = dispose;

    /// <summary>
    /// Gets the buffer that collects entity events for the owning save.
    /// </summary>
    public AsyncEventBuffer Buffer { get; } = buffer;

    /// <summary>
    /// Restores the async-flow buffer slot this scope occupied.
    /// </summary>
    public void Dispose()
    {
        Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}

public class AsyncLocalEventStore(IUnitOfWorkManager uow) : IAsyncLocalEventStore
{
    // Save-scoped buffers for flows without an ambient unit of work. AsyncLocal keeps concurrent saves
    // in separate async flows isolated; the save pipeline always closes the slot it opened.
    private static readonly AsyncLocal<AsyncEventBuffer?> _detachedBuffer = new();

    /// <inheritdoc />
    public AsyncEventBuffer? GetBuffer()
    {
        return uow.Current is IUnitOfWorkInternals internals ? internals.GetEventBuffer() : null;
    }

    /// <inheritdoc />
    public AsyncEventBuffer? GetActiveBuffer()
    {
        if (uow.Current is IUnitOfWorkInternals internals)
        {
            return internals.GetOrCreateEventBuffer();
        }

        return _detachedBuffer.Value;
    }

    /// <inheritdoc />
    public DetachedEventBufferScope? TryBeginDetachedBuffer()
    {
        // Inside a unit of work the unit's own buffer owns event deferral until commit.
        if (uow.Current != null)
        {
            return null;
        }

        var previous = _detachedBuffer.Value;
        var buffer = new AsyncEventBuffer();
        _detachedBuffer.Value = buffer;
        return new DetachedEventBufferScope(buffer, () => _detachedBuffer.Value = previous);
    }
}

/// <summary>
/// Resolves the event buffer entity events are staged into.
/// </summary>
public interface IAsyncLocalEventStore
{
    /// <summary>
    /// Gets the ambient unit-of-work event buffer, if one exists.
    /// </summary>
    AsyncEventBuffer? GetBuffer();

    /// <summary>
    /// Gets the buffer events are currently staged into: the ambient unit-of-work buffer (created on demand)
    /// or the save-scoped buffer of a save running without a unit of work. Returns <see langword="null"/>
    /// when no boundary provides buffering.
    /// </summary>
    AsyncEventBuffer? GetActiveBuffer();

    /// <summary>
    /// Opens a save-scoped buffer for the current async flow; returns <see langword="null"/> when an
    /// ambient unit of work already buffers events.
    /// </summary>
    DetachedEventBufferScope? TryBeginDetachedBuffer();
}

/// <summary>
/// Used to trigger entity change events.
/// </summary>
public class AsyncLocalEventPublisher(
    IObjectMapper entityToEtoMapper,
    IOptions<DistributedEntityEventOptions> distributedEntityEventOptions,
    ILocalEventBus localEventBus,
    IDistributedEventBus distributedEventBus,
    IAsyncLocalEventStore bufferStore,
    IEnumerable<IEntityEventPublishSwitch>? publishSwitches) : IAsyncLocalEventPublisher
{
    /// <summary>
    /// Gets or sets the local event bus
    /// </summary>
    public ILocalEventBus LocalEventBus { get; set; } = localEventBus;
    
    /// <summary>
    /// Gets or sets the distributed event bus
    /// </summary>
    public IDistributedEventBus DistributedEventBus { get; set; } = distributedEventBus;
    
    /// <summary>
    /// Gets the entity to ETO mapper
    /// </summary>
    protected IObjectMapper EntityToEtoMapper { get; } = entityToEtoMapper;
    
    /// <summary>
    /// Gets the distributed entity event options
    /// </summary>
    protected DistributedEntityEventOptions DistributedEntityEventOptions { get; } = distributedEntityEventOptions.Value;

    private readonly IEnumerable<IEntityEventPublishSwitch> _publishSwitches = publishSwitches ?? Array.Empty<IEntityEventPublishSwitch>();

    /// <summary>
    /// Adds an entity created event to the event buffer
    /// </summary>
    /// <param name="entity">The entity that was created</param>
    public virtual void AddEntityCreatedEvent(object entity)
    {
        if (!ShouldPublishEventForEntity(entity, out var eventOption)) return;
        
        // Check if create events are disabled for this entity
        if (eventOption.DisabledAutoEntityEventType.HasFlag(EDisabledAutoEntityEventType.Create))
            return;

        if (eventOption.EnableLocalEvent)
        {
            TriggerEventWithEntity(
                LocalEventBus,
                typeof(EntityCreatedEventData<>),
                entity,
                entity
            );
        }

        if (eventOption.EnableDistributedEvent)
        {
            var eto = eventOption.EtoMappingType != null ? EntityToEtoMapper.Map(entity, entity.GetType(), eventOption.EtoMappingType) : entity;

            TriggerEventWithEntity(
                DistributedEventBus,
                typeof(EntityCreatedEto<>),
                eto,
                entity
            );
        }
    }

    /// <summary>
    /// Determines if events should be published for the given entity
    /// </summary>
    /// <param name="entity">The entity to check</param>
    /// <param name="eventOption">The event options for the entity if found</param>
    /// <returns>True if events should be published, false otherwise</returns>
    private bool ShouldPublishEventForEntity(object entity,[NotNullWhen(true)] out EntityEventOption? eventOption)
    {
        if (!_publishSwitches.All(s => s.CanPublish(entity)))
        {
            eventOption = null;
            return false;
        }
        
        return DistributedEntityEventOptions
            .AutoEventOptionDict
            .TryGetValue(entity.GetType(), out eventOption);
    }

    /// <summary>
    /// Adds an entity updated event to the event buffer
    /// </summary>
    /// <param name="entity">The entity that was updated</param>
    public virtual void AddEntityUpdatedEvent(object entity)
    {
        if (!ShouldPublishEventForEntity(entity, out var eventOption)) return;
        
        // Check if update events are disabled for this entity
        if (eventOption.DisabledAutoEntityEventType.HasFlag(EDisabledAutoEntityEventType.Update))
            return;

        if (eventOption.EnableLocalEvent)
        {
            TriggerEventWithEntity(
                LocalEventBus,
                typeof(EntityUpdatedEventData<>),
                entity,
                entity
            );
        }

        if (eventOption.EnableDistributedEvent)
        {
            var eto = eventOption.EtoMappingType != null ? EntityToEtoMapper.Map(entity, entity.GetType(), eventOption.EtoMappingType) : entity;

            TriggerEventWithEntity(
                DistributedEventBus,
                typeof(EntityUpdatedEto<>),
                eto,
                entity
            );
        }
    }

    /// <summary>
    /// Adds an entity deleted event to the event buffer
    /// </summary>
    /// <param name="entity">The entity that was deleted</param>
    public virtual void AddEntityDeletedEvent(object entity)
    {
        if (!ShouldPublishEventForEntity(entity, out var eventOption)) return;
        
        // Check if delete events are disabled for this entity
        if (eventOption.DisabledAutoEntityEventType.HasFlag(EDisabledAutoEntityEventType.Delete))
            return;

        if (eventOption.EnableLocalEvent)
        {
            TriggerEventWithEntity(
                LocalEventBus,
                typeof(EntityDeletedEventData<>),
                entity,
                entity
            );
        }

        if (eventOption.EnableDistributedEvent)
        {
            var eto = eventOption.EtoMappingType != null ? EntityToEtoMapper.Map(entity, entity.GetType(), eventOption.EtoMappingType) : entity;

            TriggerEventWithEntity(
                DistributedEventBus,
                typeof(EntityDeletedEto<>),
                eto,
                entity
            );
        }
    }

    /// <summary>
    /// Flushes the event buffer, publishing all pending events
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task FlushEventBuffer()
    {
        var buffer = bufferStore.GetBuffer();
        if (buffer != null)
        {
            await buffer.Flush(LocalEventBus, DistributedEventBus);
        }
    }


    #region Store

    /// <summary>
    /// Triggers an event with the specified entity
    /// </summary>
    /// <param name="eventPublisher">The event publisher to use</param>
    /// <param name="genericEventType">The generic event type</param>
    /// <param name="entityOrEto">The entity or ETO object</param>
    /// <param name="originalEntity">The original entity</param>
    protected virtual void TriggerEventWithEntity(
        IEventBus eventPublisher,
        Type genericEventType,
        object entityOrEto,
        object originalEntity)
    {
        var entityType = entityOrEto.GetType();
        var eventType = genericEventType.MakeGenericType(entityType);
        var eventData = Activator.CreateInstance(eventType, entityOrEto)!;


        var eventRecord = new TransactionEventRecord(eventType, eventData, originalEntity);

        var buffer = bufferStore.GetActiveBuffer()
            ?? throw new InvalidOperationException(
                "Entity event publishing requires an event boundary. Inside a unit of work events defer until commit; " +
                "outside one, save through SaveChangesAsync, which publishes the events it staged right after the save commits. " +
                "Synchronous SaveChanges without a unit of work cannot publish entity events.");
        if (eventPublisher == DistributedEventBus)
        {
            AddOrReplaceEvent(buffer.DistributedEvents, buffer.DistributedEventsHash, eventRecord);
        }
        else
        {
            AddOrReplaceEvent(buffer.LocalEvents, buffer.LocalEventsHash, eventRecord);
        }
    }

    /// <inheritdoc />
    public DetachedEventBufferScope? TryBeginDetachedEventBuffer()
    {
        return bufferStore.TryBeginDetachedBuffer();
    }

    /// <inheritdoc />
    public Task PublishDetachedEventsAsync(DetachedEventBufferScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return scope.Buffer.Flush(LocalEventBus, DistributedEventBus);
    }
    
    /// <summary>
    /// Adds or replaces an event in the event list
    /// </summary>
    /// <param name="events">The list of events</param>
    /// <param name="eventHashSet">The hash set of event hashes</param>
    /// <param name="eventRecord">The event record to add or replace</param>
    public virtual void AddOrReplaceEvent(List<TransactionEventRecord> events, HashSet<int> eventHashSet, TransactionEventRecord eventRecord)
    {
        var hash = eventRecord.GetEventHashCode();
        if (hash == null)
        {
            events.Add(eventRecord);
            return;
        }
        if (eventHashSet.Add(hash.Value))
        {
            events.Add(eventRecord);
        }
        else
        {
            // In case of a hash collision.
            var foundIndex = events.FindIndex(p => IsSameEntityEventRecord(p, eventRecord));
            if (foundIndex < 0)
            {
                events.Add(eventRecord);
            }
        }
    }
    
    /// <summary>
    /// Determines if two event records refer to the same entity event
    /// </summary>
    /// <param name="record1">The first event record</param>
    /// <param name="record2">The second event record</param>
    /// <returns>True if the records refer to the same entity event, false otherwise</returns>
    public bool IsSameEntityEventRecord(TransactionEventRecord record1, TransactionEventRecord record2)
    {
        if (record1.EventType != record2.EventType)
        {
            return false;
        }

        if (record1.OriginEntity is not IEntity record1OriginalEntity || record2.OriginEntity is not IEntity record2OriginalEntity)
        {
            return false;
        }

        return EntityHelper.EntityEquals(record1OriginalEntity, record2OriginalEntity);
    }
    #endregion
}
