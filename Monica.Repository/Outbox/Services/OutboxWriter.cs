using Monica.Repository.Outbox.Abstractions;
using Monica.Repository.Persistence.Services;

namespace Monica.Repository.Outbox.Services;

internal sealed class OutboxWriter<TDbContext>(TDbContext context) : IOutboxWriter<TDbContext>
    where TDbContext : RepositoryDbContext<TDbContext>
{
    public Guid Enqueue<TMessage>(TMessage message) where TMessage : class => context.StageOutboxMessage(message);
}
