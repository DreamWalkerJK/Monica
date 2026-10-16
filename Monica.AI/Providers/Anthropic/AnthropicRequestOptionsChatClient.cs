using System.Runtime.CompilerServices;
using Anthropic.Models.Messages;
using Microsoft.Extensions.AI;

namespace Monica.AI.Providers.Anthropic;

/// <summary>Maps explicit per-model reasoning choices to Anthropic adaptive effort or fixed thinking budgets.</summary>
internal sealed class AnthropicRequestOptionsChatClient(IChatClient innerClient, string modelName, int defaultMaxTokens)
    : DelegatingChatClient(innerClient)
{
    public override Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        base.GetResponseAsync(messages, Configure(options), cancellationToken);

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in base.GetStreamingResponseAsync(messages, Configure(options), cancellationToken).ConfigureAwait(false))
        {
            yield return update;
        }
    }

    private ChatOptions? Configure(ChatOptions? options)
    {
        var effort = options?.AdditionalProperties?.GetValueOrDefault("monica.reasoning.effort") as string;
        var budget = options?.AdditionalProperties?.GetValueOrDefault("monica.reasoning.budget_tokens") as int?;
        if (string.IsNullOrWhiteSpace(effort) && budget is null) return options;

        var configured = options?.Clone() ?? new ChatOptions();
        var previousFactory = configured.RawRepresentationFactory;
        configured.Reasoning = null;
        configured.RawRepresentationFactory = client =>
        {
            var raw = previousFactory?.Invoke(client) as MessageCreateParams ?? new MessageCreateParams
            {
                Model = configured.ModelId ?? modelName, MaxTokens = configured.MaxOutputTokens ?? defaultMaxTokens, Messages = []
            };
            if (budget is { } tokens)
            {
                if (tokens < 1024 || tokens >= raw.MaxTokens)
                {
                    throw new ArgumentException("Anthropic thinking budget must be at least 1024 tokens and smaller than the maximum output-token budget.");
                }

                raw = raw with { Thinking = new ThinkingConfigParam(new ThinkingConfigEnabled(tokens)) };
            }
            else
            {
                raw = raw with
                {
                    Thinking = string.Equals(effort, "none", StringComparison.OrdinalIgnoreCase)
                        ? new ThinkingConfigParam(new ThinkingConfigDisabled())
                        : new ThinkingConfigParam(new ThinkingConfigAdaptive())
                };
            }

            if (!string.IsNullOrWhiteSpace(effort) && !string.Equals(effort, "none", StringComparison.OrdinalIgnoreCase))
            {
                raw = raw with { OutputConfig = (raw.OutputConfig ?? new OutputConfig()) with { Effort = effort } };
            }

            return raw;
        };
        return configured;
    }
}
