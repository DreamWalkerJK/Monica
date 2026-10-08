using AwesomeAssertions;
using Monica.Profiling.RuntimeMetrics.Models;
using Monica.Profiling.UIMemoryAnalysis.Support;
using Xunit;

namespace Test.Monica.Profiling.UIMemoryAnalysis.Support;

/// <summary>Exercises source identity and numeric geometry only, without rendering or collection.</summary>
public sealed class MemoryHistoryPlotGeometryTests
{
    private static readonly DateTime Origin = new(2026, 10, 9, 1, 2, 3, DateTimeKind.Utc);

    [Fact]
    public void Create_WhenInputIsMissingOrEmpty_ShouldExposeNoInventedObservationsOrAxes()
    {
        var missing = MemoryHistoryPlotGeometry.Create(null, MemoryHistoryMetric.HeapMegabytes);
        var empty = MemoryHistoryPlotGeometry.Create([], MemoryHistoryMetric.HeapMegabytes);

        missing.IsMissingInput.Should().BeTrue();
        empty.IsMissingInput.Should().BeFalse();
        foreach (var geometry in new[] { missing, empty })
        {
            geometry.HasValues.Should().BeFalse();
            geometry.InputCount.Should().Be(0);
            geometry.Points.Should().BeEmpty();
            geometry.Segments.Should().BeEmpty();
            geometry.ValueTicks.Should().BeEmpty();
            geometry.TimeTicks.Should().BeEmpty();
        }
    }

    [Fact]
    public void Create_WhenEveryValueIsNonFinite_ShouldPreserveMissingnessInsteadOfPlottingZero()
    {
        var samples = new[] { Heap(0, double.NaN), Heap(1, double.PositiveInfinity), Heap(2, double.NegativeInfinity) };

        var geometry = MemoryHistoryPlotGeometry.Create(samples, MemoryHistoryMetric.HeapMegabytes);

        geometry.HasValues.Should().BeFalse();
        geometry.InputCount.Should().Be(3);
        geometry.OmittedValueCount.Should().Be(3);
        geometry.Points.Should().BeEmpty();
        geometry.Segments.Should().BeEmpty();
        geometry.ValueTicks.Should().BeEmpty();
        geometry.TimeTicks.Should().BeEmpty();
        double.IsNaN(samples[0].GcHeapSizeMB).Should().BeTrue();
        samples[1].GcHeapSizeMB.Should().Be(double.PositiveInfinity);
    }

    [Fact]
    public void Create_WhenOneActualZeroExists_ShouldRetainItsIdentityAndCenterItsTimestamp()
    {
        var sample = Heap(7, 0);

        var geometry = MemoryHistoryPlotGeometry.Create([sample], MemoryHistoryMetric.HeapMegabytes);

        geometry.HasValues.Should().BeTrue();
        geometry.OmittedValueCount.Should().Be(0);
        var point = geometry.Points.Should().ContainSingle().Subject;
        point.Sample.Should().BeSameAs(sample);
        point.RawValue.Should().Be(0);
        point.Value.Should().Be(0);
        point.X.Should().Be((MemoryHistoryPlotGeometry.Left + MemoryHistoryPlotGeometry.Right) / 2);
        point.Y.Should().Be(MemoryHistoryPlotGeometry.Bottom);
        geometry.TimeTicks.Should().ContainSingle().Subject.TimestampUtc.Should().Be(sample.Timestamp);
        geometry.ValueTicks.Select(tick => tick.Value).Should().OnlyContain(value => double.IsFinite(value));
    }

    [Fact]
    public void Create_WhenAllObservationsAreZero_ShouldKeepEachReportedSampleWithoutAddingData()
    {
        var samples = new[] { Heap(0, 0), Heap(2, 0), Heap(10, 0) };

        var geometry = MemoryHistoryPlotGeometry.Create(samples, MemoryHistoryMetric.HeapMegabytes);

        geometry.Points.Select(point => point.Sample).Should().Equal(samples);
        geometry.Points.Should().OnlyContain(point => point.Value == 0 && point.RawValue == 0 && point.Y == MemoryHistoryPlotGeometry.Bottom);
        geometry.Segments.Should().ContainSingle().Subject.Should().HaveCount(samples.Length);
        geometry.Minimum.Should().Be(0);
        geometry.Maximum.Should().BeGreaterThan(0, "an axis domain is distinct from its reported observations");
    }

