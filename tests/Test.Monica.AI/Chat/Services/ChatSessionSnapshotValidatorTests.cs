using AwesomeAssertions;
using Monica.AI.Chat.Models;
using Monica.AI.Chat.Services;
using Monica.AI.Models;

namespace Test.Monica.AI.Chat.Services;

public sealed class ChatSessionSnapshotValidatorTests
{
    [Fact]
    public void Validate_WhenLoadedSessionIdentityDoesNotMatch_ShouldRejectSnapshot()
    {
        var snapshot = CreateValidSnapshot();

        var act = () => ChatSessionSnapshotValidator.Validate(snapshot, "another-session");

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*SessionId*another-session*session-1*");
    }

    [Fact]
    public void Validate_WhenUserRoleIsForgedAsSystem_ShouldRejectSnapshot()
    {
        var snapshot = CreateValidSnapshot();
        var forgedTurn = snapshot.Turns[0] with
        {
            UserMessage = snapshot.Turns[0].UserMessage with { Role = AIChatRole.System }
        };

        var act = () => ChatSessionSnapshotValidator.Validate(snapshot with { Turns = [forgedTurn] });

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*UserMessage*role 'User'*kind 'Message'*");
    }

    [Fact]
    public void Validate_WhenErrorUsesNormalMessageKind_ShouldRejectSnapshot()
    {
        var snapshot = CreateValidSnapshot();
        var turn = snapshot.Turns[0];
        var invalidError = turn.ErrorMessages[0] with { Kind = AIChatMessageKind.Message };

        var act = () => ChatSessionSnapshotValidator.Validate(snapshot with
        {
            Turns = [turn with { ErrorMessages = [invalidError] }]
        });

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*ErrorMessages*role 'Assistant'*kind 'Error'*");
    }

    [Fact]
    public void Validate_WhenMessageIdsAreDuplicated_ShouldRejectSnapshot()
    {
        var snapshot = CreateValidSnapshot();
        var turn = snapshot.Turns[0];
        var duplicate = turn.AssistantMessage! with { Id = turn.UserMessage.Id };

        var act = () => ChatSessionSnapshotValidator.Validate(snapshot with
        {
            Turns = [turn with { AssistantMessage = duplicate }]
        });

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*AssistantMessage.Id*unique*");
    }

    [Fact]
    public void Validate_WhenStepReferencesUnknownTurn_ShouldRejectSnapshot()
    {
        var snapshot = CreateValidSnapshot();

        var act = () => ChatSessionSnapshotValidator.Validate(snapshot with
        {
            ExecutionSteps = [new ChatExecutionStep { Id = "step", Sequence = 1, TurnId = "missing", StartedAt = snapshot.CreatedAt }]
        });

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*unknown turn*");
    }

    [Fact]
    public void Validate_WhenExecutionSequenceRegresses_ShouldRejectSnapshot()
    {
        var snapshot = CreateValidSnapshot();
        var act = () => ChatSessionSnapshotValidator.Validate(snapshot with
        {
            ExecutionSteps = [
                new ChatExecutionStep { Id = "first", Sequence = 2, StartedAt = snapshot.CreatedAt },
                new ChatExecutionStep { Id = "second", Sequence = 1, StartedAt = snapshot.CreatedAt }]
        });

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*sequence*ordered*");
    }

    [Fact]
    public void Validate_WhenTranscriptTimestampExceedsSessionLifetime_ShouldRejectSnapshot()
    {
        var snapshot = CreateValidSnapshot();
        var turn = snapshot.Turns[0];
        var futureAssistant = turn.AssistantMessage! with
        {
            CreatedAt = snapshot.UpdatedAt.AddSeconds(1)
        };

        var act = () => ChatSessionSnapshotValidator.Validate(snapshot with
        {
            Turns = [turn with { AssistantMessage = futureAssistant }]
        });

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*AssistantMessage.CreatedAt*session lifetime*");
    }

