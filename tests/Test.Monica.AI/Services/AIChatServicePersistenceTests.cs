using System.Runtime.CompilerServices;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Monica.AI.Abstractions;
using Monica.AI.AgentCapabilities.Abstractions;
using Monica.AI.AgentCapabilities.Models;
using Monica.AI.Chat.Abstractions;
using Monica.AI.Chat.Models;
using Monica.AI.Chat.Services;
using Monica.AI.Configuration.Models;
using Monica.AI.Models;
using Monica.AI.Providers.OpenAI;
using Monica.AI.Services;
using Monica.AI.Services.Support;
using Monica.Modules;
using NSubstitute;

namespace Test.Monica.AI.Services;

public sealed class AIChatServicePersistenceTests
{
    [Fact]
    public async Task Context_WhenModelCapacityIsUnknown_ShouldUseFallbackWithoutInventingMetadata()
    {
        using var fixture = new RuntimeFixture();
        var model = new LLMModelInfo { ModelName = "model" };
        fixture.Models = [model];
        await using var session = await fixture.Service.CreateSessionAsync("provider", ct: TestContext.Current.CancellationToken);

        session.ContextUsage.ContextWindow.Should().Be(262144);
        session.ContextUsage.ContextWindowSource.Should().Be(ChatContextCapacitySource.Default);
        session.Settings.ContextWindow.Should().BeNull();
        model.ContextWindow.Should().BeNull();
        await fixture.Service.SendMessageAsync(session, "Remember this message", TestContext.Current.CancellationToken);
        session.ExecutionSteps.Single().Request!.Settings.ContextWindow.Should().Be(262144);

        var snapshot = await fixture.Service.CreateSnapshotAsync(session, TestContext.Current.CancellationToken);
        await using var restored = fixture.Service.RestoreSession(
            JsonSerializer.Deserialize<ChatSessionSnapshot>(JsonSerializer.Serialize(snapshot))!);

        restored.ContextUsage.ContextWindow.Should().Be(262144);
        restored.ContextUsage.ContextWindowSource.Should().Be(ChatContextCapacitySource.Default);
        restored.Settings.ContextWindow.Should().BeNull();
        restored.Turns.Should().ContainSingle();
    }

    [Fact]
    public async Task Context_WhenModelOrOverrideChanges_ShouldRefreshCapacityAndItsSource()
    {
        using var fixture = new RuntimeFixture();
        fixture.Models = [.. fixture.Models, new LLMModelInfo { ModelName = "unknown" }];
        await using var session = await fixture.Service.CreateSessionAsync("provider", ct: TestContext.Current.CancellationToken);
        session.ContextUsage.ContextWindowSource.Should().Be(ChatContextCapacitySource.Model);

        fixture.Service.UpdateSettings(session, session.Settings with { ModelName = "unknown" });
        session.ContextUsage.ContextWindow.Should().Be(262144);
        session.ContextUsage.ContextWindowSource.Should().Be(ChatContextCapacitySource.Default);
        fixture.Service.UpdateSettings(session, session.Settings with { ContextWindow = 65536 });
        session.ContextUsage.ContextWindow.Should().Be(65536);
        session.ContextUsage.ContextWindowSource.Should().Be(ChatContextCapacitySource.Conversation);

        fixture.Models = [new LLMModelInfo { ModelName = "unknown", ContextWindow = 524288 }];
        fixture.Service.UpdateSettings(session, session.Settings with { ContextWindow = null });
        session.ContextUsage.ContextWindow.Should().Be(524288);
        session.ContextUsage.ContextWindowSource.Should().Be(ChatContextCapacitySource.Model);
        session.Settings.ContextWindow.Should().BeNull();
    }

    [Fact]
    public async Task Restore_WhenProviderIsUnavailable_ShouldRecalculateFallbackOrExplicitCapacity()
    {
        using var fixture = new RuntimeFixture();
        var snapshot = Conversation(1) with
        {
            Settings = new ChatSessionSettings("unavailable", "model"),
            ContextUsage = new ChatContextUsage { ContextWindow = 100000, ContextWindowSource = ChatContextCapacitySource.Model }
        };
        await using var restored = fixture.Service.RestoreSession(snapshot);
        restored.ContextUsage.ContextWindow.Should().Be(262144);
        restored.ContextUsage.ContextWindowSource.Should().Be(ChatContextCapacitySource.Default);
        restored.Settings.ContextWindow.Should().BeNull();

        await using var explicitCapacity = fixture.Service.RestoreSession(snapshot with
        {
            Settings = snapshot.Settings with { ContextWindow = 65536 }
        });
        explicitCapacity.ContextUsage.ContextWindow.Should().Be(65536);
        explicitCapacity.ContextUsage.ContextWindowSource.Should().Be(ChatContextCapacitySource.Conversation);
    }

