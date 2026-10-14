using Monica.OpenTelemetry.InProcessCollector.Models;

namespace Monica.OpenTelemetry.InProcessCollector.Services.Support;

/// <summary>
/// Calculates approximate histogram statistics from the bounded retained sample window.
/// </summary>
internal static class HistogramAggregator
{
    private const int BUCKET_COUNT = 10;

    public static HistogramSnapshot? Build(IReadOnlyList<MetricPointSnapshot> samples)
    {
        if (samples.Count == 0)
        {
            return null;
        }

        var values = samples
            .Select(static sample => sample.Value)
            .Where(double.IsFinite)
            .Order()
            .ToArray();

        if (values.Length == 0)
        {
            return null;
        }

        var min = values[0];
        var max = values[^1];

        return new HistogramSnapshot
        {
            Count = values.Length,
            NonFiniteSampleCount = samples.Count - values.Length,
            Min = min,
            Max = max,
            Average = Average(values),
            P50 = Percentile(values, 0.50),
            P95 = Percentile(values, 0.95),
            P99 = Percentile(values, 0.99),
            Buckets = BuildBuckets(values, min, max)
        };
    }

    private static double Average(double[] values)
    {
        // Scaling bounds every summand and avoids overflow from otherwise finite large values.
        var scale = Math.Max(Math.Abs(values[0]), Math.Abs(values[^1]));
        if (scale == 0) return 0;
        var normalized = values.Select(value => value / scale).Average();
        return Math.Clamp(normalized, -1, 1) * scale;
    }

    private static double Percentile(double[] orderedValues, double percentile)
    {
        if (orderedValues.Length == 1)
        {
            return orderedValues[0];
        }

        var index = (orderedValues.Length - 1) * percentile;
        var lowerIndex = (int)Math.Floor(index);
        var upperIndex = (int)Math.Ceiling(index);

        if (lowerIndex == upperIndex)
        {
            return orderedValues[lowerIndex];
        }

        var weight = index - lowerIndex;
        return Interpolate(orderedValues[lowerIndex], orderedValues[upperIndex], weight);
    }

    private static IReadOnlyList<HistogramBucketSnapshot> BuildBuckets(double[] orderedValues, double min, double max)
    {
        if (min.Equals(max))
        {
            return
            [
                new HistogramBucketSnapshot
                {
                    LowerBound = min,
                    UpperBound = max,
                    Count = orderedValues.Length
                }
            ];
        }

        // Weighted endpoints avoid overflowing (max - min), even across +/-Double.MaxValue.
        var upperBounds = new double[BUCKET_COUNT];
        for (var i = 0; i < BUCKET_COUNT; i++)
        {
            upperBounds[i] = i == BUCKET_COUNT - 1 ? max : Interpolate(min, max, (i + 1) / (double)BUCKET_COUNT);
        }
        var counts = new long[BUCKET_COUNT];

        foreach (var value in orderedValues)
        {
            var bucketIndex = 0;
            while (bucketIndex < BUCKET_COUNT - 1 && value > upperBounds[bucketIndex]) bucketIndex++;
            counts[bucketIndex]++;
        }

        var buckets = new List<HistogramBucketSnapshot>(BUCKET_COUNT);
        for (var i = 0; i < BUCKET_COUNT; i++)
        {
            buckets.Add(new HistogramBucketSnapshot
            {
                LowerBound = i == 0 ? min : upperBounds[i - 1],
                UpperBound = upperBounds[i],
                Count = counts[i]
            });
        }

        return buckets;
    }

    private static double Interpolate(double lower, double upper, double weight)
    {
        // The result is within finite endpoints; clamp any final floating-point rounding overflow.
        return Math.Clamp(lower * (1 - weight) + upper * weight, lower, upper);
    }
}
