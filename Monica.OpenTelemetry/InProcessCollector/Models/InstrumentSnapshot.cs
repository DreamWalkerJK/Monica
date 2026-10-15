using System.Text.Json.Serialization;

namespace Monica.OpenTelemetry.InProcessCollector.Models;

/// <summary>
/// Snapshot of one published .NET metric instrument and its retained tag-set series.
/// </summary>
public sealed class InstrumentSnapshot
{
    /// <summary>
    /// Gets an opaque identity stable only for this published instrument in one collector lifetime.
    /// Null denotes a legacy snapshot without identity; this is not a durable or cross-host key.
    /// </summary>
    public string? InstrumentId { get; init; }

    /// <summary>
    /// Gets the meter name that owns the instrument.
    /// </summary>
    public required string MeterName { get; init; }

    /// <summary>Gets the version declared by the owning meter, when supplied.</summary>
    public string? MeterVersion { get; init; }

    /// <summary>
    /// Gets the instrument name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the best-effort instrument kind inferred from the instrument type.
    /// </summary>
    public required string Kind { get; init; }

    /// <summary>
    /// Gets the unit declared by the instrument.
    /// </summary>
    public string? Unit { get; init; }

    /// <summary>
    /// Gets the instrument description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets whether the instrument is observable.
    /// </summary>
    public bool IsObservable { get; init; }

    /// <summary>
    /// Gets whether the collector dropped additional tag sets after reaching the configured bound.
    /// </summary>
    public bool Overflow { get; init; }

    /// <summary>
    /// Gets the number of dropped samples caused by tag-set overflow.
    /// </summary>
    public long DroppedSamples { get; init; }

    /// <summary>
    /// Gets retained tag-set series for this instrument.
    /// </summary>
    public IReadOnlyList<TagSetSamples> TagSets { get; init; } = [];

    /// <summary>
    /// Gets the sum of the latest binary64 values across retained tag sets.
    /// This is not a meaningful physical aggregate for every gauge or histogram; non-finite values propagate.
    /// </summary>
    [JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals)]
    public double CurrentValue => TagSets.Sum(static tagSet => tagSet.CurrentValue);

    /// <summary>
    /// Gets the sum of the latest per-series deltas, without aligning their observation timestamps.
    /// </summary>
    [JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals)]
    public double Delta => TagSets.Sum(static tagSet => tagSet.Delta);
}
