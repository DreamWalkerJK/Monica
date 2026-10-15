using AwesomeAssertions;
using Monica.Profiling.RuntimeMetrics.Models;
using Monica.Profiling.UIRuntimeMetrics.Support;
using Xunit;

namespace Test.Monica.Profiling.UIRuntimeMetrics.Support;

/// <summary>Exercises observation identity and deterministic plot behavior without rendering or collection.</summary>
public sealed class RuntimeTrendPlotProjectionTests
{
    private static readonly DateTime Origin = new(2026, 10, 15, 1, 2, 3, DateTimeKind.Utc);

    [Fact]
    public void Create_WhenInputIsMissingOrEmpty_ShouldExposeNoInventedSamplesOrAxes()
    {
        var missing = RuntimeTrendPlotProjection.Create(null, RuntimeTrendMetric.Cpu);
        var empty = RuntimeTrendPlotProjection.Create([], RuntimeTrendMetric.Cpu);

        missing.IsMissingInput.Should().BeTrue();
        empty.IsMissingInput.Should().BeFalse();
        foreach (var projection in new[] { missing, empty })
        {
            projection.Samples.Should().BeEmpty();
            projection.Segments.Should().BeEmpty();
            projection.HitRegions.Should().BeEmpty();
            projection.YTicks.Should().BeEmpty();
            projection.TimeTicks.Should().BeEmpty();
            projection.StartUtc.Should().BeNull();
            projection.EndUtc.Should().BeNull();
            projection.InvalidValueCount.Should().Be(0);
        }
    }

    [Fact]
    public void Create_WhenFractionalTimestampIntervalsAreIrregular_ShouldUseTheExactElapsedTimeDomain()
    {
        var start = Origin.AddTicks(1234);
        var samples = new[]
        {
            Cpu(start, 10),
            Cpu(start.AddMilliseconds(25), 20),
            Cpu(start.AddMilliseconds(250), 30)
        };

        var projection = RuntimeTrendPlotProjection.Create(samples, RuntimeTrendMetric.Cpu);

        projection.StartUtc.Should().Be(start);
        projection.EndUtc.Should().Be(start.AddMilliseconds(250));
        projection.Samples.Select(sample => sample.Point).Should().Equal(samples);
        projection.Samples.Select(sample => sample.SourceIndex).Should().Equal(0, 1, 2);
        var firstX = projection.Samples[0].X;
        var lastX = projection.Samples[2].X;
        firstX.Should().BeLessThan(lastX);
        ((projection.Samples[1].X - firstX) / (lastX - firstX)).Should().BeApproximately(.1, 1e-12);
        projection.TimeTicks[0].UtcTimestamp.Should().Be(start);
        projection.TimeTicks[^1].UtcTimestamp.Should().Be(start.AddMilliseconds(250));
    }

    [Fact]
    public void Create_WhenInputIsUnsortedWithCoincidentTimes_ShouldPreserveSourceOrderAndReferenceIdentity()
    {
        var last = Cpu(Origin.AddSeconds(10), 30);
        var first = Cpu(Origin, 10);
        var coincident = Cpu(Origin, 20);
        var samples = new[] { last, first, coincident };

        var projection = RuntimeTrendPlotProjection.Create(samples, RuntimeTrendMetric.Cpu);

        projection.Samples.Select(sample => sample.Point).Should().Equal(samples);
        projection.Samples.Select(sample => sample.SourceIndex).Should().Equal(0, 1, 2);
        projection.StartUtc.Should().Be(first.Timestamp);
        projection.EndUtc.Should().Be(last.Timestamp);
        projection.Samples[0].X.Should().BeGreaterThan(projection.Samples[1].X);
        projection.Samples[1].X.Should().Be(projection.Samples[2].X);
        projection.Segments.Should().ContainSingle().Subject.Select(sample => sample.Point).Should().Equal(samples);
        samples[0].Should().BeSameAs(last);
    }

