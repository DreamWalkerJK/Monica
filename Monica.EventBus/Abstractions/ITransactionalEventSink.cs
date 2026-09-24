using Monica.EventBus.Models;

namespace Monica.EventBus.Abstractions;

/// <summary>Prepares and stages required events within the current writable operation.</summary>
public interface ITransactionalEventSink
{
    /// <summary>
    /// Validates operation ownership, invokes synchronous preparation once, and stages the complete
    /// captured batch without committing. A preparation or staging failure makes the operation
    /// rollback-only, even when the caller catches the exception.
    /// </summary>
    ValueTask StageAsync(Func<IReadOnlyList<EventMessage>> prepare, CancellationToken cancellationToken);
}
