using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.AI;
using Monica.AI.Chat.Services;
using Monica.AI.Providers;
using Monica.AI.Providers.OpenAI;

namespace Test.Monica.AI.Providers.OpenAI;

public sealed class OpenAIChatProtocolTests
{
    [Fact]
    public async Task Request_WhenNativeReasoningInterleavesTaggedParts_ShouldPreserveBothWireChannels()
    {
        using var handler = new WireHandler(Completion("answer"));
        using var http = new HttpClient(handler);
        using var client = CreateClient(http);
        var message = new ChatMessage(ChatRole.Assistant,
        [
            Mark("first", ChatReasoningFormat.THINK_TAGS), new TextContent(string.Empty),
            Mark("native", ChatReasoningFormat.NATIVE_FIELD), new TextContent(string.Empty),
            Mark("second", ChatReasoningFormat.THINK_TAGS), new TextContent("answer")
        ]);

        await client.GetResponseAsync([message], cancellationToken: TestContext.Current.CancellationToken);

        using var request = JsonDocument.Parse(handler.Request!);
        var replayed = request.RootElement.GetProperty("messages")[0];
        ReadText(replayed).Should().Be("<think>firstsecond</think>answer");
        replayed.GetProperty("reasoning_content").GetString().Should().Be("native");

        static TextReasoningContent Mark(string text, string format) => new(text)
        {
            AdditionalProperties = new AdditionalPropertiesDictionary { [ChatReasoningFormat.PROPERTY_KEY] = format }
        };
    }

    [Theory]
    [InlineData(OpenAIProtocolProfile.Standard)]
    [InlineData(OpenAIProtocolProfile.DeepSeek)]
    public async Task Response_WhenNativeReasoningIsObserved_ShouldReplayItAcrossProfiles(OpenAIProtocolProfile profile)
    {
        using var handler = new WireHandler(Completion("answer", "native thought"));
        using var http = new HttpClient(handler);
        using var client = CreateClient(http, profile);
        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "first")],
            cancellationToken: TestContext.Current.CancellationToken);
        var observed = response.Messages.Single();
        ChatReasoningFormat.Read(observed.Contents.OfType<TextReasoningContent>().Single()).Should().Be(ChatReasoningFormat.NATIVE_FIELD);

        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "first"), observed, new ChatMessage(ChatRole.User, "second")],
            cancellationToken: TestContext.Current.CancellationToken);

        using var request = JsonDocument.Parse(handler.Request!);
        request.RootElement.GetProperty("messages")[1].GetProperty("reasoning_content").GetString().Should().Be("native thought");
        ReadText(request.RootElement.GetProperty("messages")[1]).Should().Be("answer");
    }

    [Theory]
    [InlineData(OpenAIProtocolProfile.Standard)]
    [InlineData(OpenAIProtocolProfile.DeepSeek)]
    public async Task Request_WhenReasoningHasNoObservedFormat_ShouldNotInventNativeWireField(OpenAIProtocolProfile profile)
    {
        using var handler = new WireHandler(Completion("answer"));
        using var http = new HttpClient(handler);
        using var client = CreateClient(http, profile);
        await client.GetResponseAsync([new ChatMessage(ChatRole.Assistant, [new TextReasoningContent("unmarked"), new TextContent("answer")])],
            cancellationToken: TestContext.Current.CancellationToken);

        using var request = JsonDocument.Parse(handler.Request!);
        request.RootElement.GetProperty("messages")[0].TryGetProperty("reasoning_content", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Response_WhenTaggedReasoningIsNormalized_ShouldReplayOneTaggedBlockWithOriginalToolCall()
    {
        const string toolCall = """, "tool_calls":[{"id":"call-1","type":"function","function":{"name":"weather","arguments":"{\"city\":\"Paris\"}"}}]""";
        using var handler = new WireHandler(Completion("<think>plan</think>answer", extra: toolCall));
        using var http = new HttpClient(handler);
        using var client = new TaggedReasoningChatClient(CreateClient(http));
        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "weather")],
            cancellationToken: TestContext.Current.CancellationToken);
        var message = response.Messages.Single();
        response.RawRepresentation.Should().BeNull();
        message.RawRepresentation.Should().BeNull();
        message.Text.Should().Be("answer");
        message.Contents.OfType<TextReasoningContent>().Select(content => content.Text).Should().Contain("plan");

        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "weather"), message,
                new ChatMessage(ChatRole.Tool, [new FunctionResultContent("call-1", "cloudy")])],
            cancellationToken: TestContext.Current.CancellationToken);

        using var request = JsonDocument.Parse(handler.Request!);
        var replayed = request.RootElement.GetProperty("messages")[1];
        ReadText(replayed).Should().Be("<think>plan</think>answer");
        replayed.GetProperty("tool_calls")[0].GetProperty("id").GetString().Should().Be("call-1");
        replayed.TryGetProperty("reasoning_content", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Stream_WhenNativeReasoningAndTextArriveTogether_ShouldLeaveLiteralThinkExampleVisible()
    {
        var stream = "data: " + JsonSerializer.Serialize(new
        {
            id = "completion", @object = "chat.completion.chunk", created = 1718345013, model = "alias",
            choices = new[] { new { index = 0, delta = new { role = "assistant", content = "<think>literal code</think>", reasoning_content = "native thought" }, finish_reason = "stop" } }
        }) + "\n\ndata: [DONE]\n\n";
        using var handler = new WireHandler(stream, "text/event-stream");
        using var http = new HttpClient(handler);
        using var client = new TaggedReasoningChatClient(CreateClient(http));
        var response = await client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "sample")],
            cancellationToken: TestContext.Current.CancellationToken).ToChatResponseAsync(TestContext.Current.CancellationToken);

        response.Text.Should().Be("<think>literal code</think>");
        var reasoning = response.Messages.Single().Contents.OfType<TextReasoningContent>().Should().ContainSingle().Which;
        reasoning.Text.Should().Be("native thought");
        ChatReasoningFormat.Read(reasoning).Should().Be(ChatReasoningFormat.NATIVE_FIELD);
    }

    private static IChatClient CreateClient(HttpClient http, OpenAIProtocolProfile profile = OpenAIProtocolProfile.Standard)
    {
        var sdk = new global::OpenAI.OpenAIClient(new ApiKeyCredential("test-key"), new global::OpenAI.OpenAIClientOptions
        {
            Endpoint = new Uri("https://model.example/v1"), Transport = new HttpClientPipelineTransport(http)
        });
        return new OpenAIRequestOptionsChatClient(sdk.GetChatClient("alias").AsIChatClient(),
            OpenAIProviderApiMode.Chat, OpenAIResponsesHistoryMode.LocalHistory, null, null, profile);
    }

    private static string Completion(string text, string? reasoning = null, string extra = "")
        => "{\"id\":\"completion\",\"object\":\"chat.completion\",\"created\":1718345013,\"model\":\"alias\",\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":"
           + JsonSerializer.Serialize(text) + (reasoning is null ? "" : ",\"reasoning_content\":" + JsonSerializer.Serialize(reasoning))
           + extra + "},\"finish_reason\":\"stop\"}]}";

    private static string ReadText(JsonElement message)
        => message.GetProperty("content") is { ValueKind: JsonValueKind.String } text ? text.GetString()!
            : string.Concat(message.GetProperty("content").EnumerateArray().Select(part => part.GetProperty("text").GetString()));

    private sealed class WireHandler(string response, string mediaType = "application/json") : HttpMessageHandler
    {
        internal string? Request { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response, Encoding.UTF8, mediaType) };
        }
    }
}
