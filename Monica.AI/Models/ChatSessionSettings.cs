namespace Monica.AI.Models;

/// <summary>Immutable choices applied when the next chat turn begins.</summary>
/// <param name="ProviderId">Host-wide provider identifier.</param>
/// <param name="ModelName">Model name, or null to use the provider default.</param>
/// <param name="SystemPrompt">Session-specific instructions, or null for host/provider defaults.</param>
/// <param name="ReasoningLevel">Configured reasoning level ID, or null to use the model default.</param>
public sealed record ChatSessionSettings(
    string ProviderId,
    string? ModelName = null,
    string? SystemPrompt = null,
    string? ReasoningLevel = null)
{
    /// <summary>Optional context-window override; null uses model metadata or the 256K planning default when metadata is absent.</summary>
    public int? ContextWindow { get; init; }
    /// <summary>Optional requested output-token limit, also reserved when estimating available input capacity.
    /// Must not exceed the selected model's known output maximum. Null leaves the provider default unchanged
    /// and does not create an explicit Monica output reservation.</summary>
    public int? MaxOutputTokens { get; init; }
    /// <summary>Optional sampling temperature supported by the selected provider.</summary>
    public float? Temperature { get; init; }
    /// <summary>Whether to summarize older context before a turn approaches the model limit. Defaults to true.</summary>
    public bool AutomaticCompaction { get; init; } = true;
    /// <summary>Input-window occupancy that triggers automatic compaction. Defaults to 0.8; must lie between zero and one.</summary>
    public double CompactionThreshold { get; init; } = .8;
    /// <summary>Minimum complete recent turns retained verbatim during compaction. Defaults to four.</summary>
    public int RetainedTurns { get; init; } = 4;

    internal void Validate(LLMModelInfo? model = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ProviderId);
        if (ContextWindow is <= 0 || MaxOutputTokens is <= 0
            || ContextWindow.HasValue && MaxOutputTokens >= ContextWindow)
        {
            throw new ArgumentException("Context and output limits must be positive, with output smaller than context.");
        }

        if (MaxOutputTokens is { } requested && model?.MaxOutputTokens is { } maximum && requested > maximum)
            throw new ArgumentOutOfRangeException(nameof(MaxOutputTokens), requested,
                $"The requested output budget exceeds the maximum of {maximum} tokens supported by model '{model.ModelName}'.");

        if (!double.IsFinite(CompactionThreshold) || CompactionThreshold is <= 0 or >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(CompactionThreshold));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(RetainedTurns, 1);
    }
}