    [Fact]
    public void Create_WhenTimestampIntervalsAreIrregular_ShouldScaleByElapsedTimeInsteadOfRowIndex()
    {
        var samples = new[] { Heap(0, 3), Heap(2, 6), Heap(10, 9) };

        var geometry = MemoryHistoryPlotGeometry.Create(samples, MemoryHistoryMetric.HeapMegabytes);

        geometry.Points[0].X.Should().Be(MemoryHistoryPlotGeometry.Left);
        geometry.Points[1].X.Should().BeApproximately(MemoryHistoryPlotGeometry.Left + .2 * (MemoryHistoryPlotGeometry.Right - MemoryHistoryPlotGeometry.Left), 1e-10);
        geometry.Points[2].X.Should().Be(MemoryHistoryPlotGeometry.Right);
        geometry.TimeTicks[1].TimestampUtc.Should().Be(Origin.AddSeconds(5));
    }

    [Fact]
    public void Create_WhenInputIsUnsortedWithDuplicateTimestamps_ShouldKeepStableDistinctSourceIdentities()
    {
        var last = Heap(10, 3);
        var first = Heap(0, 1);
        var coincident = Heap(0, 2);

        var geometry = MemoryHistoryPlotGeometry.Create([last, first, coincident], MemoryHistoryMetric.HeapMegabytes);

        geometry.Points.Select(point => point.Sample).Should().Equal(first, coincident, last);
        geometry.Points.Select(point => point.SourceIndex).Should().Equal(1, 2, 0);
        geometry.Points[0].X.Should().Be(geometry.Points[1].X);
        geometry.Points[0].Y.Should().NotBe(geometry.Points[1].Y);
    }

    [Fact]
    public void Create_WhenAnInteriorValueIsMissing_ShouldLeaveARealGapInTheLine()
    {
        var samples = new[] { Heap(0, 4), Heap(2, double.NaN), Heap(5, 8), Heap(10, 9) };

        var geometry = MemoryHistoryPlotGeometry.Create(samples, MemoryHistoryMetric.HeapMegabytes);

        geometry.InputCount.Should().Be(4);
        geometry.OmittedValueCount.Should().Be(1);
        geometry.Points.Select(point => point.SourceIndex).Should().Equal(0, 2, 3);
        geometry.Segments.Should().HaveCount(2);
        geometry.Segments[0].Select(point => point.Sample).Should().Equal(samples[0]);
        geometry.Segments[1].Select(point => point.Sample).Should().Equal(samples[2], samples[3]);
        geometry.Points[1].X.Should().Be((MemoryHistoryPlotGeometry.Left + MemoryHistoryPlotGeometry.Right) / 2);
    }

    [Fact]
    public void Create_WhenEndpointsHaveNoFiniteValue_ShouldKeepTheActualObservationWindow()
    {
        var samples = new[] { Heap(0, double.NaN), Heap(5, 8), Heap(10, double.PositiveInfinity) };

        var geometry = MemoryHistoryPlotGeometry.Create(samples, MemoryHistoryMetric.HeapMegabytes);

        geometry.Points.Should().ContainSingle().Subject.X.Should().Be((MemoryHistoryPlotGeometry.Left + MemoryHistoryPlotGeometry.Right) / 2);
        geometry.TimeTicks[0].TimestampUtc.Should().Be(samples[0].Timestamp);
        geometry.TimeTicks[^1].TimestampUtc.Should().Be(samples[^1].Timestamp);
        geometry.OmittedValueCount.Should().Be(2);
    }

    [Fact]
    public void Create_WhenMemoryBasesDiffer_ShouldPreserveDecimalMegabytesAndOriginalAllocationBytesPerSecond()
    {
        var sample = new RuntimeMetricsPoint { Timestamp = Origin, GcHeapSizeBytes = 3750000, AllocationRateBytesPerSecond = 1500000.25 };

        var heap = MemoryHistoryPlotGeometry.Create([sample], MemoryHistoryMetric.HeapMegabytes);
        var allocation = MemoryHistoryPlotGeometry.Create([sample], MemoryHistoryMetric.AllocationMebibytesPerSecond);

        heap.Unit.Should().Be("MB");
        heap.RawUnit.Should().Be("B");
        heap.Points.Single().Value.Should().Be(3.75);
        heap.Points.Single().RawValue.Should().Be(3750000);
        allocation.Unit.Should().Be("MiB/s");
        allocation.RawUnit.Should().Be("B/s");
        allocation.Points.Single().Value.Should().Be(sample.AllocationRateBps / 1048576d);
        allocation.Points.Single().RawValue.Should().Be(sample.AllocationRateBps);
        allocation.Points.Single().Sample.Should().BeSameAs(sample);
    }

