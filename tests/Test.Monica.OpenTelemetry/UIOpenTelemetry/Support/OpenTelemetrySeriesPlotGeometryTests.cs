using AwesomeAssertions;
using Monica.OpenTelemetry.InProcessCollector.Models;
using Monica.OpenTelemetry.UI.UIOpenTelemetry.Support;
using Xunit;

namespace Test.Monica.OpenTelemetry.UIOpenTelemetry.Support;

public sealed class OpenTelemetrySeriesPlotGeometryTests
{
    [Fact]
    public void Create_WhenHistoryExceedsSixHundredIrregularObservations_ShouldPreserveEverySourcePoint()
    {
        const int count = 641;
        var samples = new MetricPointSnapshot[count];
        long elapsedTicks = 0;
        for (var index = 0; index < count; index++)
        {
            elapsedTicks += (index % 7 + 1) * 12345L;
            samples[index] = new(DateTimeOffset.UnixEpoch.AddTicks(elapsedTicks), index % 53 - 26d);
        }

        var geometry = OpenTelemetrySeriesPlotGeometry.Create(samples);

        geometry.InputCount.Should().Be(count);
        geometry.OmittedValueCount.Should().Be(0);
        geometry.Points.Should().HaveCount(count);
        geometry.Segments.Should().ContainSingle().Which.Should().HaveCount(count);
        geometry.StartTimeUtc.Should().Be(samples[0].TimestampUtc);
        geometry.EndTimeUtc.Should().Be(samples[^1].TimestampUtc);
        geometry.Points[0].X.Should().Be(OpenTelemetrySeriesPlotGeometry.Left);
        geometry.Points[^1].X.Should().Be(OpenTelemetrySeriesPlotGeometry.Right);
        for (var index = 0; index < count; index++)
        {
            var point = geometry.Points[index];
            point.SourceIndex.Should().Be(index);
            point.Sample.Should().BeSameAs(samples[index]);
            point.TimestampUtc.Should().Be(samples[index].TimestampUtc);
            BitConverter.DoubleToInt64Bits(point.RawValue).Should().Be(BitConverter.DoubleToInt64Bits(samples[index].Value));
        }
    }

    [Fact]
    public void Create_WhenObservationTimesAreIrregular_ShouldUseElapsedTimeInsteadOfSamplePosition()
    {
        MetricPointSnapshot[] samples =
        [
            new(DateTimeOffset.UnixEpoch, 0),
            new(DateTimeOffset.UnixEpoch.AddSeconds(1), 1),
            new(DateTimeOffset.UnixEpoch.AddSeconds(9), 9),
            new(DateTimeOffset.UnixEpoch.AddSeconds(10), 10)
        ];

        var geometry = OpenTelemetrySeriesPlotGeometry.Create(samples);

        geometry.Points.Select(static point => point.X).Should().HaveCount(4);
        geometry.Points[0].X.Should().Be(24);
        geometry.Points[1].X.Should().BeApproximately(119.2, 1e-9);
        geometry.Points[2].X.Should().BeApproximately(880.8, 1e-9);
        geometry.Points[3].X.Should().Be(976);
        geometry.TimeTicks.Select(static tick => tick.TimestampUtc).Should().Equal(
            DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddSeconds(5), DateTimeOffset.UnixEpoch.AddSeconds(10));
    }

    [Fact]
    public void Create_WhenNonFiniteValuesAppearBetweenOrOutsideFiniteSamples_ShouldKeepTimeExtentAndBreakSegments()
    {
        double[] values = [double.NaN, -5, 3, double.PositiveInfinity, 7, double.NegativeInfinity, 11, 13, double.NaN];
        var samples = values.Select((value, index) =>
            new MetricPointSnapshot(DateTimeOffset.UnixEpoch.AddSeconds(index), value)).ToArray();

        var geometry = OpenTelemetrySeriesPlotGeometry.Create(samples);

        geometry.InputCount.Should().Be(9);
        geometry.OmittedValueCount.Should().Be(4);
        geometry.HasValues.Should().BeTrue();
        geometry.StartTimeUtc.Should().Be(DateTimeOffset.UnixEpoch);
        geometry.EndTimeUtc.Should().Be(DateTimeOffset.UnixEpoch.AddSeconds(8));
        geometry.Points.Select(static point => point.SourceIndex).Should().Equal(1, 2, 4, 6, 7);
        geometry.Segments.Select(static segment => segment.Count).Should().Equal(2, 1, 2);
        geometry.Segments[0].Select(static point => point.SourceIndex).Should().Equal(1, 2);
        geometry.Segments[1].Select(static point => point.SourceIndex).Should().Equal(4);
        geometry.Segments[2].Select(static point => point.SourceIndex).Should().Equal(6, 7);
        geometry.Points[0].X.Should().BeGreaterThan(OpenTelemetrySeriesPlotGeometry.Left);
        geometry.Points[^1].X.Should().BeLessThan(OpenTelemetrySeriesPlotGeometry.Right);
        geometry.Points.Should().OnlyContain(static point => double.IsFinite(point.X) && double.IsFinite(point.Y));
        geometry.Points.Should().OnlyContain(point => ReferenceEquals(point.Sample, samples[point.SourceIndex]));
    }

