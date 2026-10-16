using Monica.Profiling.RuntimeMetrics.Models;

namespace Monica.Profiling.UIMemoryAnalysis.Support;

/// <summary>Identifies one source-qualified memory-history measurement and its plot unit.</summary>
public enum MemoryHistoryMetric
{
    /// <summary>The collector's managed heap bytes divided by 1,000,000 for decimal MB.</summary>
    HeapMegabytes,
    /// <summary>The collector's reported bytes per second divided by 1048576 for binary MiB/s.</summary>
    AllocationMebibytesPerSecond
}

/// <summary>Retains one actual sample identity, raw value and finite display coordinate.</summary>
internal sealed record MemoryHistoryPlotPoint(
    RuntimeMetricsPoint Sample,
    int SourceIndex,
    DateTime TimestampUtc,
    double RawValue,
    double Value,
    double X,
    double Y);

internal sealed record MemoryHistoryPlotTick(double Value, double Y);
internal sealed record MemoryHistoryTimeTick(DateTime TimestampUtc, double X);

/// <summary>
/// Derives a timestamp-scaled plot without sampling, interpreting non-finite values as zero,
/// or joining line segments across missing measurements.
/// </summary>
internal sealed class MemoryHistoryPlotGeometry
{
    public const double Width = 1000;
    public const double Height = 240;
    public const double Left = 24;
    public const double Right = Width - Left;
    public const double Top = 12;
    public const double Bottom = Height - Top;

    private MemoryHistoryPlotGeometry() { }

    public string Unit { get; private init; } = string.Empty;
    public string RawUnit { get; private init; } = string.Empty;
    public bool IsMissingInput { get; private init; }
    public int InputCount { get; private init; }
    public int OmittedValueCount { get; private init; }
    public bool HasValues => Points.Count > 0;
    public double Minimum { get; private init; }
    public double Maximum { get; private init; }
    public IReadOnlyList<MemoryHistoryPlotPoint> Points { get; private init; } = [];
    public IReadOnlyList<IReadOnlyList<MemoryHistoryPlotPoint>> Segments { get; private init; } = [];
    public IReadOnlyList<MemoryHistoryPlotTick> ValueTicks { get; private init; } = [];
    public IReadOnlyList<MemoryHistoryTimeTick> TimeTicks { get; private init; } = [];

    public static MemoryHistoryPlotGeometry Create(
        IReadOnlyList<RuntimeMetricsPoint>? samples,
        MemoryHistoryMetric metric)
    {
        var (unit, rawUnit) = metric switch
        {
            MemoryHistoryMetric.HeapMegabytes => ("MB", "B"),
            MemoryHistoryMetric.AllocationMebibytesPerSecond => ("MiB/s", "B/s"),
            _ => throw new ArgumentOutOfRangeException(nameof(metric))
        };
        if (samples is null || samples.Count == 0)
            return new() { Unit = unit, RawUnit = rawUnit, IsMissingInput = samples is null };

        var readings = samples.Select((sample, index) =>
        {
            var raw = metric == MemoryHistoryMetric.HeapMegabytes ? sample.GcHeapSizeBytes : sample.AllocationRateBytesPerSecond;
            var value = metric == MemoryHistoryMetric.HeapMegabytes ? raw / 1000000d : raw / 1048576d;
            return new Reading(sample, index, AsUtc(sample.Timestamp), raw, value);
        }).OrderBy(reading => reading.TimestampUtc).ThenBy(reading => reading.SourceIndex).ToArray();
        var valid = readings.Where(reading => reading.CanPlot).ToArray();
        if (valid.Length == 0)
            return new() { Unit = unit, RawUnit = rawUnit, InputCount = samples.Count, OmittedValueCount = samples.Count };

        var minimum = Math.Min(0, valid.Min(reading => reading.Value));
        var maximum = Math.Max(0, valid.Max(reading => reading.Value));
        // A nonzero display domain makes an all-zero observation visible, without adding a sample.
        if (minimum == maximum) maximum = 1;
        var magnitude = Math.Max(Math.Abs(minimum), Math.Abs(maximum));
        var scaledMinimum = minimum / magnitude;
        var scaledMaximum = maximum / magnitude;
        double Y(double value) => Bottom - (value / magnitude - scaledMinimum) /
            (scaledMaximum - scaledMinimum) * (Bottom - Top);
        var start = readings[0].TimestampUtc;
        var end = readings[^1].TimestampUtc;
        var spanTicks = end.Ticks - start.Ticks;
        double X(DateTime timestamp) => spanTicks == 0 ? (Left + Right) / 2 :
            Left + (double)(timestamp.Ticks - start.Ticks) / spanTicks * (Right - Left);

        var points = new List<MemoryHistoryPlotPoint>(valid.Length);
        var segments = new List<IReadOnlyList<MemoryHistoryPlotPoint>>();
        var segment = new List<MemoryHistoryPlotPoint>();
        foreach (var reading in readings)
        {
            if (!reading.CanPlot)
            {
                if (segment.Count > 0) segments.Add(segment.ToArray());
                segment.Clear();
                continue;
            }
            var point = new MemoryHistoryPlotPoint(reading.Sample, reading.SourceIndex, reading.TimestampUtc,
                reading.RawValue, reading.Value, X(reading.TimestampUtc), Y(reading.Value));
            points.Add(point);
            segment.Add(point);
        }
        if (segment.Count > 0) segments.Add(segment.ToArray());
        var ticks = Enumerable.Range(0, 5).Select(index =>
        {
            var fraction = index / 4d;
            // A convex combination avoids overflowing a signed maximum-minus-minimum range.
            var value = maximum * (1 - fraction) + minimum * fraction;
            return new MemoryHistoryPlotTick(value, Top + fraction * (Bottom - Top));
        }).ToArray();
        var times = spanTicks == 0
            ? new[] { new MemoryHistoryTimeTick(start, X(start)) }
            : new[] { start, start.AddTicks(spanTicks / 2), end }
                .Select(timestamp => new MemoryHistoryTimeTick(timestamp, X(timestamp))).ToArray();
        return new()
        {
            Unit = unit,
            RawUnit = rawUnit,
            InputCount = samples.Count,
            OmittedValueCount = samples.Count - valid.Length,
            Minimum = minimum,
            Maximum = maximum,
            Points = points.ToArray(),
            Segments = segments.ToArray(),
            ValueTicks = ticks,
            TimeTicks = times
        };
    }

    private static DateTime AsUtc(DateTime timestamp) => timestamp.Kind == DateTimeKind.Unspecified
        ? DateTime.SpecifyKind(timestamp, DateTimeKind.Utc)
        : timestamp.ToUniversalTime();

    private sealed record Reading(RuntimeMetricsPoint Sample, int SourceIndex, DateTime TimestampUtc, double RawValue, double Value)
    {
        // A conversion that underflows a nonzero raw value must not forge a zero observation.
        public bool CanPlot => double.IsFinite(RawValue) && double.IsFinite(Value) && (Value != 0 || RawValue == 0);
    }
}
