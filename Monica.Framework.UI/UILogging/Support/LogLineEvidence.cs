using Monica.Framework.UI.UILogging.Models;

namespace Monica.Framework.UI.UILogging.Support;

/// <summary>Detached physical-line evidence captured from one source-qualified reader view.</summary>
/// <remarks>The source generation is an observed content generation, not an operating-system file identity or archive guarantee.</remarks>
public sealed record LogLineEvidence
{
    /// <summary>The immutable line captured at invocation, including original text and source/view positions.</summary>
    public required LogLineViewModel Line { get; init; }

    /// <summary>The observed source belonging to the line; absent on standalone legacy buffers.</summary>
    public LogSourceIdentity? Source { get; init; }

    /// <summary>The successful view read time supplied by the reader, rather than a parsed log timestamp.</summary>
    public DateTimeOffset? ViewReadAt { get; init; }

    /// <summary>The instant at which the inspector captured this detached evidence.</summary>
    public required DateTimeOffset InspectedAt { get; init; }

    /// <summary>The zero-based index in the complete visible list before ledger pagination.</summary>
    public required int SourceIndex { get; init; }

    /// <summary>Freezes the immutable source and line records before any asynchronous dialog work starts.</summary>
    public static LogLineEvidence Capture(LogLineViewModel line, LogSourceIdentity? source, DateTimeOffset? viewReadAt, int sourceIndex) => new()
    {
        Line = line with { },
        Source = source is null ? null : source with { },
        ViewReadAt = viewReadAt,
        InspectedAt = DateTimeOffset.UtcNow,
        SourceIndex = sourceIndex
    };
}
