using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Monica.Core.Execution;
using Monica.EventBus;
using Monica.EventBus.Abstractions;
using Monica.Repository.Inbox.Annotations;
using Monica.Repository.Inbox.Models;
using Monica.Repository.UnitOfWork.Services;

namespace Monica.Repository.Inbox.Services;

/// <summary>Stages a consumer receipt and its handler effects inside the same operation transaction.</summary>
public sealed class InboxExecutionBehavior<TInput, TResult>(
    UnitOfWorkManager unitOfWork,
    IEventDeliveryContext delivery,
    TimeProvider time) : IExecutionBehavior<TInput, TResult>
{
    /// <inheritdoc />
    public async Task<TResult> ExecuteAsync(ExecutionContext<TInput> context, ExecutionDelegate<TResult> next)
    {
        var descriptor = context.Descriptor;
        if (descriptor.Point != EventBusExecutionPoints.LocalHandler
            && descriptor.Point != EventBusExecutionPoints.DistributedHandler)
            return await next();

        var attribute = descriptor.EntryMethod?.GetCustomAttribute<InboxAttribute>(inherit: true)
            ?? descriptor.ComponentType.GetCustomAttribute<InboxAttribute>(inherit: true);
        if (attribute is null) return await next();
        if (descriptor.TransactionMode != ExecutionTransactionMode.Automatic)
            throw new InvalidOperationException($"Inbox handler '{descriptor.ComponentType}' must use an automatic transaction.");

        var metadata = delivery.Current;
        if (metadata is null || string.IsNullOrWhiteSpace(metadata.MessageId) || string.IsNullOrWhiteSpace(metadata.Source))
            throw new InvalidOperationException($"Inbox handler '{descriptor.ComponentType}' requires stable incoming source and message identity.");
        var db = unitOfWork.RequireInboxOwner();
        var receipts = db.Set<InboxReceipt>();
        var consumer = attribute.Consumer;
        var source = metadata.Source;
        var messageId = metadata.MessageId;
        if (await receipts.AsNoTracking().AnyAsync(x => x.Consumer == consumer && x.Source == source
                && x.MessageId == messageId, context.CancellationToken))
        {
            if (typeof(TResult) != typeof(ExecutionUnit))
                throw new InvalidOperationException("Inbox deduplication requires a no-result EventBus handler boundary.");
            return (TResult)(object)ExecutionUnit.Value;
        }

        // The database composite primary key arbitrates concurrent deliveries. A losing transaction
        // rolls back and its broker retry observes the winner's committed receipt.
        receipts.Add(new InboxReceipt
        {
            Consumer = consumer,
            Source = source,
            MessageId = messageId,
            ReceivedAtUtc = time.GetUtcNow().UtcDateTime
        });
        return await next();
    }
}
