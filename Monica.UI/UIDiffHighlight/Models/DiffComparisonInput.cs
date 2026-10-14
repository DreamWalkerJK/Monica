namespace Monica.UI.UIDiffHighlight.Models;

/// <summary>Detached comparison inputs. Record equality compares fields without delimiter keys.</summary>
internal sealed record DiffComparisonInput(string OriginText, string NewText, DiffComparisonOptions? Options)
{
    internal bool IsEmpty => OriginText.Length == 0 && NewText.Length == 0;

    // A null options snapshot preserves the facade's configured-default behavior.
    internal static DiffComparisonInput Capture(string? originText, string? newText, DiffHighlightOptions? options) =>
        new(originText ?? string.Empty, newText ?? string.Empty, options is null ? null : DiffComparisonOptions.Capture(options));
}

/// <summary>Immutable option values captured before entering the asynchronous service.</summary>
internal sealed record DiffComparisonOptions(
    EDiffHighlightMode Mode, EDiffOutputFormat OutputFormat, bool IgnoreWhitespace, bool IgnoreCase,
    int ContextLines, int MaxCharacterDiffLength, DiffComparisonStyle? Style)
{
    internal static DiffComparisonOptions Capture(DiffHighlightOptions options) => new(
        options.Mode, options.OutputFormat, options.IgnoreWhitespace, options.IgnoreCase,
        options.ContextLines, options.MaxCharacterDiffLength,
        options.Style is null ? null : DiffComparisonStyle.Capture(options.Style));

    /// <summary>Creates a fresh service request; no mutable draft object is shared with computation.</summary>
    internal DiffHighlightOptions ToOptions() => new()
    {
        Mode = Mode, OutputFormat = OutputFormat, IgnoreWhitespace = IgnoreWhitespace, IgnoreCase = IgnoreCase,
        ContextLines = ContextLines, MaxCharacterDiffLength = MaxCharacterDiffLength, Style = Style?.ToStyle()
    };
}

internal sealed record DiffComparisonStyle(
    string AddedLineStyle, string DeletedLineStyle, string ModifiedLineStyle, string UnchangedLineStyle,
    string AddedCharacterStyle, string DeletedCharacterStyle, string LineNumberStyle, string ContainerStyle,
    bool IncludeDefaultCss)
{
    internal static DiffComparisonStyle Capture(DiffHighlightStyle style) => new(
        style.AddedLineStyle, style.DeletedLineStyle, style.ModifiedLineStyle, style.UnchangedLineStyle,
        style.AddedCharacterStyle, style.DeletedCharacterStyle, style.LineNumberStyle, style.ContainerStyle,
        style.IncludeDefaultCss);

    internal DiffHighlightStyle ToStyle() => new()
    {
        AddedLineStyle = AddedLineStyle, DeletedLineStyle = DeletedLineStyle, ModifiedLineStyle = ModifiedLineStyle,
        UnchangedLineStyle = UnchangedLineStyle, AddedCharacterStyle = AddedCharacterStyle,
        DeletedCharacterStyle = DeletedCharacterStyle, LineNumberStyle = LineNumberStyle,
        ContainerStyle = ContainerStyle, IncludeDefaultCss = IncludeDefaultCss
    };
}
