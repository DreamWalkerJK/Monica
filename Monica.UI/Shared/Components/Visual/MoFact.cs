namespace Monica.UI.Shared.Components.Visual;

/// <summary>One contiguous fact in a shared fact grid: a short label, a light 24px value, and an optional unit.</summary>
public sealed record MoFact(string Label, string Value, string? Unit = null);
