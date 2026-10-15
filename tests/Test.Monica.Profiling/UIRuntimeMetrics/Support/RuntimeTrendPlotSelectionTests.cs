using AwesomeAssertions;
using Monica.Profiling.RuntimeMetrics.Models;
using Monica.Profiling.UIRuntimeMetrics.Support;
using Xunit;

namespace Test.Monica.Profiling.UIRuntimeMetrics.Support;

/// <summary>Exercises selection identity and refresh reconciliation without browser events or component rendering.</summary>
public sealed class RuntimeTrendPlotSelectionTests
{
    private static readonly DateTime Origin = new(2026, 10, 15, 1, 2, 3, DateTimeKind.Utc);

    [Fact]
    public void Reconcile_WhenRangeHasNotBeenUsed_ShouldFollowTheNewestObservationWithoutOpeningAPreview()
    {
        var first = Cpu(0);
        var second = Cpu(1);
        var newest = Cpu(2);
        var selection = new RuntimeTrendPlotSelection();

        selection.Reconcile(Project(first, second));
        selection.RangeSample!.Point.Should().BeSameAs(second);
        selection.RangeIndex.Should().Be(1);
        selection.Reconcile(Project(first, second, newest));

        selection.RangeSample!.Point.Should().BeSameAs(newest);
        selection.RangeIndex.Should().Be(2);
        selection.ActiveSample.Should().BeNull();
        selection.HasFocus.Should().BeFalse();
        selection.WasEvicted.Should().BeFalse();
    }

    [Fact]
    public void Reconcile_WhenSelectedReferenceRemainsButItsOrdinalMoves_ShouldRetainTheObservationAtItsNewOrdinal()
    {
        var oldest = Cpu(0);
        var selected = Cpu(1);
        var last = Cpu(2);
        var newest = Cpu(3);
        var original = Project(oldest, selected, last);
        var selection = new RuntimeTrendPlotSelection();
        selection.Reconcile(original);
        selection.TrySelectRange(original, 1).Should().BeTrue();
        var refreshed = Project(selected, last, newest);

        selection.Reconcile(refreshed);

        selection.RangeSample.Should().BeSameAs(refreshed.Samples[0]);
        selection.RangeSample!.Point.Should().BeSameAs(selected);
        selection.RangeIndex.Should().Be(0);
        selection.ActiveSample.Should().BeSameAs(selection.RangeSample);
        selection.HasFocus.Should().BeTrue();
        selection.WasEvicted.Should().BeFalse();
    }

    [Fact]
    public void Reconcile_WhenSamplesHaveTheSameTimestampAndValue_ShouldKeepReferenceIdentityDistinct()
    {
        var first = Cpu(0, 25);
        var coincident = Cpu(0, 25);
        var original = Project(first, coincident);
        var selection = new RuntimeTrendPlotSelection();
        selection.Reconcile(original);
        selection.TrySelectRange(original, 0).Should().BeTrue();

        selection.Reconcile(Project(coincident, first));

        selection.RangeSample!.Point.Should().BeSameAs(first);
        selection.RangeSample!.Point.Should().NotBeSameAs(coincident);
        selection.RangeIndex.Should().Be(1);
        selection.WasEvicted.Should().BeFalse();
    }

    [Fact]
    public void Reconcile_WhenRefreshContainsOnlyClonedValues_ShouldReportEvictionAndSelectTheOldestRetainedReference()
    {
        var first = Cpu(0, 10);
        var selected = Cpu(1, 20);
        var last = Cpu(2, 30);
        var original = Project(first, selected, last);
        var selection = new RuntimeTrendPlotSelection();
        selection.Reconcile(original);
        selection.TrySelectRange(original, 1).Should().BeTrue();
        selection.TryPreview(original.Samples[2]).Should().BeTrue();
        var refreshed = Project(Cpu(0, 10), Cpu(1, 20), Cpu(2, 30));

        selection.Reconcile(refreshed);

        selection.RangeSample.Should().BeSameAs(refreshed.Samples[0]);
        selection.RangeSample!.Point.Should().NotBeSameAs(first);
        selection.RangeSample!.Point.Should().NotBeSameAs(selected);
        selection.RangeIndex.Should().Be(0);
        selection.PointerSample.Should().BeNull();
        selection.ActiveSample.Should().BeSameAs(selection.RangeSample);
        selection.HasFocus.Should().BeTrue();
        selection.WasEvicted.Should().BeTrue();
    }

