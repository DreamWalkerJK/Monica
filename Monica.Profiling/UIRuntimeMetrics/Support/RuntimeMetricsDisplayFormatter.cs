using System.Globalization;

namespace Monica.Profiling.UIRuntimeMetrics.Support;

/// <summary>
/// Formats runtime observations without changing their measurement or collection semantics.
/// </summary>
internal static class RuntimeMetricsDisplayFormatter
{
    private const string UNAVAILABLE = "—";
    private const double BYTES_PER_KIBIBYTE = 1024d;
    private const double BYTES_PER_MEBIBYTE = BYTES_PER_KIBIBYTE * BYTES_PER_KIBIBYTE;

    /// <summary>
    /// Formats a measured percentage, or an unavailable marker when no observation exists.
    /// </summary>
    public static string Percent(double? value) =>
        value.HasValue ? $"{value.Value.ToString("0.0", CultureInfo.CurrentCulture)}%" : UNAVAILABLE;

    /// <summary>
    /// Formats a value already expressed in decimal megabytes, without applying a byte conversion.
    /// </summary>
    public static string Megabytes(double? value) =>
        value.HasValue ? $"{value.Value.ToString("0.0", CultureInfo.CurrentCulture)} MB" : UNAVAILABLE;

    /// <summary>
    /// Formats a signed change already expressed in decimal megabytes; zero carries no direction.
    /// </summary>
    public static string SignedMegabytes(double value) =>
        $"{value.ToString("+0.0;-0.0;0.0", CultureInfo.CurrentCulture)} MB";

    /// <summary>
    /// Formats the collector-reported byte rate using binary KiB/s or MiB/s units when appropriate.
    /// The value is not normalized by either the collector interval or the view refresh interval.
    /// </summary>
    public static string ReportedAllocationRate(double? bytesPerSecond)
    {
        if (!bytesPerSecond.HasValue)
        {
            return UNAVAILABLE;
        }

        var value = bytesPerSecond.Value;
        var absoluteValue = Math.Abs(value);
        var (scaledValue, unit) = absoluteValue switch
        {
            >= BYTES_PER_MEBIBYTE => (value / BYTES_PER_MEBIBYTE, "MiB/s"),
            >= BYTES_PER_KIBIBYTE => (value / BYTES_PER_KIBIBYTE, "KiB/s"),
            _ => (value, "B/s")
        };

        return $"{scaledValue.ToString("0.##", CultureInfo.CurrentCulture)} {unit}";
    }

    /// <summary>
    /// Formats a sample's host-local time with milliseconds, or an unavailable marker.
    /// Unspecified sample timestamps are interpreted as UTC, matching the collector contract.
    /// </summary>
    public static string LocalTime(DateTime? timestamp) =>
        timestamp.HasValue
            ? AsUtc(timestamp.Value).ToLocalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)
            : UNAVAILABLE;

    /// <summary>
    /// Formats a host-local date, time and explicit UTC offset, or an unavailable marker.
    /// </summary>
    public static string LocalDateTime(DateTimeOffset? timestamp) =>
        timestamp?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture) ?? UNAVAILABLE;

    /// <summary>
    /// Formats the exact sample timestamp as round-trip UTC text.
    /// Unspecified sample timestamps are interpreted as UTC, matching the collector contract.
    /// </summary>
    public static string MachineTime(DateTime timestamp) =>
        AsUtc(timestamp).ToString("O", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats an unscaled measurement as culture-independent, round-trip numeric evidence.
    /// </summary>
    public static string Raw(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static DateTime AsUtc(DateTime timestamp) =>
        timestamp.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(timestamp, DateTimeKind.Utc)
            : timestamp.ToUniversalTime();
}
