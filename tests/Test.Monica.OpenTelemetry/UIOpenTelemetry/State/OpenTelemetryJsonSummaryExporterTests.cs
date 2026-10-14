using System.Globalization;
using System.Text.Json;
using AwesomeAssertions;
using Monica.OpenTelemetry.InProcessCollector.Models;
using Monica.OpenTelemetry.UI.UIOpenTelemetry.State;
using Xunit;

namespace Test.Monica.OpenTelemetry.UIOpenTelemetry.State;

public sealed class OpenTelemetryJsonSummaryExporterTests
{
    private static readonly DateTimeOffset CAPTURED_AT =
        new DateTimeOffset(2026, 10, 9, 8, 9, 10, TimeSpan.FromHours(-4)).AddTicks(1234);

    [Fact]
    public void Export_WhenSnapshotContainsNativeIdentityAndResourceMetadata_ShouldPreserveThemWithoutSampleHistory()
    {
        var series = CreateSeries(samples: [new(CAPTURED_AT.AddSeconds(-1), 1), new(CAPTURED_AT, 2)]);
        var instrument = new InstrumentSnapshot
        {
            InstrumentId = "opaque/instrument-17",
            MeterName = "Native.Meter",
            MeterVersion = "4.5",
            Name = "native.value",
            Kind = "ObservableGauge",
            Unit = "ms",
            Description = "quoted \"description\"\nsecond line",
            IsObservable = true,
            Overflow = true,
            DroppedSamples = 7,
            TagSets = [series]
        };
        var snapshot = CreateSnapshot(instrument);

        using var document = JsonDocument.Parse(OpenTelemetryJsonSummaryExporter.Export(snapshot));
        var root = document.RootElement;
        var exportedInstrument = root.GetProperty("instruments")[0];
        var exportedSeries = exportedInstrument.GetProperty("tagSets")[0];

        root.GetProperty("serviceName").GetString().Should().Be("Diagnostic service");
        root.GetProperty("serviceVersion").GetString().Should().Be("2.3");
        root.GetProperty("deploymentEnvironment").GetString().Should().Be("test");
        root.GetProperty("timestampUtc").GetDateTimeOffset().Should().Be(CAPTURED_AT);
        root.GetProperty("timestampUtc").GetDateTimeOffset().Offset.Should().Be(CAPTURED_AT.Offset);
        root.GetProperty("meterPatterns")[0].GetString().Should().Be("Native.*");
        root.GetProperty("samplesPerSeries").GetInt32().Should().Be(20);
        root.GetProperty("maxTagSetsPerInstrument").GetInt32().Should().Be(30);
        root.GetProperty("hasOverflow").GetBoolean().Should().BeTrue();
        exportedInstrument.GetProperty("instrumentId").GetString().Should().Be("opaque/instrument-17");
        exportedInstrument.GetProperty("meterName").GetString().Should().Be("Native.Meter");
        exportedInstrument.GetProperty("meterVersion").GetString().Should().Be("4.5");
        exportedInstrument.GetProperty("name").GetString().Should().Be("native.value");
        exportedInstrument.GetProperty("kind").GetString().Should().Be("ObservableGauge");
        exportedInstrument.GetProperty("unit").GetString().Should().Be("ms");
        exportedInstrument.GetProperty("description").GetString().Should().Be(instrument.Description);
        exportedInstrument.GetProperty("isObservable").GetBoolean().Should().BeTrue();
        exportedInstrument.GetProperty("overflow").GetBoolean().Should().BeTrue();
        exportedInstrument.GetProperty("droppedSamples").GetInt64().Should().Be(7);
        exportedInstrument.GetProperty("tagSetCount").GetInt32().Should().Be(1);
        exportedSeries.GetProperty("key").GetString().Should().Be(series.Key);
        exportedSeries.GetProperty("tags").GetProperty("native.tag").GetString().Should().Be("quoted \"tag\"\nsecond line");
        exportedSeries.GetProperty("currentValue").GetDouble().Should().Be(2);
        exportedSeries.GetProperty("delta").GetDouble().Should().Be(1);
        exportedSeries.GetProperty("lastUpdatedUtc").GetDateTimeOffset().Should().Be(CAPTURED_AT);
        exportedSeries.GetProperty("lastUpdatedUtc").GetDateTimeOffset().Offset.Should().Be(CAPTURED_AT.Offset);
        exportedSeries.GetProperty("retainedSampleCount").GetInt32().Should().Be(2);
        exportedSeries.GetProperty("finiteSampleCount").GetInt32().Should().Be(2);
        exportedSeries.GetProperty("nonFiniteSampleCount").GetInt32().Should().Be(0);
        exportedSeries.TryGetProperty("samples", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(double.NaN, "NaN")]
    [InlineData(double.PositiveInfinity, "Infinity")]
    [InlineData(double.NegativeInfinity, "-Infinity")]
    public void Export_WhenRawMeasurementsAreNonFinite_ShouldPreserveNamedStringsAndFiniteHistogramStatistics(
        double value, string expected)
    {
        var histogram = new HistogramSnapshot
        {
            Count = 1,
            NonFiniteSampleCount = 1,
            Min = 1,
            Max = 1,
            Average = 1,
            P50 = 1,
            P95 = 1,
            P99 = 1,
            Buckets = [new HistogramBucketSnapshot { LowerBound = 1, UpperBound = 1, Count = 1 }]
        };
        var series = CreateSeries(value: value, delta: value,
            samples: [new(CAPTURED_AT.AddSeconds(-1), 1), new(CAPTURED_AT, value)],
            histogram: histogram);

        using var document = JsonDocument.Parse(OpenTelemetryJsonSummaryExporter.Export(
            CreateSnapshot(CreateInstrument([series], kind: "Histogram"))));
        var instrument = document.RootElement.GetProperty("instruments")[0];
        var exportedSeries = instrument.GetProperty("tagSets")[0];
        var exportedHistogram = exportedSeries.GetProperty("histogram");

        instrument.GetProperty("currentValue").GetString().Should().Be(expected);
        instrument.GetProperty("delta").GetString().Should().Be(expected);
        exportedSeries.GetProperty("currentValue").GetString().Should().Be(expected);
        exportedSeries.GetProperty("delta").GetString().Should().Be(expected);
        exportedSeries.GetProperty("retainedSampleCount").GetInt32().Should().Be(2);
        exportedSeries.GetProperty("finiteSampleCount").GetInt32().Should().Be(1);
        exportedSeries.GetProperty("nonFiniteSampleCount").GetInt32().Should().Be(1);
        exportedHistogram.GetProperty("count").GetInt64().Should().Be(1);
        exportedHistogram.GetProperty("nonFiniteSampleCount").GetInt64().Should().Be(1);
        exportedHistogram.GetProperty("average").ValueKind.Should().Be(JsonValueKind.Number);
        exportedHistogram.GetProperty("average").GetDouble().Should().Be(1);
        exportedHistogram.GetProperty("buckets")[0].GetProperty("count").GetInt64().Should().Be(1);
        exportedSeries.TryGetProperty("samples", out _).Should().BeFalse();
    }

    [Fact]
    public void Export_WhenFiniteSeriesOverflowTheInstrumentSum_ShouldKeepEachFiniteNumberAndNameTheAggregateInfinity()
    {
        var first = CreateSeries(value: double.MaxValue, delta: double.MaxValue);
        var second = new TagSetSamples
        {
            Key = "second",
            CurrentValue = double.MaxValue,
            Delta = double.MaxValue,
            LastUpdatedUtc = CAPTURED_AT,
            Samples = [new(CAPTURED_AT, double.MaxValue)]
        };

        using var document = JsonDocument.Parse(OpenTelemetryJsonSummaryExporter.Export(
            CreateSnapshot(CreateInstrument([first, second]))));
        var instrument = document.RootElement.GetProperty("instruments")[0];

        instrument.GetProperty("currentValue").GetString().Should().Be("Infinity");
        instrument.GetProperty("delta").GetString().Should().Be("Infinity");
        foreach (var series in instrument.GetProperty("tagSets").EnumerateArray())
        {
            series.GetProperty("currentValue").ValueKind.Should().Be(JsonValueKind.Number);
            series.GetProperty("currentValue").GetDouble().Should().Be(double.MaxValue);
            series.GetProperty("finiteSampleCount").GetInt32().Should().Be(1);
            series.GetProperty("nonFiniteSampleCount").GetInt32().Should().Be(0);
        }
    }

    [Fact]
    public void Export_WhenAllRetainedMeasurementsAreInvalid_ShouldKeepRawCountsAndANullHistogram()
    {
        var series = CreateSeries(value: double.NaN, delta: double.NaN, samples:
        [
            new(CAPTURED_AT, double.NaN),
            new(CAPTURED_AT, double.PositiveInfinity),
            new(CAPTURED_AT, double.NegativeInfinity)
        ]);
        var instrument = new InstrumentSnapshot
        {
            MeterName = "Native.Meter",
            Name = "native.histogram",
            Kind = "Histogram",
            TagSets = [series]
        };

        using var document = JsonDocument.Parse(OpenTelemetryJsonSummaryExporter.Export(CreateSnapshot(instrument)));
        var exportedInstrument = document.RootElement.GetProperty("instruments")[0];
        var exportedSeries = exportedInstrument.GetProperty("tagSets")[0];

        exportedInstrument.GetProperty("instrumentId").ValueKind.Should().Be(JsonValueKind.Null);
        exportedInstrument.GetProperty("meterVersion").ValueKind.Should().Be(JsonValueKind.Null);
        exportedSeries.GetProperty("currentValue").GetString().Should().Be("NaN");
        exportedSeries.GetProperty("retainedSampleCount").GetInt32().Should().Be(3);
        exportedSeries.GetProperty("finiteSampleCount").GetInt32().Should().Be(0);
        exportedSeries.GetProperty("nonFiniteSampleCount").GetInt32().Should().Be(3);
        exportedSeries.GetProperty("histogram").ValueKind.Should().Be(JsonValueKind.Null);
        exportedSeries.TryGetProperty("samples", out _).Should().BeFalse();
    }

    [Fact]
    public void Export_WhenCultureUsesComma_ShouldKeepJsonNumbersAndExactTagText()
    {
        var priorCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var series = new TagSetSamples
            {
                Key = "numeric tag",
                Tags = new Dictionary<string, string> { ["a:b"] = "1,5" },
                CurrentValue = 1234.5,
                Delta = -0.5,
                LastUpdatedUtc = CAPTURED_AT,
                Samples = [new(CAPTURED_AT, 1234.5)]
            };

            using var document = JsonDocument.Parse(OpenTelemetryJsonSummaryExporter.Export(
                CreateSnapshot(CreateInstrument([series]))));
            var exportedSeries = document.RootElement.GetProperty("instruments")[0].GetProperty("tagSets")[0];

            exportedSeries.GetProperty("currentValue").ValueKind.Should().Be(JsonValueKind.Number);
            exportedSeries.GetProperty("currentValue").GetDouble().Should().Be(1234.5);
            exportedSeries.GetProperty("delta").GetDouble().Should().Be(-0.5);
            exportedSeries.GetProperty("tags").GetProperty("a:b").GetString().Should().Be("1,5");
        }
        finally
        {
            CultureInfo.CurrentCulture = priorCulture;
        }
    }

