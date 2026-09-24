using AwesomeAssertions;
using Monica.AI.Chat.Models;
using Monica.AI.Models;
using Monica.AI.UI.UIChat.Support;

namespace Test.Monica.AI.UI.UIChat.Components;

public sealed class ChatTrajectoryProjectionTests
{
    private static readonly DateTimeOffset START = new(2026, 9, 16, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Ledger_ShouldExposeOriginalRolesAndChangedInstructionsWithoutDuplicatingAssistantText()
    {
        var steps = new[]
        {
            Request("first", "turn-1", 1, 0, 2, "Initial instructions", "Think through the evidence"),
            new ChatExecutionStep { Id = "tool", TurnId = "turn-1", Sequence = 2,
                Kind = ChatExecutionStepKind.Tool, StartedAt = START.AddSeconds(2), CompletedAt = START.AddSeconds(3),
                Status = ChatExecutionStatus.Completed,
                Tool = new ChatToolExecution { CallId = "call", Name = "lookup", Arguments = "{\"query\":\"fact\"}", Result = "verified result" } },
            Request("answer", "turn-1", 3, 3, 5, "Initial instructions", "First answer"),
            Request("second", "turn-2", 4, 60, 64, "Updated instructions", "Second answer")
        };
        var projection = ChatTrajectoryProjection.Create([Turn("turn-1", 0), Turn("turn-2", 60)], steps, "Unsaved preference", START.AddMinutes(2));

        projection.TurnCount.Should().Be(2);
        projection.ModelCalls.Should().Be(3);
        projection.ToolCalls.Should().Be(1);
        projection.Records.Select(record => record.Role).Should().Equal(
            TrajectoryRole.System, TrajectoryRole.User, TrajectoryRole.Assistant, TrajectoryRole.Tool,
            TrajectoryRole.Assistant, TrajectoryRole.User, TrajectoryRole.System, TrajectoryRole.Assistant);
        projection.Records[0].Preview.Should().Be("Initial instructions");
        projection.Groups[1].TurnNumber.Should().Be(2);
        projection.Groups[1].Records[1].Preview.Should().Be("Updated instructions");
        projection.Records.Should().NotContain(record => record.Preview.Contains("Unsaved preference", StringComparison.Ordinal));
        projection.Records.Where(record => record.Role == TrajectoryRole.Tool).Single().Result.Should().Be("verified result");
        projection.RecordedDuration.Should().Be(TimeSpan.FromSeconds(9));
    }

    [Fact]
    public void Ledger_WhenInstructionsUseSystemMessages_ShouldDisplayTheActualRecordedPrompt()
    {
        var request = Request("request", "turn-1", 1, 0, 2, null, "Answer");
        request = request with { Request = request.Request! with
        {
            Messages = [new ChatContextMessage { Role = AIChatRole.System, Parts = [ChatContentPart.FromText("Recorded contributor instructions")] }]
        } };
        var projection = ChatTrajectoryProjection.Create([Turn("turn-1", 0)], [request], "Current draft preference", START.AddMinutes(1));

        projection.Records[0].Role.Should().Be(TrajectoryRole.System);
        projection.Records[0].Preview.Should().Be("Recorded contributor instructions");

        request = request with { Request = request.Request with { Instructions = "Recorded contributor instructions" } };
        ChatTrajectoryProjection.Create([Turn("turn-1", 0)], [request], null, START.AddMinutes(1))
            .Records[0].Preview.Should().Be("Recorded contributor instructions");
    }

    [Fact]
    public void Search_ShouldRevealMatchingRecordsInsideFoldedTurnsAndKeepOriginalTurnNumbers()
    {
        var projection = ChatTrajectoryProjection.Create([Turn("turn-1", 0), Turn("turn-2", 60)],
            [Request("first", "turn-1", 1, 0, 2, "Rules", "ordinary"),
             Request("second", "turn-2", 2, 60, 61, "Rules", "distinctive evidence")], null, START.AddMinutes(2));

        var rows = projection.Rows("distinctive", new HashSet<string> { "turn-1", "turn-2" });

        rows.Should().HaveCount(2);
        rows[0].Group!.TurnNumber.Should().Be(2);
        rows[1].Record!.Step!.Id.Should().Be("second");
        projection.Rows(null, new HashSet<string> { "turn-1", "turn-2" }).Should().HaveCount(3);
    }

    [Fact]
    public void Timing_ShouldCountOverlappingWorkOnceAndExcludeIdleTime()
    {
        var projection = ChatTrajectoryProjection.Create([Turn("turn-1", 0)],
            [Request("first", "turn-1", 1, 0, 10, null, ""),
             Request("overlap", "turn-1", 2, 4, 12, null, ""),
             Request("later", "turn-1", 3, 40, 45, null, "")], null, START.AddMinutes(1));

        projection.RecordedDuration.Should().Be(TimeSpan.FromSeconds(17));
        projection.ExtentSeconds.Should().Be(45);
    }

    [Fact]
    public void ManualCompaction_ShouldRemainBetweenTurnsAndCountItsActualProviderCall()
    {
        var summary = Request("summary", null, 2, 10, 12, null, "A useful summary");
        summary = summary with { Request = summary.Request! with { IsCompaction = true } };
        var compact = new ChatExecutionStep { Id = "compact", Sequence = 1, Kind = ChatExecutionStepKind.Compaction,
            StartedAt = START.AddSeconds(9), CompletedAt = START.AddSeconds(13), Status = ChatExecutionStatus.Completed };
        var projection = ChatTrajectoryProjection.Create([Turn("turn-1", 0), Turn("turn-2", 20)], [compact, summary], null, START.AddMinutes(1));

        projection.TurnCount.Should().Be(2);
        projection.Groups.Select(group => group.TurnNumber).Should().Equal(1, null, null, 2);
        projection.ModelCalls.Should().Be(1);
        projection.Records.Single(record => record.Step?.Id == "summary").HeadingKey.Should().Be("Workbench:SummaryRequest");
        projection.RecordedDuration.Should().Be(TimeSpan.FromSeconds(4));
    }

    [Fact]
    public void Timeline_ShouldRepresentEveryRecordedEventWhileBoundingDenseMarks()
    {
        var turns = Enumerable.Range(0, 600).Select(index => Turn($"turn-{index}", index / 10d)).ToArray();
        var steps = turns.Select((turn, index) => Request($"request-{index}", turn.Id, index + 1, index / 10d, index / 10d + .05, "Rules", "answer")).ToArray();
        var projection = ChatTrajectoryProjection.Create(turns, steps, null, START.AddMinutes(2));
        var viewport = new TrajectoryViewport();
        viewport.UpdateExtent(projection.ExtentSeconds);

        var marks = TrajectoryTimelineProjection.Create(projection, viewport, START.AddMinutes(2));

        marks.Should().HaveCountLessThanOrEqualTo(480);
        marks.SelectMany(mark => mark.Records).Should().BeEquivalentTo(projection.Records);
        marks.Where(mark => mark.Lane == 0).Should().OnlyContain(mark => mark.IsEvent);
        marks.Should().Contain(mark => mark.Records.Count > 1);
    }

    private static TrajectoryTurnSource Turn(string id, double seconds) => new(id,
        new ChatMessageSnapshot { Id = $"user-{id}", Role = AIChatRole.User, CreatedAt = START.AddSeconds(seconds), Parts = [ChatContentPart.FromText($"Question {id}")] },
        new ChatMessageSnapshot { Id = $"assistant-{id}", Role = AIChatRole.Assistant, Parts = [ChatContentPart.FromText("Transcript fallback")] }, []);

    private static ChatExecutionStep Request(string id, string? turnId, long sequence, double start, double end, string? instructions, string answer) => new()
    {
        Id = id, TurnId = turnId, Sequence = sequence, Kind = ChatExecutionStepKind.ModelRequest,
        StartedAt = START.AddSeconds(start), CompletedAt = START.AddSeconds(end), Status = ChatExecutionStatus.Completed,
        Request = new ChatModelRequest { ProviderId = "provider", ModelName = "model", Settings = new ChatSessionSettings("provider", "model", instructions, null),
            Instructions = instructions, Output = [ChatContentPart.FromText(answer)] }
    };
}
