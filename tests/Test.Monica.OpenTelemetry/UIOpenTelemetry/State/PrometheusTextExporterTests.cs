using System.Globalization;
using AwesomeAssertions;
using Monica.OpenTelemetry.InProcessCollector.Models;
using Monica.OpenTelemetry.UI.UIOpenTelemetry.State;
using Xunit;

namespace Test.Monica.OpenTelemetry.UIOpenTelemetry.State;

public sealed class PrometheusTextExporterTests
{
    private static readonly DateTimeOffset CAPTURED_AT = new(2026, 10, 9, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    public void Export_WhenNativeNamesOrMetersCollide_ShouldPreserveEveryIdentityInOneGaugeFamily()
    {
        var snapshot = CreateSnapshot(
            CreateInstrument(name: "native.value", instrumentId: "first", meterName: "Meter.A"),
            CreateInstrument(name: "native.value", instrumentId: "second", meterName: "Meter.B"),
            CreateInstrument(name: "native_value", instrumentId: "third", meterName: "Meter.A"),
            CreateInstrument(name: "native.value_count", instrumentId: "fourth", meterName: "Meter.A"),
            CreateInstrument(name: "native.value", instrumentId: "fifth", meterName: "Meter.A", meterVersion: "2"));

        var exported = PrometheusTextExporter.Export(snapshot);
        var samples = GetSamples(exported, "monica_diagnostic_current_value");
        var types = exported.Split('\n').Where(static line => line.StartsWith("# TYPE ", StringComparison.Ordinal)).ToArray();

        samples.Should().HaveCount(5);
        samples.Distinct(StringComparer.Ordinal).Should().HaveCount(5);
        samples.Should().Contain(line => line.Contains("instrument_id=\"first\"", StringComparison.Ordinal)
                                         && line.Contains("meter_name=\"Meter.A\"", StringComparison.Ordinal));
        samples.Should().Contain(line => line.Contains("instrument_id=\"second\"", StringComparison.Ordinal)
                                         && line.Contains("meter_name=\"Meter.B\"", StringComparison.Ordinal));
        samples.Should().Contain(line => line.Contains("instrument_name=\"native_value\"", StringComparison.Ordinal));
        samples.Should().Contain(line => line.Contains("instrument_name=\"native.value_count\"", StringComparison.Ordinal));
        samples.Should().Contain(line => line.Contains("instrument_id=\"fifth\"", StringComparison.Ordinal)
                                         && line.Contains("meter_version=\"2\"", StringComparison.Ordinal));
        types.Should().OnlyContain(static line => line.EndsWith(" gauge", StringComparison.Ordinal));
        types.Distinct(StringComparer.Ordinal).Should().HaveCount(types.Length);
        types.Count(static line => line == "# TYPE monica_diagnostic_current_value gauge").Should().Be(1);
        exported.Should().NotContain("# TYPE native");
    }

    [Theory]
    [InlineData("Counter")]
    [InlineData("ObservableCounter")]
    [InlineData("UpDownCounter")]
    [InlineData("ObservableUpDownCounter")]
    [InlineData("Gauge")]
    [InlineData("ObservableGauge")]
    [InlineData("Histogram")]
    public void Export_WhenInstrumentKindChanges_ShouldKeepDiagnosticGaugeSemanticsAndExactNativeKind(string kind)
    {
        var exported = PrometheusTextExporter.Export(CreateSnapshot(CreateInstrument(kind: kind)));

        exported.Should().Contain("# TYPE monica_diagnostic_current_value gauge\n");
        GetSamples(exported, "monica_diagnostic_current_value").Should().ContainSingle()
            .Which.Should().Contain($"instrument_kind=\"{kind}\"");
        exported.Should().NotContain(" counter\n").And.NotContain(" histogram\n");
    }

    [Fact]
    public void Export_WhenUserLabelKeysContainPunctuationEmptyOrSurrogates_ShouldEncodeDistinctLegalNames()
    {
        var tags = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [""] = "",
            ["a.b"] = "dot",
            ["a_b"] = "underscore",
            ["a:b"] = "colon",
            ["le"] = "owner",
            ["instrument_id"] = "user",
            ["\uD800"] = "first surrogate",
            ["\uD801"] = "second surrogate"
        };
        var exported = PrometheusTextExporter.Export(CreateSnapshot(
            CreateInstrument(tagSets: [CreateSeries(key: "keys", tags: tags)])));
        var sample = GetSamples(exported, "monica_diagnostic_current_value").Single();

        sample.Should().Contain("tag_u=\"\"")
            .And.Contain("tag_u0061002E0062=\"dot\"")
            .And.Contain("tag_u0061005F0062=\"underscore\"")
            .And.Contain("tag_u0061003A0062=\"colon\"")
            .And.Contain("tag_u006C0065=\"owner\"")
            .And.Contain("tag_u0069006E0073007400720075006D0065006E0074005F00690064=\"user\"")
            .And.Contain("tag_uD800=\"first surrogate\"")
            .And.Contain("tag_uD801=\"second surrogate\"")
            .And.Contain("instrument_id=\"instrument-1\"");
        sample.Should().NotContain(",le=\"").And.NotContain(",a:b=");
    }