    [Fact]
    public void Export_WhenDistinctSurrogatesBecomeReadableReplacementText_ShouldPreserveExactSeriesAndTagIdentity()
    {
        var firstSurrogate = new string((char)0xD800, 1);
        var secondSurrogate = new string((char)0xD801, 1);
        var first = new TagSetSamples
        {
            Key = firstSurrogate,
            Tags = new Dictionary<string, string>
            {
                [secondSurrogate] = firstSurrogate,
                [firstSurrogate] = secondSurrogate
            },
            CurrentValue = double.NaN,
            Delta = double.PositiveInfinity,
            Samples = [new(CAPTURED_AT, double.NaN)]
        };
        var second = new TagSetSamples
        {
            Key = secondSurrogate,
            Tags = new Dictionary<string, string> { [firstSurrogate] = firstSurrogate },
            CurrentValue = 1.25,
            Delta = double.NegativeInfinity,
            Samples = [new(CAPTURED_AT, 1.25)]
        };

        using var document = JsonDocument.Parse(OpenTelemetryJsonSummaryExporter.Export(
            CreateSnapshot(CreateInstrument([first, second]))));
        var exportedSeries = document.RootElement.GetProperty("instruments")[0].GetProperty("tagSets");
        var firstExport = exportedSeries[0];
        var secondExport = exportedSeries[1];

        firstExport.GetProperty("key").GetString().Should().Be("\uFFFD");
        secondExport.GetProperty("key").GetString().Should().Be("\uFFFD");
        firstExport.GetProperty("keyUtf16").GetString().Should().Be("uD800");
        secondExport.GetProperty("keyUtf16").GetString().Should().Be("uD801");
        var readableTags = firstExport.GetProperty("tags").EnumerateObject().ToArray();
        readableTags.Should().HaveCount(2);
        readableTags.Select(tag => tag.Name).Should().OnlyContain(name => name == "\uFFFD");
        readableTags.Select(tag => tag.Value.GetString()).Should().OnlyContain(value => value == "\uFFFD");
        var firstEntries = firstExport.GetProperty("tagEntries");
        firstEntries.GetArrayLength().Should().Be(2);
        firstEntries[0].GetProperty("keyUtf16").GetString().Should().Be("uD800");
        firstEntries[0].GetProperty("valueUtf16").GetString().Should().Be("uD801");
        firstEntries[1].GetProperty("keyUtf16").GetString().Should().Be("uD801");
        firstEntries[1].GetProperty("valueUtf16").GetString().Should().Be("uD800");
        secondExport.GetProperty("tagEntries")[0].GetProperty("valueUtf16").GetString().Should().Be("uD800");
        firstExport.GetProperty("currentValue").GetString().Should().Be("NaN");
        firstExport.GetProperty("delta").GetString().Should().Be("Infinity");
        secondExport.GetProperty("currentValue").ValueKind.Should().Be(JsonValueKind.Number);
        secondExport.GetProperty("currentValue").GetDouble().Should().Be(1.25);
        secondExport.GetProperty("delta").GetString().Should().Be("-Infinity");
    }

