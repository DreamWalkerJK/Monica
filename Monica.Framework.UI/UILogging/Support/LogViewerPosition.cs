using Monica.Framework.UI.UILogging.Models;

namespace Monica.Framework.UI.UILogging.Support;

/// <summary>Detached navigation intent for one source-qualified physical-line ledger.</summary>
/// <remarks>
/// Restore this position only against the same observed source generation. An anchor or selection
/// is valid only when both its physical number and ordinal raw text still match the visible evidence.
/// This value owns no reader session, browser reference, or running producer.
/// </remarks>
public sealed record LogViewerPosition
{
    /// <summary>The exact file and observed generation belonging to the captured visible line.</summary>
    public required LogSourceIdentity Source { get; init; }

    /// <summary>The physical number of the first line intersecting the local ledger viewport.</summary>
    public required long AnchorLineNumber { get; init; }

    /// <summary>The unchanged raw text of that anchor, including empty lines and whitespace.</summary>
    public required string AnchorRaw { get; init; }

    /// <summary>The anchor's top offset from the local viewport in CSS pixels; negative values preserve partial rows.</summary>
    public double AnchorOffset { get; init; }

    /// <summary>The selected physical line, when selection remains in this source-qualified visible list.</summary>
    public long? SelectedLineNumber { get; init; }

    /// <summary>The selected line's exact raw text; null means there is no restorable selection.</summary>
    public string? SelectedRaw { get; init; }

    /// <summary>Whether the reader deliberately browses earlier evidence instead of following the newest row.</summary>
    public bool BrowsingEarlier { get; init; }
}
