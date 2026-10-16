namespace Monica.UI.Shared.Components.Visual;

/// <summary>One node on a critical-path spine: a name, an optional sublabel, an optional right-aligned detail, and a role color.</summary>
public sealed record MoSpineItem(
    string Name,
    string? Sublabel = null,
    string? Detail = null,
    MoVisualRole Role = MoVisualRole.Neutral);
