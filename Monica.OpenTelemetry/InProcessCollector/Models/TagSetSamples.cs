using System.Text.Json.Serialization;
using Monica.OpenTelemetry.InProcessCollector.Models.Support;

namespace Monica.OpenTelemetry.InProcessCollector.Models;

/// <summary>
/// Retained samples for one metric instrument and one exact tag set.
/// </summary>
public sealed class TagSetSamples
{
    /// <summary>
    /// Gets the collision-safe stable identifier for the collector's normalized string-valued tag set.
    /// It does not preserve the original tag object's CLR type.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets the ordinal normalized string tags associated with this series.
    /// </summary>
    /// <remarks>
    /// JSON keeps dictionary keys verbatim rather than applying the host's dictionary-key naming policy,
    /// so case-distinct keys remain distinct. String encoding still follows the serializer's Unicode behavior.
    /// </remarks>
    [JsonConverter(typeof(OrdinalTagDictionaryJsonConverter))]
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets the latest retained binary64 value: a running total for direct counters, otherwise a raw measurement.
    /// Raw NaN and infinities are preserved instead of replaced with zero.
    /// </summary>
    [JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals)]
    public double CurrentValue { get; init; }

    /// <summary>
    /// Gets the delta between the two latest retained samples, or zero when fewer than two samples exist.
    /// </summary>
    [JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals)]
    public double Delta { get; init; }

    /// <summary>
    /// Gets the UTC timestamp of the latest retained sample.
    /// </summary>
    public DateTimeOffset LastUpdatedUtc { get; init; }

    /// <summary>
    /// Gets retained samples for this series.
    /// </summary>
    public IReadOnlyList<MetricPointSnapshot> Samples { get; init; } = [];

    /// <summary>Gets the number of finite values among the retained raw samples.</summary>
    public int FiniteSampleCount => Samples.Count(static sample => double.IsFinite(sample.Value));

    /// <summary>Gets the retained NaN/infinite count excluded from finite histogram statistics.</summary>
    public int NonFiniteSampleCount => Samples.Count - FiniteSampleCount;

    /// <summary>
    /// Gets finite-only retained-window statistics for a histogram, or null without a finite retained value.
    /// </summary>
    public HistogramSnapshot? Histogram { get; init; }
}
