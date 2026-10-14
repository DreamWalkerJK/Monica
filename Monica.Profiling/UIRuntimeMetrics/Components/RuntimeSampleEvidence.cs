using Monica.Profiling.RuntimeMetrics.Models;

namespace Monica.Profiling.UIRuntimeMetrics.Components;

/// <summary>
/// Retains one observation and the successful view read that supplied it for a frozen inspector.
/// Later page refreshes and collector history eviction do not replace the selected observation.
/// </summary>
/// <param name="Point">The selected, init-only runtime observation with its original UTC sample timestamp.</param>
/// <param name="CapturedAt">The time of the successful view read that supplied the observation.</param>
public sealed record RuntimeSampleEvidence(RuntimeMetricsPoint Point, DateTimeOffset CapturedAt);