    [Fact]
    public void Create_WhenTimestampKindsDiffer_ShouldNormalizeUtcWithoutMutatingTheOriginalObservations()
    {
        var unspecified = Cpu(DateTime.SpecifyKind(Origin.AddTicks(1234), DateTimeKind.Unspecified), 10);
        var local = Cpu(Origin.AddSeconds(2).AddTicks(5678).ToLocalTime(), 20);

        var projection = RuntimeTrendPlotProjection.Create([unspecified, local], RuntimeTrendMetric.Cpu);

        projection.Samples[0].Point.Should().BeSameAs(unspecified);
        projection.Samples[1].Point.Should().BeSameAs(local);
        projection.Samples.Select(sample => sample.UtcTimestamp).Should().Equal(Origin.AddTicks(1234), Origin.AddSeconds(2).AddTicks(5678));
        projection.Samples.Should().OnlyContain(sample => sample.UtcTimestamp.Kind == DateTimeKind.Utc);
        projection.TimeTicks.Should().OnlyContain(tick => tick.UtcTimestamp.Kind == DateTimeKind.Utc);
        projection.StartUtc!.Value.Kind.Should().Be(DateTimeKind.Utc);
        projection.EndUtc!.Value.Kind.Should().Be(DateTimeKind.Utc);
        unspecified.Timestamp.Kind.Should().Be(DateTimeKind.Unspecified);
        local.Timestamp.Kind.Should().Be(DateTimeKind.Local);
    }

    [Fact]
    public void Create_WhenCpuExceedsOneHundredPercent_ShouldPreserveTheReportedValueAndExpandItsAxis()
    {
        var sample = Cpu(Origin, 345.75);

        var projection = RuntimeTrendPlotProjection.Create([sample], RuntimeTrendMetric.Cpu);

        projection.Unit.Should().Be("%");
        projection.RawUnit.Should().Be("%");
        var actual = projection.Samples.Should().ContainSingle().Subject;
        actual.Point.Should().BeSameAs(sample);
        actual.RawValue.Should().Be(345.75);
        actual.Value.Should().Be(345.75);
        actual.Y.Should().NotBeNull();
        projection.MaximumValue.Should().BeGreaterThanOrEqualTo(345.75);
        sample.CpuUsagePercent.Should().Be(345.75);
    }

    [Fact]
    public void Create_WhenWorkingSetUsesBytes_ShouldRetainRawBytesAndPlotDecimalMegabytes()
    {
        var sample = new RuntimeMetricsPoint { Timestamp = Origin, WorkingSetBytes = 3_750_000 };

        var projection = RuntimeTrendPlotProjection.Create([sample], RuntimeTrendMetric.WorkingSetMegabytes);

        projection.Unit.Should().Be("MB");
        projection.RawUnit.Should().Be("B");
        var actual = projection.Samples.Should().ContainSingle().Subject;
        actual.Point.Should().BeSameAs(sample);
        actual.RawValue.Should().Be(3_750_000);
        actual.Value.Should().Be(3.75);
        actual.Y.Should().NotBeNull();
        sample.WorkingSetBytes.Should().Be(3_750_000);
    }

    [Fact]
    public void Create_WhenOneActualZeroExists_ShouldCenterItsTimeWithoutAddingObservations()
    {
        var sample = Cpu(Origin.AddTicks(1234), 0);

        var projection = RuntimeTrendPlotProjection.Create([sample], RuntimeTrendMetric.Cpu);

        var actual = projection.Samples.Should().ContainSingle().Subject;
        actual.Point.Should().BeSameAs(sample);
        actual.SourceIndex.Should().Be(0);
        actual.RawValue.Should().Be(0);
        actual.Value.Should().Be(0);
        actual.X.Should().Be(.5);
        actual.Y.Should().NotBeNull();
        double.IsFinite(actual.Y!.Value).Should().BeTrue();
        projection.StartUtc.Should().Be(sample.Timestamp);
        projection.EndUtc.Should().Be(sample.Timestamp);
        projection.TimeTicks.Should().ContainSingle().Subject.UtcTimestamp.Should().Be(sample.Timestamp);
        projection.Segments.Should().ContainSingle().Subject.Should().ContainSingle();
        projection.HitRegions.Should().ContainSingle().Subject.Sample.Point.Should().BeSameAs(sample);
    }

