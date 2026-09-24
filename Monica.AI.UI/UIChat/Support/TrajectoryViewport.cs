namespace Monica.AI.UI.UIChat.Support;

/// <summary>A bounded time window for deterministic timeline zoom, interval focus, and panning.</summary>
internal sealed class TrajectoryViewport
{
    internal double Start { get; private set; }
    internal double End { get; private set; } = 1;
    internal double Extent { get; private set; } = 1;
    internal double Width => End - Start;

    internal void UpdateExtent(double extent)
    {
        var followedEnd = Math.Abs(End - Extent) < .001;
        var full = Start == 0 && followedEnd;
        Extent = Math.Max(1, extent);
        if (full) { Start = 0; End = Extent; }
        else if (followedEnd) End = Extent;
        Clamp();
    }

    internal void Reset() { Start = 0; End = Extent; }
    internal void Zoom(double factor)
    {
        var center = (Start + End) / 2;
        var width = Math.Clamp(Width * factor, Math.Min(.05, Extent), Extent);
        Focus(center - width / 2, center + width / 2);
    }
    internal void Pan(double fraction) => Focus(Start + Width * fraction, End + Width * fraction);
    internal void Focus(double start, double end)
    {
        if (end <= start) return;
        Start = start; End = end; Clamp();
    }
    private void Clamp()
    {
        var width = Math.Clamp(End - Start, Math.Min(.05, Extent), Extent);
        Start = Math.Clamp(Start, 0, Math.Max(0, Extent - width));
        End = Start + width;
    }
}
