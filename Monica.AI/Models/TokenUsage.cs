using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;

namespace Monica.AI.Models;

/// <summary>Provider-reported token counts; null means the provider did not report that measurement.</summary>
public sealed record TokenUsage
{
    /// <summary>Total input tokens for this request.</summary>
    public int? InputTokens { get; init; }
    /// <summary>Total output tokens, normally including reasoning.</summary>
    public int? OutputTokens { get; init; }
    /// <summary>Reasoning tokens included in the output count.</summary>
    public int? ReasoningTokens { get; init; }
    /// <summary>Input tokens served from cache.</summary>
    public int? CachedInputTokens { get; init; }
    /// <summary>Provider-reported total, when available.</summary>
    public int? TotalTokens { get; init; }
    /// <summary>Known total, calculated from input and output only when both were reported.</summary>
    [JsonIgnore]
    public int? EffectiveTotalTokens => TotalTokens ?? (InputTokens is { } input && OutputTokens is { } output
        ? (int)Math.Min((long)input + output, int.MaxValue) : null);
    /// <summary>Known cached fraction, unavailable when the necessary counts were omitted.</summary>
    [JsonIgnore]
    public double? CachedInputRatio => InputTokens is > 0 && CachedInputTokens is { } cached ? (double)cached / InputTokens.Value : null;

    /// <summary>Aggregates complete measurements; a missing field in any request keeps that aggregate unknown.</summary>
    public static TokenUsage Sum(IEnumerable<TokenUsage> usages)
    {
        var values = usages.ToArray();
        return new TokenUsage
        {
            InputTokens = SumKnown(values.Select(value => value.InputTokens)),
            OutputTokens = SumKnown(values.Select(value => value.OutputTokens)),
            ReasoningTokens = SumKnown(values.Select(value => value.ReasoningTokens)),
            CachedInputTokens = SumKnown(values.Select(value => value.CachedInputTokens)),
            TotalTokens = SumKnown(values.Select(value => value.EffectiveTotalTokens))
        };
    }

    internal static TokenUsage FromProvider(UsageDetails usage) => new()
    {
        InputTokens = ToCount(usage.InputTokenCount), OutputTokens = ToCount(usage.OutputTokenCount),
        ReasoningTokens = ToCount(usage.ReasoningTokenCount), CachedInputTokens = ToCount(usage.CachedInputTokenCount),
        TotalTokens = ToCount(usage.TotalTokenCount)
    };

    private static int? ToCount(long? value) => value is null or < 0 ? null : (int)Math.Min(value.Value, int.MaxValue);

    private static int? SumKnown(IEnumerable<int?> values)
    {
        long sum = 0;
        var any = false;
        foreach (var value in values)
        {
            if (value is null) return null;
            any = true;
            sum += value.Value;
        }
        return any ? (int)Math.Min(sum, int.MaxValue) : null;
    }
}