    [Fact]
    public void Create_WhenEveryObservationIsZero_ShouldKeepEveryMeasuredZeroOnAFiniteAxis()
    {
        var samples = new[] { Cpu(Origin, 0), Cpu(Origin.AddSeconds(2), 0), Cpu(Origin.AddSeconds(10), 0) };

        var projection = RuntimeTrendPlotProjection.Create(samples, RuntimeTrendMetric.Cpu);

        projection.Samples.Select(sample => sample.Point).Should().Equal(samples);
        projection.Samples.Should().OnlyContain(sample => sample.RawValue == 0 && sample.Value == 0 && sample.Y.HasValue && double.IsFinite(sample.Y.Value));
        projection.Samples.Select(sample => sample.Y).Distinct().Should().ContainSingle();
        projection.Segments.Should().ContainSingle().Subject.Should().HaveCount(samples.Length);
        projection.MinimumValue.Should().Be(0);
        projection.MaximumValue.Should().BeGreaterThan(0);
        projection.InvalidValueCount.Should().Be(0);
        projection.YTicks.Should().OnlyContain(tick => double.IsFinite(tick.Value) && double.IsFinite(tick.Y));
    }

    [Fact]
    public void Create_WhenEveryTimestampIsTheSame_ShouldKeepEachOrdinalIdentityAtTheActualTime()
    {
        var samples = new[] { Cpu(Origin, 10), Cpu(Origin, 20), Cpu(Origin, 30) };

        var projection = RuntimeTrendPlotProjection.Create(samples, RuntimeTrendMetric.Cpu);

        projection.Samples.Select(sample => sample.Point).Should().Equal(samples);
        projection.Samples.Select(sample => sample.SourceIndex).Should().Equal(0, 1, 2);
        projection.Samples.Should().OnlyContain(sample => sample.X == .5 && sample.Y.HasValue && double.IsFinite(sample.Y.Value));
        projection.TimeTicks.Should().ContainSingle().Subject.UtcTimestamp.Should().Be(Origin);
        projection.HitRegions.Select(region => region.Sample.SourceIndex).Should().Equal(0, 1, 2);
        projection.HitRegions.Select(region => (region.StartX, region.EndX)).Distinct().Should().ContainSingle();
        projection.HitRegions.Should().OnlyContain(region => region.StartX < region.EndX);
    }

    [Fact]
    public void Create_WhenInteriorValuesAreNonFinite_ShouldRetainTheirEvidenceAndBreakSourceAdjacentSegments()
    {
        var samples = new[]
        {
            Cpu(Origin, 10), Cpu(Origin.AddSeconds(1), double.NaN), Cpu(Origin.AddSeconds(2), 20),
            Cpu(Origin.AddSeconds(3), double.PositiveInfinity), Cpu(Origin.AddSeconds(4), double.NegativeInfinity),
            Cpu(Origin.AddSeconds(5), 30), Cpu(Origin.AddSeconds(6), 40)
        };

        var projection = RuntimeTrendPlotProjection.Create(samples, RuntimeTrendMetric.Cpu);

        projection.Samples.Select(sample => sample.Point).Should().Equal(samples);
        projection.Samples.Select(sample => sample.SourceIndex).Should().Equal(0, 1, 2, 3, 4, 5, 6);
        projection.InvalidValueCount.Should().Be(3);
        double.IsNaN(projection.Samples[1].RawValue).Should().BeTrue();
        projection.Samples[3].RawValue.Should().Be(double.PositiveInfinity);
        projection.Samples[4].RawValue.Should().Be(double.NegativeInfinity);
        projection.Samples.Where(sample => !double.IsFinite(sample.RawValue)).Should().OnlyContain(sample => sample.Y == null);
        projection.Segments.Should().HaveCount(3);
        projection.Segments[0].Select(sample => sample.SourceIndex).Should().Equal(0);
        projection.Segments[1].Select(sample => sample.SourceIndex).Should().Equal(2);
        projection.Segments[2].Select(sample => sample.SourceIndex).Should().Equal(5, 6);
        projection.Segments.SelectMany(segment => segment).Should().OnlyContain(sample => sample.Y.HasValue && double.IsFinite(sample.Y.Value));
        double.IsNaN(samples[1].CpuUsagePercent).Should().BeTrue();
        samples[3].CpuUsagePercent.Should().Be(double.PositiveInfinity);
        samples[4].CpuUsagePercent.Should().Be(double.NegativeInfinity);
    }

