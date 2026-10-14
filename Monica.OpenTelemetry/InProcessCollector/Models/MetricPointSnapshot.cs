using System.Text.Json.Serialization;

namespace Monica.OpenTelemetry.InProcessCollector.Models;

/// <summary>
/// A single numeric sample captured for one metric series.
/// </summary>
/// <param name="TimestampUtc">UTC timestamp when the sample was captured by the in-process collector.</param>
/// <param name="Value">
/// The retained binary64 value, including NaN/infinities. Integer/decimal measurements are converted to double;
/// precision outside that representation is not retained. Direct counter values accumulate admitted deltas.
/// </param>
public sealed record MetricPointSnapshot(DateTimeOffset TimestampUtc,
    [property: JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals)] double Value);
