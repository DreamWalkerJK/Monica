using Monica.EventBus.Models;

namespace Monica.Repository.Outbox.Services;

internal interface IOutboxStoreContext
{
    bool HasOutbox { get; }
    bool HasPendingOutboxMessages { get; }
    void StagePreparedMessage(EventMessage message);
}