    [Fact]
    public void Create_WhenEveryValueIsNonFinite_ShouldPreserveSamplesWithoutInventingAZeroLine()
    {
        var samples = new[]
        {
            Cpu(Origin, double.NaN), Cpu(Origin.AddSeconds(1), double.PositiveInfinity), Cpu(Origin.AddSeconds(2), double.NegativeInfinity)
        };

        var projection = RuntimeTrendPlotProjection.Create(samples, RuntimeTrendMetric.Cpu);

        projection.Samples.Select(sample => sample.Point).Should().Equal(samples);
        projection.Samples.Should().OnlyContain(sample => sample.Y == null && double.IsFinite(sample.X));
        projection.InvalidValueCount.Should().Be(samples.Length);
        projection.Segments.Should().BeEmpty();
        projection.StartUtc.Should().Be(samples[0].Timestamp);
        projection.EndUtc.Should().Be(samples[^1].Timestamp);
    }

    [Fact]
    public void Create_WhenEndpointValuesAreMissing_ShouldKeepTheFullObservationWindow()
    {
        var samples = new[]
        {
            Cpu(Origin.AddTicks(1234), double.NaN), Cpu(Origin.AddSeconds(5).AddTicks(1234), 20),
            Cpu(Origin.AddSeconds(10).AddTicks(1234), double.PositiveInfinity)
        };

        var projection = RuntimeTrendPlotProjection.Create(samples, RuntimeTrendMetric.Cpu);

        projection.StartUtc.Should().Be(samples[0].Timestamp);
        projection.EndUtc.Should().Be(samples[^1].Timestamp);
        projection.Samples[1].X.Should().Be(.5);
        projection.TimeTicks[0].UtcTimestamp.Should().Be(samples[0].Timestamp);
        projection.TimeTicks[^1].UtcTimestamp.Should().Be(samples[^1].Timestamp);
        projection.InvalidValueCount.Should().Be(2);
        projection.Segments.Should().ContainSingle().Subject.Should().ContainSingle().Subject.Point.Should().BeSameAs(samples[1]);
    }

    [Fact]
    public void Create_WhenSignedCpuValuesSpanDoubleLimits_ShouldKeepFiniteGeometryWithoutClampingEvidence()
    {
        var samples = new[] { Cpu(Origin, -double.MaxValue), Cpu(Origin.AddSeconds(1), 0), Cpu(Origin.AddSeconds(2), double.MaxValue) };

        var projection = RuntimeTrendPlotProjection.Create(samples, RuntimeTrendMetric.Cpu);

        projection.MinimumValue.Should().Be(-double.MaxValue);
        projection.MaximumValue.Should().Be(double.MaxValue);
        projection.Samples.Select(sample => sample.RawValue).Should().Equal(-double.MaxValue, 0, double.MaxValue);
        projection.Samples.Select(sample => sample.Value).Should().Equal(-double.MaxValue, 0, double.MaxValue);
        projection.Samples.Should().OnlyContain(sample => sample.Y.HasValue && double.IsFinite(sample.Y.Value) && sample.Y.Value >= 0 && sample.Y.Value <= 1);
        projection.Samples[0].Y!.Value.Should().BeGreaterThan(projection.Samples[1].Y!.Value);
        projection.Samples[1].Y!.Value.Should().BeGreaterThan(projection.Samples[2].Y!.Value);
        projection.YTicks.Should().NotBeEmpty().And.OnlyContain(tick => double.IsFinite(tick.Value) && double.IsFinite(tick.Y));
        projection.InvalidValueCount.Should().Be(0);
    }

