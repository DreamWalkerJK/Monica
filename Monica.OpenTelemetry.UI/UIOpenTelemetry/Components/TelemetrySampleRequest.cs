using Monica.OpenTelemetry.InProcessCollector.Models;

namespace Monica.OpenTelemetry.UI.UIOpenTelemetry.Components;

/// <summary>Identifies a rendered row in the exact selected adopted series, without a timestamp-only lookup.</summary>
/// <param name="Series">The exact detached source series whose row supplied the request.</param>
/// <param name="SourceIndex">The original index in that series' complete retained sample list.</param>
public sealed record TelemetrySampleRequest(TagSetSamples Series, int SourceIndex);
