using Monica.Profiling.RuntimeMetrics.Models;

namespace Monica.Profiling.UIRuntimeMetrics.Support;

/// <summary>Identifies a Runtime plot's measurement, unit and palette role.</summary>
public enum RuntimeTrendMetric
{
    /// <summary>The collector's unscaled process CPU percentage, using the Primary palette role.</summary>
    Cpu,

    /// <summary>The collector's working-set bytes divided by 1,000,000, using the Info palette role.</summary>
    WorkingSetMegabytes
}

/// <summary>Retains an original observation and its unrounded, timestamp-scaled coordinates.</summary>
internal sealed record RuntimeTrendPlotSample(
    RuntimeMetricsPoint Point,
    int SourceIndex,
    DateTime UtcTimestamp,
    double RawValue,
    double Value,
    double X,
    double? Y)
{
    public bool IsFinite => Y.HasValue;
}

internal sealed record RuntimeTrendPlotValueTick(double Value, double Y);
internal sealed record RuntimeTrendPlotTimeTick(DateTime UtcTimestamp, double X);
internal sealed record RuntimeTrendPlotHitRegion(RuntimeTrendPlotSample Sample, double StartX, double EndX);

/// <summary>
/// Projects all original observations in source order, using elapsed UTC time and a zero-inclusive
/// value scale. Coordinates are fractions; missing values remain explicit gaps rather than zeroes.
/// </summary>
internal sealed class RuntimeTrendPlotProjection
{
    private RuntimeTrendPlotProjection() { }

    public RuntimeTrendMetric Metric { get; private init; }
    public string Unit { get; private init; } = string.Empty;
    public string RawUnit { get; private init; } = string.Empty;
    public bool IsMissingInput { get; private init; }
    public DateTime? StartUtc { get; private init; }
    public DateTime? EndUtc { get; private init; }
    public double MinimumValue { get; private init; }
    public double MaximumValue { get; private init; } = 1;
    public int InvalidValueCount { get; private init; }
    public IReadOnlyList<RuntimeTrendPlotSample> Samples { get; private init; } = [];
    public IReadOnlyList<IReadOnlyList<RuntimeTrendPlotSample>> Segments { get; private init; } = [];
    public IReadOnlyList<RuntimeTrendPlotHitRegion> HitRegions { get; private init; } = [];
    public IReadOnlyList<RuntimeTrendPlotValueTick> YTicks { get; private init; } = [];
    public IReadOnlyList<RuntimeTrendPlotTimeTick> TimeTicks { get; private init; } = [];