    [Fact]
    public void Create_WhenWorkingSetConversionUnderflows_ShouldRetainRawEvidenceAsAnUnplottableObservation()
    {
        var sample = new RuntimeMetricsPoint { Timestamp = Origin, WorkingSetBytes = double.Epsilon };

        var projection = RuntimeTrendPlotProjection.Create([sample], RuntimeTrendMetric.WorkingSetMegabytes);

        var actual = projection.Samples.Should().ContainSingle().Subject;
        actual.Point.Should().BeSameAs(sample);
        actual.RawValue.Should().Be(double.Epsilon);
        actual.Y.Should().BeNull();
        projection.InvalidValueCount.Should().Be(1);
        projection.Segments.Should().BeEmpty();
        sample.WorkingSetBytes.Should().Be(double.Epsilon);
    }

    [Fact]
    public void Create_WhenObservationWindowIsShorterThanASecond_ShouldProduceDistinctPositiveTimeTicksAndExactEndpoints()
    {
        var start = Origin.AddTicks(1234);
        var end = start.AddTicks(31);

        var projection = RuntimeTrendPlotProjection.Create([Cpu(start, 10), Cpu(end, 20)], RuntimeTrendMetric.Cpu);

        projection.TimeTicks.Should().HaveCountGreaterThanOrEqualTo(2);
        projection.TimeTicks[0].UtcTimestamp.Should().Be(start);
        projection.TimeTicks[^1].UtcTimestamp.Should().Be(end);
        projection.TimeTicks[0].X.Should().Be(projection.Samples[0].X);
        projection.TimeTicks[^1].X.Should().Be(projection.Samples[^1].X);
        for (var index = 1; index < projection.TimeTicks.Count; index++)
        {
            projection.TimeTicks[index].UtcTimestamp.Should().BeAfter(projection.TimeTicks[index - 1].UtcTimestamp);
            projection.TimeTicks[index].X.Should().BeGreaterThan(projection.TimeTicks[index - 1].X);
        }
    }

    [Fact]
    public void Create_WhenHoverIntervalsAreIrregular_ShouldUseTemporalMidpointsAndRetainCoincidentIdentities()
    {
        var samples = new[] { Cpu(Origin, 10), Cpu(Origin.AddSeconds(2), 20), Cpu(Origin.AddSeconds(2), 30), Cpu(Origin.AddSeconds(10), 40) };

        var projection = RuntimeTrendPlotProjection.Create(samples, RuntimeTrendMetric.Cpu);

        projection.HitRegions.Select(region => region.Sample.Point).Should().Equal(samples);
        projection.HitRegions.Select(region => region.Sample.SourceIndex).Should().Equal(0, 1, 2, 3);
        projection.HitRegions[0].StartX.Should().Be(projection.Samples[0].X);
        projection.HitRegions[0].EndX.Should().BeApproximately(.1, 1e-12);
        projection.HitRegions[1].StartX.Should().Be(projection.HitRegions[0].EndX);
        projection.HitRegions[1].EndX.Should().BeApproximately(.6, 1e-12);
        projection.HitRegions[2].StartX.Should().Be(projection.HitRegions[1].StartX);
        projection.HitRegions[2].EndX.Should().Be(projection.HitRegions[1].EndX);
        projection.HitRegions[3].StartX.Should().Be(projection.HitRegions[1].EndX);
        projection.HitRegions[3].EndX.Should().Be(projection.Samples[^1].X);
        projection.HitRegions.Should().OnlyContain(region => double.IsFinite(region.StartX) && double.IsFinite(region.EndX) && region.StartX < region.EndX);
    }

    [Fact]
    public void Create_WhenMetricIsUnknown_ShouldRejectAnUnownedMeasurementContract()
    {
        Action act = () => RuntimeTrendPlotProjection.Create([], (RuntimeTrendMetric)99);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("metric");
    }

    private static RuntimeMetricsPoint Cpu(DateTime timestamp, double value) =>
        new() { Timestamp = timestamp, CpuUsagePercent = value };
}
