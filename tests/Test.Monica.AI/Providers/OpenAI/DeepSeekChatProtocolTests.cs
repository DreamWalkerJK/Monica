using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.AI;
using Monica.AI.Providers;
using Monica.AI.Providers.OpenAI;

namespace Test.Monica.AI.Providers.OpenAI;

public sealed class DeepSeekChatProtocolTests
{
    [Theory]
    [InlineData("none", "disabled", null)]
    [InlineData("high", "enabled", "high")]
    [InlineData("max", "enabled", "max")]
    public async Task GetResponseAsync_WhenDeepSeekEffortIsSelected_ShouldUseDocumentedWireFields(
        string effort, string thinkingType, string? wireEffort)
    {
        using var handler = new WireHandler(COMPLETION_JSON);
        using var httpClient = new HttpClient(handler);
        using var client = CreateClient(httpClient);

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], new ChatOptions
        {
            MaxOutputTokens = 512,
            AdditionalProperties = new AdditionalPropertiesDictionary { ["monica.reasoning.effort"] = effort }
        }, TestContext.Current.CancellationToken);

        using var request = JsonDocument.Parse(handler.Request!);
        request.RootElement.GetProperty("thinking").GetProperty("type").GetString().Should().Be(thinkingType);
        request.RootElement.GetProperty("max_tokens").GetInt32().Should().Be(512);
        request.RootElement.TryGetProperty("max_completion_tokens", out _).Should().BeFalse();
        if (wireEffort is null) request.RootElement.TryGetProperty("reasoning_effort", out _).Should().BeFalse();
        else request.RootElement.GetProperty("reasoning_effort").GetString().Should().Be(wireEffort);
        response.Messages.Single().Contents.OfType<TextReasoningContent>().Single().Text.Should().Be("original thought");
        response.Usage!.CachedInputTokenCount.Should().Be(60);
        response.Usage.AdditionalCounts!["prompt_cache_miss_tokens"].Should().Be(40);
    }

    [Fact]
    public async Task GetResponseAsync_WhenHistoryIncludesReasoning_ShouldReplayAllAssistantTurnsAndToolCalls()
    {
        using var handler = new WireHandler(COMPLETION_JSON);
        using var httpClient = new HttpClient(handler);
        using var client = CreateClient(httpClient);
        var first = new ChatMessage(ChatRole.Assistant,
            [new TextReasoningContent("earlier reasoning"), new TextContent("earlier answer")]);
        var toolCall = new ChatMessage(ChatRole.Assistant,
        [
            new TextReasoningContent("original "), new TextReasoningContent("tool reasoning"),
            new FunctionCallContent("call-1", "weather", new Dictionary<string, object?> { ["city"] = "Hangzhou" })
        ]);
        ChatMessage[] messages =
        [
            new(ChatRole.User, "question one"), first, new(ChatRole.User, "question two"), toolCall,
            new(ChatRole.Tool, [new FunctionResultContent("call-1", "cloudy")])
        ];
        OpenAIChatProtocol.MarkNativeReasoning(first.Contents);
        OpenAIChatProtocol.MarkNativeReasoning(toolCall.Contents);

        await client.GetResponseAsync(messages, cancellationToken: TestContext.Current.CancellationToken);

        using var request = JsonDocument.Parse(handler.Request!);
        var sent = request.RootElement.GetProperty("messages");
        sent.GetArrayLength().Should().Be(5);
        sent[1].GetProperty("reasoning_content").GetString().Should().Be("earlier reasoning");
        sent[3].GetProperty("reasoning_content").GetString().Should().Be("original tool reasoning");
        sent[3].GetProperty("tool_calls")[0].GetProperty("id").GetString().Should().Be("call-1");
        sent[4].GetProperty("tool_call_id").GetString().Should().Be("call-1");
        first.RawRepresentation.Should().BeNull();
        toolCall.RawRepresentation.Should().BeNull();
    }

    [Fact]
    public async Task GetStreamingResponseAsync_WhenUsageIsInEmptyChoiceChunk_ShouldReadReasoningAndCacheCounters()
    {
        const string stream = """
            data: {"id":"chat-1","object":"chat.completion.chunk","created":1718345013,"model":"deepseek-flash","choices":[{"index":0,"delta":{"role":"assistant","reasoning_content":"streamed reasoning"}}]}

            data: {"id":"chat-1","object":"chat.completion.chunk","created":1718345013,"model":"deepseek-flash","choices":[{"index":0,"delta":{"content":"answer"},"finish_reason":"stop"}]}

            data: {"id":"chat-1","object":"chat.completion.chunk","created":1718345013,"model":"deepseek-flash","choices":[],"usage":{"prompt_tokens":100,"completion_tokens":10,"total_tokens":110,"prompt_cache_hit_tokens":60,"prompt_cache_miss_tokens":40}}

            data: [DONE]


            """;
        using var handler = new WireHandler(stream, "text/event-stream");
        using var httpClient = new HttpClient(handler);
        using var client = CreateClient(httpClient);
        var updates = new List<ChatResponseUpdate>();

        await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hello")],
                           cancellationToken: TestContext.Current.CancellationToken)) updates.Add(update);

        updates.SelectMany(static update => update.Contents).OfType<TextReasoningContent>()
            .Single().Text.Should().Be("streamed reasoning");
        var usage = updates.SelectMany(static update => update.Contents).OfType<UsageContent>().Single().Details;
        usage.CachedInputTokenCount.Should().Be(60);
        usage.InputTokenCount.Should().Be(100);
        usage.AdditionalCounts!["prompt_cache_miss_tokens"].Should().Be(40);
    }

    [Theory]
    [InlineData(null, OpenAIProtocolProfile.Auto, OpenAIProtocolProfile.Standard)]
    [InlineData("https://api.deepseek.com/v1", OpenAIProtocolProfile.Auto, OpenAIProtocolProfile.DeepSeek)]
    [InlineData("https://api.deepseek.com.proxy.example", OpenAIProtocolProfile.Auto, OpenAIProtocolProfile.Standard)]
    [InlineData("https://proxy.example", OpenAIProtocolProfile.DeepSeek, OpenAIProtocolProfile.DeepSeek)]
    [InlineData("https://api.deepseek.com", OpenAIProtocolProfile.Standard, OpenAIProtocolProfile.Standard)]
    public void ResolveProtocolProfile_ShouldRecognizeOnlyOfficialHostOrExplicitSelection(
        string? endpoint, OpenAIProtocolProfile configured, OpenAIProtocolProfile expected)
    {
        OpenAIProvider.ResolveProtocolProfile(new OpenAIProviderOptions
        {
            ApiKey = "test-key", BaseUrl = endpoint, ProtocolProfile = configured
        }).Should().Be(expected);
    }

    private static IChatClient CreateClient(HttpClient httpClient)
    {
        var sdk = new global::OpenAI.OpenAIClient(new ApiKeyCredential("test-key"), new global::OpenAI.OpenAIClientOptions
        {
            Endpoint = new Uri("https://deepseek.example/v1"), Transport = new HttpClientPipelineTransport(httpClient)
        });
        return new OpenAIRequestOptionsChatClient(sdk.GetChatClient("deepseek-flash").AsIChatClient(),
            OpenAIProviderApiMode.Chat, OpenAIResponsesHistoryMode.LocalHistory, null, null, OpenAIProtocolProfile.DeepSeek);
    }

    private sealed class WireHandler(string response, string mediaType = "application/json") : HttpMessageHandler
    {
        internal string? Request { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response, Encoding.UTF8, mediaType) };
        }
    }

    private const string COMPLETION_JSON = """
        {"id":"chat-1","object":"chat.completion","created":1718345013,"model":"deepseek-flash","choices":[{"index":0,"message":{"role":"assistant","content":"answer","reasoning_content":"original thought"},"finish_reason":"stop"}],"usage":{"prompt_tokens":100,"completion_tokens":10,"total_tokens":110,"prompt_cache_hit_tokens":60,"prompt_cache_miss_tokens":40}}
        """;
}
