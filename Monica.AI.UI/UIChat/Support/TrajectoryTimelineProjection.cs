namespace Monica.AI.UI.UIChat.Support;

/// <summary>A single event or an explicitly grouped cluster occupying one of three timeline lanes.</summary>
internal sealed record TrajectoryTimelineMark(int Lane, double Start, double End, IReadOnlyList<TrajectoryRecord> Records)
{
    internal bool IsEvent => Lane == 0 || Records.All(record => record.Step?.CompletedAt is null && record.Step?.Status != AI.Chat.Models.ChatExecutionStatus.Running);
    internal TrajectoryRecord First => Records[0];
}

internal static class TrajectoryTimelineProjection
{
    private const int BUCKETS_PER_LANE = 160;

    internal static IReadOnlyList<TrajectoryTimelineMark> Create(ChatTrajectoryProjection projection,
        TrajectoryViewport viewport, DateTimeOffset now)
    {
        if (projection.Origin is not { } origin) return [];
        // Very large histories are clustered by visible position. Every recorded event remains represented;
        // clusters are labeled as groups and resolve into individual records as the operator zooms in.
        return projection.Records.Where(record => record.StartedAt is not null)
            .Select(record => new
            {
                Record = record,
                Start = (record.StartedAt!.Value - origin).TotalSeconds,
                End = ((ChatTrajectoryProjection.End(record, now) ?? record.StartedAt!.Value) - origin).TotalSeconds
            })
            .Where(item => item.Start <= viewport.End && item.End >= viewport.Start)
            .GroupBy(item => (item.Record.Lane, Bucket: (int)Math.Clamp(
                (item.Start - viewport.Start) / viewport.Width * BUCKETS_PER_LANE, 0, BUCKETS_PER_LANE - 1)))
            .Select(group => new TrajectoryTimelineMark(group.Key.Lane, group.Min(item => item.Start),
                group.Max(item => item.End), group.Select(item => item.Record).ToArray()))
            .OrderBy(mark => mark.Start).ToArray();
    }
}
