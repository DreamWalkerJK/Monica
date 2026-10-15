namespace Monica.UI.UIDiffHighlight.Models;

/// <summary>Owns one whole accepted comparison without exposing its mutable service result.</summary>
internal sealed class DiffComparisonEvidence
{
    private readonly DiffHighlightResult _result;

    internal DiffComparisonEvidence(long revision, DiffComparisonInput input, DiffHighlightResult result)
    {
        Revision = revision;
        Input = input;
        _result = CopyResult(result);
        ReceivedAt = DateTimeOffset.Now;
    }

    internal long Revision { get; }
    internal DiffComparisonInput Input { get; }
    /// <summary>Local receipt time of the accepted copy, not a backend history timestamp.</summary>
    internal DateTimeOffset ReceivedAt { get; }

    /// <summary>
    /// Creates an independent rendering projection. Retain it for this evidence instance;
    /// changing the projection cannot mutate accepted evidence or another reader's projection.
    /// </summary>
    internal DiffHighlightResult CreateResult() => CopyResult(_result);

    private static DiffHighlightResult CopyResult(DiffHighlightResult result) => new()
    {
        HighlightedContent = result.HighlightedContent,
        ProcessingTimeMs = result.ProcessingTimeMs,
        Options = DiffComparisonOptions.Capture(result.Options).ToOptions(),
        Statistics = new()
        {
            TotalChanges = result.Statistics.TotalChanges,
            AddedLines = result.Statistics.AddedLines,
            DeletedLines = result.Statistics.DeletedLines,
            ModifiedLines = result.Statistics.ModifiedLines,
            UnchangedLines = result.Statistics.UnchangedLines,
            TotalOldLines = result.Statistics.TotalOldLines,
            TotalNewLines = result.Statistics.TotalNewLines
        },
        Lines = result.Lines.Select(line => new DiffLine
        {
            Type = line.Type, OldContent = line.OldContent, NewContent = line.NewContent,
            OldLineNumber = line.OldLineNumber, NewLineNumber = line.NewLineNumber,
            CharacterDiffs = line.CharacterDiffs?.Select(range => new DiffCharacterRange
            {
                Type = range.Type, Start = range.Start, Length = range.Length, Content = range.Content
            }).ToList()
        }).ToList()
    };
}

/// <summary>A failed attempt remains separate from the previous accepted comparison.</summary>
internal sealed record DiffComparisonFailure(long Revision, DiffComparisonInput Input, string Message);

/// <summary>One atomic read of the controller's draft, accepted evidence, and operation state.</summary>
internal sealed record DiffComparisonState(
    long DraftRevision, DiffComparisonInput? Draft, DiffComparisonEvidence? Accepted,
    DiffComparisonFailure? Failure, bool IsBusy, bool HasPendingRequest, bool IsRetired)
{
    internal bool IsAcceptedCurrent => Accepted is not null && Accepted.Input == Draft;
    internal bool IsFailureCurrent => Failure is not null && Failure.Input == Draft;
}
