using AwesomeAssertions;
using Monica.OpenTelemetry.InProcessCollector.Models;
using Monica.OpenTelemetry.InProcessCollector.Services.Support;
using Xunit;

namespace Test.Monica.OpenTelemetry.InProcessCollector.Services.Support;

public sealed class HistogramAggregatorTests
{
    [Fact]
    public void Build_WhenNoFiniteValuesExist_ShouldNotInventADistribution()
    {
        HistogramAggregator.Build([]).Should().BeNull();
        HistogramAggregator.Build(Samples(double.NaN, double.PositiveInfinity, double.NegativeInfinity)).Should().BeNull();
    }

    [Fact]
    public void Build_WhenRawWindowIncludesNonfiniteValues_ShouldCountOnlyFiniteObservationsWithoutChangingTheInput()
    {
        var input = Samples(double.NaN, 2, double.PositiveInfinity, 4, double.NegativeInfinity);

        var result = HistogramAggregator.Build(input)!;

        result.Count.Should().Be(2);
        result.NonFiniteSampleCount.Should().Be(3);
        result.Min.Should().Be(2);
        result.Max.Should().Be(4);
        result.Average.Should().Be(3);
        result.P50.Should().Be(3);
        result.P95.Should().BeApproximately(3.9, 0.000001);
        result.P99.Should().BeApproximately(3.98, 0.000001);
        result.Buckets.Sum(bucket => bucket.Count).Should().Be(2);
        input.Should().HaveCount(5);
        double.IsNaN(input[0].Value).Should().BeTrue();
        input[2].Value.Should().Be(double.PositiveInfinity);
        input[4].Value.Should().Be(double.NegativeInfinity);
    }

    [Fact]
    public void Build_WhenAValueEqualsAnInternalUpperEdge_ShouldCountItInTheInclusiveUpperBucket()
    {
        var result = HistogramAggregator.Build(Samples(0, 1, 10))!;

        result.Buckets[0].LowerBound.Should().Be(0);
        result.Buckets[0].UpperBound.Should().Be(1);
        result.Buckets[0].Count.Should().Be(2);
        result.Buckets.Skip(1).Sum(bucket => bucket.Count).Should().Be(1);
        result.Buckets[^1].UpperBound.Should().Be(10);
        result.Buckets.Sum(bucket => bucket.Count).Should().Be(result.Count);
    }

    [Theory]
    [InlineData(double.MaxValue)]
    [InlineData(-double.MaxValue)]
    [InlineData(double.Epsilon)]
    [InlineData(0)]
    public void Build_WhenFiniteValuesAreConstant_ShouldKeepFiniteMeanAndOneExactBucket(double value)
    {
        var result = HistogramAggregator.Build(Samples(value, value, value))!;

        result.Count.Should().Be(3);
        result.NonFiniteSampleCount.Should().Be(0);
        result.Min.Should().Be(value);
        result.Max.Should().Be(value);
        result.Average.Should().Be(value);
        result.P50.Should().Be(value);
        result.P95.Should().Be(value);
        result.P99.Should().Be(value);
        result.Buckets.Should().ContainSingle().Which.Count.Should().Be(3);
        result.Buckets[0].LowerBound.Should().Be(value);
        result.Buckets[0].UpperBound.Should().Be(value);
    }

    [Fact]
    public void Build_WhenFiniteExtremesWouldOverflowRange_ShouldKeepFiniteOrderedBoundsAndConserveCounts()
    {
        var result = HistogramAggregator.Build(Samples(-double.MaxValue, double.MaxValue))!;

        result.Count.Should().Be(2);
        result.Min.Should().Be(-double.MaxValue);
        result.Max.Should().Be(double.MaxValue);
        result.Average.Should().Be(0);
        result.P50.Should().Be(0);
        double.IsFinite(result.P95).Should().BeTrue();
        double.IsFinite(result.P99).Should().BeTrue();
        result.Buckets.Should().AllSatisfy(bucket =>
        {
            double.IsFinite(bucket.LowerBound).Should().BeTrue();
            double.IsFinite(bucket.UpperBound).Should().BeTrue();
            bucket.LowerBound.Should().BeLessThanOrEqualTo(bucket.UpperBound);
        });
        result.Buckets.Select(bucket => bucket.UpperBound).Should().BeInAscendingOrder();
        result.Buckets.Sum(bucket => bucket.Count).Should().Be(2);
        result.Buckets[0].Count.Should().Be(1);
        result.Buckets[^1].Count.Should().Be(1);
    }

    [Fact]
    public void Build_WhenLargeFiniteValuesWouldOverflowTheirSum_ShouldKeepTheMeanWithinObservedBounds()
    {
        var result = HistogramAggregator.Build(Samples(double.MaxValue / 2, double.MaxValue, double.MaxValue))!;

        double.IsFinite(result.Average).Should().BeTrue();
        result.Average.Should().BeInRange(result.Min, result.Max);
        result.Average.Should().BeApproximately(double.MaxValue * (5.0 / 6), double.MaxValue * 0.000001);
        result.Buckets.Sum(bucket => bucket.Count).Should().Be(3);
    }

    private static MetricPointSnapshot[] Samples(params double[] values) => values
        .Select((value, index) => new MetricPointSnapshot(DateTimeOffset.UnixEpoch.AddSeconds(index), value))
        .ToArray();
}
