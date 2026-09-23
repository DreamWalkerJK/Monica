namespace Monica.EventBus.Models;

/// <summary>
/// Identifies a delivered event. An absent message ID or source means the transport did not
/// supply a stable identity and the delivery cannot be used for durable deduplication.
/// </summary>
/// <param name="MessageId">Stable producer message ID, when supplied.</param>
/// <param name="Source">Stable producer application identity, when supplied.</param>
/// <param name="EventName">Declared event contract name.</param>
/// <param name="TopicName">Actual transport destination.</param>
/// <param name="ServiceKey">Optional keyed bus identifier.</param>
/// <param name="TraceParent">W3C trace parent, when supplied.</param>
/// <param name="TraceState">W3C trace state, when supplied.</param>
public sealed record EventDeliveryMetadata(
    string? MessageId,
    string? Source,
    string EventName,
    string TopicName,
    string? ServiceKey,
    string? TraceParent,
    string? TraceState)
{
    /// <summary>Gets the provider partition key captured from the publishing contract.</summary>
    public string? TransportKey { get; init; }
}
