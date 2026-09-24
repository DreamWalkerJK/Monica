using AwesomeAssertions;
using System.ClientModel.Primitives;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Monica.AI.Providers;
using Monica.AI.Providers.OpenAI;
using OpenAI.Responses;

namespace Test.Monica.AI.Providers.OpenAI;

public sealed class OpenAIRequestOptionsChatClientTests
{
    [Fact]
    public async Task GetResponseAsync_WhenCustomReasoningEffortIsConfigured_ShouldSendExactValueWithoutEnumCoercion()
    {
        using var innerClient = new CapturingChatClient();
        using var client = new OpenAIRequestOptionsChatClient(innerClient, OpenAIProviderApiMode.Chat,
            OpenAIResponsesHistoryMode.LocalHistory, null, null);
        var options = new ChatOptions
        {
            Reasoning = new ReasoningOptions { Effort = ReasoningEffort.High },
            AdditionalProperties = new AdditionalPropertiesDictionary { ["monica.reasoning.effort"] = "minimal" }
        };

        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], options, TestContext.Current.CancellationToken);
        var raw = innerClient.CapturedOptions!.RawRepresentationFactory!(innerClient)
            .Should().BeOfType<global::OpenAI.Chat.ChatCompletionOptions>().Which;

        using var payload = JsonDocument.Parse(ModelReaderWriter.Write(raw).ToString());
        payload.RootElement.GetProperty("reasoning_effort").GetString().Should().Be("minimal");
        innerClient.CapturedOptions.Reasoning.Should().BeNull();
        options.Reasoning!.Effort.Should().Be(ReasoningEffort.High);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("none")]
    [InlineData("high")]
    public async Task GetResponseAsync_WhenUsingLocalHistory_ShouldRequestReplayDataAndSelectedReasoningSummary(string? effort)
    {
        using var innerClient = new CapturingChatClient();
        using var client = new OpenAIRequestOptionsChatClient(
            innerClient,
            OpenAIProviderApiMode.Responses,
            OpenAIResponsesHistoryMode.LocalHistory,
            promptCacheKey: null,
            promptCacheRetention: null);

        _ = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "turn two")],
            new ChatOptions { AdditionalProperties = new AdditionalPropertiesDictionary { ["monica.reasoning.effort"] = effort } },
            cancellationToken: TestContext.Current.CancellationToken);

        var options = innerClient.CapturedOptions;
        options.Should().NotBeNull();
#pragma warning disable OPENAI001
        var rawFactory = options!.RawRepresentationFactory;
        rawFactory.Should().NotBeNull();
        var responseOptions = rawFactory!(innerClient)
            .Should().BeOfType<CreateResponseOptions>().Which;
        responseOptions.StoredOutputEnabled.Should().BeFalse();
        responseOptions.PreviousResponseId.Should().BeNull();
        using var payload = JsonDocument.Parse(ModelReaderWriter.Write(responseOptions).ToString());
        payload.RootElement.GetProperty("include").EnumerateArray().Select(static item => item.GetString())
            .Should().ContainSingle().Which.Should().Be("reasoning.encrypted_content");
        if (effort is null)
        {
            payload.RootElement.TryGetProperty("reasoning", out _).Should().BeFalse();
        }
        else
        {
            var reasoning = payload.RootElement.GetProperty("reasoning");
            reasoning.GetProperty("effort").GetString().Should().Be(effort);
            if (effort == "none") reasoning.TryGetProperty("summary", out _).Should().BeFalse();
            else reasoning.GetProperty("summary").GetString().Should().Be("auto");
        }
#pragma warning restore OPENAI001
    }

    [Fact]
    public async Task GetStreamingResponseAsync_WhenEndpointFailsAfterPartialContent_ShouldKeepDeltasAndSurfaceFailure()
    {
        using var innerClient = new UnknownTerminalReasoningStatusChatClient();
        using var client = new OpenAIRequestOptionsChatClient(
            innerClient,
            OpenAIProviderApiMode.Responses,
            OpenAIResponsesHistoryMode.LocalHistory,
            promptCacheKey: null,
            promptCacheRetention: null);

        var updates = new List<ChatResponseUpdate>();
        var consume = async () =>
        {
            await foreach (var update in client.GetStreamingResponseAsync(
                               [new ChatMessage(ChatRole.User, "reason")],
                               cancellationToken: TestContext.Current.CancellationToken))
            {
                updates.Add(update);
            }
        };

        await consume.Should().ThrowAsync<ArgumentOutOfRangeException>().WithMessage("*Unknown ReasoningStatus value*");

        updates.SelectMany(static update => update.Contents)
            .OfType<TextReasoningContent>()
            .Select(static content => content.Text)
            .Should().ContainSingle().Which.Should().Be("thinking");
        updates.SelectMany(static update => update.Contents)
            .OfType<TextContent>()
            .Select(static content => content.Text)
            .Should().ContainSingle().Which.Should().Be("answer");
    }

    private sealed class CapturingChatClient : IChatClient
    {
        internal ChatOptions? CapturedOptions { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CapturedOptions = options;
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            CapturedOptions = options;
            await Task.CompletedTask;
            yield break;
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
            => serviceType == typeof(IChatClient) ? this : null;

        public void Dispose()
        {
        }
    }

    private sealed class UnknownTerminalReasoningStatusChatClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextReasoningContent("thinking")]);
            yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("answer")]);
            await Task.Yield();
            throw new ArgumentOutOfRangeException(
                "value",
                string.Empty,
                "Unknown ReasoningStatus value.");
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
            => serviceType == typeof(IChatClient) ? this : null;

        public void Dispose()
        {
        }
    }
}
