namespace Monica.OpenTelemetry.InProcessCollector.Models;

/// <summary>
/// Approximate histogram statistics retained for one tag set in the in-process collector.
/// </summary>
public sealed class HistogramSnapshot
{
    /// <summary>
    /// Gets the number of finite retained samples used to calculate these window statistics.
    /// This is not a lifetime observation count or an OpenTelemetry SDK histogram aggregate.
    /// </summary>
    public long Count { get; init; }

    /// <summary>Gets the retained NaN/infinite count excluded from these finite-only statistics.</summary>
    public long NonFiniteSampleCount { get; init; }

    /// <summary>
    /// Gets the minimum retained value.
    /// </summary>
    public double Min { get; init; }

    /// <summary>
    /// Gets the maximum retained value.
    /// </summary>
    public double Max { get; init; }

    /// <summary>
    /// Gets the average retained value.
    /// </summary>
    public double Average { get; init; }

    /// <summary>
    /// Gets the approximate 50th percentile from retained samples.
    /// </summary>
    public double P50 { get; init; }

    /// <summary>
    /// Gets the approximate 95th percentile from retained samples.
    /// </summary>
    public double P95 { get; init; }

    /// <summary>
    /// Gets the approximate 99th percentile from retained samples.
    /// </summary>
    public double P99 { get; init; }

    /// <summary>
    /// Gets approximate bucket counts for retained samples.
    /// </summary>
    public IReadOnlyList<HistogramBucketSnapshot> Buckets { get; init; } = [];
}
