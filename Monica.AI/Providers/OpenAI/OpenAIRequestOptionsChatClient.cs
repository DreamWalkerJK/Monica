using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
using ChatCompletionOptions = OpenAI.Chat.ChatCompletionOptions;

namespace Monica.AI.Providers.OpenAI;

/// <summary>
/// Applies OpenAI-specific request options that Microsoft.Extensions.AI does not expose directly.
/// </summary>
internal sealed class OpenAIRequestOptionsChatClient(
    IChatClient innerClient,
    OpenAIProviderApiMode apiMode,
    OpenAIResponsesHistoryMode responsesHistoryMode,
    string? promptCacheKey,
    OpenAIPromptCacheRetention? promptCacheRetention,
    OpenAIProtocolProfile protocolProfile = OpenAIProtocolProfile.Standard)
    : DelegatingChatClient(innerClient)
{
    private readonly OpenAIProviderApiMode _apiMode = apiMode;
    private readonly OpenAIResponsesHistoryMode _responsesHistoryMode = responsesHistoryMode;
    private readonly string? _promptCacheKey = NormalizePromptCacheKey(promptCacheKey);
    private readonly string? _promptCacheRetention = ConvertRetention(promptCacheRetention);
    private readonly bool _usesDeepSeekChat = apiMode == OpenAIProviderApiMode.Chat && protocolProfile == OpenAIProtocolProfile.DeepSeek;

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await base.GetResponseAsync(ConfigureMessages(messages), ConfigureOptions(options), cancellationToken)
            .ConfigureAwait(false);
        if (_apiMode == OpenAIProviderApiMode.Chat && response.RawRepresentation is global::OpenAI.Chat.ChatCompletion raw)
        {
            OpenAIChatProtocol.MarkNativeReasoning(response.Messages.SelectMany(static message => message.Contents));
            if (_usesDeepSeekChat) OpenAIChatProtocol.NormalizeUsage(response.Usage, raw.Usage);
        }
        return response;
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in base.GetStreamingResponseAsync(
                           ConfigureMessages(messages), ConfigureOptions(options), cancellationToken).ConfigureAwait(false))
        {
            if (_apiMode == OpenAIProviderApiMode.Chat && update.RawRepresentation is global::OpenAI.Chat.StreamingChatCompletionUpdate)
                OpenAIChatProtocol.MarkNativeReasoning(update.Contents);
            if (_usesDeepSeekChat)
            {
                foreach (var usage in update.Contents.OfType<UsageContent>())
                {
                    OpenAIChatProtocol.NormalizeUsage(usage.Details, usage.RawRepresentation as global::OpenAI.Chat.ChatTokenUsage);
                }
            }
            yield return update;
        }
    }

    private IEnumerable<ChatMessage> ConfigureMessages(IEnumerable<ChatMessage> messages) =>
        _apiMode == OpenAIProviderApiMode.Chat ? OpenAIChatProtocol.RestoreReasoning(messages) : messages;

    private ChatOptions ConfigureOptions(ChatOptions? options)
    {
        var configuredOptions = options?.Clone() ?? new ChatOptions();
        var previousFactory = configuredOptions.RawRepresentationFactory;
        var maxOutputTokens = configuredOptions.MaxOutputTokens;
        if (_usesDeepSeekChat) configuredOptions.MaxOutputTokens = null;
        var effort = configuredOptions.AdditionalProperties?.GetValueOrDefault("monica.reasoning.effort") as string;
        if (_usesDeepSeekChat && string.IsNullOrWhiteSpace(effort) && configuredOptions.Reasoning?.Effort is { } standardEffort)
        {
            effort = standardEffort == ReasoningEffort.ExtraHigh ? "xhigh" : standardEffort.ToString().ToLowerInvariant();
        }
        if (configuredOptions.AdditionalProperties?.GetValueOrDefault("monica.reasoning.budget_tokens") is not null)
        {
            throw new NotSupportedException("OpenAI-compatible requests support reasoning effort, but have no standard reasoning-token budget field.");
        }

        if (!string.IsNullOrWhiteSpace(effort)) configuredOptions.Reasoning = null;

        configuredOptions.RawRepresentationFactory = chatClient =>
        {
            var rawOptions = previousFactory?.Invoke(chatClient);
#pragma warning disable OPENAI001
            object configuredRawOptions = _apiMode switch
            {
                OpenAIProviderApiMode.Responses => ConfigureResponseOptions(
                    rawOptions as CreateResponseOptions ?? new CreateResponseOptions(), effort),
                OpenAIProviderApiMode.Chat => ConfigureChatCompletionOptions(
                    rawOptions as ChatCompletionOptions ?? new ChatCompletionOptions(), effort, maxOutputTokens),
                _ => throw new InvalidOperationException($"Unsupported OpenAI API mode '{_apiMode}'.")
            };
#pragma warning restore OPENAI001

            return configuredRawOptions;
        };

        return configuredOptions;
    }

