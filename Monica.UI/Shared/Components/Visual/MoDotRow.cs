namespace Monica.UI.Shared.Components.Visual;

/// <summary>One row of a dot distribution: a key, the exact counted items, and the tint intensity between 0 and 1.</summary>
public sealed record MoDotRow(string Key, int Count, double Intensity = 1.0, MoVisualRole Role = MoVisualRole.Primary);
