using AwesomeAssertions;
using Monica.OpenTelemetry.InProcessCollector.Models;
using Monica.OpenTelemetry.UI.UIOpenTelemetry.Support;
using Xunit;

namespace Test.Monica.OpenTelemetry.UIOpenTelemetry.Support;

public sealed class OpenTelemetrySnapshotOwnershipTests
{
    [Fact]
    public void Detach_WhenSourceCollectionsAreMutated_ShouldRetainTheCompleteFrozenGraphAndRawObservationIdentity()
    {
        var timestamp = new DateTimeOffset(2026, 10, 9, 8, 9, 10, TimeSpan.FromHours(-4)).AddTicks(1234);
        var negativeZero = BitConverter.Int64BitsToDouble(long.MinValue);
        var point = new MetricPointSnapshot(timestamp.AddTicks(-1), negativeZero);
        var samples = new List<MetricPointSnapshot>
        {
            point,
            new(timestamp, double.NaN),
            new(timestamp.AddTicks(1), double.PositiveInfinity)
        };
        var tags = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["\uD800"] = "first",
            ["\uD801"] = "second",
            ["control\u001E\u001F"] = "value"
        };
        var bucket = new HistogramBucketSnapshot { LowerBound = negativeZero, UpperBound = negativeZero, Count = 1 };
        var buckets = new List<HistogramBucketSnapshot> { bucket };
        var histogram = new HistogramSnapshot
        {
            Count = 1,
            NonFiniteSampleCount = 2,
            Min = negativeZero,
            Max = negativeZero,
            Average = negativeZero,
            P50 = negativeZero,
            P95 = negativeZero,
            P99 = negativeZero,
            Buckets = buckets
        };
        var series = new TagSetSamples
        {
            Key = "raw\u001E\u001F\uD800",
            Tags = tags,
            CurrentValue = double.PositiveInfinity,
            Delta = double.NaN,
            LastUpdatedUtc = timestamp.AddTicks(1),
            Samples = samples,
            Histogram = histogram
        };
        var tagSets = new List<TagSetSamples> { series };
        var instrument = new InstrumentSnapshot
        {
            InstrumentId = "native-17",
            MeterName = "Native.Meter",
            MeterVersion = "4.5",
            Name = "native.histogram",
            Kind = "Histogram",
            Unit = "s",
            Description = "exact description",
            IsObservable = false,
            Overflow = true,
            DroppedSamples = 9,
            TagSets = tagSets
        };
        var instruments = new List<InstrumentSnapshot> { instrument };
        var patterns = new List<string> { "Native.*", "Other.Meter" };
        var source = new OpenTelemetrySnapshot
        {
            ResourceServiceName = "Diagnostic service",
            ResourceServiceVersion = "2.3",
            DeploymentEnvironment = "test",
            TimestampUtc = timestamp,
            MeterPatterns = patterns,
            SamplesPerSeries = 30,
            MaxTagSetsPerInstrument = 40,
            Instruments = instruments
        };

        var detached = OpenTelemetrySnapshotOwnership.Detach(source);
        patterns[0] = "Changed.*";
        tags["\uD800"] = "changed";
        tags.Clear();
        samples[0] = new(timestamp, 123);
        samples.Clear();
        buckets[0] = new HistogramBucketSnapshot { LowerBound = 99, UpperBound = 99, Count = 99 };
        buckets.Clear();
        tagSets.Clear();
        instruments.Clear();