    [Fact]
    public void Create_WhenAllValuesAreInvalid_ShouldRetainCoverageWithoutInventingDrawablePoints()
    {
        MetricPointSnapshot[] samples =
        [
            new(DateTimeOffset.UnixEpoch, double.NaN),
            new(DateTimeOffset.UnixEpoch.AddSeconds(3), double.PositiveInfinity),
            new(DateTimeOffset.UnixEpoch.AddSeconds(10), double.NegativeInfinity)
        ];

        var geometry = OpenTelemetrySeriesPlotGeometry.Create(samples);

        geometry.IsMissingInput.Should().BeFalse();
        geometry.InputCount.Should().Be(3);
        geometry.OmittedValueCount.Should().Be(3);
        geometry.HasValues.Should().BeFalse();
        geometry.Points.Should().BeEmpty();
        geometry.Segments.Should().BeEmpty();
        geometry.ValueTicks.Should().BeEmpty();
        geometry.StartTimeUtc.Should().Be(DateTimeOffset.UnixEpoch);
        geometry.EndTimeUtc.Should().Be(DateTimeOffset.UnixEpoch.AddSeconds(10));
    }

    [Fact]
    public void Create_WhenInputIsNullOrEmpty_ShouldDistinguishMissingInputFromAnEmptyHistory()
    {
        var missing = OpenTelemetrySeriesPlotGeometry.Create(null);
        var empty = OpenTelemetrySeriesPlotGeometry.Create([]);

        missing.IsMissingInput.Should().BeTrue();
        empty.IsMissingInput.Should().BeFalse();
        foreach (var geometry in new[] { missing, empty })
        {
            geometry.InputCount.Should().Be(0);
            geometry.OmittedValueCount.Should().Be(0);
            geometry.HasValues.Should().BeFalse();
            geometry.StartTimeUtc.Should().BeNull();
            geometry.EndTimeUtc.Should().BeNull();
            geometry.Points.Should().BeEmpty();
            geometry.Segments.Should().BeEmpty();
            geometry.TimeTicks.Should().BeEmpty();
            geometry.ValueTicks.Should().BeEmpty();
        }
    }

    [Fact]
    public void Create_WhenInputIsNotChronological_ShouldSortGeometryAndKeepOriginalIndicesSamplesAndOffsets()
    {
        var offsetTime = new DateTimeOffset(1970, 1, 1, 8, 0, 1, TimeSpan.FromHours(8));
        MetricPointSnapshot[] samples =
        [
            new(DateTimeOffset.UnixEpoch.AddSeconds(2), 20),
            new(DateTimeOffset.UnixEpoch, 0),
            new(offsetTime, 10)
        ];

        var geometry = OpenTelemetrySeriesPlotGeometry.Create(samples);

        geometry.Points.Select(static point => point.SourceIndex).Should().Equal(1, 2, 0);
        geometry.Points[0].Sample.Should().BeSameAs(samples[1]);
        geometry.Points[1].Sample.Should().BeSameAs(samples[2]);
        geometry.Points[2].Sample.Should().BeSameAs(samples[0]);
        geometry.Points[1].TimestampUtc.Should().Be(DateTimeOffset.UnixEpoch.AddSeconds(1));
        geometry.Points[1].TimestampUtc.Offset.Should().Be(TimeSpan.Zero);
        geometry.Points[1].Sample.TimestampUtc.Ticks.Should().Be(offsetTime.Ticks);
        geometry.Points[1].Sample.TimestampUtc.Offset.Should().Be(TimeSpan.FromHours(8));
        samples[0].Value.Should().Be(20);
        samples[1].Value.Should().Be(0);
        samples[2].TimestampUtc.Offset.Should().Be(TimeSpan.FromHours(8));
    }

    [Fact]
    public void Create_WhenTimesAndValuesRepeat_ShouldKeepEveryOriginalObservationIncludingInvalidGaps()
    {
        var repeated = new MetricPointSnapshot(DateTimeOffset.UnixEpoch, 5);
        MetricPointSnapshot[] samples = [repeated, repeated, new(DateTimeOffset.UnixEpoch, double.NaN), repeated];

        var geometry = OpenTelemetrySeriesPlotGeometry.Create(samples);

        geometry.InputCount.Should().Be(4);
        geometry.OmittedValueCount.Should().Be(1);
        geometry.Points.Should().HaveCount(3);
        geometry.Points.Select(static point => point.SourceIndex).Should().Equal(0, 1, 3);
        geometry.Points.Should().OnlyContain(point => ReferenceEquals(point.Sample, repeated) && point.X == 500);
        geometry.Segments.Select(static segment => segment.Count).Should().Equal(2, 1);
        geometry.TimeTicks.Should().ContainSingle().Which.X.Should().Be(500);
    }

