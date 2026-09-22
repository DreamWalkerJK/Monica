using Monica.Repository.UnitOfWork.Services;

namespace Monica.Repository.UnitOfWork.Abstractions;

/// <summary>
/// Used to trigger entity change events.
/// </summary>
public interface IAsyncLocalEventPublisher
{
    /// <summary>
    /// Buffers (or, outside a unit of work with an open save scope, queues) an entity created event.
    /// </summary>
    /// <param name="entity">The entity that was created.</param>
    void AddEntityCreatedEvent(object entity);

    /// <summary>
    /// Buffers (or, outside a unit of work with an open save scope, queues) an entity updated event.
    /// </summary>
    /// <param name="entity">The entity that was updated.</param>
    void AddEntityUpdatedEvent(object entity);

    /// <summary>
    /// Buffers (or, outside a unit of work with an open save scope, queues) an entity deleted event.
    /// </summary>
    /// <param name="entity">The entity that was deleted.</param>
    void AddEntityDeletedEvent(object entity);

    /// <summary>
    /// Bulk publishes events in the active unit-of-work buffer.
    /// </summary>
    Task FlushEventBuffer();

    /// <summary>
    /// Opens a save-scoped event buffer used when no ambient unit of work is active.
    /// </summary>
    /// <returns>
    /// A scope whose buffer collects the entity events staged during one save, or <see langword="null"/>
    /// when an ambient unit of work already owns event deferral until commit or event publishing is disabled.
    /// </returns>
    DetachedEventBufferScope? TryBeginDetachedEventBuffer();

    /// <summary>
    /// Publishes and clears the events collected by a detached buffer, for example right after a save
    /// outside a unit of work committed successfully.
    /// </summary>
    /// <param name="scope">A scope previously returned by <see cref="TryBeginDetachedEventBuffer"/>.</param>
    Task PublishDetachedEventsAsync(DetachedEventBufferScope scope);
}

/// <summary>
/// No-op publisher used when entity change events are disabled.
/// </summary>
public class NullAsyncLocalEventPublisher : IAsyncLocalEventPublisher
{
    /// <inheritdoc />
    public void AddEntityCreatedEvent(object entity)
    {
        return;
    }

    /// <inheritdoc />
    public void AddEntityUpdatedEvent(object entity)
    {
        return;
    }

    /// <inheritdoc />
    public void AddEntityDeletedEvent(object entity)
    {
        return;
    }

    /// <inheritdoc />
    public Task FlushEventBuffer()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public DetachedEventBufferScope? TryBeginDetachedEventBuffer()
    {
        return null;
    }

    /// <inheritdoc />
    public Task PublishDetachedEventsAsync(DetachedEventBufferScope scope)
    {
        return Task.CompletedTask;
    }
}