    [Fact]
    public void Reconcile_WhenSelectedObservationIsEvicted_ShouldClearPointerAndSelectTheOldestRetainedObservation()
    {
        var evicted = Cpu(0);
        var retained = Cpu(1);
        var original = Project(evicted, retained);
        var selection = new RuntimeTrendPlotSelection();
        selection.Reconcile(original);
        selection.TrySelectRange(original, 0).Should().BeTrue();
        selection.TryPreview(original.Samples[1]).Should().BeTrue();

        selection.Reconcile(Project(retained, Cpu(2)));

        selection.RangeSample!.Point.Should().BeSameAs(retained);
        selection.RangeIndex.Should().Be(0);
        selection.PointerSample.Should().BeNull();
        selection.ActiveSample.Should().BeSameAs(selection.RangeSample);
        selection.WasEvicted.Should().BeTrue();
    }

    [Fact]
    public void TrySelectRange_WhenQueuedOrdinalComesFromAnOlderProjection_ShouldResolveItsOriginalReferenceAtTheCurrentOrdinal()
    {
        var oldest = Cpu(0);
        var intended = Cpu(1);
        var last = Cpu(2);
        var rendered = Project(oldest, intended, last);
        var selection = new RuntimeTrendPlotSelection();
        selection.Reconcile(rendered);
        selection.Reconcile(Project(intended, last, Cpu(3)));

        var accepted = selection.TrySelectRange(rendered, 1);

        accepted.Should().BeTrue();
        selection.RangeSample!.Point.Should().BeSameAs(intended);
        selection.RangeSample!.Point.Should().NotBeSameAs(last);
        selection.RangeIndex.Should().Be(0);
        selection.WasEvicted.Should().BeFalse();
    }

    [Fact]
    public void TrySelectRange_WhenQueuedObservationWasEvictedOrOrdinalIsInvalid_ShouldRejectInputWithoutRetargeting()
    {
        var evicted = Cpu(0);
        var retained = Cpu(1);
        var rendered = Project(evicted, retained);
        var selection = new RuntimeTrendPlotSelection();
        selection.Reconcile(rendered);
        selection.TrySelectRange(rendered, 0).Should().BeTrue();
        selection.Reconcile(Project(retained, Cpu(2)));
        var current = selection.RangeSample;

        selection.TrySelectRange(rendered, 0).Should().BeFalse();
        selection.TrySelectRange(rendered, -1).Should().BeFalse();
        selection.TrySelectRange(rendered, rendered.Samples.Count).Should().BeFalse();
        selection.TryPreview(rendered.Samples[0]).Should().BeFalse();

        selection.RangeSample.Should().BeSameAs(current);
        selection.RangeSample!.Point.Should().BeSameAs(retained);
        selection.RangeIndex.Should().Be(0);
        selection.PointerSample.Should().BeNull();
        selection.WasEvicted.Should().BeTrue();
    }

    [Fact]
    public void FocusRange_WhenPointerWasPreviewingAnotherSample_ShouldRestoreAndRetainTheIndependentRangeSelection()
    {
        var first = Cpu(0);
        var selected = Cpu(1);
        var projection = Project(first, selected);
        var selection = new RuntimeTrendPlotSelection();
        selection.Reconcile(projection);
        selection.TryPreview(projection.Samples[0]).Should().BeTrue();
        selection.ActiveSample!.Point.Should().BeSameAs(first);

        selection.FocusRange();

        selection.PointerSample.Should().BeNull();
        selection.ActiveSample!.Point.Should().BeSameAs(selected);
        selection.HasFocus.Should().BeTrue();
        selection.TryPreview(projection.Samples[0]).Should().BeTrue();
        selection.ActiveSample!.Point.Should().BeSameAs(first);
        selection.RangeSample!.Point.Should().BeSameAs(selected);

        selection.ClearPointer();

        selection.ActiveSample!.Point.Should().BeSameAs(selected);
        selection.RangeIndex.Should().Be(1);
        selection.BlurRange();
        selection.ActiveSample.Should().BeNull();
        selection.HasFocus.Should().BeFalse();
        selection.RangeSample!.Point.Should().BeSameAs(selected);
    }

    [Fact]
    public void ClearPointer_WhenAnOlderRegionLeavesAfterANewerPreview_ShouldRetainTheNewerPointerOwner()
    {
        var first = Cpu(0);
        var newerPreview = Cpu(1);
        var rendered = Project(first, newerPreview);
        var selection = new RuntimeTrendPlotSelection();
        selection.Reconcile(rendered);
        selection.TryPreview(rendered.Samples[0]).Should().BeTrue();
        selection.TryPreview(rendered.Samples[1]).Should().BeTrue();

        selection.ClearPointer(rendered.Samples[0]);

        selection.PointerSample.Should().BeSameAs(rendered.Samples[1]);
        selection.ActiveSample!.Point.Should().BeSameAs(newerPreview);
        selection.Reconcile(Project(newerPreview, Cpu(2)));
        selection.ClearPointer(rendered.Samples[1]);
        selection.PointerSample.Should().BeNull();
        selection.ActiveSample.Should().BeNull();
    }

