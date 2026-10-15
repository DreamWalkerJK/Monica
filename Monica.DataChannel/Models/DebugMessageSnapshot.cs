using Monica.DataChannel.Pipeline;

namespace Monica.DataChannel.Models;

/// <summary>Immutable evidence frozen when the debugger observes a message before endpoint dispatch.</summary>
/// <param name="Sequence">The middleware-local sequence, preserved across queue clears.</param>
/// <param name="Timestamp">The UTC capture time.</param>
/// <param name="Source">The side from which the message entered the pipeline.</param>
/// <param name="FormattedContent">Complete content produced by the debugger's formatting policy.</param>
/// <param name="MessageType">The observed payload CLR type, or null text for an absent payload.</param>
/// <param name="Payload">Detached serialized payload evidence.</param>
/// <param name="Metadata">Detached metadata values, preserving nulls.</param>
/// <remarks>A retained snapshot stays valid after trimming or clearing. It does not prove downstream receipt.</remarks>
public sealed record DebugMessageSnapshot(long Sequence, DateTime Timestamp, ChannelSide Source,
    string FormattedContent, string MessageType, DataChannelValueSnapshot Payload,
    IReadOnlyDictionary<string, DataChannelValueSnapshot> Metadata);
