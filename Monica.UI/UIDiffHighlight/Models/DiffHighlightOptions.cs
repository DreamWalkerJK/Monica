namespace Monica.UI.UIDiffHighlight.Models;

/// <summary>
/// Configuration options for diff highlighting.
/// </summary>
public class DiffHighlightOptions
{
    /// <summary>
    /// Gets or sets the comparison mode.
    /// </summary>
    public EDiffHighlightMode Mode { get; set; } = EDiffHighlightMode.Line;
    
    /// <summary>
    /// Gets or sets the output format.
    /// </summary>
    public EDiffOutputFormat OutputFormat { get; set; } = EDiffOutputFormat.Html;
    
    /// <summary>
    /// Gets or sets a value indicating whether whitespace should be ignored.
    /// </summary>
    public bool IgnoreWhitespace { get; set; } = false;
    
    /// <summary>
    /// Gets or sets a value indicating whether casing should be ignored.
    /// </summary>
    public bool IgnoreCase { get; set; } = false;
    
    /// <summary>
    /// Gets or sets the requested number of surrounding context lines. Defaults to 3.
    /// </summary>
    /// <remarks>
    /// The built-in algorithms and renderers currently retain every line and do not
    /// apply context trimming. This value is retained in the result options.
    /// </remarks>
    public int ContextLines { get; set; } = 3;
    
    /// <summary>
    /// Gets or sets the maximum line length eligible for character-level diffing. Defaults to 1000.
    /// </summary>
    /// <remarks>
    /// The registered <c>SimpleMyersDiffAlgorithm</c> also limits character matching
    /// to 1000 UTF-16 code units per nonempty side. Raising this value does not remove
    /// that fixed limit; ineligible changes remain separate added and deleted lines.
    /// </remarks>
    public int MaxCharacterDiffLength { get; set; } = 1000;
    
    /// <summary>
    /// Gets or sets the optional custom style configuration.
    /// </summary>
    public DiffHighlightStyle? Style { get; set; }
}
