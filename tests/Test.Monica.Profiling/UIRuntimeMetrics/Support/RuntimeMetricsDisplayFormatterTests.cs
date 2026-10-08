using System.Globalization;
using AwesomeAssertions;
using Monica.Profiling.UIRuntimeMetrics.Support;
using Xunit;

namespace Test.Monica.Profiling.UIRuntimeMetrics.Support;

public sealed class RuntimeMetricsDisplayFormatterTests
{
    [Fact]
    public void Format_WhenObservationIsMissing_ShouldDistinguishItFromMeasuredZero()
    {
        RuntimeMetricsDisplayFormatter.Percent(null).Should().Be("—");
        RuntimeMetricsDisplayFormatter.Megabytes(null).Should().Be("—");
        RuntimeMetricsDisplayFormatter.ReportedAllocationRate(null).Should().Be("—");
        RuntimeMetricsDisplayFormatter.LocalTime(null).Should().Be("—");
        RuntimeMetricsDisplayFormatter.LocalDateTime(null).Should().Be("—");

        RuntimeMetricsDisplayFormatter.Percent(0).Should().Be($"{0d:0.0}%");
        RuntimeMetricsDisplayFormatter.Megabytes(0).Should().Be($"{0d:0.0} MB");
        RuntimeMetricsDisplayFormatter.ReportedAllocationRate(0).Should().Be("0 B/s");
        RuntimeMetricsDisplayFormatter.SignedMegabytes(0).Should().Be($"{0d:0.0} MB");
    }

    [Theory]
    [InlineData(1023d, 1023d, "B/s")]
    [InlineData(1024d, 1d, "KiB/s")]
    [InlineData(1536d, 1.5d, "KiB/s")]
    [InlineData(1_048_576d, 1d, "MiB/s")]
    [InlineData(1_572_864d, 1.5d, "MiB/s")]
    [InlineData(-1024d, -1d, "KiB/s")]
    [InlineData(-1_572_864d, -1.5d, "MiB/s")]
    public void ReportedAllocationRate_WhenCrossingBinaryThresholds_ShouldScaleWithoutChangingTheReportedRate(
        double reportedBytesPerSecond, double scaledValue, string expectedUnit)
    {
        RuntimeMetricsDisplayFormatter.ReportedAllocationRate(reportedBytesPerSecond)
            .Should().Be($"{scaledValue.ToString("0.##", CultureInfo.CurrentCulture)} {expectedUnit}");
    }

    [Theory]
    [InlineData("en-US", "1.3%", "1.3 MB", "+1.3 MB", "-1.3 MB", "1.5 KiB/s")]
    [InlineData("fr-FR", "1,3%", "1,3 MB", "+1,3 MB", "-1,3 MB", "1,5 KiB/s")]
    public void Format_WhenCultureChanges_ShouldLocalizeDisplayAndKeepAlreadyDecimalMegabytesUnscaled(
        string cultureName, string percent, string megabytes, string increase, string decrease, string allocation)
    {
        var priorCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

            RuntimeMetricsDisplayFormatter.Percent(1.26).Should().Be(percent);
            RuntimeMetricsDisplayFormatter.Megabytes(1.26).Should().Be(megabytes);
            RuntimeMetricsDisplayFormatter.SignedMegabytes(1.26).Should().Be(increase);
            RuntimeMetricsDisplayFormatter.SignedMegabytes(-1.26).Should().Be(decrease);
            RuntimeMetricsDisplayFormatter.ReportedAllocationRate(1536).Should().Be(allocation);
        }
        finally
        {
            CultureInfo.CurrentCulture = priorCulture;
        }
    }

    [Fact]
    public void Raw_WhenDisplayCultureUsesComma_ShouldPreserveUnscaledRoundTripEvidence()
    {
        var priorCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            const double rawMeasurement = 1_234_567.8901234567;

            var evidence = RuntimeMetricsDisplayFormatter.Raw(rawMeasurement);

            evidence.Should().Be("1234567.8901234567");
            double.Parse(evidence, CultureInfo.InvariantCulture).Should().Be(rawMeasurement);
        }
        finally
        {
            CultureInfo.CurrentCulture = priorCulture;
        }
    }

    [Fact]
    public void FormatTime_WhenSampleIsUtcOrUnspecified_ShouldRetainTheInstantAndMilliseconds()
    {
        var utc = new DateTime(2026, 10, 9, 3, 4, 5, 678, DateTimeKind.Utc).AddTicks(1234);
        var unspecified = DateTime.SpecifyKind(utc, DateTimeKind.Unspecified);
        var local = utc.ToLocalTime();

        foreach (var timestamp in new[] { utc, unspecified, local })
        {
            RuntimeMetricsDisplayFormatter.LocalTime(timestamp).Should().Be(
                local.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture));
            var evidence = RuntimeMetricsDisplayFormatter.MachineTime(timestamp);
            evidence.Should().Be("2026-10-09T03:04:05.6781234Z");
            DateTime.ParseExact(evidence, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                .Should().Be(utc);
        }
    }

    [Fact]
    public void LocalDateTime_WhenCaptureHasAnOffset_ShouldShowHostLocalOffsetWithoutChangingTheInstant()
    {
        var capturedAt = new DateTimeOffset(2026, 10, 9, 8, 9, 10, 321, TimeSpan.FromHours(-4));

        var formatted = RuntimeMetricsDisplayFormatter.LocalDateTime(capturedAt);

        formatted.Should().Be(capturedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture));
        DateTimeOffset.ParseExact(formatted, "yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture)
            .Should().Be(capturedAt);
    }
}
