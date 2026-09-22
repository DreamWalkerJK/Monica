using Microsoft.EntityFrameworkCore;

namespace Monica.Repository.Outbox.Abstractions;

/// <summary>Stages integration messages for the specified context's next save and transaction.</summary>
public interface IOutboxWriter<TDbContext> : IOutboxWriter where TDbContext : DbContext;

/// <summary>
/// A writer already bound to one context. Resolve <see cref="IOutboxWriter{TDbContext}"/> to select
/// the store explicitly; this interface is not registered as an ambiguous global writer.
/// </summary>
public interface IOutboxWriter
{
    /// <summary>Snapshots a registered payload immediately. Save/commit remains owned by the operation.</summary>
    Guid Enqueue<TMessage>(TMessage message) where TMessage : class;
}
