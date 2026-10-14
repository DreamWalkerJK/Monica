using System.Globalization;
using System.Text;
using Monica.OpenTelemetry.InProcessCollector.Models;

namespace Monica.OpenTelemetry.UI.UIOpenTelemetry.State;

/// <summary>
/// Converts a diagnostic snapshot into fixed Prometheus gauge families for clipboard export.
/// </summary>
/// <remarks>
/// These gauges describe current values and retained windows; the SDK Prometheus endpoint owns cumulative scrape metrics.
/// Native instrument metadata remains in labels. User label names are <c>tag_u</c> followed by four uppercase hexadecimal
/// digits per UTF-16 code unit, so empty keys and even distinct unpaired surrogate keys remain distinguishable.
/// The encoded tag-set key preserves absent-versus-empty tags, which Prometheus otherwise treats as equivalent.
/// Missing collector instrument IDs receive an export-local ordinal with <c>instrument_id_source="snapshot"</c>.
/// </remarks>
internal static class PrometheusTextExporter
{
    private const string FAMILY_PREFIX = "monica_diagnostic_";

    public static string Export(OpenTelemetrySnapshot snapshot)
    {
        var builder = new StringBuilder();
        builder.Append("# Monica diagnostic current-state export. Use the SDK Prometheus endpoint for cumulative scrape metrics.\n")
            .Append("# Snapshot timestamp: ")
            .Append(snapshot.TimestampUtc.ToString("O", CultureInfo.InvariantCulture))
            .Append('\n');

        var series = snapshot.Instruments
            .Select(static (instrument, index) => (Instrument: instrument, Index: index))
            .OrderBy(static item => item.Instrument.MeterName, StringComparer.Ordinal)
            .ThenBy(static item => item.Instrument.MeterVersion, StringComparer.Ordinal)
            .ThenBy(static item => item.Instrument.Name, StringComparer.Ordinal)
            .ThenBy(static item => item.Instrument.InstrumentId, StringComparer.Ordinal)
            .SelectMany(item => item.Instrument.TagSets
                .OrderBy(static tagSet => tagSet.Key, StringComparer.Ordinal)
                .Select(tagSet => new DiagnosticSeries(
                    tagSet,
                    CreateLabels(snapshot, item.Instrument, tagSet, item.Index))))
            .ToList();

        AppendGauge(builder, "current_value", "Latest retained diagnostic value; direct counters retain running totals.", series,
            static tagSet => FormatNumber(tagSet.CurrentValue));
        AppendGauge(builder, "delta", "Difference between the two latest retained values, or zero with fewer than two.", series,
            static tagSet => FormatNumber(tagSet.Delta));
        AppendGauge(builder, "retained_sample_count", "Number of retained values in the diagnostic window.", series,
            static tagSet => tagSet.Samples.Count.ToString(CultureInfo.InvariantCulture));
        AppendGauge(builder, "retained_finite_sample_count", "Number of finite retained values in the diagnostic window.", series,
            static tagSet => tagSet.FiniteSampleCount.ToString(CultureInfo.InvariantCulture));
        AppendGauge(builder, "retained_non_finite_sample_count", "Number of non-finite retained values in the diagnostic window.", series,
            static tagSet => tagSet.NonFiniteSampleCount.ToString(CultureInfo.InvariantCulture));

        var histograms = series.Where(static item => item.TagSet.Histogram is not null).ToList();
        AppendGauge(builder, "histogram_window_count", "Number of finite retained samples used by the diagnostic histogram.", histograms,
            static tagSet => tagSet.Histogram!.Count.ToString(CultureInfo.InvariantCulture));
        AppendGauge(builder, "histogram_window_non_finite_sample_count", "Number of retained raw samples excluded from finite histogram statistics.", histograms,
            static tagSet => tagSet.Histogram!.NonFiniteSampleCount.ToString(CultureInfo.InvariantCulture));
        AppendGauge(builder, "histogram_window_min", "Minimum finite value in the retained diagnostic histogram window.", histograms,
            static tagSet => FormatNumber(tagSet.Histogram!.Min));
        AppendGauge(builder, "histogram_window_max", "Maximum finite value in the retained diagnostic histogram window.", histograms,
            static tagSet => FormatNumber(tagSet.Histogram!.Max));
        AppendGauge(builder, "histogram_window_mean", "Mean finite value in the retained diagnostic histogram window.", histograms,
            static tagSet => FormatNumber(tagSet.Histogram!.Average));
        AppendGauge(builder, "histogram_window_p50", "Approximate 50th percentile of finite retained diagnostic histogram samples.", histograms,
            static tagSet => FormatNumber(tagSet.Histogram!.P50));
        AppendGauge(builder, "histogram_window_p95", "Approximate 95th percentile of finite retained diagnostic histogram samples.", histograms,
            static tagSet => FormatNumber(tagSet.Histogram!.P95));
        AppendGauge(builder, "histogram_window_p99", "Approximate 99th percentile of finite retained diagnostic histogram samples.", histograms,
            static tagSet => FormatNumber(tagSet.Histogram!.P99));
        AppendHistogramBuckets(builder, histograms);

        return builder.ToString();
    }

