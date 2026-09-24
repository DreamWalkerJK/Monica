namespace Monica.EventBus.Models;

/// <summary>
/// Prepared transport message. The body is a UTF-8 JSON snapshot owned by the producer;
/// providers send it without serializing the application event again.
/// </summary>
/// <param name="Metadata">Stable routing and delivery metadata.</param>
/// <param name="Scope">Local or distributed delivery scope.</param>
/// <param name="Body">Read-only serialized event payload.</param>
public sealed record EventMessage(
    EventDeliveryMetadata Metadata,
    EventSubscriptionScope Scope,
    ReadOnlyMemory<byte> Body);
