using Monica.AI.Abstractions;
using Monica.AI.Configuration.Models;
using Monica.AI.Providers;
using Monica.AI.Providers.Anthropic;
using Monica.AI.Providers.OpenAI;
using Monica.AI.Services;

namespace Monica.AI.Configuration.Services;

/// <summary>Constructs owned SDK providers from one captured configuration and credential.</summary>
internal static class AIConfiguredProviderFactory
{
    internal static IAIProvider Create(AIProviderOptions options, AIModelCatalog catalog) => options switch
    {
        OpenAIProviderOptions openAI => new OpenAIProvider(openAI, catalog),
        AnthropicProviderOptions anthropic => new AnthropicProvider(anthropic, catalog),
        _ => throw new InvalidOperationException("Unsupported configurable provider options.")
    };

    internal static AIProviderOptions CreateOptions(AIProviderConfiguration configuration, string? apiKey)
    {
        AIProviderOptions options = configuration.ProviderType switch
        {
            EAIProviderType.OpenAI => new OpenAIProviderOptions
            {
                ApiKey = apiKey ?? "", ApiMode = configuration.OpenAIApiMode,
                ProtocolProfile = configuration.OpenAIProtocolProfile,
                ResponsesHistoryMode = configuration.ResponsesHistoryMode, PromptCacheKey = configuration.PromptCacheKey,
                PromptCacheRetention = configuration.PromptCacheRetention, Organization = configuration.Organization, Project = configuration.Project
            },
            EAIProviderType.Anthropic => new AnthropicProviderOptions { ApiKey = apiKey ?? "" },
            _ => throw new InvalidOperationException("Unsupported configurable provider type.")
        };
        options.ProviderId = configuration.ProviderId;
        options.DisplayName = configuration.DisplayName;
        options.BaseUrl = configuration.BaseUrl;
        options.Enabled = configuration.Enabled;
        options.IsDefault = configuration.IsDefault;
        options.DefaultModel = configuration.DefaultModel;
        options.SystemPrompt = configuration.SystemPrompt;
        options.TimeoutSeconds = configuration.TimeoutSeconds;
        options.Models = configuration.Models.Select(static model => model.ToModelInfo()).ToList();
        return options;
    }
}