    [Fact]
    public void Export_WhenTagIsEmptyOrAbsent_ShouldKeepDifferentSeriesIdentities()
    {
        var exported = PrometheusTextExporter.Export(CreateSnapshot(CreateInstrument(tagSets:
        [
            CreateSeries(key: "", tags: new Dictionary<string, string>()),
            CreateSeries(key: "5:empty0:", tags: new Dictionary<string, string> { ["empty"] = "" })
        ])));
        var samples = GetSamples(exported, "monica_diagnostic_current_value");

        samples.Should().HaveCount(2);
        samples.Should().Contain(line => line.Contains("tag_set_key=\"u\"", StringComparison.Ordinal));
        samples.Should().Contain(line => line.Contains("tag_set_key=\"u0035003A0065006D0070007400790030003A\"", StringComparison.Ordinal)
                                         && line.Contains("tag_u0065006D007000740079=\"\"", StringComparison.Ordinal));
        samples.Distinct(StringComparer.Ordinal).Should().HaveCount(2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Export_WhenCollectorIdentityIsMissing_ShouldUseAnExplicitExportLocalIdentityWithoutColliding(string? missingId)
    {
        var exported = PrometheusTextExporter.Export(CreateSnapshot(
            CreateInstrument(instrumentId: missingId),
            CreateInstrument(instrumentId: missingId),
            CreateInstrument(instrumentId: "0")));
        var samples = GetSamples(exported, "monica_diagnostic_current_value");

        samples.Should().HaveCount(3);
        samples.Distinct(StringComparer.Ordinal).Should().HaveCount(3);
        samples.Should().Contain(line => line.Contains("instrument_id=\"0\",instrument_id_source=\"snapshot\"", StringComparison.Ordinal));
        samples.Should().Contain(line => line.Contains("instrument_id=\"1\",instrument_id_source=\"snapshot\"", StringComparison.Ordinal));
        samples.Should().Contain(line => line.Contains("instrument_id=\"0\",instrument_id_source=\"collector\"", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(double.NaN, "NaN")]
    [InlineData(double.PositiveInfinity, "+Inf")]
    [InlineData(double.NegativeInfinity, "-Inf")]
    public void Export_WhenRawValueIsNonFinite_ShouldUseLegalPrometheusNumberTokens(double value, string expected)
    {
        var exported = PrometheusTextExporter.Export(CreateSnapshot(
            CreateInstrument(tagSets: [CreateSeries(value: value, delta: value)])));

        GetSamples(exported, "monica_diagnostic_current_value").Should().ContainSingle()
            .Which.Should().EndWith(" " + expected);
        GetSamples(exported, "monica_diagnostic_delta").Should().ContainSingle()
            .Which.Should().EndWith(" " + expected);
        GetSamples(exported, "monica_diagnostic_retained_finite_sample_count").Should().ContainSingle()
            .Which.Should().EndWith(" 0");
        GetSamples(exported, "monica_diagnostic_retained_non_finite_sample_count").Should().ContainSingle()
            .Which.Should().EndWith(" 1");
    }

    [Fact]
    public void Export_WhenHistogramContainsFiniteAndExcludedSamples_ShouldEmitNoncumulativeWindowGauges()
    {
        var histogram = new HistogramSnapshot
        {
            Count = 3,
            NonFiniteSampleCount = 2,
            Min = 0,
            Max = 4,
            Average = 2,
            P50 = 2,
            P95 = 3.8,
            P99 = 3.96,
            Buckets =
            [
                new HistogramBucketSnapshot { LowerBound = 0, UpperBound = 1, Count = 1 },
                new HistogramBucketSnapshot { LowerBound = 1, UpperBound = 4, Count = 2 }
            ]
        };
        var series = CreateSeries(
            key: "histogram",
            value: double.NaN,
            delta: double.NaN,
            tags: new Dictionary<string, string> { ["le"] = "original" },
            samples:
            [
                new(CAPTURED_AT, 0), new(CAPTURED_AT, 2), new(CAPTURED_AT, 4),
                new(CAPTURED_AT, double.PositiveInfinity), new(CAPTURED_AT, double.NaN)
            ],
            histogram: histogram);

        var exported = PrometheusTextExporter.Export(CreateSnapshot(
            CreateInstrument(kind: "Histogram", tagSets: [series])));
        var buckets = GetSamples(exported, "monica_diagnostic_histogram_window_bucket_count");

        GetSamples(exported, "monica_diagnostic_histogram_window_count").Should().ContainSingle()
            .Which.Should().EndWith(" 3");
        GetSamples(exported, "monica_diagnostic_histogram_window_non_finite_sample_count").Should().ContainSingle()
            .Which.Should().EndWith(" 2");
        GetSamples(exported, "monica_diagnostic_retained_sample_count").Should().ContainSingle()
            .Which.Should().EndWith(" 5");
        GetSamples(exported, "monica_diagnostic_retained_finite_sample_count").Should().ContainSingle()
            .Which.Should().EndWith(" 3");
        GetSamples(exported, "monica_diagnostic_histogram_window_mean").Should().ContainSingle()
            .Which.Should().EndWith(" 2");
        buckets.Should().HaveCount(2);
        buckets[0].Should().Contain("bucket_index=\"0\"").And.Contain("lower_bound=\"0\"")
            .And.Contain("upper_bound=\"1\"").And.Contain("lower_inclusive=\"true\"").And.EndWith(" 1");
        buckets[1].Should().Contain("bucket_index=\"1\"").And.Contain("lower_bound=\"1\"")
            .And.Contain("upper_bound=\"4\"").And.Contain("lower_inclusive=\"false\"").And.EndWith(" 2");
        buckets.Should().OnlyContain(static line => line.Contains("upper_inclusive=\"true\"", StringComparison.Ordinal)
                                                   && line.Contains("tag_u006C0065=\"original\"", StringComparison.Ordinal));
        exported.Should().NotContain(" histogram\n").And.NotContain("_sum{").And.NotContain(",le=\"");
    }

    [Fact]
    public void Export_WhenHistogramHasOnlyInvalidRawSamples_ShouldReportExclusionsWithoutInventingStatistics()
    {
        var series = CreateSeries(value: double.NaN, samples:
        [
            new(CAPTURED_AT, double.NaN),
            new(CAPTURED_AT, double.PositiveInfinity),
            new(CAPTURED_AT, double.NegativeInfinity)
        ]);
        var exported = PrometheusTextExporter.Export(CreateSnapshot(
            CreateInstrument(kind: "Histogram", tagSets: [series])));

        GetSamples(exported, "monica_diagnostic_retained_finite_sample_count").Should().ContainSingle()
            .Which.Should().EndWith(" 0");
        GetSamples(exported, "monica_diagnostic_retained_non_finite_sample_count").Should().ContainSingle()
            .Which.Should().EndWith(" 3");
        exported.Should().NotContain("monica_diagnostic_histogram_window_");
    }

    [Fact]
    public void Export_WhenLabelsNeedEscapingAndCultureUsesComma_ShouldPreserveValuesWithLfRecords()
    {
        var priorCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            const string text = "first\\second\n\"third\"";
            var exported = PrometheusTextExporter.Export(CreateSnapshot(CreateInstrument(
                description: text,
                tagSets: [CreateSeries(value: 1234.5, tags: new Dictionary<string, string> { ["text"] = text })])));

            var sample = GetSamples(exported, "monica_diagnostic_current_value").Single();
            sample.Should().Contain("instrument_description=\"first\\\\second\\n\\\"third\\\"\"")
                .And.Contain("tag_u0074006500780074=\"first\\\\second\\n\\\"third\\\"\"")
                .And.EndWith(" 1234.5");
            exported.Should().NotContain("\r").And.EndWith("\n");
            exported.Should().Contain("# Snapshot timestamp: 2026-10-09T03:04:05.0000000+00:00\n");
        }
        finally
        {
            CultureInfo.CurrentCulture = priorCulture;
        }
    }

    [Fact]
    public void Export_WhenInputOrderingChanges_ShouldKeepCollectorIdentifiedOutputDeterministic()
    {
        var firstSeries = CreateSeries(key: "series", tags: new Dictionary<string, string> { ["z"] = "last", ["a"] = "first" });
        var reorderedSeries = CreateSeries(key: "series", tags: new Dictionary<string, string> { ["a"] = "first", ["z"] = "last" });
        var first = CreateInstrument(instrumentId: "first", tagSets: [firstSeries]);
        var reorderedFirst = CreateInstrument(instrumentId: "first", tagSets: [reorderedSeries]);
        var second = CreateInstrument(instrumentId: "second");

        var original = PrometheusTextExporter.Export(CreateSnapshot(first, second));
        var reordered = PrometheusTextExporter.Export(CreateSnapshot(second, reorderedFirst));

        reordered.Should().Be(original);
    }

    private static OpenTelemetrySnapshot CreateSnapshot(params InstrumentSnapshot[] instruments) => new()
    {
        ResourceServiceName = "Diagnostics",
        ResourceServiceVersion = "2.3",
        DeploymentEnvironment = "test",
        TimestampUtc = CAPTURED_AT,
        Instruments = instruments
    };

    private static InstrumentSnapshot CreateInstrument(
        string name = "native.value",
        string? instrumentId = "instrument-1",
        string meterName = "Meter",
        string? meterVersion = "1",
        string kind = "Gauge",
        string? description = null,
        IReadOnlyList<TagSetSamples>? tagSets = null) => new()
    {
        InstrumentId = instrumentId,
        MeterName = meterName,
        MeterVersion = meterVersion,
        Name = name,
        Kind = kind,
        Unit = "ms",
        Description = description,
        IsObservable = kind.StartsWith("Observable", StringComparison.Ordinal),
        TagSets = tagSets ?? [CreateSeries()]
    };

    private static TagSetSamples CreateSeries(
        string key = "series",
        double value = 1,
        double delta = 0,
        IReadOnlyDictionary<string, string>? tags = null,
        IReadOnlyList<MetricPointSnapshot>? samples = null,
        HistogramSnapshot? histogram = null) => new()
    {
        Key = key,
        Tags = tags ?? new Dictionary<string, string>(),
        CurrentValue = value,
        Delta = delta,
        LastUpdatedUtc = CAPTURED_AT,
        Samples = samples ?? [new MetricPointSnapshot(CAPTURED_AT, value)],
        Histogram = histogram
    };

    private static string[] GetSamples(string exported, string family) => exported.Split('\n')
        .Where(line => line.StartsWith(family + "{", StringComparison.Ordinal))
        .ToArray();
}
