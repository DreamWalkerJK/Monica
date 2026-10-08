using Monica.OpenTelemetry.InProcessCollector.Models;

namespace Monica.OpenTelemetry.UI.UIOpenTelemetry.Support;

internal sealed record OpenTelemetrySeriesPlotPoint(
    MetricPointSnapshot Sample, int SourceIndex, DateTimeOffset TimestampUtc, double RawValue, double X, double Y);
internal sealed record OpenTelemetrySeriesPlotTick(double Value, double Y);
internal sealed record OpenTelemetrySeriesTimeTick(DateTimeOffset TimestampUtc, double X);

/// <summary>
/// Projects every finite raw observation using its actual UTC timestamp. Non-finite observations
/// retain their time-domain extent and break line segments; no units or counter rates are inferred.
/// </summary>
internal sealed class OpenTelemetrySeriesPlotGeometry
{
    public const double Width = 1000;
    public const double Height = 240;
    public const double Left = 24;
    public const double Right = Width - Left;
    public const double Top = 12;
    public const double Bottom = Height - Top;

    private OpenTelemetrySeriesPlotGeometry() { }

    public bool IsMissingInput { get; private init; }
    public int InputCount { get; private init; }
    public int OmittedValueCount { get; private init; }
    public bool HasValues => Points.Count > 0;
    public double Minimum { get; private init; }
    public double Maximum { get; private init; }
    public DateTimeOffset? StartTimeUtc { get; private init; }
    public DateTimeOffset? EndTimeUtc { get; private init; }
    public IReadOnlyList<OpenTelemetrySeriesPlotPoint> Points { get; private init; } = [];
    public IReadOnlyList<IReadOnlyList<OpenTelemetrySeriesPlotPoint>> Segments { get; private init; } = [];
    public IReadOnlyList<OpenTelemetrySeriesPlotTick> ValueTicks { get; private init; } = [];
    public IReadOnlyList<OpenTelemetrySeriesTimeTick> TimeTicks { get; private init; } = [];

    public static OpenTelemetrySeriesPlotGeometry Create(IReadOnlyList<MetricPointSnapshot>? samples)
    {
        if (samples is null || samples.Count == 0) return new() { IsMissingInput = samples is null };
        var readings = samples.Select((sample, index) => new Reading(sample, index, sample.TimestampUtc.ToUniversalTime()))
            .OrderBy(static reading => reading.TimestampUtc).ThenBy(static reading => reading.SourceIndex).ToArray();
        var start = readings[0].TimestampUtc;
        var end = readings[^1].TimestampUtc;
        var valid = readings.Where(static reading => double.IsFinite(reading.Sample.Value)).ToArray();
        if (valid.Length == 0) return new()
        {
            InputCount = samples.Count, OmittedValueCount = samples.Count, StartTimeUtc = start, EndTimeUtc = end
        };

        var minimum = Math.Min(0, valid.Min(static reading => reading.Sample.Value));
        var maximum = Math.Max(0, valid.Max(static reading => reading.Sample.Value));
        if (minimum == maximum) maximum = 1;
        var magnitude = Math.Max(Math.Abs(minimum), Math.Abs(maximum));
        var scaledMinimum = minimum / magnitude;
        var scaledMaximum = maximum / magnitude;
        double Y(double value) => Bottom - (value / magnitude - scaledMinimum) /
            (scaledMaximum - scaledMinimum) * (Bottom - Top);
        var spanTicks = end.Ticks - start.Ticks;
        double X(DateTimeOffset timestamp) => spanTicks == 0 ? (Left + Right) / 2 :
            Left + (double)(timestamp.Ticks - start.Ticks) / spanTicks * (Right - Left);

        var points = new List<OpenTelemetrySeriesPlotPoint>(valid.Length);
        var segments = new List<IReadOnlyList<OpenTelemetrySeriesPlotPoint>>();
        var segment = new List<OpenTelemetrySeriesPlotPoint>();
        foreach (var reading in readings)
        {
            if (!double.IsFinite(reading.Sample.Value))
            {
                if (segment.Count > 0) segments.Add(Array.AsReadOnly(segment.ToArray()));
                segment.Clear();
                continue;
            }
            var point = new OpenTelemetrySeriesPlotPoint(reading.Sample, reading.SourceIndex,
                reading.TimestampUtc, reading.Sample.Value, X(reading.TimestampUtc), Y(reading.Sample.Value));
            points.Add(point);
            segment.Add(point);
        }
        if (segment.Count > 0) segments.Add(Array.AsReadOnly(segment.ToArray()));
        var ticks = Enumerable.Range(0, 5).Select(index =>
        {
            var fraction = index / 4d;
            // Convex interpolation and a normalized axis avoid overflow for opposite signed binary64 extremes.
            return new OpenTelemetrySeriesPlotTick(maximum * (1 - fraction) + minimum * fraction,
                Top + fraction * (Bottom - Top));
        }).ToArray();
        var times = spanTicks == 0
            ? new[] { new OpenTelemetrySeriesTimeTick(start, X(start)) }
            : new[] { start, start.AddTicks(spanTicks / 2), end }
                .Select(timestamp => new OpenTelemetrySeriesTimeTick(timestamp, X(timestamp))).ToArray();
        return new()
        {
            InputCount = samples.Count,
            OmittedValueCount = samples.Count - valid.Length,
            Minimum = minimum,
            Maximum = maximum,
            StartTimeUtc = start,
            EndTimeUtc = end,
            Points = Array.AsReadOnly(points.ToArray()),
            Segments = Array.AsReadOnly(segments.ToArray()),
            ValueTicks = Array.AsReadOnly(ticks),
            TimeTicks = Array.AsReadOnly(times)
        };
    }

    private sealed record Reading(MetricPointSnapshot Sample, int SourceIndex, DateTimeOffset TimestampUtc);
}
