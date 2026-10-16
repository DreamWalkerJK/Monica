using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Monica.OpenTelemetry.InProcessCollector.Models;

namespace Monica.OpenTelemetry.UI.UIOpenTelemetry.State;

/// <summary>
/// Exports compact dashboard JSON that keeps the current metric state and omits retained sample history.
/// </summary>
/// <remarks>
/// Finite measurements remain JSON numbers; non-finite values are the strings <c>NaN</c>, <c>Infinity</c>, and
/// <c>-Infinity</c>. Instrument IDs are opaque within one collector lifetime. Histogram statistics describe only
/// finite retained measurements; the per-series counts also describe raw measurements excluded from those statistics.
/// Each series retains the readable <c>key</c> and <c>tags</c> fields. Its <c>keyUtf16</c> and ordinally ordered
/// <c>tagEntries</c> preserve exact normalized .NET strings, including unpaired surrogates that readable JSON may
/// replace. Every encoded string starts with <c>u</c>, followed by four uppercase hexadecimal digits per UTF-16
/// code unit; the empty string is <c>u</c>. Each tag entry contains <c>keyUtf16</c> and <c>valueUtf16</c>.
/// Decode each four-digit group independently without Unicode normalization or surrogate replacement. These fields
/// preserve the collector's normalized strings, not the original CLR types of tag values.
/// </remarks>
internal static class OpenTelemetryJsonSummaryExporter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    public static string Export(OpenTelemetrySnapshot snapshot)
    {
        return JsonSerializer.Serialize(CreateSummary(snapshot), SerializerOptions);
    }

    private static SnapshotSummary CreateSummary(OpenTelemetrySnapshot snapshot)
    {
        return new SnapshotSummary(
            snapshot.ResourceServiceName,
            snapshot.ResourceServiceVersion,
            snapshot.DeploymentEnvironment,
            snapshot.TimestampUtc,
            snapshot.MeterPatterns,
            snapshot.SamplesPerSeries,
            snapshot.MaxTagSetsPerInstrument,
            snapshot.HasOverflow,
            snapshot.Instruments.Select(CreateInstrumentSummary).ToList());
    }

    private static InstrumentSummary CreateInstrumentSummary(InstrumentSnapshot instrument)
    {
        return new InstrumentSummary(
            instrument.InstrumentId,
            instrument.MeterName,
            instrument.MeterVersion,
            instrument.Name,
            instrument.Kind,
            instrument.Unit,
            instrument.Description,
            instrument.IsObservable,
            instrument.CurrentValue,
            instrument.Delta,
            instrument.Overflow,
            instrument.DroppedSamples,
            instrument.TagSets.Count,
            instrument.TagSets.Select(CreateTagSetSummary).ToList());
    }

    private static TagSetSummary CreateTagSetSummary(TagSetSamples tagSet)
    {
        return new TagSetSummary(
            tagSet.Key,
            tagSet.Tags,
            tagSet.CurrentValue,
            tagSet.Delta,
            tagSet.LastUpdatedUtc,
            tagSet.Samples.Count,
            tagSet.FiniteSampleCount,
            tagSet.NonFiniteSampleCount,
            tagSet.Histogram,
            EncodeUtf16(tagSet.Key),
            tagSet.Tags
                .OrderBy(tag => tag.Key, StringComparer.Ordinal)
                .Select(tag => new TagEntrySummary(EncodeUtf16(tag.Key), EncodeUtf16(tag.Value)))
                .ToList());
    }

    private static string EncodeUtf16(string value)
    {
        var encoded = new StringBuilder("u");
        foreach (var codeUnit in value)
        {
            encoded.Append(((int)codeUnit).ToString("X4", CultureInfo.InvariantCulture));
        }

        return encoded.ToString();
    }

    private sealed record SnapshotSummary(
        string ServiceName,
        string? ServiceVersion,
        string? DeploymentEnvironment,
        DateTimeOffset TimestampUtc,
        IReadOnlyList<string> MeterPatterns,
        int SamplesPerSeries,
        int MaxTagSetsPerInstrument,
        bool HasOverflow,
        IReadOnlyList<InstrumentSummary> Instruments);

    private sealed record InstrumentSummary(
        string? InstrumentId,
        string MeterName,
        string? MeterVersion,
        string Name,
        string Kind,
        string? Unit,
        string? Description,
        bool IsObservable,
        double CurrentValue,
        double Delta,
        bool Overflow,
        long DroppedSamples,
        int TagSetCount,
        IReadOnlyList<TagSetSummary> TagSets);

    private sealed record TagSetSummary(
        string Key,
        IReadOnlyDictionary<string, string> Tags,
        double CurrentValue,
        double Delta,
        DateTimeOffset LastUpdatedUtc,
        int RetainedSampleCount,
        int FiniteSampleCount,
        int NonFiniteSampleCount,
        HistogramSnapshot? Histogram,
        string KeyUtf16,
        IReadOnlyList<TagEntrySummary> TagEntries);

    private sealed record TagEntrySummary(string KeyUtf16, string ValueUtf16);
}
