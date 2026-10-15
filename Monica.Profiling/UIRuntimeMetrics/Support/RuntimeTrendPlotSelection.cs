using Monica.Profiling.RuntimeMetrics.Models;

namespace Monica.Profiling.UIRuntimeMetrics.Support;

/// <summary>
/// Keeps transient plot selection tied to retained observation references rather than their moving ordinals.
/// The native browser's selected sample and pointer preview are separate; neither freezes inspector evidence.
/// </summary>
internal sealed class RuntimeTrendPlotSelection
{
    private RuntimeTrendPlotProjection _projection = RuntimeTrendPlotProjection.Create([], RuntimeTrendMetric.Cpu);
    private bool _hasRangeSelection;
    private bool _dismissed;

    public RuntimeTrendPlotSample? RangeSample { get; private set; }
    public RuntimeTrendPlotSample? PointerSample { get; private set; }
    public bool HasFocus { get; private set; }
    public bool WasEvicted { get; private set; }
    public int RangeIndex => RangeSample?.SourceIndex ?? 0;
    public RuntimeTrendPlotSample? ActiveSample => _dismissed ? null : PointerSample ?? (HasFocus ? RangeSample : null);

    public void Reconcile(RuntimeTrendPlotProjection projection)
    {
        _projection = projection;
        if (projection.Samples.Count == 0)
        {
            RangeSample = null;
            PointerSample = null;
            HasFocus = false;
            WasEvicted = false;
            _hasRangeSelection = false;
            _dismissed = false;
            return;
        }

        var retained = Find(RangeSample?.Point);
        if (!_hasRangeSelection)
        {
            RangeSample = projection.Samples[^1];
        }
        else if (retained is not null)
        {
            RangeSample = retained;
        }
        else
        {
            RangeSample = projection.Samples[0];
            PointerSample = null;
            WasEvicted = true;
        }
        PointerSample = Find(PointerSample?.Point);
    }

    public void FocusRange()
    {
        if (RangeSample is null) return;
        PointerSample = null;
        HasFocus = true;
        _hasRangeSelection = true;
        _dismissed = false;
    }

    public void BlurRange() => HasFocus = false;

    /// <summary>Resolves the ordinal against the render that produced the event before checking current retention.</summary>
    public bool TrySelectRange(RuntimeTrendPlotProjection renderedProjection, int index)
    {
        if (index < 0 || index >= renderedProjection.Samples.Count
            || Find(renderedProjection.Samples[index].Point) is not { } retained)
        {
            return false;
        }

        RangeSample = retained;
        PointerSample = null;
        HasFocus = true;
        _hasRangeSelection = true;
        WasEvicted = false;
        _dismissed = false;
        return true;
    }

    public bool TryPreview(RuntimeTrendPlotSample renderedSample)
    {
        if (Find(renderedSample.Point) is not { } retained) return false;
        PointerSample = retained;
        _dismissed = false;
        return true;
    }

    public void ClearPointer() => PointerSample = null;

    /// <summary>A queued leave for another observation cannot clear the current preview.</summary>
    public void ClearPointer(RuntimeTrendPlotSample renderedSample)
    {
        if (ReferenceEquals(PointerSample?.Point, renderedSample.Point)) PointerSample = null;
    }

    public void Dismiss()
    {
        PointerSample = null;
        _dismissed = true;
    }

    private RuntimeTrendPlotSample? Find(RuntimeMetricsPoint? point) => point is null ? null
        : _projection.Samples.FirstOrDefault(sample => ReferenceEquals(sample.Point, point));
}
