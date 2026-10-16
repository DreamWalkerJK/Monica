using System.Runtime.CompilerServices;
using AwesomeAssertions;
using Microsoft.Extensions.AI;
using Monica.AI.Chat.Services;
using Monica.AI.Models;
using Monica.AI.Providers;
using Monica.AI.Providers.OpenAI;
using Monica.AI.Services;
using Test.Monica.AI.Support;

namespace Test.Monica.AI.Providers.OpenAI;

public sealed class TaggedReasoningChatClientTests
{
    [Theory]
    [InlineData("An annotated answer")]
    [InlineData("<think>an annotated literal example</think>")]
    public async Task Stream_WhenTextHasAnnotations_ShouldPreserveOriginalVisibleContent(string text)
    {
        var content = new TextContent(text)
        {
            Annotations = [new CitationAnnotation { Title = "Source", Url = new Uri("https://source.example") }],
            AdditionalProperties = new AdditionalPropertiesDictionary { ["source"] = "provider" }
        };
        using var inner = new ScriptedClient([[content]]);
        using var client = new TaggedReasoningChatClient(inner);
        var response = await client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "question")],
            cancellationToken: TestContext.Current.CancellationToken).ToChatResponseAsync(TestContext.Current.CancellationToken);

        response.Text.Should().Be(text);
        response.Messages.Single().Contents.Should().ContainSingle().Which.Should().BeSameAs(content);
    }

    [Theory]
    [InlineData("<think>reason</think>answer", "reason", "answer")]
    [InlineData(" \n<think>reason\nmore</think>\nanswer", "reason\nmore", " \n\nanswer")]
    [InlineData("<think></think>answer", "", "answer")]
    [InlineData("<think>unfinished</thi", "unfinished</thi", "")]
    [InlineData("<think>reason</think><think>literal</think>", "reason", "<think>literal</think>")]
    public async Task Stream_WhenTagsSplitAtEveryCharacter_ShouldSeparateOnlyLeadingBlock(string input, string reasoning, string answer)
    {
        using var inner = new ScriptedClient(input.Select(character => new AIContent[] { new TextContent(character.ToString()) }).ToArray());
        using var client = new TaggedReasoningChatClient(inner);
        var response = await client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "question")],
            cancellationToken: TestContext.Current.CancellationToken).ToChatResponseAsync(TestContext.Current.CancellationToken);

        response.Text.Should().Be(answer);
        var parts = response.Messages.Single().Contents.OfType<TextReasoningContent>().ToArray();
        string.Concat(parts.Select(part => part.Text)).Should().Be(reasoning);
        parts.Should().NotBeEmpty().And.OnlyContain(part => ChatReasoningFormat.Read(part) == ChatReasoningFormat.THINK_TAGS);
        response.Usage.Should().BeNull();
    }

    [Theory]
    [InlineData("<thi")]
    [InlineData("<thinking>ordinary</thinking>")]
    [InlineData("<THINK>ordinary</THINK>")]
    [InlineData("```xml\n<think>ordinary</think>\n```")]
    [InlineData("The literal <think> tag is an example.")]
    [InlineData("  \n")]
    public async Task Stream_WhenTextIsNotExactLeadingBlock_ShouldKeepEveryCharacter(string input)
    {
        using var inner = new ScriptedClient(input.Select(character => new AIContent[] { new TextContent(character.ToString()) }).ToArray());
        using var client = new TaggedReasoningChatClient(inner);
        var response = await client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "question")],
            cancellationToken: TestContext.Current.CancellationToken).ToChatResponseAsync(TestContext.Current.CancellationToken);

        response.Text.Should().Be(input);
        response.Messages.SelectMany(message => message.Contents).OfType<TextReasoningContent>().Should().BeEmpty();
    }

    [Theory]
    [InlineData(false, "<thi", "<thi", "")]
    [InlineData(true, "<thi", "<thi", "")]
    [InlineData(false, "<think>received</thi", "", "received</thi")]
    [InlineData(true, "<think>received</thi", "", "received</thi")]
    public async Task Stream_WhenProviderFails_ShouldFlushReceivedPrefixAndPropagateFailure(bool cancelled, string input, string answer, string reasoning)
    {
        var failure = cancelled ? (Exception)new OperationCanceledException("cancelled") : new IOException("provider failed");
        using var inner = new ScriptedClient([[new TextContent(input)]], failure);
        using var client = new TaggedReasoningChatClient(inner);
        var updates = new List<ChatResponseUpdate>();
        var consume = async () =>
        {
            await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "question")],
                               cancellationToken: TestContext.Current.CancellationToken)) updates.Add(update);
        };

        (await consume.Should().ThrowAsync<Exception>()).Which.Should().BeSameAs(failure);
        string.Concat(updates.SelectMany(update => update.Contents).OfType<TextContent>().Select(content => content.Text)).Should().Be(answer);
        string.Concat(updates.SelectMany(update => update.Contents).OfType<TextReasoningContent>().Select(content => content.Text)).Should().Be(reasoning);
        updates.Should().OnlyContain(update => update.FinishReason == null);
        updates.SelectMany(update => update.Contents).OfType<UsageContent>().Should().BeEmpty();
    }

    [Fact]
    public async Task Stream_WhenNativeReasoningFollowsTaggedReasoning_ShouldPreserveDistinctFormatsThroughAggregation()
    {
        using var inner = new ScriptedClient([
            [new TextContent("<think>tagged</think>")],
            [new TextReasoningContent("native") { AdditionalProperties = new AdditionalPropertiesDictionary { [ChatReasoningFormat.PROPERTY_KEY] = ChatReasoningFormat.NATIVE_FIELD } }],
            [new TextContent("answer")]
        ]);
        using var client = new TaggedReasoningChatClient(inner);
        var response = await client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "question")],
            cancellationToken: TestContext.Current.CancellationToken).ToChatResponseAsync(TestContext.Current.CancellationToken);

        var reasoning = response.Messages.Single().Contents.OfType<TextReasoningContent>().ToArray();
        reasoning.Select(content => content.Text).Should().Equal("tagged", "native");
        reasoning.Select(ChatReasoningFormat.Read).Should().Equal(ChatReasoningFormat.THINK_TAGS, ChatReasoningFormat.NATIVE_FIELD);
        response.Text.Should().Be("answer");
    }

    [Fact]
    public async Task Stream_WhenContentChanges_ShouldClearOnlyClonedRawRepresentationAndRetainUsage()
    {
        var raw = new object();
        var usage = new UsageDetails { InputTokenCount = 12, OutputTokenCount = 4 };
        using var inner = new ScriptedClient([[new TextContent("<think>reason</think>answer"), new UsageContent(usage)]], raw: raw);
        using var client = new TaggedReasoningChatClient(inner);
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "question")],
                           cancellationToken: TestContext.Current.CancellationToken)) updates.Add(update);

        inner.LastUpdate!.RawRepresentation.Should().BeSameAs(raw);
        updates.Single().RawRepresentation.Should().BeNull();
        updates.Single().Contents.OfType<UsageContent>().Single().Details.Should().BeSameAs(usage);
        usage.ReasoningTokenCount.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public async Task Provider_WhenModelCapabilityIsExplicitlyEnabled_ShouldNormalizeTaggedResponse(bool? supportsReasoning)
    {
        const string body = """
            {"id":"completion","object":"chat.completion","created":1718345013,"model":"any-alias","choices":[{"index":0,"message":{"role":"assistant","content":"<think>reason</think>answer"},"finish_reason":"stop"}]}
            """;
        await using var endpoint = new LoopbackModelEndpoint((_, _) => Task.FromResult((200, body)));
        using var provider = new OpenAIProvider(new OpenAIProviderOptions
        {
            ApiKey = "test-key", BaseUrl = endpoint.BaseUrl, ApiMode = OpenAIProviderApiMode.Chat,
            Models = [new LLMModelInfo { ModelName = "any-alias", SupportsReasoning = supportsReasoning }]
        }, new AIModelCatalog());
        var response = await provider.GetChatClient().GetResponseAsync([new ChatMessage(ChatRole.User, "question")],
            cancellationToken: TestContext.Current.CancellationToken);

        response.Text.Should().Be(supportsReasoning == true ? "answer" : "<think>reason</think>answer");
    }

    private sealed class ScriptedClient(IReadOnlyList<AIContent[]> contents, Exception? failure = null, object? raw = null) : IChatClient
    {
        internal ChatResponseUpdate? LastUpdate { get; private set; }
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var content in contents)
            {
                LastUpdate = new ChatResponseUpdate(ChatRole.Assistant, content) { MessageId = "response", RawRepresentation = raw };
                yield return LastUpdate;
                await Task.Yield();
            }
            if (failure is not null) throw failure;
        }
        public object? GetService(Type type, object? key = null) => null;
        public void Dispose() { }
    }
}
