using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Monica.AI.Abstractions;
using Monica.AI.AgentCapabilities.Abstractions;
using Monica.AI.Chat.Abstractions;
using Monica.AI.Chat.Models;
using Monica.AI.Models;
using Monica.AI.Services;
using Monica.AI.Services.Support;
using Monica.Modules;
using NSubstitute;
using Test.Monica.AI.Support;

namespace Test.Monica.AI.Chat.Models;

public sealed class ChatSessionSnapshotTests
{
    [Fact]
    public async Task Snapshot_RoundTrip_RetainsOrderedContentAndMeasurementsWithoutSdkState()
    {
        var service = CreateService();
        var original = CreateSnapshot();
        await using var session = service.RestoreSession(original);
        var serialized = JsonSerializer.Serialize(await service.CreateSnapshotAsync(session, TestContext.Current.CancellationToken));
        var restored = JsonSerializer.Deserialize<ChatSessionSnapshot>(serialized)!;

        restored.Turns.Single().AssistantMessage!.Parts.Select(part => part.Kind)
            .Should().Equal(ChatContentKind.Reasoning, ChatContentKind.ToolCall, ChatContentKind.Text);
        restored.ExecutionSteps.Single().Request!.Usage!.CachedInputTokens.Should().BeNull();
        restored.ExecutionSteps.Single().Request!.TimeToFirstToken.Should().Be(TimeSpan.FromMilliseconds(125));
        restored.ContextMessages.Should().HaveCount(2);
        serialized.Should().NotContain("AgentSessionState").And.NotContain("stateBag");
        session.IsRuntimeActive.Should().BeFalse();
    }

    [Fact]
    public void Snapshot_OwnsListsAndJsonDocumentValues()
    {
        ChatContextMessage message;
        var parts = new List<ChatContentPart>();
        using (var json = JsonDocument.Parse("""{"path":"owned.md"}"""))
        {
            parts.Add(new ChatContentPart { Kind = ChatContentKind.ToolCall, CallId = "call", ToolName = "read", Data = json.RootElement });
            message = new ChatContextMessage { Parts = parts };
        }
        parts.Clear();
        var contexts = new List<ChatContextMessage> { message };
        var snapshot = CreateSnapshot() with { ContextMessages = contexts };
        contexts.Clear();
        snapshot.ContextMessages.Single().Parts.Single().Data!.Value.GetProperty("path").GetString().Should().Be("owned.md");
    }

    [Fact]
    public async Task Restore_UnfinishedTurnAndStep_BecomeInterrupted()
    {
        var snapshot = CreateSnapshot();
        await using var session = CreateService().RestoreSession(snapshot with
        {
            Turns = [snapshot.Turns[0] with { Status = ChatExecutionStatus.AwaitingApproval }],
            ExecutionSteps = [snapshot.ExecutionSteps[0] with { Status = ChatExecutionStatus.Running, CompletedAt = null }]
        });
        session.Turns.Single().Status.Should().Be(ChatExecutionStatus.Interrupted);
        session.ExecutionSteps.Single().Status.Should().Be(ChatExecutionStatus.Interrupted);
        session.ActiveTurn.Should().BeNull();
    }

    private static AIChatService CreateService() => new(
        Substitute.For<IAIProviderFactory>(), Options.Create(new ModuleAIOption()),
        new TestAIChatAgentFactory(static (_, _, _) => throw new InvalidOperationException("Snapshots must not activate runtime.")),
        Substitute.For<IAgentCapabilityStateStore>(),
        new AgentStreamingCoordinator(new AIChatRuntimeContextAccessor(), new AgentResponseUpdateChannelContext()),
        Substitute.For<IChatAttachmentStore>());

    private static ChatSessionSnapshot CreateSnapshot()
    {
        var now = DateTimeOffset.UtcNow.AddMinutes(-1);
        var settings = new ChatSessionSettings("provider", "model", "prompt");
        return new ChatSessionSnapshot
        {
            SessionId = "session", Title = "Saved", CreatedAt = now, UpdatedAt = now.AddSeconds(20), Settings = settings,
            Turns = [new ChatTurnSnapshot
            {
                Id = "turn", Status = ChatExecutionStatus.Completed, StartedAt = now,
                UserMessage = new ChatMessageSnapshot { Id = "user", Role = AIChatRole.User, Parts = [ChatContentPart.FromText("Question")], CreatedAt = now },
                AssistantMessage = new ChatMessageSnapshot
                {
                    Id = "assistant", Role = AIChatRole.Assistant, CreatedAt = now.AddSeconds(1),
                    Parts = [new ChatContentPart { Kind = ChatContentKind.Reasoning, Text = "Think" },
                        new ChatContentPart { Kind = ChatContentKind.ToolCall, ToolName = "lookup", CallId = "call" }, ChatContentPart.FromText("Answer")]
                }
            }],
            ContextMessages = [new ChatContextMessage { TurnId = "turn", Role = AIChatRole.User, Parts = [ChatContentPart.FromText("Question")] },
                new ChatContextMessage { TurnId = "turn", Role = AIChatRole.Assistant, Parts = [ChatContentPart.FromText("Answer")] }],
            ExecutionSteps = [new ChatExecutionStep
            {
                Id = "step", Sequence = 1, TurnId = "turn", Kind = ChatExecutionStepKind.ModelRequest,
                Status = ChatExecutionStatus.Completed, StartedAt = now, CompletedAt = now.AddSeconds(2),
                Request = new ChatModelRequest { ProviderId = "provider", Settings = settings,
                    Usage = new TokenUsage { InputTokens = 42, OutputTokens = 12 }, TimeToFirstToken = TimeSpan.FromMilliseconds(125) }
            }]
        };
    }
}
