using Anthropic.Models.Messages;
using AwesomeAssertions;
using Microsoft.Extensions.AI;
using Monica.AI.Providers.Anthropic;
using NSubstitute;

namespace Test.Monica.AI.Providers.Anthropic;

public sealed class AnthropicRequestOptionsChatClientTests
{
    [Fact]
    public async Task GetResponseAsync_WhenAdaptiveEffortIsSelected_ShouldSendExactProviderValue()
    {
        var inner = Substitute.For<IChatClient>();
        ChatOptions? captured = null;
        inner.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured = call.ArgAt<ChatOptions>(1);
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
            });
        using var client = new AnthropicRequestOptionsChatClient(inner, "model", 4096);
        var options = new ChatOptions
        {
            AdditionalProperties = new AdditionalPropertiesDictionary { ["monica.reasoning.effort"] = "max" }
        };

        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], options, TestContext.Current.CancellationToken);
        var raw = (MessageCreateParams)captured!.RawRepresentationFactory!(inner)!;

        raw.Thinking!.Value.Should().BeOfType<ThinkingConfigAdaptive>();
        raw.OutputConfig!.Effort!.Raw().Should().Be("max");
        raw.MaxTokens.Should().Be(4096);
        raw.Model.Raw().Should().Be("model");
    }

    [Fact]
    public async Task GetResponseAsync_WhenBudgetIsExplicit_ShouldKeepBudgetAndRejectInsufficientOutput()
    {
        var inner = Substitute.For<IChatClient>();
        ChatOptions? captured = null;
        inner.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured = call.ArgAt<ChatOptions>(1);
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
            });
        using var client = new AnthropicRequestOptionsChatClient(inner, "model", 4096);
        var options = new ChatOptions
        {
            MaxOutputTokens = 4096,
            AdditionalProperties = new AdditionalPropertiesDictionary { ["monica.reasoning.budget_tokens"] = 2048 }
        };

        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], options, TestContext.Current.CancellationToken);
        var raw = (MessageCreateParams)captured!.RawRepresentationFactory!(inner)!;
        raw.Thinking!.Value.Should().BeOfType<ThinkingConfigEnabled>().Which.BudgetTokens.Should().Be(2048);
        (raw.OutputConfig?.Effort).Should().BeNull("a budget-only level must not invent an effort from its display identifier");

        options.MaxOutputTokens = 1024;
        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], options, TestContext.Current.CancellationToken);
        var materialize = () => captured!.RawRepresentationFactory!(inner);
        materialize.Should().Throw<ArgumentException>().WithMessage("*smaller than*");
    }
}
