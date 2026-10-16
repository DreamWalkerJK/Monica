using System.Runtime.CompilerServices;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.AI;
using Monica.AI.Chat.Models;
using Monica.AI.Models;
using Monica.AI.Services.Support;

namespace Test.Monica.AI.Services.Support;

public sealed class RecordingChatClientTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Streaming_WhenOnlyOneUpdateContainsGeneratedContent_ShouldNotReportThroughput(bool includeEmptyUpdates)
    {
        AIContent[] generated = [new TextReasoningContent("reason"), new TextContent("M")];
        AIContent[][] updates = includeEmptyUpdates
            ? [[new TextReasoningContent(string.Empty)], generated, [new TextContent(string.Empty)], [new TextReasoningContent(string.Empty)]]
            : [generated];

        var request = await RecordAsync(updates);

        request.TimeToFirstToken.Should().NotBeNull();
        request.GenerationDuration.Should().Be(TimeSpan.Zero);
        request.Usage!.OutputTokens.Should().Be(3);
        request.OutputTokensPerSecond.Should().BeNull();
        var restored = JsonSerializer.Deserialize<ChatModelRequest>(JsonSerializer.Serialize(request))!;
        restored.GenerationDuration.Should().Be(TimeSpan.Zero);
        restored.OutputTokensPerSecond.Should().BeNull();
    }

    [Fact]
    public async Task Streaming_WhenEveryTextDeltaIsEmpty_ShouldLeaveTimingUnavailable()
    {
        var request = await RecordAsync([[new TextContent(string.Empty)], [new TextReasoningContent(string.Empty)]]);

        request.TimeToFirstToken.Should().BeNull();
        request.GenerationDuration.Should().BeNull();
        request.OutputTokensPerSecond.Should().BeNull();
        request.Usage!.OutputTokens.Should().Be(3);
    }

    [Fact]
    public async Task Streaming_WhenContentArrivesAcrossUpdates_ShouldMeasureObservedGenerationInterval()
    {
        var request = await RecordAsync([[new TextReasoningContent("reason")], [new TextContent("M")]]);

        request.TimeToFirstToken.Should().NotBeNull();
        request.GenerationDuration.Should().BeGreaterThan(TimeSpan.Zero);
        request.OutputTokensPerSecond.Should().NotBeNull();
    }

    [Fact]
    public async Task NonStreaming_WhenResponseHasUsage_ShouldNotInferGenerationTimingFromRequestDuration()
    {
        var request = await RecordAsync([], streaming: false);

        request.Usage!.OutputTokens.Should().Be(3);
        request.TimeToFirstToken.Should().BeNull();
        request.GenerationDuration.Should().BeNull();
        request.OutputTokensPerSecond.Should().BeNull();
    }

    private static async Task<ChatModelRequest> RecordAsync(AIContent[][] updates, bool streaming = true)
    {
        var settings = new ChatSessionSettings("provider", "model") { AutomaticCompaction = false };
        await using var session = new ChatSession(settings, "Timing", AIChatRuntimeContext.Empty);
        using var inner = new ScriptedClient(updates);
        using var recorder = new RecordingChatClient(inner, new ChatRunContext(session, null, settings, 1));
        ChatMessage[] input = [new(ChatRole.User, "question")];
        if (streaming)
        {
            await foreach (var _ in recorder.GetStreamingResponseAsync(input, cancellationToken: TestContext.Current.CancellationToken)) { }
        }
        else await recorder.GetResponseAsync(input, cancellationToken: TestContext.Current.CancellationToken);
        return session.ExecutionSteps.Single().Request!;
    }

    private sealed class ScriptedClient(AIContent[][] updates) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "M")) { Usage = Usage() });

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var contents in updates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new ChatResponseUpdate(ChatRole.Assistant, contents);
                await Task.Yield();
            }
            yield return new ChatResponseUpdate(ChatRole.Assistant, [new UsageContent(Usage())]);
        }

        private static UsageDetails Usage() => new() { InputTokenCount = 10, OutputTokenCount = 3 };
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }
}