    [Fact]
    public async Task Settings_WhenOutputBudgetExhaustsFallbackCapacity_ShouldRejectWithoutChangingChoices()
    {
        using var fixture = new RuntimeFixture();
        fixture.Models = [new LLMModelInfo { ModelName = "model" }];
        await using var session = await fixture.Service.CreateSessionAsync("provider", ct: TestContext.Current.CancellationToken);
        var original = session.Settings;

        var change = () => fixture.Service.UpdateSettings(session, original with { MaxOutputTokens = 262144 });

        change.Should().Throw<ArgumentException>();
        session.Settings.Should().Be(original);
        session.ContextUsage.ContextWindowSource.Should().Be(ChatContextCapacitySource.Default);
        session.ContextUsage.ReservedOutputTokens.Should().BeNull();
    }

    [Fact]
    public async Task Settings_WhenModelChanges_ShouldRefreshCapacityAndUseNewRequestSnapshot()
    {
        using var fixture = new RuntimeFixture();
        await using var session = await fixture.Service.CreateSessionAsync("provider", ct: TestContext.Current.CancellationToken);
        session.ContextUsage.ContextWindow.Should().Be(100000);
        session.ContextUsage.ReservedOutputTokens.Should().BeNull();
        await fixture.Service.SendMessageAsync(session, "First", TestContext.Current.CancellationToken);
        fixture.Service.UpdateSettings(session, session.Settings with { ModelName = "other", ReasoningLevel = "deep", MaxOutputTokens = 500 });
        session.ContextUsage.ContextWindow.Should().Be(20000);
        session.ContextUsage.ReservedOutputTokens.Should().Be(500);
        session.ContextUsage.LastRequestInputTokens.Should().BeNull();

        await fixture.Service.SendMessageAsync(session, "Second", TestContext.Current.CancellationToken);

        fixture.Client.Requests.Last().Options!.ModelId.Should().Be("other");
        fixture.Client.Requests.Last().Options!.Reasoning!.Effort.Should().Be(ReasoningEffort.High);
        session.ExecutionSteps.Last().Request!.Settings.ModelName.Should().Be("other");
        session.ExecutionSteps.First().Request!.Settings.ModelName.Should().Be("model");
        fixture.ReleasedLeases.Should().Be(2);
    }

    [Fact]
    public async Task Settings_WhenOutputBudgetExceedsNewModelCapacity_ShouldLeavePreviousChoicesIntact()
    {
        using var fixture = new RuntimeFixture();
        await using var session = await fixture.Service.CreateSessionAsync("provider", ct: TestContext.Current.CancellationToken);
        var settings = session.Settings;
        var usage = session.ContextUsage;

        var change = () => fixture.Service.UpdateSettings(session,
            settings with { ModelName = "other", MaxOutputTokens = 30000 });

        change.Should().Throw<ArgumentException>();
        session.Settings.Should().Be(settings);
        session.ContextUsage.Should().Be(usage);
    }

    [Fact]
    public async Task Settings_WhenOutputBudgetExceedsSupportedMaximum_ShouldLeavePreviousChoicesIntact()
    {
        using var fixture = new RuntimeFixture();
        await using var session = await fixture.Service.CreateSessionAsync("provider", ct: TestContext.Current.CancellationToken);
        fixture.Service.UpdateSettings(session, session.Settings with { MaxOutputTokens = 8192 });
        var settings = session.Settings;
        var usage = session.ContextUsage;

        var change = () => fixture.Service.UpdateSettings(session, settings with { MaxOutputTokens = 8193 });

        change.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*maximum of 8192 tokens*");
        session.Settings.Should().Be(settings);
        session.ContextUsage.Should().Be(usage);
        session.ContextUsage.ReservedOutputTokens.Should().Be(8192);
        fixture.Service.UpdateSettings(session, session.Settings with { MaxOutputTokens = null });
        session.ContextUsage.ReservedOutputTokens.Should().BeNull();
    }

    [Fact]
    public async Task Send_WhenHostLowersModelOutputMaximum_ShouldRejectTheNextRequestBeforeProviderInvocation()
    {
        using var fixture = new RuntimeFixture();
        await using var session = await fixture.Service.CreateSessionAsync("provider", ct: TestContext.Current.CancellationToken);
        fixture.Service.UpdateSettings(session, session.Settings with { MaxOutputTokens = 4096 });
        await fixture.Service.SendMessageAsync(session, "First", TestContext.Current.CancellationToken);
        fixture.Client.Requests.Single().Options!.MaxOutputTokens.Should().Be(4096);
        fixture.Models = [new LLMModelInfo { ModelName = "model", ContextWindow = 100000, MaxOutputTokens = 2048 }];

        var send = () => fixture.Service.SendMessageAsync(session, "Second", TestContext.Current.CancellationToken);

        await send.Should().ThrowAsync<ArgumentOutOfRangeException>().WithMessage("*maximum of 2048 tokens*");
        fixture.Client.Requests.Should().ContainSingle();
        session.Turns.Last().Status.Should().Be(ChatExecutionStatus.Failed);
        session.IsRuntimeActive.Should().BeFalse();
        fixture.ReleasedLeases.Should().Be(2);
        session.Settings.MaxOutputTokens.Should().Be(4096);
    }

