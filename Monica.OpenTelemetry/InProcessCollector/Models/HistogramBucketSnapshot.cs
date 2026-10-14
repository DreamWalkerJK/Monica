namespace Monica.OpenTelemetry.InProcessCollector.Models;

/// <summary>
/// Approximate retained-sample count for one histogram bucket.
/// </summary>
public sealed class HistogramBucketSnapshot
{
    /// <summary>
    /// Gets the lower bound: inclusive for the first bucket and exclusive for subsequent buckets.
    /// </summary>
    public double LowerBound { get; init; }

    /// <summary>
    /// Gets the inclusive upper bound of the bucket.
    /// </summary>
    public double UpperBound { get; init; }

    /// <summary>
    /// Gets the number of retained samples in the bucket.
    /// </summary>
    public long Count { get; init; }
}
