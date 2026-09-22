using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Monica.EventBus.Abstractions;
using Monica.Repository.Outbox.Models;
using Monica.Repository.Persistence.Services;

namespace Monica.Repository.Outbox.Services;

/// <summary>
/// Explicit outbox drain for a worker or test. Each call uses a fresh scope and only committed rows.
/// Delivery is at least once; expired claims may be redelivered. Processing stops at the first pending
/// failure or live claim, preserving sequence selection without silently skipping failed notifications.
/// </summary>
public sealed class OutboxDispatcher<TDbContext>(IServiceScopeFactory scopes, TimeProvider time)
    where TDbContext : RepositoryDbContext<TDbContext>
{
    /// <summary>
    /// Delivers at most the requested number of messages. Returns acknowledged count.
    /// A transport failure leaves the message pending and propagates; invoke again to retry.
    /// Applications own scheduling, backoff, monitoring, retention and consumer idempotency.
    /// </summary>
    public async Task<int> DrainAsync(int maximumMessages = 100, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumMessages, 1);
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var options = scope.ServiceProvider.GetRequiredService<OutboxRegistration<TDbContext>>().Options;
        var rows = db.Set<OutboxMessage>();
        var delivered = 0;
        while (delivered < maximumMessages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = await rows.AsNoTracking().Where(x => x.DeliveredAtUtc == null)
                .OrderBy(x => x.Sequence).FirstOrDefaultAsync(cancellationToken);
            if (row is null) break;
            var now = time.GetUtcNow().UtcDateTime;
            var lease = Guid.NewGuid();
            var until = now.Add(options.DeliveryLease);
            var claimed = await rows.Where(x => x.Sequence == row.Sequence && x.DeliveredAtUtc == null
                    && (x.LeaseUntilUtc == null || x.LeaseUntilUtc <= now))
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.LeaseId, lease)
                    .SetProperty(x => x.LeaseUntilUtc, until).SetProperty(x => x.Attempts, x => x.Attempts + 1), cancellationToken);
            if (claimed == 0) break;

            try
            {
                var (contract, envelope) = OutboxSerializer.Read(row, options);
                IEventBus bus = contract.Destination == OutboxDestination.Local
                    ? scope.ServiceProvider.GetRequiredService<ILocalEventBus>()
                    : scope.ServiceProvider.GetRequiredService<IDistributedEventBus>();
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                deadline.CancelAfter(options.DeliveryLease);
                await bus.PublishAsync(envelope.GetType(), envelope, topicName: contract.Name, cancellationToken: deadline.Token);
                var acknowledgedAt = time.GetUtcNow().UtcDateTime;
                var acknowledged = await rows.Where(x => x.Sequence == row.Sequence && x.LeaseId == lease)
                    .ExecuteUpdateAsync(update => update.SetProperty(x => x.DeliveredAtUtc, acknowledgedAt)
                        .SetProperty(x => x.LeaseId, (Guid?)null).SetProperty(x => x.LeaseUntilUtc, (DateTime?)null), cancellationToken);
                if (acknowledged == 0) break;
                delivered++;
            }
            catch (Exception exception)
            {
                try
                {
                    // Cleanup is independent of an already canceled delivery. A lost claim is never overwritten.
                    await rows.Where(x => x.Sequence == row.Sequence && x.LeaseId == lease)
                        .ExecuteUpdateAsync(update => update.SetProperty(x => x.LeaseId, (Guid?)null)
                            .SetProperty(x => x.LeaseUntilUtc, (DateTime?)null), CancellationToken.None);
                }
                catch (Exception cleanup) { exception.Data["Monica.Outbox.ClaimReleaseException"] = cleanup; }
                throw;
            }
        }
        return delivered;
    }
}