    [Fact]
    public void Create_WhenFiniteValuesHaveOppositeBinary64Extremes_ShouldKeepCoordinatesAndAxisTicksFinite()
    {
        var negativeZero = BitConverter.Int64BitsToDouble(long.MinValue);
        double[] values = [-double.MaxValue, double.MaxValue, double.Epsilon, -double.Epsilon, negativeZero, 0];
        var samples = values.Select((value, index) =>
            new MetricPointSnapshot(DateTimeOffset.UnixEpoch.AddTicks(index), value)).ToArray();

        var geometry = OpenTelemetrySeriesPlotGeometry.Create(samples);

        geometry.Points.Should().HaveCount(values.Length);
        geometry.OmittedValueCount.Should().Be(0);
        geometry.Minimum.Should().Be(-double.MaxValue);
        geometry.Maximum.Should().Be(double.MaxValue);
        geometry.Points.Should().OnlyContain(static point =>
            double.IsFinite(point.X) && point.X >= OpenTelemetrySeriesPlotGeometry.Left && point.X <= OpenTelemetrySeriesPlotGeometry.Right &&
            double.IsFinite(point.Y) && point.Y >= OpenTelemetrySeriesPlotGeometry.Top && point.Y <= OpenTelemetrySeriesPlotGeometry.Bottom);
        geometry.ValueTicks.Should().OnlyContain(static tick => double.IsFinite(tick.Value) && double.IsFinite(tick.Y));
        geometry.ValueTicks[0].Value.Should().Be(double.MaxValue);
        geometry.ValueTicks[^1].Value.Should().Be(-double.MaxValue);
        geometry.Points[0].Y.Should().Be(OpenTelemetrySeriesPlotGeometry.Bottom);
        geometry.Points[1].Y.Should().Be(OpenTelemetrySeriesPlotGeometry.Top);
        for (var index = 0; index < values.Length; index++)
        {
            BitConverter.DoubleToInt64Bits(geometry.Points[index].RawValue).Should().Be(BitConverter.DoubleToInt64Bits(values[index]));
        }
    }

    [Fact]
    public void Create_WhenTheWholeRangeIsSubnormal_ShouldPreserveSignedValuesAndFiniteGeometry()
    {
        double[] values = [-2 * double.Epsilon, -double.Epsilon, 0, double.Epsilon, 2 * double.Epsilon];
        var samples = values.Select((value, index) =>
            new MetricPointSnapshot(DateTimeOffset.UnixEpoch.AddTicks(index), value)).ToArray();

        var geometry = OpenTelemetrySeriesPlotGeometry.Create(samples);

        geometry.Points.Should().HaveCount(5);
        geometry.OmittedValueCount.Should().Be(0);
        geometry.Points.Should().OnlyContain(static point => double.IsFinite(point.X) && double.IsFinite(point.Y));
        geometry.ValueTicks.Should().OnlyContain(static tick => double.IsFinite(tick.Value) && double.IsFinite(tick.Y));
        geometry.Points[0].Y.Should().Be(OpenTelemetrySeriesPlotGeometry.Bottom);
        geometry.Points[^1].Y.Should().Be(OpenTelemetrySeriesPlotGeometry.Top);
        geometry.Points[1].Y.Should().BeGreaterThan(geometry.Points[2].Y);
        geometry.Points[3].Y.Should().BeLessThan(geometry.Points[2].Y);
        geometry.Points.Select(static point => point.RawValue).Should().Equal(values);
    }

    [Fact]
    public void Create_WhenASingleObservedValueIsZero_ShouldRepresentAMeasuredZeroWithAnUsableAxis()
    {
        var sample = new MetricPointSnapshot(DateTimeOffset.UnixEpoch, 0);

        var geometry = OpenTelemetrySeriesPlotGeometry.Create([sample]);

        geometry.HasValues.Should().BeTrue();
        geometry.InputCount.Should().Be(1);
        geometry.OmittedValueCount.Should().Be(0);
        geometry.Minimum.Should().Be(0);
        geometry.Maximum.Should().BeGreaterThan(0);
        var point = geometry.Points.Should().ContainSingle().Which;
        point.Sample.Should().BeSameAs(sample);
        point.RawValue.Should().Be(0);
        point.X.Should().Be(500);
        double.IsFinite(point.Y).Should().BeTrue();
        geometry.TimeTicks.Should().ContainSingle();
    }
}