    [Theory]
    [InlineData("", "u")]
    [InlineData("A:a", "u0041003A0061")]
    [InlineData("\0\n\u001E\u001F", "u0000000A001E001F")]
    [InlineData("\uD83D\uDE00", "uD83DDE00")]
    public void Export_WhenStringsAreEmptyOrdinaryControlOrSurrogatePairs_ShouldEncodeEveryCodeUnit(
        string text, string expectedEncoding)
    {
        var series = new TagSetSamples
        {
            Key = text,
            Tags = new Dictionary<string, string> { [text] = text },
            CurrentValue = 1.25,
            Delta = -0.5,
            Samples = [new(CAPTURED_AT, 1.25)]
        };

        using var document = JsonDocument.Parse(OpenTelemetryJsonSummaryExporter.Export(
            CreateSnapshot(CreateInstrument([series]))));
        var exportedSeries = document.RootElement.GetProperty("instruments")[0].GetProperty("tagSets")[0];

        exportedSeries.GetProperty("key").GetString().Should().Be(text);
        exportedSeries.GetProperty("tags").GetProperty(text).GetString().Should().Be(text);
        exportedSeries.GetProperty("keyUtf16").GetString().Should().Be(expectedEncoding);
        var entry = exportedSeries.GetProperty("tagEntries")[0];
        entry.GetProperty("keyUtf16").GetString().Should().Be(expectedEncoding);
        entry.GetProperty("valueUtf16").GetString().Should().Be(expectedEncoding);
        exportedSeries.GetProperty("currentValue").ValueKind.Should().Be(JsonValueKind.Number);
        exportedSeries.GetProperty("currentValue").GetDouble().Should().Be(1.25);
        exportedSeries.GetProperty("delta").GetDouble().Should().Be(-0.5);
    }

