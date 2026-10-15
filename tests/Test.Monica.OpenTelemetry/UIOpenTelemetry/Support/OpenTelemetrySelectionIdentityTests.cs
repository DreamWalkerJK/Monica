using AwesomeAssertions;
using Monica.OpenTelemetry.InProcessCollector.Models;
using Monica.OpenTelemetry.UI.UIOpenTelemetry.Support;
using Xunit;

namespace Test.Monica.OpenTelemetry.UIOpenTelemetry.Support;

public sealed class OpenTelemetrySelectionIdentityTests
{
    [Fact]
    public void ReconcileInstrument_WhenNativeIdMatches_ShouldUseItWithoutFallingBackToDisplayMetadata()
    {
        var previous = CreateInstrument(instrumentId: "opaque-1");
        var unrelated = CreateInstrument(instrumentId: "opaque-2");
        var matching = CreateInstrument(instrumentId: "opaque-1", meterName: "Changed.Metadata", meterVersion: "2");

        OpenTelemetrySelectionIdentity.ReconcileInstrument(previous, [unrelated, matching])
            .Should().BeSameAs(matching);
        OpenTelemetrySelectionIdentity.ReconcileInstrument(previous, [unrelated]).Should().BeNull();
    }

    [Fact]
    public void ReconcileInstrument_WhenTwoCandidatesHaveTheNativeId_ShouldRejectAmbiguousIdentity()
    {
        var previous = CreateInstrument(instrumentId: "opaque-1");
        var first = CreateInstrument(instrumentId: "opaque-1");
        var second = CreateInstrument(instrumentId: "opaque-1", meterVersion: "2");

        OpenTelemetrySelectionIdentity.ReconcileInstrument(previous, [first, second]).Should().BeNull();
        OpenTelemetrySelectionIdentity.ReconcileInstrument(previous, []).Should().BeNull();
    }

    [Fact]
    public void ReconcileInstrument_WhenNativeIdDiffersOnlyInCase_ShouldKeepOrdinalIdentity()
    {
        var previous = CreateInstrument(instrumentId: "Opaque-1");
        var different = CreateInstrument(instrumentId: "opaque-1");

        OpenTelemetrySelectionIdentity.ReconcileInstrument(previous, [different]).Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ReconcileInstrument_WhenLegacyIdentityIsMissing_ShouldRequireUniqueExactVersionedMetadata(string? missingId)
    {
        var previous = CreateInstrument(instrumentId: missingId, meterVersion: "1");
        var differentVersion = CreateInstrument(instrumentId: null, meterVersion: "2");
        var matching = CreateInstrument(instrumentId: null, meterVersion: "1");
        var nativeCandidate = CreateInstrument(instrumentId: "new-native-id", meterVersion: "1");

        OpenTelemetrySelectionIdentity.ReconcileInstrument(previous, [differentVersion, matching, nativeCandidate])
            .Should().BeSameAs(matching);
        OpenTelemetrySelectionIdentity.ReconcileInstrument(previous, [differentVersion, nativeCandidate])
            .Should().BeNull();
    }

    [Fact]
    public void ReconcileInstrument_WhenLegacyMetadataIsDuplicated_ShouldRejectAmbiguity()
    {
        var previous = CreateInstrument(instrumentId: null);
        var first = CreateInstrument(instrumentId: null);
        var second = CreateInstrument(instrumentId: "");

        OpenTelemetrySelectionIdentity.ReconcileInstrument(previous, [first, second]).Should().BeNull();
    }

    [Theory]
    [InlineData("meter")]
    [InlineData("version")]
    [InlineData("name")]
    [InlineData("kind")]
    [InlineData("unit")]
    [InlineData("description")]
    [InlineData("observable")]
    public void ReconcileInstrument_WhenOneLegacyMetadataFieldChanges_ShouldRejectTheCandidate(string field)
    {
        var previous = CreateInstrument(instrumentId: null);
        var candidate = CreateInstrument(
            instrumentId: null,
            meterName: field == "meter" ? "native.meter" : "Native.Meter",
            meterVersion: field == "version" ? "2" : "1",
            name: field == "name" ? "Native.Value" : "native.value",
            kind: field == "kind" ? "ObservableGauge" : "Gauge",
            unit: field == "unit" ? "s" : "ms",
            description: field == "description" ? "other description" : "description",
            observable: field == "observable");

        OpenTelemetrySelectionIdentity.ReconcileInstrument(previous, [candidate]).Should().BeNull();
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", null)]
    [InlineData("1", "2")]
    public void ReconcileInstrument_WhenLegacyVersionsAreDifferent_ShouldDistinguishNullEmptyAndDeclaredVersions(
        string? previousVersion, string? candidateVersion)
    {
        var previous = CreateInstrument(instrumentId: null, meterVersion: previousVersion);
        var candidate = CreateInstrument(instrumentId: null, meterVersion: candidateVersion);

        OpenTelemetrySelectionIdentity.ReconcileInstrument(previous, [candidate]).Should().BeNull();
    }

    [Fact]
    public void ReconcileSeries_WhenTagOrderChanges_ShouldMatchExactRawKeysAndValues()
    {
        const string key = "raw\u001E\u001F\uD800";
        var previous = CreateSeries(key, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [""] = "",
            ["z"] = "last",
            ["\uD800"] = "first surrogate",
            ["\uD801"] = "second surrogate"
        });
        var matching = CreateSeries(key, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["\uD801"] = "second surrogate",
            ["\uD800"] = "first surrogate",
            ["z"] = "last",
            [""] = ""
        }, value: 99);

        OpenTelemetrySelectionIdentity.ReconcileSeries(previous, [matching]).Should().BeSameAs(matching);
    }