    [Fact]
    public async Task Replay_WhenReasoningHasProviderSignature_ShouldRetainOriginalOnlyForItsModel()
    {
        using var fixture = new RuntimeFixture();
        fixture.Client.Stream = (index, _, _) => index == 0
            ? [new TextReasoningContent("Original reasoning")
                {
                    AdditionalProperties = new AdditionalPropertiesDictionary { ["reasoningItemId"] = "reasoning-1" }
                },
                new TextReasoningContent(null) { ProtectedData = "signed-provider-payload" },
                new TextContent("First answer")]
            : [new TextContent("Continued")];
        await using var session = await fixture.Service.CreateSessionAsync("provider", ct: TestContext.Current.CancellationToken);
        await fixture.Service.SendMessageAsync(session, "First", TestContext.Current.CancellationToken);
        var snapshot = await fixture.Service.CreateSnapshotAsync(session, TestContext.Current.CancellationToken);
        snapshot.ExecutionSteps.Single().Request!.Output.Should().OnlyContain(part => part.ProtectedData == null);
        await using var restored = fixture.Service.RestoreSession(JsonSerializer.Deserialize<ChatSessionSnapshot>(JsonSerializer.Serialize(snapshot))!);

        await fixture.Service.SendMessageAsync(restored, "Second", TestContext.Current.CancellationToken);

        var replayed = fixture.Client.Requests.Last().Messages.SelectMany(message => message.Contents).OfType<TextReasoningContent>()
            .Should().ContainSingle().Which;
        replayed.Text.Should().Be("Original reasoning");
        replayed.ProtectedData.Should().Be("signed-provider-payload");
        replayed.AdditionalProperties!["reasoningItemId"].Should().Be("reasoning-1");
        fixture.Service.UpdateSettings(restored, restored.Settings with { ModelName = "other" });
        await fixture.Service.SendMessageAsync(restored, "Third", TestContext.Current.CancellationToken);
        fixture.Client.Requests.Last().Messages.SelectMany(message => message.Contents).OfType<TextReasoningContent>().Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Replay_WhenReasoningCrossesToolLoopAndSnapshot_ShouldRetainObservedFormat(bool nativeField)
    {
        var tool = AIFunctionFactory.Create(() => "cloudy", "weather");
        using var fixture = new RuntimeFixture([tool], inner => new TaggedReasoningChatClient(inner));
        fixture.Client.Stream = (index, _, _) => index switch
        {
            0 => [.. Reasoning("Before tool"), new FunctionCallContent("weather-call", "weather", new Dictionary<string, object?>())],
            1 => [.. Reasoning("After tool"), new TextContent("The weather is cloudy.")],
            _ => [new TextContent("Continued")]
        };
        await using var session = await fixture.Service.CreateSessionAsync("provider", ct: TestContext.Current.CancellationToken);
        await fixture.Service.SendMessageAsync(session, "Weather?", TestContext.Current.CancellationToken);
        var expectedFormat = nativeField ? ChatReasoningFormat.NATIVE_FIELD : ChatReasoningFormat.THINK_TAGS;
        var inLoop = fixture.Client.Requests[1].Messages.SelectMany(message => message.Contents).OfType<TextReasoningContent>();
        inLoop.Should().ContainSingle().Which.Text.Should().Be("Before tool");
        inLoop.Select(ChatReasoningFormat.Read).Should().OnlyContain(format => format == expectedFormat);
        session.Turns.Single().AssistantMessage!.Content.Should().Be("The weather is cloudy.");

        var snapshot = await fixture.Service.CreateSnapshotAsync(session, TestContext.Current.CancellationToken);
        var durable = JsonSerializer.Deserialize<ChatSessionSnapshot>(JsonSerializer.Serialize(snapshot))!;
        durable.ContextMessages.SelectMany(message => message.Parts).Where(part => part.Kind == ChatContentKind.Reasoning)
            .Should().HaveCount(2).And.OnlyContain(part => part.ReasoningFormat == expectedFormat);
        await using var restored = fixture.Service.RestoreSession(durable);
        await fixture.Service.SendMessageAsync(restored, "Continue", TestContext.Current.CancellationToken);

        var replayed = fixture.Client.Requests.Last().Messages.SelectMany(message => message.Contents).OfType<TextReasoningContent>().ToArray();
        replayed.Select(content => content.Text).Should().Equal("Before tool", "After tool");
        replayed.Select(ChatReasoningFormat.Read).Should().OnlyContain(format => format == expectedFormat);
        fixture.Service.UpdateSettings(restored, restored.Settings with { ModelName = "other" });
        await fixture.Service.SendMessageAsync(restored, "Switch model", TestContext.Current.CancellationToken);
        fixture.Client.Requests.Last().Messages.SelectMany(message => message.Contents).OfType<TextReasoningContent>().Should().BeEmpty();

        AIContent[] Reasoning(string text)
        {
            if (nativeField)
            {
                var content = new TextReasoningContent(text);
                OpenAIChatProtocol.MarkNativeReasoning([content]);
                return [content];
            }
            return [new TextContent("<thi"), new TextContent("nk>" + text + "</th"), new TextContent("ink>")];
        }
    }

    [Fact]
    public async Task Dispose_WhenProviderStillStreaming_ShouldCancelAndAwaitOwnedOperation()
    {
        using var fixture = new RuntimeFixture();
        fixture.Client.WaitAfterFirstContent = true;
        var session = await fixture.Service.CreateSessionAsync("provider", ct: TestContext.Current.CancellationToken);
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var consuming = Task.Run(async () =>
        {
            await foreach (var update in fixture.Service.SendMessageStreamingAsync(session, "Start", TestContext.Current.CancellationToken))
                if (update is ChatTextDeltaEvent) received.TrySetResult();
        }, TestContext.Current.CancellationToken);
        await received.Task.WaitAsync(TestContext.Current.CancellationToken);

        await session.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await consuming;

        session.IsBusy.Should().BeFalse();
        session.Turns.Single().Status.Should().Be(ChatExecutionStatus.Cancelled);
        fixture.ReleasedLeases.Should().Be(1);
    }

    [Fact]
    public async Task Send_WhenHistoryContainsTools_ShouldReplayCompletePairsFromStableMessages()
    {
        using var fixture = new RuntimeFixture();
        var snapshot = Conversation(1);
        var call = new ChatContentPart { Kind = ChatContentKind.ToolCall, CallId = "old-call", ToolName = "lookup", Data = JsonSerializer.SerializeToElement(new { city = "Paris" }) };
        var result = new ChatContentPart { Kind = ChatContentKind.ToolResult, CallId = "old-call", Text = "sunny" };
        snapshot = snapshot with { ContextMessages = [snapshot.ContextMessages[0],
            new ChatContextMessage { TurnId = "turn-0", Role = AIChatRole.Assistant, Parts = [call] },
            new ChatContextMessage { TurnId = "turn-0", Role = AIChatRole.Tool, Parts = [result] }, snapshot.ContextMessages[1]] };
        await using var session = fixture.Service.RestoreSession(snapshot);

        var answer = await fixture.Service.SendMessageAsync(session, "Continue", TestContext.Current.CancellationToken);

        answer.Should().Be("answer");
        fixture.Client.Requests.Single().Messages.SelectMany(message => message.Contents).OfType<FunctionCallContent>()
            .Should().ContainSingle().Which.CallId.Should().Be("old-call");
        fixture.Client.Requests.Single().Messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>()
            .Should().ContainSingle().Which.CallId.Should().Be("old-call");
        session.IsRuntimeActive.Should().BeFalse();
        fixture.ReleasedLeases.Should().Be(1);
        fixture.Client.DisposeCount.Should().Be(0);
    }

    [Fact]
    public async Task Send_WhenToolLoopHasTwoAnonymousResponses_ShouldRecordEachProviderCallAndToolInOrder()
    {
        var tool = AIFunctionFactory.Create((string city) => $"Weather in {city}", "weather");
        using var fixture = new RuntimeFixture([tool]);
        fixture.Client.Stream = (index, _, _) => index == 0
            ? [new TextReasoningContent("Find weather."), new FunctionCallContent("weather-call", "weather", new Dictionary<string, object?> { ["city"] = "Paris" })]
            : [new TextContent("It is sunny.")];
        await using var session = await fixture.Service.CreateSessionAsync("provider", ct: TestContext.Current.CancellationToken);

        var events = new List<ChatStreamEvent>();
        await foreach (var update in fixture.Service.SendMessageStreamingAsync(session, "Weather?", TestContext.Current.CancellationToken)) events.Add(update);

        session.ExecutionSteps.Select(step => step.Kind).Should().Equal(ChatExecutionStepKind.ModelRequest, ChatExecutionStepKind.Tool, ChatExecutionStepKind.ModelRequest);
        session.ExecutionSteps.Select(step => step.Status).Should().OnlyContain(status => status == ChatExecutionStatus.Completed);
        session.ExecutionSteps[0].Request!.Usage!.InputTokens.Should().Be(10);
        session.ExecutionSteps[2].Request!.Usage!.InputTokens.Should().Be(11);
        session.ExecutionSteps[2].Request!.Usage!.CachedInputTokens.Should().BeNull();
        session.ExecutionSteps[0].Request!.TimeToFirstToken.Should().NotBeNull();
        session.ExecutionSteps[0].Request!.Tools.Single().Name.Should().Be("weather");
        session.Turns.Single().AssistantMessage!.Content.Should().Be("It is sunny.");
        session.Turns.Single().AssistantMessage!.Parts.Select(part => part.Kind).Should().ContainInOrder(
            ChatContentKind.Reasoning, ChatContentKind.ToolCall, ChatContentKind.ToolResult, ChatContentKind.Text);
        events.OfType<ChatStepChangedEvent>().Should().NotBeEmpty();
        var durable = await fixture.Service.CreateSnapshotAsync(session, TestContext.Current.CancellationToken);
        await using var restored = fixture.Service.RestoreSession(JsonSerializer.Deserialize<ChatSessionSnapshot>(JsonSerializer.Serialize(durable))!);
        restored.ExecutionSteps.Should().HaveCount(3);
        restored.ExecutionSteps[1].Tool!.Result.Should().Contain("Paris");
    }

    [Fact]
    public async Task Send_WhenCancelledAfterText_ShouldRetainPartialOutputAndReleaseLease()
    {
        using var fixture = new RuntimeFixture();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        fixture.Client.WaitAfterFirstContent = true;
        await using var session = await fixture.Service.CreateSessionAsync("provider", ct: cancellation.Token);
        await foreach (var update in fixture.Service.SendMessageStreamingAsync(session, "Start", cancellation.Token))
            if (update is ChatTextDeltaEvent) cancellation.Cancel();

        session.Turns.Single().Status.Should().Be(ChatExecutionStatus.Cancelled);
        session.Turns.Single().AssistantMessage!.Content.Should().Be("answer");
        session.ExecutionSteps.Single().Status.Should().Be(ChatExecutionStatus.Cancelled);
        fixture.ReleasedLeases.Should().Be(1);
        session.IsRuntimeActive.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Stop_WhenApprovalIsPaused_ShouldTerminateTurnWithoutInvokingToolOrModel(bool dispose)
    {
        var invoked = false;
        var tool = new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() => { invoked = true; return "done"; }, "write"));
        using var fixture = new RuntimeFixture([tool]);
        fixture.Client.Stream = (_, _, _) => [new FunctionCallContent("approval-call", "write", new Dictionary<string, object?>())];
        await using var session = await fixture.Service.CreateSessionAsync("provider", ct: TestContext.Current.CancellationToken);
        await foreach (var _ in fixture.Service.SendMessageStreamingAsync(session, "Write", TestContext.Current.CancellationToken)) { }
        session.ActiveTurn!.Status.Should().Be(ChatExecutionStatus.AwaitingApproval);
        fixture.ReleasedLeases.Should().Be(0);
        var turn = session.Turns.Single();
        var retry = () => session.RewindForRetry(turn.AssistantMessage!.Id);
        var edit = () => session.RewindForEdit(turn.UserMessage.Id, "Edited");
        retry.Should().Throw<InvalidOperationException>().WithMessage("*pending approval*");
        edit.Should().Throw<InvalidOperationException>().WithMessage("*pending approval*");
        session.Turns.Should().ContainSingle().Which.Should().BeSameAs(turn);

        if (dispose) await session.DisposeAsync();
        else await fixture.Service.CancelAsync(session);

        session.ActiveTurn.Should().BeNull();
        session.Turns.Single().Status.Should().Be(ChatExecutionStatus.Cancelled);
        session.IsRuntimeActive.Should().BeFalse();
        invoked.Should().BeFalse();
        fixture.Client.Requests.Should().ContainSingle();
        fixture.ReleasedLeases.Should().Be(1);
    }

