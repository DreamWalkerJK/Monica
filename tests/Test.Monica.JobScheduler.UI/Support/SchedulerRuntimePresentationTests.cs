using Monica.JobScheduler.UI.UIJobScheduler.Support;
using Xunit;

namespace Test.Monica.JobScheduler.UI.Support;

public sealed class SchedulerRuntimePresentationTests
{
    private static readonly DateTimeOffset Capture = new(2026, 10, 11, 8, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Threshold = TimeSpan.FromSeconds(15);

    [Fact]
    public void ClassifyOwnerObservation_WhenObservationIsAheadOfCapture_ShouldExposeClockOrderDiscrepancy()
    {
        Assert.Equal(SchedulerOwnerObservationFreshness.AheadOfCapture,
            SchedulerRuntimePresentation.ClassifyOwnerObservation(Capture, Capture.AddTicks(1), Threshold));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(150000000L)]
    public void ClassifyOwnerObservation_WhenAgeIsAtOrInsideThreshold_ShouldBeRecent(long ageTicks)
    {
        Assert.Equal(SchedulerOwnerObservationFreshness.Recent,
            SchedulerRuntimePresentation.ClassifyOwnerObservation(Capture, Capture.AddTicks(-ageTicks), Threshold));
    }

    [Fact]
    public void ClassifyOwnerObservation_WhenAgeExceedsThresholdByOneTick_ShouldBeStale()
    {
        Assert.Equal(SchedulerOwnerObservationFreshness.Stale,
            SchedulerRuntimePresentation.ClassifyOwnerObservation(Capture, Capture - Threshold - TimeSpan.FromTicks(1), Threshold));
    }

    [Fact]
    public void ClassifyOwnerObservation_WhenCaptureIsRetained_ShouldKeepItsClassificationUntilNewEvidenceIsAccepted()
    {
        var observation = Capture - Threshold;
        var accepted = SchedulerRuntimePresentation.ClassifyOwnerObservation(Capture, observation, Threshold);
        var newerCapture = Capture.AddMinutes(1);

        Assert.Equal(SchedulerOwnerObservationFreshness.Recent, accepted);
        Assert.Equal(accepted, SchedulerRuntimePresentation.ClassifyOwnerObservation(Capture.ToOffset(TimeSpan.FromHours(8)), observation, Threshold));
        Assert.Equal(SchedulerOwnerObservationFreshness.Stale,
            SchedulerRuntimePresentation.ClassifyOwnerObservation(newerCapture, observation, Threshold));
    }

    [Fact]
    public void FormatExactInterval_WhenReadableRoundingWouldLosePrecision_ShouldPreserveTicks()
    {
        var interval = TimeSpan.FromTicks(123456789);
        var exact = SchedulerRuntimePresentation.FormatExactInterval(interval);

        Assert.Equal("00:00:12.3456789", exact);
        Assert.Equal(interval, TimeSpan.ParseExact(exact, "c", System.Globalization.CultureInfo.InvariantCulture));
    }
}
