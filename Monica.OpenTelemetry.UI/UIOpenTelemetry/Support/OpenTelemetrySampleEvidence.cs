using Monica.OpenTelemetry.InProcessCollector.Models;

namespace Monica.OpenTelemetry.UI.UIOpenTelemetry.Support;

/// <summary>A frozen, exact retained observation selected from one successfully adopted page snapshot.</summary>
/// <param name="Instrument">The detached instrument, including its collector-lifetime identity and metadata.</param>
/// <param name="Series">The detached exact tag-set series containing the observation.</param>
/// <param name="Point">The unchanged binary64 value and source observation timestamp.</param>
/// <param name="SourceIndex">The observation's original index in this series' retained samples.</param>
/// <param name="SnapshotTimestampUtc">The collector's snapshot-build time in UTC.</param>
/// <param name="CapturedAt">The successful view-adoption time in UTC, independent of measurement time.</param>
public sealed record OpenTelemetrySampleEvidence(
    InstrumentSnapshot Instrument,
    TagSetSamples Series,
    MetricPointSnapshot Point,
    int SourceIndex,
    DateTimeOffset SnapshotTimestampUtc,
    DateTimeOffset CapturedAt);
