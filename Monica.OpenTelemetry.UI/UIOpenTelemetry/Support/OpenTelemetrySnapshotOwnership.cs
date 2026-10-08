using System.Collections.ObjectModel;
using Monica.OpenTelemetry.InProcessCollector.Models;

namespace Monica.OpenTelemetry.UI.UIOpenTelemetry.Support;

/// <summary>Detaches the mutable collection graph at the page's successful-read boundary.</summary>
internal static class OpenTelemetrySnapshotOwnership
{
    public static OpenTelemetrySnapshot Detach(OpenTelemetrySnapshot source) => new()
    {
        ResourceServiceName = source.ResourceServiceName,
        ResourceServiceVersion = source.ResourceServiceVersion,
        DeploymentEnvironment = source.DeploymentEnvironment,
        TimestampUtc = source.TimestampUtc,
        MeterPatterns = Array.AsReadOnly(source.MeterPatterns.ToArray()),
        SamplesPerSeries = source.SamplesPerSeries,
        MaxTagSetsPerInstrument = source.MaxTagSetsPerInstrument,
        Instruments = Array.AsReadOnly(source.Instruments.Select(Detach).ToArray())
    };

    private static InstrumentSnapshot Detach(InstrumentSnapshot source) => new()
    {
        InstrumentId = source.InstrumentId,
        MeterName = source.MeterName,
        MeterVersion = source.MeterVersion,
        Name = source.Name,
        Kind = source.Kind,
        Unit = source.Unit,
        Description = source.Description,
        IsObservable = source.IsObservable,
        Overflow = source.Overflow,
        DroppedSamples = source.DroppedSamples,
        TagSets = Array.AsReadOnly(source.TagSets.Select(Detach).ToArray())
    };

    private static TagSetSamples Detach(TagSetSamples source) => new()
    {
        Key = source.Key,
        Tags = new ReadOnlyDictionary<string, string>(source.Tags.ToDictionary(
            static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal)),
        CurrentValue = source.CurrentValue,
        Delta = source.Delta,
        LastUpdatedUtc = source.LastUpdatedUtc,
        // Point records contain immutable scalar values and preserve the exact source observation identity.
        Samples = Array.AsReadOnly(source.Samples.ToArray()),
        Histogram = source.Histogram is { } histogram ? Detach(histogram) : null
    };

    private static HistogramSnapshot Detach(HistogramSnapshot source) => new()
    {
        Count = source.Count,
        NonFiniteSampleCount = source.NonFiniteSampleCount,
        Min = source.Min,
        Max = source.Max,
        Average = source.Average,
        P50 = source.P50,
        P95 = source.P95,
        P99 = source.P99,
        Buckets = Array.AsReadOnly(source.Buckets.Select(static bucket => new HistogramBucketSnapshot
        {
            LowerBound = bucket.LowerBound,
            UpperBound = bucket.UpperBound,
            Count = bucket.Count
        }).ToArray())
    };
}