    [Fact]
    public void Dismiss_WhenRangeIsFocused_ShouldHidePreviewThroughRefreshWithoutLosingFocusOrSelection()
    {
        var first = Cpu(0);
        var selected = Cpu(1);
        var projection = Project(first, selected);
        var selection = new RuntimeTrendPlotSelection();
        selection.Reconcile(projection);
        selection.TrySelectRange(projection, 1).Should().BeTrue();
        selection.TryPreview(projection.Samples[0]).Should().BeTrue();

        selection.Dismiss();
        selection.Reconcile(Project(first, selected, Cpu(2)));

        selection.ActiveSample.Should().BeNull();
        selection.PointerSample.Should().BeNull();
        selection.RangeSample!.Point.Should().BeSameAs(selected);
        selection.RangeIndex.Should().Be(1);
        selection.HasFocus.Should().BeTrue();
        selection.FocusRange();
        selection.ActiveSample!.Point.Should().BeSameAs(selected);
    }

    [Fact]
    public void Reconcile_WhenRetainedPointerThenPointerEvictionOccurs_ShouldPreserveTheIndependentRangeSelection()
    {
        var selected = Cpu(0);
        var previewed = Cpu(1);
        var newest = Cpu(2);
        var original = Project(selected, previewed);
        var selection = new RuntimeTrendPlotSelection();
        selection.Reconcile(original);
        selection.TrySelectRange(original, 0).Should().BeTrue();
        selection.TryPreview(original.Samples[1]).Should().BeTrue();
        var refreshed = Project(selected, previewed, newest);

        selection.Reconcile(refreshed);

        selection.PointerSample.Should().BeSameAs(refreshed.Samples[1]);
        selection.ActiveSample!.Point.Should().BeSameAs(previewed);
        selection.RangeSample!.Point.Should().BeSameAs(selected);
        selection.Reconcile(Project(selected, newest));
        selection.PointerSample.Should().BeNull();
        selection.ActiveSample!.Point.Should().BeSameAs(selected);
        selection.WasEvicted.Should().BeFalse();
    }

    [Fact]
    public void Reconcile_WhenHistoryBecomesEmpty_ShouldClearRetiredReferencesAndResumeFollowingNewHistory()
    {
        var original = Project(Cpu(0), Cpu(1));
        var selection = new RuntimeTrendPlotSelection();
        selection.Reconcile(original);
        selection.TrySelectRange(original, 0).Should().BeTrue();
        selection.TryPreview(original.Samples[1]).Should().BeTrue();

        selection.Reconcile(Project());

        selection.RangeSample.Should().BeNull();
        selection.PointerSample.Should().BeNull();
        selection.ActiveSample.Should().BeNull();
        selection.RangeIndex.Should().Be(0);
        selection.HasFocus.Should().BeFalse();
        selection.WasEvicted.Should().BeFalse();
        selection.FocusRange();
        selection.HasFocus.Should().BeFalse();
        selection.ActiveSample.Should().BeNull();
        var newest = Cpu(3);
        selection.Reconcile(Project(Cpu(2), newest));
        selection.RangeSample!.Point.Should().BeSameAs(newest);
        selection.RangeIndex.Should().Be(1);
        selection.ActiveSample.Should().BeNull();
    }

    [Fact]
    public void TrySelectRange_WhenObservationCannotBePlotted_ShouldStillExposeItsOriginalEvidence()
    {
        var missing = Cpu(1, double.NaN);
        var projection = Project(Cpu(0), missing, Cpu(2));
        var selection = new RuntimeTrendPlotSelection();
        selection.Reconcile(projection);

        selection.TrySelectRange(projection, 1).Should().BeTrue();

        selection.RangeSample!.Point.Should().BeSameAs(missing);
        selection.ActiveSample!.Point.Should().BeSameAs(missing);
        selection.ActiveSample!.Y.Should().BeNull();
        double.IsNaN(selection.ActiveSample!.RawValue).Should().BeTrue();
        selection.TryPreview(projection.Samples[1]).Should().BeTrue();
        selection.PointerSample!.Point.Should().BeSameAs(missing);
    }

    private static RuntimeMetricsPoint Cpu(int seconds, double value = 10) =>
        new() { Timestamp = Origin.AddSeconds(seconds), CpuUsagePercent = value };

    private static RuntimeTrendPlotProjection Project(params RuntimeMetricsPoint[] samples) =>
        RuntimeTrendPlotProjection.Create(samples, RuntimeTrendMetric.Cpu);
}
