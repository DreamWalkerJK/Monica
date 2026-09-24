using Monica.Repository.UnitOfWork.Abstractions;

namespace Monica.Repository.UnitOfWork.Services;

/// <summary>Controls entity-change projection emission in the current dependency-injection scope.</summary>
public sealed class EntityEventPublicationScope : IEntityEventPublishSwitch
{
    private int _suppressionDepth;

    /// <inheritdoc />
    public bool IsEnabled => Volatile.Read(ref _suppressionDepth) == 0;

    /// <inheritdoc />
    public bool CanPublish(object entity) => IsEnabled;

    /// <summary>Suppresses entity-change projections until the returned lease is disposed.</summary>
    public IDisposable Suspend()
    {
        Interlocked.Increment(ref _suppressionDepth);
        return new Lease(this);
    }

    private sealed class Lease(EntityEventPublicationScope owner) : IDisposable
    {
        private EntityEventPublicationScope? _owner = owner;

        public void Dispose()
        {
            var owner = Interlocked.Exchange(ref _owner, null);
            if (owner is not null) Interlocked.Decrement(ref owner._suppressionDepth);
        }
    }
}