    public static RuntimeTrendPlotProjection Create(
        IReadOnlyList<RuntimeMetricsPoint>? samples,
        RuntimeTrendMetric metric)
    {
        var unit = UnitFor(metric);
        var rawUnit = metric == RuntimeTrendMetric.Cpu ? "%" : "B";
        if (samples is null || samples.Count == 0)
        {
            return new() { Metric = metric, Unit = unit, RawUnit = rawUnit, IsMissingInput = samples is null };
        }

        var readings = samples.Select((point, index) =>
        {
            var raw = metric == RuntimeTrendMetric.Cpu ? point.CpuUsagePercent : point.WorkingSetBytes;
            var value = metric == RuntimeTrendMetric.Cpu ? raw : raw / 1_000_000d;
            return new Reading(point, index, RuntimeMetricsDisplayFormatter.AsUtc(point.Timestamp), raw, value);
        }).ToArray();
        var finite = readings.Where(reading => reading.CanPlot).ToArray();
        var minimum = finite.Length == 0 ? 0 : Math.Min(0, finite.Min(reading => reading.Value));
        var maximum = finite.Length == 0 ? 1 : Math.Max(0, finite.Max(reading => reading.Value));
        // A positive all-zero display scale does not add or change any observation.
        if (minimum == maximum) maximum = 1;
        var magnitude = Math.Max(Math.Abs(minimum), Math.Abs(maximum));
        var scaledMinimum = minimum / magnitude;
        var scaledMaximum = maximum / magnitude;
        double Y(double value) => 1 - (value / magnitude - scaledMinimum) / (scaledMaximum - scaledMinimum);

        var start = readings.Min(reading => reading.UtcTimestamp);
        var end = readings.Max(reading => reading.UtcTimestamp);
        var spanTicks = end.Ticks - start.Ticks;
        double X(DateTime timestamp) => spanTicks == 0 ? .5 : (double)(timestamp.Ticks - start.Ticks) / spanTicks;

        var projected = readings.Select(reading => new RuntimeTrendPlotSample(
            reading.Point, reading.SourceIndex, reading.UtcTimestamp, reading.RawValue, reading.Value,
            X(reading.UtcTimestamp), reading.CanPlot ? Y(reading.Value) : null)).ToArray();
        var segments = new List<IReadOnlyList<RuntimeTrendPlotSample>>();
        var segment = new List<RuntimeTrendPlotSample>();
        foreach (var sample in projected)
        {
            if (sample.IsFinite)
            {
                segment.Add(sample);
            }
            else if (segment.Count > 0)
            {
                segments.Add(segment.ToArray());
                segment.Clear();
            }
        }
        if (segment.Count > 0) segments.Add(segment.ToArray());

        var valueTicks = Enumerable.Range(0, 5).Select(index =>
        {
            var fraction = index / 4d;
            // A scaled convex combination keeps even an extreme signed domain finite.
            var value = index == 0 ? maximum : index == 4 ? minimum
                : magnitude * (scaledMaximum * (1 - fraction) + scaledMinimum * fraction);
            return new RuntimeTrendPlotValueTick(value, Y(value));
        }).DistinctBy(tick => tick.Value).ToArray();
        var timeTicks = spanTicks == 0
            ? new[] { new RuntimeTrendPlotTimeTick(start, .5) }
            : Enumerable.Range(0, 5).Select(index =>
            {
                // Divide before multiplying: the DateTime domain can span more than int64 / 4 ticks.
                var offset = spanTicks / 4 * index + spanTicks % 4 * index / 4;
                var timestamp = start.AddTicks(offset);
                return new RuntimeTrendPlotTimeTick(timestamp, X(timestamp));
            }).DistinctBy(tick => tick.UtcTimestamp).ToArray();

        return new()
        {
            Metric = metric,
            Unit = unit,
            RawUnit = rawUnit,
            StartUtc = start,
            EndUtc = end,
            MinimumValue = minimum,
            MaximumValue = maximum,
            InvalidValueCount = readings.Length - finite.Length,
            Samples = projected,
            Segments = segments.ToArray(),
            HitRegions = CreateHitRegions(projected),
            YTicks = valueTicks,
            TimeTicks = timeTicks
        };
    }

    public static string UnitFor(RuntimeTrendMetric metric) => metric switch
    {
        RuntimeTrendMetric.Cpu => "%",
        RuntimeTrendMetric.WorkingSetMegabytes => "MB",
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, "Unknown Runtime trend metric.")
    };

    private static RuntimeTrendPlotHitRegion[] CreateHitRegions(RuntimeTrendPlotSample[] samples)
    {
        var groups = samples.GroupBy(sample => sample.X).OrderBy(group => group.Key).ToArray();
        var regions = new RuntimeTrendPlotHitRegion[samples.Length];
        for (var index = 0; index < groups.Length; index++)
        {
            var start = index == 0 ? 0 : (groups[index - 1].Key + groups[index].Key) / 2;
            var end = index == groups.Length - 1 ? 1 : (groups[index].Key + groups[index + 1].Key) / 2;
            foreach (var sample in groups[index])
            {
                regions[sample.SourceIndex] = new(sample, start, end);
            }
        }
        return regions;
    }

    private sealed record Reading(RuntimeMetricsPoint Point, int SourceIndex, DateTime UtcTimestamp, double RawValue, double Value)
    {
        // Decimal conversion underflow cannot truthfully become a measured zero.
        public bool CanPlot => double.IsFinite(RawValue) && double.IsFinite(Value) && (Value != 0 || RawValue == 0);
    }
}