    [Fact]
    public void Export_WhenTagInsertionOrdersDiffer_ShouldOrderExactEntriesByOrdinalNormalizedKey()
    {
        var first = new TagSetSamples
        {
            Key = "same series",
            Tags = new Dictionary<string, string> { ["a"] = "lower", ["A"] = "upper", [""] = "empty" }
        };
        var second = new TagSetSamples
        {
            Key = "same series",
            Tags = new Dictionary<string, string> { [""] = "empty", ["A"] = "upper", ["a"] = "lower" }
        };

        using var document = JsonDocument.Parse(OpenTelemetryJsonSummaryExporter.Export(
            CreateSnapshot(CreateInstrument([first, second]))));
        var exportedSeries = document.RootElement.GetProperty("instruments")[0].GetProperty("tagSets");
        var firstEntries = exportedSeries[0].GetProperty("tagEntries");
        var secondEntries = exportedSeries[1].GetProperty("tagEntries");

        firstEntries.GetRawText().Should().Be(secondEntries.GetRawText());
        firstEntries.EnumerateArray().Select(entry => entry.GetProperty("keyUtf16").GetString())
            .Should().Equal("u", "u0041", "u0061");
    }

    private static OpenTelemetrySnapshot CreateSnapshot(params InstrumentSnapshot[] instruments) => new()
    {
        ResourceServiceName = "Diagnostic service",
        ResourceServiceVersion = "2.3",
        DeploymentEnvironment = "test",
        TimestampUtc = CAPTURED_AT,
        MeterPatterns = ["Native.*"],
        SamplesPerSeries = 20,
        MaxTagSetsPerInstrument = 30,
        Instruments = instruments
    };

    private static InstrumentSnapshot CreateInstrument(IReadOnlyList<TagSetSamples> tagSets, string kind = "Gauge") => new()
    {
        InstrumentId = "instrument-1",
        MeterName = "Native.Meter",
        MeterVersion = "1",
        Name = "native.value",
        Kind = kind,
        TagSets = tagSets
    };

    private static TagSetSamples CreateSeries(
        double value = 2,
        double delta = 1,
        IReadOnlyList<MetricPointSnapshot>? samples = null,
        HistogramSnapshot? histogram = null) => new()
    {
        Key = "exact tag-set key",
        Tags = new Dictionary<string, string> { ["native.tag"] = "quoted \"tag\"\nsecond line" },
        CurrentValue = value,
        Delta = delta,
        LastUpdatedUtc = CAPTURED_AT,
        Samples = samples ?? [new(CAPTURED_AT, value)],
        Histogram = histogram
    };
}