    [Fact]
    public void Validate_WhenProviderDoesNotReportCacheOrReasoningTokens_ShouldAcceptSnapshot()
    {
        var snapshot = CreateValidSnapshot();
        var act = () => ChatSessionSnapshotValidator.Validate(snapshot with
        {
            ExecutionSteps = [CreateRequest(snapshot, new TokenUsage { InputTokens = 10, OutputTokens = 5 })]
        });

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenRequestTimestampIsDefault_ShouldRejectSnapshot()
    {
        var snapshot = CreateValidSnapshot();
        var act = () => ChatSessionSnapshotValidator.Validate(snapshot with
        {
            ExecutionSteps = [CreateRequest(snapshot) with { StartedAt = default }]
        });

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*ExecutionSteps[0].StartedAt*supported timestamp range*");
    }

    [Fact]
    public void Validate_WhenRequestUsageIsNegative_ShouldRejectSnapshot()
    {
        var snapshot = CreateValidSnapshot();
        var act = () => ChatSessionSnapshotValidator.Validate(snapshot with
        {
            ExecutionSteps = [CreateRequest(snapshot, new TokenUsage { InputTokens = -1 })]
        });

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*ExecutionSteps[0].Request.Usage*cannot be negative*");
    }

    [Fact]
    public void Validate_WhenToolTimestampIsUnreasonablyFuture_ShouldRejectSnapshot()
    {
        var snapshot = CreateValidSnapshot();
        var act = () => ChatSessionSnapshotValidator.Validate(snapshot with
        {
            ExecutionSteps = [CreateToolCall(DateTimeOffset.UtcNow.AddDays(2))]
        });

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*ExecutionSteps[0].StartedAt*supported timestamp range*");
    }

    [Fact]
    public void Validate_WhenToolCompletionPrecedesStart_ShouldRejectSnapshot()
    {
        var snapshot = CreateValidSnapshot();
        var startedAt = new DateTimeOffset(2026, 7, 13, 2, 9, 21, TimeSpan.Zero);
        var act = () => ChatSessionSnapshotValidator.Validate(snapshot with
        {
            ExecutionSteps = [CreateToolCall(startedAt) with { CompletedAt = startedAt.AddSeconds(-1) }]
        });

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*ExecutionSteps[0].CompletedAt*cannot precede the operation start time*");
    }

    private static ChatSessionSnapshot CreateValidSnapshot() => new()
    {
        SessionId = "session-1",
        Title = "Persisted chat",
        CreatedAt = new DateTimeOffset(2026, 7, 13, 1, 0, 0, TimeSpan.Zero),
        UpdatedAt = new DateTimeOffset(2026, 7, 13, 1, 1, 0, TimeSpan.Zero),
        Settings = new ChatSessionSettings("provider", "model", "prompt"),
        Revision = 2,
        Turns =
        [
            new ChatTurnSnapshot
            {
                UserMessage = CreateMessage(
                    "user-1",
                    AIChatRole.User,
                    AIChatMessageKind.Message,
                    "question",
                    5),
                AssistantMessage = CreateMessage(
                    "assistant-1",
                    AIChatRole.Assistant,
                    AIChatMessageKind.Message,
                    "answer",
                    10),
                ErrorMessages =
                [
                    CreateMessage(
                        "error-1",
                        AIChatRole.Assistant,
                        AIChatMessageKind.Error,
                        "failed",
                        20)
                ]
            }
        ]
    };

    private static ChatMessageSnapshot CreateMessage(
        string id,
        AIChatRole role,
        AIChatMessageKind kind,
        string content,
        int seconds) => new()
        {
            Id = id,
            Role = role,
            Kind = kind,
            Parts = [ChatContentPart.FromText(content)],
            CreatedAt = new DateTimeOffset(2026, 7, 13, 1, 0, seconds, TimeSpan.Zero)
        };

    private static ChatExecutionStep CreateToolCall(DateTimeOffset startedAt) => new()
    {
        Id = "tool-step",
        Sequence = 1,
        Kind = ChatExecutionStepKind.Tool,
        Tool = new ChatToolExecution { CallId = "call-1", Name = "lookup" },
        Status = ChatExecutionStatus.Completed,
        StartedAt = startedAt,
        CompletedAt = startedAt.AddSeconds(1)
    };

    private static ChatExecutionStep CreateRequest(ChatSessionSnapshot snapshot, TokenUsage? usage = null) => new()
    {
        Id = "request-step", Sequence = 1, Kind = ChatExecutionStepKind.ModelRequest,
        StartedAt = snapshot.CreatedAt,
        Request = new ChatModelRequest { ProviderId = "provider", Settings = snapshot.Settings, Usage = usage }
    };
}