    [Fact]
    public void ReconcileSeries_WhenLoneSurrogateKeysDiffer_ShouldKeepTheirRawIdentityDistinct()
    {
        var previous = CreateSeries("\uD800", new Dictionary<string, string> { ["\uD800"] = "value" });
        var differentKey = CreateSeries("\uD801", new Dictionary<string, string> { ["\uD800"] = "value" });
        var differentTag = CreateSeries("\uD800", new Dictionary<string, string> { ["\uD801"] = "value" });
        var matching = CreateSeries("\uD800", new Dictionary<string, string> { ["\uD800"] = "value" });

        OpenTelemetrySelectionIdentity.ReconcileSeries(previous, [differentKey, differentTag, matching])
            .Should().BeSameAs(matching);
        OpenTelemetrySelectionIdentity.ReconcileSeries(previous, [differentKey, differentTag]).Should().BeNull();
    }

    [Fact]
    public void ReconcileSeries_WhenProviderUsesCaseInsensitiveKeys_ShouldStillCompareOrdinalTagNames()
    {
        var previous = CreateSeries("same legacy key", new Dictionary<string, string> { ["Tag"] = "value" });
        var different = CreateSeries("same legacy key",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["tag"] = "value" });
        var matching = CreateSeries("same legacy key",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Tag"] = "value" });

        OpenTelemetrySelectionIdentity.ReconcileSeries(previous, [different]).Should().BeNull();
        OpenTelemetrySelectionIdentity.ReconcileSeries(previous, [different, matching]).Should().BeSameAs(matching);
    }

    [Fact]
    public void ReconcileSeries_WhenValueCaseOrTagPresenceChanges_ShouldRejectTheCandidate()
    {
        var previous = CreateSeries("same legacy key", new Dictionary<string, string> { ["Tag"] = "" });
        var absent = CreateSeries("same legacy key", new Dictionary<string, string>());
        var previousValue = CreateSeries("same legacy key", new Dictionary<string, string> { ["Tag"] = "Value" });
        var differentValue = CreateSeries("same legacy key", new Dictionary<string, string> { ["Tag"] = "value" });

        OpenTelemetrySelectionIdentity.ReconcileSeries(previous, [absent]).Should().BeNull();
        OpenTelemetrySelectionIdentity.ReconcileSeries(previousValue, [differentValue]).Should().BeNull();
    }

    [Fact]
    public void ReconcileSeries_WhenExactTagIdentityIsDuplicated_ShouldRejectAmbiguityWithoutComparingMeasurements()
    {
        var previous = CreateSeries("exact", new Dictionary<string, string> { ["tag"] = "value" });
        var first = CreateSeries("exact", new Dictionary<string, string> { ["tag"] = "value" }, value: 1);
        var second = CreateSeries("exact", new Dictionary<string, string> { ["tag"] = "value" }, value: 2);

        OpenTelemetrySelectionIdentity.ReconcileSeries(previous, [first, second]).Should().BeNull();
        OpenTelemetrySelectionIdentity.ReconcileSeries(previous, []).Should().BeNull();
    }

    private static InstrumentSnapshot CreateInstrument(
        string? instrumentId = "native-1",
        string meterName = "Native.Meter",
        string? meterVersion = "1",
        string name = "native.value",
        string kind = "Gauge",
        string? unit = "ms",
        string? description = "description",
        bool observable = false) => new()
    {
        InstrumentId = instrumentId,
        MeterName = meterName,
        MeterVersion = meterVersion,
        Name = name,
        Kind = kind,
        Unit = unit,
        Description = description,
        IsObservable = observable
    };

    private static TagSetSamples CreateSeries(string key, IReadOnlyDictionary<string, string> tags, double value = 1) => new()
    {
        Key = key,
        Tags = tags,
        CurrentValue = value,
        Samples = [new MetricPointSnapshot(DateTimeOffset.UnixEpoch, value)]
    };
}