    [Fact]
    public void Create_WhenSignedValuesSpanDoubleLimits_ShouldKeepFiniteGeometryWithoutClampingEvidence()
    {
        var samples = new[]
        {
            new RuntimeMetricsPoint { Timestamp = Origin, GcHeapSizeBytes = -double.MaxValue },
            new RuntimeMetricsPoint { Timestamp = Origin.AddSeconds(1), GcHeapSizeBytes = 0 },
            new RuntimeMetricsPoint { Timestamp = Origin.AddSeconds(2), GcHeapSizeBytes = double.MaxValue }
        };

        var geometry = MemoryHistoryPlotGeometry.Create(samples, MemoryHistoryMetric.HeapMegabytes);

        geometry.Minimum.Should().Be(-double.MaxValue / 1000000d);
        geometry.Maximum.Should().Be(double.MaxValue / 1000000d);
        geometry.Points.Select(point => point.RawValue).Should().Equal(-double.MaxValue, 0, double.MaxValue);
        geometry.Points.Select(point => point.Y).Should().Equal(MemoryHistoryPlotGeometry.Bottom,
            (MemoryHistoryPlotGeometry.Top + MemoryHistoryPlotGeometry.Bottom) / 2, MemoryHistoryPlotGeometry.Top);
        geometry.ValueTicks.Should().OnlyContain(tick => double.IsFinite(tick.Value) && double.IsFinite(tick.Y));
    }

    [Fact]
    public void Create_WhenUnitConversionUnderflowsANonzeroRawValue_ShouldNotInventAZeroObservation()
    {
        var sample = new RuntimeMetricsPoint { Timestamp = Origin, GcHeapSizeBytes = double.Epsilon };

        var geometry = MemoryHistoryPlotGeometry.Create([sample], MemoryHistoryMetric.HeapMegabytes);

        geometry.HasValues.Should().BeFalse();
        geometry.OmittedValueCount.Should().Be(1);
        geometry.Points.Should().BeEmpty();
        sample.GcHeapSizeBytes.Should().Be(double.Epsilon);
    }

    [Fact]
    public void Create_WhenTimestampKindsDiffer_ShouldNormalizeUtcCoordinatesWithoutMutatingSourceIdentity()
    {
        var unspecified = new RuntimeMetricsPoint { Timestamp = DateTime.SpecifyKind(Origin, DateTimeKind.Unspecified), GcHeapSizeBytes = 1000000 };
        var local = new RuntimeMetricsPoint { Timestamp = Origin.AddSeconds(2).ToLocalTime(), GcHeapSizeBytes = 2000000 };

        var geometry = MemoryHistoryPlotGeometry.Create([unspecified, local], MemoryHistoryMetric.HeapMegabytes);

        geometry.Points[0].TimestampUtc.Should().Be(Origin);
        geometry.Points[1].TimestampUtc.Should().Be(Origin.AddSeconds(2));
        geometry.Points.Should().OnlyContain(point => point.TimestampUtc.Kind == DateTimeKind.Utc);
        unspecified.Timestamp.Kind.Should().Be(DateTimeKind.Unspecified);
        local.Timestamp.Kind.Should().Be(DateTimeKind.Local);
    }

    [Fact]
    public void Create_WhenHistoryExceedsTypicalCapacity_ShouldRetainEveryActualObservation()
    {
        var samples = Enumerable.Range(0, 610).Select(index => Heap(index * index, index % 17)).ToArray();

        var geometry = MemoryHistoryPlotGeometry.Create(samples, MemoryHistoryMetric.HeapMegabytes);

        geometry.InputCount.Should().Be(samples.Length);
        geometry.OmittedValueCount.Should().Be(0);
        geometry.Points.Select(point => point.Sample).Should().Equal(samples);
        geometry.Points.Select(point => point.SourceIndex).Should().Equal(Enumerable.Range(0, samples.Length));
    }

    [Fact]
    public void Create_WhenMetricIsUnknown_ShouldRejectAnUnownedUnitContract()
    {
        Action act = () => MemoryHistoryPlotGeometry.Create([], (MemoryHistoryMetric)99);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("metric");
    }

    private static RuntimeMetricsPoint Heap(int seconds, double value) =>
        new() { Timestamp = Origin.AddSeconds(seconds), GcHeapSizeBytes = value * 1000000d };
}
