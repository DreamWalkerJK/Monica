using System.Text.Json.Serialization;

namespace Monica.AI.Chat.Models;

/// <summary>Observed and estimated context measurements; estimates never masquerade as provider usage.</summary>
public sealed record ChatContextUsage
{
    /// <summary>Actual input tokens from the last normal provider request, if reported.</summary>
    public int? LastRequestInputTokens { get; init; }
    /// <summary>Estimated tokens in the currently retained next-request context.</summary>
    public int EstimatedNextInputTokens { get; init; }
    /// <summary>Effective context capacity, including the 256K fallback when no explicit value is available.</summary>
    public int? ContextWindow { get; init; } = ChatContextCapacity.DefaultTokens;
    /// <summary>Whether the capacity comes from conversation settings, model metadata, or the planning fallback.</summary>
    public ChatContextCapacitySource ContextWindowSource { get; init; }
    /// <summary>Reserved output budget excluded from the usable input window.</summary>
    public int? ReservedOutputTokens { get; init; }
    /// <summary>Estimated occupancy of the usable input window, unavailable when its capacity is unknown.</summary>
    [JsonIgnore]
    public double? OccupancyRatio => ContextWindow is > 0
        ? (double)EstimatedNextInputTokens / Math.Max(1, ContextWindow.Value - (ReservedOutputTokens ?? 0))
        : null;
    /// <summary>Whether binary attachments make the text-based estimate incomplete.</summary>
    public bool HasUnestimatedAttachments { get; init; }
}

/// <summary>Result of an atomic context compaction; the visible transcript remains intact.</summary>
public sealed record ChatCompactionResult
{
    /// <summary>Whether an automatic threshold triggered compaction.</summary>
    public bool Automatic { get; init; }
    /// <summary>Whether a smaller context was successfully committed.</summary>
    public bool Applied { get; init; }
    /// <summary>Estimated input tokens before compaction.</summary>
    public int BeforeTokens { get; init; }
    /// <summary>Estimated input tokens after successful compaction, otherwise unchanged.</summary>
    public int AfterTokens { get; init; }
    /// <summary>Number of original context messages summarized.</summary>
    public int CompactedMessageCount { get; init; }
    /// <summary>The committed summary, when compaction succeeded.</summary>
    public string? Summary { get; init; }
    /// <summary>Explanation when there was insufficient older context to summarize.</summary>
    public string? Reason { get; init; }
}
