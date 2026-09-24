namespace Monica.Repository.UnitOfWork.Abstractions;

/// <summary>
/// Provides a hook to decide whether an entity change event should be published.
/// </summary>
public interface IEntityEventPublishSwitch
{
    /// <summary>
    /// Determines whether the current entity change event is allowed to be published.
    /// </summary>
    /// <param name="entity">The entity whose change triggered the event.</param>
    /// <returns><c>true</c> if publishing is allowed; otherwise, <c>false</c>.</returns>
    bool CanPublish(object entity);

    /// <summary>
    /// Gets whether automatic synchronization is currently enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Suspends automatic capture in the owning dependency-injection operation scope.
    /// Independent operations resolve a separate scoped switch; suppression never changes context ownership.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> that resumes synchronization when disposed.</returns>
    IDisposable Suspend();
}