#pragma warning disable OPENAI001
#pragma warning disable SCME0001
    private CreateResponseOptions ConfigureResponseOptions(CreateResponseOptions options, string? effort)
    {
        if (_responsesHistoryMode == OpenAIResponsesHistoryMode.LocalHistory)
        {
            options.StoredOutputEnabled = false;
            // Stateless replay needs the service's original encrypted reasoning, not only its visible summary.
            if (!options.IncludedProperties.Contains(IncludedResponseProperty.ReasoningEncryptedContent))
            {
                options.IncludedProperties.Add(IncludedResponseProperty.ReasoningEncryptedContent);
            }
        }

        SetPromptCacheFields(options.Patch);
        if (!string.IsNullOrWhiteSpace(effort))
        {
            options.ReasoningOptions ??= new ResponseReasoningOptions();
            options.ReasoningOptions.ReasoningEffortLevel = new ResponseReasoningEffortLevel(effort);
            if (!string.Equals(effort, "none", StringComparison.OrdinalIgnoreCase))
            {
                options.ReasoningOptions.ReasoningSummaryVerbosity ??= ResponseReasoningSummaryVerbosity.Auto;
            }
        }
        return options;
    }

    private ChatCompletionOptions ConfigureChatCompletionOptions(ChatCompletionOptions options, string? effort, int? maxOutputTokens)
    {
        SetPromptCacheFields(options.Patch);
        if (_usesDeepSeekChat)
        {
            // The SDK emits max_completion_tokens; DeepSeek documents its limit as max_tokens.
            var outputLimit = options.MaxOutputTokenCount ?? maxOutputTokens;
            options.MaxOutputTokenCount = null;
            if (outputLimit is { } tokens) options.Patch.Set("$.max_tokens"u8, tokens);
        }
        if (!string.IsNullOrWhiteSpace(effort))
        {
            if (_usesDeepSeekChat)
            {
                var disabled = string.Equals(effort, "none", StringComparison.OrdinalIgnoreCase);
                options.Patch.Set("$.thinking.type"u8, disabled ? "disabled" : "enabled");
                // Use the documented thinking toggle so disabling does not depend on an effort alias.
                options.ReasoningEffortLevel = disabled
                    ? (global::OpenAI.Chat.ChatReasoningEffortLevel?)null
                    : new global::OpenAI.Chat.ChatReasoningEffortLevel(effort);
            }
            else
            {
                options.ReasoningEffortLevel = new global::OpenAI.Chat.ChatReasoningEffortLevel(effort);
            }
        }
        return options;
    }

    private void SetPromptCacheFields(System.ClientModel.Primitives.JsonPatch patch)
    {
        if (_promptCacheKey is null)
        {
            return;
        }

        patch.Set("$.prompt_cache_key"u8, _promptCacheKey);
        if (_promptCacheRetention is not null)
        {
            patch.Set("$.prompt_cache_retention"u8, _promptCacheRetention);
        }
    }
#pragma warning restore SCME0001
#pragma warning restore OPENAI001

    private static string? ConvertRetention(OpenAIPromptCacheRetention? retention)
    {
        return retention switch
        {
            OpenAIPromptCacheRetention.InMemory => "in_memory",
            OpenAIPromptCacheRetention.TwentyFourHours => "24h",
            null => null,
            _ => throw new InvalidOperationException($"Unsupported OpenAI prompt cache retention '{retention}'.")
        };
    }

    private static string? NormalizePromptCacheKey(string? promptCacheKey)
    {
        return string.IsNullOrWhiteSpace(promptCacheKey) ? null : promptCacheKey.Trim();
    }
}
