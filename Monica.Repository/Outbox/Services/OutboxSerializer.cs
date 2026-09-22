using System.Text.Json;
using Monica.Repository.Outbox.Models;

namespace Monica.Repository.Outbox.Services;

internal static class OutboxSerializer
{
    public static OutboxMessage Capture(object payload, RepositoryOutboxOptions options, TimeProvider time)
    {
        if (!options.ContractsByType.TryGetValue(payload.GetType(), out var contract))
            throw new InvalidOperationException($"Payload '{payload.GetType()}' has no registered outbox contract.");
        return new OutboxMessage
        {
            Contract = contract.Name,
            Payload = JsonSerializer.Serialize(payload, contract.PayloadType, options.Json),
            CreatedAtUtc = time.GetUtcNow().UtcDateTime
        };
    }

    public static (OutboxContract Contract, object Envelope) Read(OutboxMessage message, RepositoryOutboxOptions options)
    {
        if (!options.ContractsByName.TryGetValue(message.Contract, out var contract))
            throw new InvalidOperationException($"Unknown outbox contract '{message.Contract}'. Register its version before dispatch.");
        var payload = JsonSerializer.Deserialize(message.Payload, contract.PayloadType, options.Json)
            ?? throw new InvalidOperationException($"Outbox message '{message.MessageId}' contains a null payload.");
        return (contract, contract.Wrap(message, payload));
    }
}