    private static IReadOnlyDictionary<string, string> CreateLabels(
        OpenTelemetrySnapshot snapshot,
        InstrumentSnapshot instrument,
        TagSetSamples tagSet,
        int instrumentIndex)
    {
        var labels = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["service_name"] = snapshot.ResourceServiceName,
            ["meter_name"] = instrument.MeterName,
            ["instrument_name"] = instrument.Name,
            ["instrument_kind"] = instrument.Kind,
            ["instrument_is_observable"] = instrument.IsObservable ? "true" : "false",
            ["instrument_id"] = string.IsNullOrEmpty(instrument.InstrumentId) ? instrumentIndex.ToString(CultureInfo.InvariantCulture) : instrument.InstrumentId,
            ["instrument_id_source"] = string.IsNullOrEmpty(instrument.InstrumentId) ? "snapshot" : "collector",
            ["tag_set_key"] = EncodeCodeUnits(tagSet.Key)
        };

        AddOptionalLabel(labels, "service_version", snapshot.ResourceServiceVersion);
        AddOptionalLabel(labels, "deployment_environment", snapshot.DeploymentEnvironment);
        AddOptionalLabel(labels, "meter_version", instrument.MeterVersion);
        AddOptionalLabel(labels, "instrument_unit", instrument.Unit);
        AddOptionalLabel(labels, "instrument_description", instrument.Description);

        foreach (var tag in tagSet.Tags)
        {
            labels.Add("tag_" + EncodeCodeUnits(tag.Key), tag.Value);
        }

        return labels;
    }

    private static void AddOptionalLabel(IDictionary<string, string> labels, string key, string? value)
    {
        if (value is not null)
        {
            labels.Add(key, value);
        }
    }

    private static void AppendGauge(
        StringBuilder builder,
        string familySuffix,
        string description,
        IReadOnlyList<DiagnosticSeries> series,
        Func<TagSetSamples, string> getValue)
    {
        if (series.Count == 0)
        {
            return;
        }

        var family = FAMILY_PREFIX + familySuffix;
        AppendMetadata(builder, family, description);
        foreach (var item in series)
        {
            AppendSample(builder, family, item.Labels, getValue(item.TagSet));
        }

        builder.Append('\n');
    }

    private static void AppendHistogramBuckets(StringBuilder builder, IReadOnlyList<DiagnosticSeries> histograms)
    {
        if (!histograms.Any(static item => item.TagSet.Histogram!.Buckets.Count > 0))
        {
            return;
        }

        const string family = FAMILY_PREFIX + "histogram_window_bucket_count";
        AppendMetadata(builder, family,
            "Finite retained samples in one noncumulative bucket; first [lower_bound,upper_bound], later (lower_bound,upper_bound].");
        foreach (var item in histograms)
        {
            var buckets = item.TagSet.Histogram!.Buckets;
            for (var index = 0; index < buckets.Count; index++)
            {
                var bucket = buckets[index];
                var labels = new SortedDictionary<string, string>(StringComparer.Ordinal);
                foreach (var label in item.Labels)
                {
                    labels.Add(label.Key, label.Value);
                }

                labels.Add("bucket_index", index.ToString(CultureInfo.InvariantCulture));
                labels.Add("lower_bound", FormatNumber(bucket.LowerBound));
                labels.Add("upper_bound", FormatNumber(bucket.UpperBound));
                labels.Add("lower_inclusive", index == 0 ? "true" : "false");
                labels.Add("upper_inclusive", "true");
                AppendSample(builder, family, labels, bucket.Count.ToString(CultureInfo.InvariantCulture));
            }
        }

        builder.Append('\n');
    }

    private static void AppendMetadata(StringBuilder builder, string family, string description)
    {
        builder.Append("# HELP ").Append(family).Append(' ').Append(description).Append('\n')
            .Append("# TYPE ").Append(family).Append(" gauge\n");
    }

    private static void AppendSample(
        StringBuilder builder,
        string family,
        IReadOnlyDictionary<string, string> labels,
        string value)
    {
        builder.Append(family).Append('{')
            .AppendJoin(',', labels.Select(static label => $"{label.Key}=\"{EscapeLabelValue(label.Value)}\""))
            .Append("} ").Append(value).Append('\n');
    }

    private static string FormatNumber(double value)
    {
        return value switch
        {
            double.PositiveInfinity => "+Inf",
            double.NegativeInfinity => "-Inf",
            _ when double.IsNaN(value) => "NaN",
            _ => value.ToString("G17", CultureInfo.InvariantCulture)
        };
    }

    private static string EscapeLabelValue(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private static string EncodeCodeUnits(string value)
    {
        var encoded = new StringBuilder("u", 1 + value.Length * 4);
        foreach (var codeUnit in value)
        {
            encoded.Append(((int)codeUnit).ToString("X4", CultureInfo.InvariantCulture));
        }

        return encoded.ToString();
    }

    private sealed record DiagnosticSeries(TagSetSamples TagSet, IReadOnlyDictionary<string, string> Labels);
}