    [Fact]
    public async Task Send_WhenProviderKeyAppearsInFailure_ShouldRedactStreamAndDurableFailure()
    {
        using var fixture = new RuntimeFixture();
        fixture.Client.Stream = (_, _, _) => throw new InvalidOperationException("Incorrect API key provided: test-api-key");
        await using var session = await fixture.Service.CreateSessionAsync("provider", ct: TestContext.Current.CancellationToken);
        var action = async () => await fixture.Service.SendMessageAsync(session, "Hello", TestContext.Current.CancellationToken);

        var failure = await action.Should().ThrowAsync<InvalidOperationException>();
        failure.Which.Message.Should().NotContain("test-api-key");
        JsonSerializer.Serialize(await fixture.Service.CreateSnapshotAsync(session, TestContext.Current.CancellationToken))
            .Should().NotContain("test-api-key");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Tool_WhenItThrowsOrReturnsAnError_ShouldRecordFailureAndRedactBeforeReplay(bool returnsError)
    {
        var failure = new InvalidOperationException("Incorrect API key provided: test-api-key");
        var tool = returnsError
            ? AIFunctionFactory.Create(() => ToolInvocationErrorResult.Create("fail", null, failure), "fail")
            : AIFunctionFactory.Create((Func<string>)(() => throw failure), "fail");
        using var fixture = new RuntimeFixture([tool]);
        fixture.Client.Stream = (index, _, _) => index == 0
            ? [new FunctionCallContent("failed-call", "fail", new Dictionary<string, object?>())]
            : [new TextContent("The lookup failed.")];
        await using var session = await fixture.Service.CreateSessionAsync("provider", ct: TestContext.Current.CancellationToken);
        var events = new List<ChatStreamEvent>();

        await foreach (var update in fixture.Service.SendMessageStreamingAsync(session, "Look up", TestContext.Current.CancellationToken)) events.Add(update);

        var step = session.ExecutionSteps.Single(item => item.Kind == ChatExecutionStepKind.Tool);
        step.Status.Should().Be(ChatExecutionStatus.Failed);
        step.Error.Should().Contain("[REDACTED]").And.NotContain("test-api-key");
        events.OfType<ChatToolEvent>().Should().Contain(item => item.Status == ChatToolEventStatus.Failed);
        JsonSerializer.Serialize(await fixture.Service.CreateSnapshotAsync(session, TestContext.Current.CancellationToken))
            .Should().NotContain("test-api-key");
        var replay = fixture.Client.Requests.Last().Messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>().Single();
        JsonSerializer.Serialize(replay.Result).Should().NotContain("test-api-key");
    }

    [Fact]
    public async Task Send_WhenFirstContentArrives_ShouldPublishLiveLedgerBeforeRequestCompletion()
    {
        using var fixture = new RuntimeFixture();
        fixture.Client.WaitAfterFirstContent = true;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await using var session = await fixture.Service.CreateSessionAsync("provider", ct: cancellation.Token);
        var observed = false;
        await foreach (var update in fixture.Service.SendMessageStreamingAsync(session, "Start", cancellation.Token))
        {
            if (update is ChatStepChangedEvent { Step.Status: ChatExecutionStatus.Running, Step.Request.Output.Count: > 0 } live)
            {
                live.Step.Request!.Output.Single().Text.Should().Be("answer");
                session.ExecutionSteps.Single().Request!.Output.Single().Text.Should().Be("answer");
                observed = true;
                cancellation.Cancel();
            }
        }
        observed.Should().BeTrue();
    }

    [Fact]
    public async Task Compact_WhenSuccessful_ShouldRetainRecentWholeTurnsAndOriginalTranscript()
    {
        using var fixture = new RuntimeFixture();
        var snapshot = Conversation(6);
        await using var session = fixture.Service.RestoreSession(snapshot);

        var result = await fixture.Service.CompactAsync(session, TestContext.Current.CancellationToken);

        result.Applied.Should().BeTrue();
        result.AfterTokens.Should().BeLessThan(result.BeforeTokens);
        session.Turns.Should().HaveCount(6);
        session.ContextMessages.Should().HaveCount(5);
        session.ContextMessages[0].IsSummary.Should().BeTrue();
        session.ContextMessages.Skip(1).Select(message => message.TurnId).Distinct().Should().Equal("turn-4", "turn-5");
        session.ExecutionSteps[1].Request!.Settings.MaxOutputTokens.Should().Be(2048);
        session.ExecutionSteps[1].Request!.IsCompaction.Should().BeTrue();
        var retry = session.RewindForRetry("assistant-2");
        retry.Text.Should().Be(snapshot.Turns[2].UserMessage.Content);
        session.ContextMessages.Should().HaveCount(4);
        session.ContextMessages.Should().OnlyContain(message => !message.IsSummary);
    }

    [Fact]
    public async Task Compact_WhenProviderFails_ShouldKeepOriginalContextAndRecordFailure()
    {
        using var fixture = new RuntimeFixture();
        fixture.Client.SummaryError = new InvalidOperationException("summary unavailable");
        var snapshot = Conversation(6);
        await using var session = fixture.Service.RestoreSession(snapshot);

        var action = async () => await fixture.Service.CompactAsync(session, TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("summary unavailable");
        session.ContextMessages.Should().BeEquivalentTo(snapshot.ContextMessages);
        session.Turns.Should().HaveCount(6);
        session.ExecutionSteps.Where(step => step.Kind == ChatExecutionStepKind.Compaction).Single().Status.Should().Be(ChatExecutionStatus.Failed);
    }

    [Fact]
    public async Task Compact_WhenNoOlderTurns_ShouldRecordNoOpWithoutProviderRequest()
    {
        using var fixture = new RuntimeFixture();
        await using var session = fixture.Service.RestoreSession(Conversation(1));
        var result = await fixture.Service.CompactAsync(session, TestContext.Current.CancellationToken);
        result.Applied.Should().BeFalse();
        session.ExecutionSteps.Should().ContainSingle().Which.Kind.Should().Be(ChatExecutionStepKind.Compaction);
        fixture.Client.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Send_WhenContextNearLimit_ShouldCompactBeforeActualModelRequest()
    {
        using var fixture = new RuntimeFixture();
        var snapshot = Conversation(6);
        await using var session = fixture.Service.RestoreSession(snapshot with
        {
            Settings = snapshot.Settings with { ContextWindow = 1000, AutomaticCompaction = true }
        });

        await fixture.Service.SendMessageAsync(session, "Continue", TestContext.Current.CancellationToken);

        session.ExecutionSteps[0].Kind.Should().Be(ChatExecutionStepKind.Compaction);
        session.ExecutionSteps[0].Compaction!.Automatic.Should().BeTrue();
        var modelInput = fixture.Client.Requests.Last().Messages;
        modelInput.Should().Contain(message => message.Text.Contains("Summary of earlier conversation", StringComparison.Ordinal));
        modelInput.Should().NotContain(message => message.Text == snapshot.Turns[0].UserMessage.Content);
        session.Turns.Should().HaveCount(7);
    }

    [Fact]
    public async Task Send_WhenImagesAndDocumentsProvided_ShouldUseCanonicalBytesAndKeepOrderedReferences()
    {
        using var fixture = new RuntimeFixture();
        var image = new ChatAttachmentReference { Id = "image", FileName = "photo.png", MediaType = "image/png", Size = 3, Kind = ChatAttachmentKind.Image };
        var document = new ChatAttachmentReference { Id = "doc", FileName = "notes.md", MediaType = "text/markdown", Size = 4, Kind = ChatAttachmentKind.Document, HasExtractedText = true };
        fixture.Attachments.ReadAsync(Arg.Any<ChatHistoryPartition>(), Arg.Any<string>(), "image", Arg.Any<CancellationToken>())
            .Returns(new ChatAttachmentData(image, [1, 2, 3]));
        fixture.Attachments.ReadAsync(Arg.Any<ChatHistoryPartition>(), Arg.Any<string>(), "doc", Arg.Any<CancellationToken>())
            .Returns(new ChatAttachmentData(document, [1, 2, 3, 4], "document facts"));
        await using var session = await fixture.Service.CreateSessionAsync("provider", ct: TestContext.Current.CancellationToken);
        session.BindPartition(new ChatHistoryPartition("user/workspace"));
        var input = new ChatUserInput { Parts = [ChatContentPart.FromText("Inspect"), ChatContentPart.FromAttachment(image), ChatContentPart.FromAttachment(document)] };

        await foreach (var _ in fixture.Service.SendMessageStreamingAsync(session, input, TestContext.Current.CancellationToken)) { }

        var contents = fixture.Client.Requests.Single().Messages.Last().Contents;
        contents.Select(content => content.GetType()).Should().Equal(typeof(TextContent), typeof(DataContent), typeof(TextContent));
        ((DataContent)contents[1]).Data.ToArray().Should().Equal(1, 2, 3);
        ((TextContent)contents[2]).Text.Should().Contain("document facts");
        var saved = await fixture.Service.CreateSnapshotAsync(session, TestContext.Current.CancellationToken);
        saved.Turns.Single().UserMessage.Parts.Select(part => part.Attachment?.Id).OfType<string>().Should().Equal("image", "doc");
        JsonSerializer.Serialize(saved).Should().NotContain("data:image");
    }

    [Fact]
    public async Task Send_WhenAttachmentMetadataIsForged_ShouldFailBeforeContactingProvider()
    {
        using var fixture = new RuntimeFixture();
        var canonical = new ChatAttachmentReference { Id = "file", FileName = "image.png", MediaType = "image/png", Size = 1, Kind = ChatAttachmentKind.Image };
        fixture.Attachments.ReadAsync(Arg.Any<ChatHistoryPartition>(), Arg.Any<string>(), "file", Arg.Any<CancellationToken>())
            .Returns(new ChatAttachmentData(canonical, [1]));
        await using var session = await fixture.Service.CreateSessionAsync("provider", ct: TestContext.Current.CancellationToken);
        session.BindPartition(new ChatHistoryPartition("owner"));
        var input = new ChatUserInput { Parts = [ChatContentPart.FromAttachment(canonical with { FileName = "forged.png" })] };
        var action = async () => { await foreach (var _ in fixture.Service.SendMessageStreamingAsync(session, input, TestContext.Current.CancellationToken)) { } };

        await action.Should().ThrowAsync<InvalidDataException>();
        fixture.Client.Requests.Should().BeEmpty();
        session.Turns.Single().Status.Should().Be(ChatExecutionStatus.Failed);
        fixture.ReleasedLeases.Should().Be(1);
    }

    [Fact]
    public void DiagnosticRedaction_WhenNestedCredentialsAppear_ShouldPreserveOrdinaryContent()
    {
        var json = JsonSerializer.SerializeToElement(new { api_key = "secret-value", nested = new { text = "Bearer abc123", max_tokens = 100 } });
        var redacted = ChatInspectionRedactor.Redact(json);
        redacted.GetProperty("api_key").GetString().Should().Be("[REDACTED]");
        redacted.GetProperty("nested").GetProperty("max_tokens").GetInt32().Should().Be(100);
        redacted.GetRawText().Should().NotContain("abc123");
    }

    private static ChatSessionSnapshot Conversation(int count)
    {
        var now = DateTimeOffset.UtcNow.AddHours(-1);
        var turns = Enumerable.Range(0, count).Select(index =>
        {
            var question = $"Question {index}: " + new string('q', 800);
            var answer = $"Answer {index}: " + new string('a', 800);
            var context = new[]
            {
                new ChatContextMessage { TurnId = $"turn-{index}", Role = AIChatRole.User, Parts = [ChatContentPart.FromText(question)] },
                new ChatContextMessage { TurnId = $"turn-{index}", Role = AIChatRole.Assistant, Parts = [ChatContentPart.FromText(answer)] }
            };
            return new ChatTurnSnapshot
            {
                Id = $"turn-{index}", Status = ChatExecutionStatus.Completed, StartedAt = now.AddMinutes(index), ContextMessages = context,
                UserMessage = new ChatMessageSnapshot { Id = $"user-{index}", Role = AIChatRole.User, Parts = context[0].Parts, CreatedAt = now.AddMinutes(index) },
                AssistantMessage = new ChatMessageSnapshot { Id = $"assistant-{index}", Role = AIChatRole.Assistant, Parts = context[1].Parts, CreatedAt = now.AddMinutes(index).AddSeconds(1) }
            };
        }).ToArray();
        return new ChatSessionSnapshot
        {
            SessionId = "restored", Title = "History", CreatedAt = now, UpdatedAt = now.AddMinutes(count),
            Settings = new ChatSessionSettings("provider", "model") { RetainedTurns = 2, AutomaticCompaction = false },
            Turns = turns, ContextMessages = turns.SelectMany(turn => turn.ContextMessages).ToArray()
        };
    }

    private sealed class RuntimeFixture : IDisposable
    {
        private readonly ServiceProvider _services = new ServiceCollection().BuildServiceProvider();
        internal ScriptedClient Client { get; } = new();
        internal IChatAttachmentStore Attachments { get; } = Substitute.For<IChatAttachmentStore>();
        internal AIChatService Service { get; }
        internal int ReleasedLeases { get; private set; }
        internal IReadOnlyList<AIModelInfo> Models { get; set; } =
        [
            new LLMModelInfo { ModelName = "model", SupportsImage = true, SupportsDocuments = true, ContextWindow = 100000, MaxOutputTokens = 8192 },
            new LLMModelInfo { ModelName = "other", ContextWindow = 20000, MaxOutputTokens = 1024, SupportsReasoning = true,
                ReasoningLevels = [new AIReasoningLevel { Id = "deep", ProviderValue = "high" }] }
        ];

        internal RuntimeFixture(IReadOnlyList<AITool>? tools = null, Func<IChatClient, IChatClient>? decorateClient = null)
        {
            var provider = Substitute.For<IAIProvider>();
            provider.Info.Returns(_ => new AIProviderInfo
            {
                ProviderId = "provider", ProviderType = "Test", DisplayName = "Test", DefaultModel = "model",
                SupportedModels = Models
            });
            provider.GetChatClient(Arg.Any<string?>()).Returns(decorateClient?.Invoke(Client) ?? Client);
            var providers = Substitute.For<IAIProviderFactory>();
            providers.GetProviderInfo("provider").Returns(_ => provider.Info);
            providers.GetDefaultProviderInfo().Returns(_ => provider.Info);
            providers.AcquireProvider(Arg.Any<string?>()).Returns(_ => new TestLease(provider, () => ReleasedLeases++));
            var capabilityStore = Substitute.For<IAgentCapabilityStateStore>();
            capabilityStore.LoadAsync(Arg.Any<CancellationToken>()).Returns(new AgentCapabilityState());
            var channels = new AgentResponseUpdateChannelContext();
            var factory = new AIChatAgentFactory(tools is null ? [] : [new Contributor(tools)],
                [new ToolInvocationTrackingAgentDecorator(NullLogger<ToolInvocationTrackingAgentDecorator>.Instance, channels)],
                NullLoggerFactory.Instance, _services);
            Service = new AIChatService(providers, Options.Create(new ModuleAIOption()), factory, capabilityStore,
                new AgentStreamingCoordinator(new AIChatRuntimeContextAccessor(), channels), Attachments);
        }

        public void Dispose() { Client.Dispose(); _services.Dispose(); }
    }

    private sealed class Contributor(IReadOnlyList<AITool> tools) : IAIChatAgentContributor
    {
        public ValueTask ContributeAsync(AIChatAgentContributionContext context, CancellationToken cancellationToken = default)
        { context.AddTools(tools); return ValueTask.CompletedTask; }
    }

    private sealed class TestLease(IAIProvider provider, Action release) : IAIProviderLease
    {
        public IAIProvider Provider => provider;
        public long ConfigurationRevision => 7;
        public string RedactDiagnostic(string message) => message.Replace("test-api-key", "[REDACTED]", StringComparison.Ordinal);
        public void Dispose() => release();
    }

    private sealed class ScriptedClient : IChatClient
    {
        private int _streamCalls;
        internal List<(IReadOnlyList<ChatMessage> Messages, ChatOptions? Options)> Requests { get; } = [];
        internal Func<int, IReadOnlyList<ChatMessage>, ChatOptions?, IReadOnlyList<AIContent>> Stream { get; set; } = (_, _, _) => [new TextContent("answer")];
        internal Exception? SummaryError { get; set; }
        internal bool WaitAfterFirstContent { get; set; }
        internal int DisposeCount { get; private set; }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            Requests.Add((messages.ToArray(), options));
            if (SummaryError is not null) throw SummaryError;
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Earlier goals and decisions.")));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var input = messages.ToArray();
            Requests.Add((input, options));
            var index = _streamCalls++;
            foreach (var content in Stream(index, input, options))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new ChatResponseUpdate(ChatRole.Assistant, [content]);
                if (WaitAfterFirstContent) await new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task.WaitAsync(cancellationToken);
                await Task.Yield();
            }
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate(ChatRole.Assistant, [new UsageContent(new UsageDetails
            {
                InputTokenCount = 10 + index, OutputTokenCount = 5
            })]);
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => serviceType == typeof(IChatClient) ? this : null;
        public void Dispose() => DisposeCount++;
    }
}