        detached.Should().NotBeSameAs(source);
        detached.ResourceServiceName.Should().Be("Diagnostic service");
        detached.ResourceServiceVersion.Should().Be("2.3");
        detached.DeploymentEnvironment.Should().Be("test");
        detached.TimestampUtc.Ticks.Should().Be(timestamp.Ticks);
        detached.TimestampUtc.Offset.Should().Be(timestamp.Offset);
        detached.MeterPatterns.Should().Equal("Native.*", "Other.Meter");
        detached.SamplesPerSeries.Should().Be(30);
        detached.MaxTagSetsPerInstrument.Should().Be(40);
        detached.HasOverflow.Should().BeTrue();
        var frozenInstrument = detached.Instruments.Should().ContainSingle().Which;
        frozenInstrument.Should().NotBeSameAs(instrument);
        frozenInstrument.InstrumentId.Should().Be("native-17");
        frozenInstrument.MeterName.Should().Be("Native.Meter");
        frozenInstrument.MeterVersion.Should().Be("4.5");
        frozenInstrument.Name.Should().Be("native.histogram");
        frozenInstrument.Kind.Should().Be("Histogram");
        frozenInstrument.Unit.Should().Be("s");
        frozenInstrument.Description.Should().Be("exact description");
        frozenInstrument.IsObservable.Should().BeFalse();
        frozenInstrument.Overflow.Should().BeTrue();
        frozenInstrument.DroppedSamples.Should().Be(9);
        var frozenSeries = frozenInstrument.TagSets.Should().ContainSingle().Which;
        frozenSeries.Should().NotBeSameAs(series);
        frozenSeries.Key.Should().Be("raw\u001E\u001F\uD800");
        frozenSeries.Tags.Should().HaveCount(3);
        frozenSeries.Tags["\uD800"].Should().Be("first");
        frozenSeries.Tags["\uD801"].Should().Be("second");
        frozenSeries.Tags["control\u001E\u001F"].Should().Be("value");
        double.IsPositiveInfinity(frozenSeries.CurrentValue).Should().BeTrue();
        double.IsNaN(frozenSeries.Delta).Should().BeTrue();
        frozenSeries.LastUpdatedUtc.Ticks.Should().Be(timestamp.AddTicks(1).Ticks);
        frozenSeries.LastUpdatedUtc.Offset.Should().Be(timestamp.Offset);
        frozenSeries.Samples.Should().HaveCount(3);
        frozenSeries.Samples[0].Should().BeSameAs(point);
        BitConverter.DoubleToInt64Bits(frozenSeries.Samples[0].Value).Should().Be(long.MinValue);
        frozenSeries.Samples[0].TimestampUtc.Offset.Should().Be(timestamp.Offset);
        double.IsNaN(frozenSeries.Samples[1].Value).Should().BeTrue();
        double.IsPositiveInfinity(frozenSeries.Samples[2].Value).Should().BeTrue();
        frozenSeries.FiniteSampleCount.Should().Be(1);
        frozenSeries.NonFiniteSampleCount.Should().Be(2);
        frozenSeries.Histogram.Should().NotBeNull();
        var frozenHistogram = frozenSeries.Histogram!;
        frozenHistogram.Should().NotBeSameAs(histogram);
        frozenHistogram.Count.Should().Be(1);
        frozenHistogram.NonFiniteSampleCount.Should().Be(2);
        BitConverter.DoubleToInt64Bits(frozenHistogram.Min).Should().Be(long.MinValue);
        BitConverter.DoubleToInt64Bits(frozenHistogram.Max).Should().Be(long.MinValue);
        BitConverter.DoubleToInt64Bits(frozenHistogram.Average).Should().Be(long.MinValue);
        BitConverter.DoubleToInt64Bits(frozenHistogram.P50).Should().Be(long.MinValue);
        BitConverter.DoubleToInt64Bits(frozenHistogram.P95).Should().Be(long.MinValue);
        BitConverter.DoubleToInt64Bits(frozenHistogram.P99).Should().Be(long.MinValue);
        var frozenBucket = frozenHistogram.Buckets.Should().ContainSingle().Which;
        frozenBucket.Should().NotBeSameAs(bucket);
        BitConverter.DoubleToInt64Bits(frozenBucket.LowerBound).Should().Be(long.MinValue);
        BitConverter.DoubleToInt64Bits(frozenBucket.UpperBound).Should().Be(long.MinValue);
        frozenBucket.Count.Should().Be(1);
    }

    [Fact]
    public void Detach_WhenConsumersAttemptCollectionMutation_ShouldExposeReadOnlyCollectionsAtEveryLevel()
    {
        var source = new OpenTelemetrySnapshot
        {
            ResourceServiceName = "service",
            MeterPatterns = ["Native.*"],
            Instruments =
            [
                new InstrumentSnapshot
                {
                    MeterName = "Native.Meter",
                    Name = "native.histogram",
                    Kind = "Histogram",
                    TagSets =
                    [
                        new TagSetSamples
                        {
                            Key = "series",
                            Tags = new Dictionary<string, string> { ["tag"] = "value" },
                            Samples = [new(DateTimeOffset.UnixEpoch, 1)],
                            Histogram = new HistogramSnapshot
                            {
                                Count = 1,
                                Buckets = [new HistogramBucketSnapshot { LowerBound = 1, UpperBound = 1, Count = 1 }]
                            }
                        }
                    ]
                }
            ]
        };
        var detached = OpenTelemetrySnapshotOwnership.Detach(source);
        var series = detached.Instruments[0].TagSets[0];
        Action[] mutations =
        [
            () => ((IList<string>)detached.MeterPatterns).Clear(),
            () => ((IList<InstrumentSnapshot>)detached.Instruments).Clear(),
            () => ((IList<TagSetSamples>)detached.Instruments[0].TagSets).Clear(),
            () => ((IDictionary<string, string>)series.Tags)["tag"] = "changed",
            () => ((IList<MetricPointSnapshot>)series.Samples).Clear(),
            () => ((IList<HistogramBucketSnapshot>)series.Histogram!.Buckets).Clear()
        ];

        foreach (var mutation in mutations)
        {
            mutation.Should().Throw<NotSupportedException>();
        }
    }

    [Fact]
    public void Detach_WhenProviderUsesCaseInsensitiveTags_ShouldPreserveActualKeysWithOrdinalLookup()
    {
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Native.Tag"] = "value" };
        var source = new OpenTelemetrySnapshot
        {
            ResourceServiceName = "service",
            Instruments =
            [
                new InstrumentSnapshot
                {
                    MeterName = "Native.Meter", Name = "native.value", Kind = "Gauge",
                    TagSets = [new TagSetSamples { Key = "series", Tags = tags }]
                }
            ]
        };

        var detached = OpenTelemetrySnapshotOwnership.Detach(source);
        var frozenTags = detached.Instruments[0].TagSets[0].Tags;

        tags.ContainsKey("native.tag").Should().BeTrue();
        frozenTags.ContainsKey("Native.Tag").Should().BeTrue();
        frozenTags.ContainsKey("native.tag").Should().BeFalse();
    }

    [Fact]
    public void Detach_WhenHistoryExceedsSixHundredSamples_ShouldPreserveEverySourceRecordWithoutTruncation()
    {
        var samples = Enumerable.Range(0, 641)
            .Select(index => new MetricPointSnapshot(DateTimeOffset.UnixEpoch.AddTicks(index * 7919L), index - 320d))
            .ToArray();
        var source = new OpenTelemetrySnapshot
        {
            ResourceServiceName = "service",
            Instruments =
            [
                new InstrumentSnapshot
                {
                    MeterName = "Native.Meter", Name = "native.value", Kind = "Gauge",
                    TagSets = [new TagSetSamples { Key = "series", Samples = samples }]
                }
            ]
        };

        var detached = OpenTelemetrySnapshotOwnership.Detach(source);
        var frozenSamples = detached.Instruments[0].TagSets[0].Samples;

        frozenSamples.Should().HaveCount(641);
        for (var index = 0; index < samples.Length; index++)
        {
            frozenSamples[index].Should().BeSameAs(samples[index]);
        }
    }

    [Fact]
    public void Detach_WhenRegistryIsEmpty_ShouldPreserveAnEmptySuccessfulGraphWithoutInventingMeasurements()
    {
        var source = new OpenTelemetrySnapshot
        {
            ResourceServiceName = "empty service",
            TimestampUtc = DateTimeOffset.UnixEpoch,
            SamplesPerSeries = 20,
            MaxTagSetsPerInstrument = 30
        };

        var detached = OpenTelemetrySnapshotOwnership.Detach(source);

        detached.Instruments.Should().BeEmpty();
        detached.MeterPatterns.Should().BeEmpty();
        detached.HasOverflow.Should().BeFalse();
        detached.TimestampUtc.Should().Be(DateTimeOffset.UnixEpoch);
        detached.SamplesPerSeries.Should().Be(20);
        detached.MaxTagSetsPerInstrument.Should().Be(30);
    }
}
