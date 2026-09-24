using Monica.AI.Configuration.Models;
using Monica.AI.Models;
using Monica.AI.Providers;
using Monica.AI.Services;

namespace Monica.AI.Configuration.Services;

/// <summary>Owns one code-defined provider baseline; registration never eagerly constructs network clients.</summary>
internal sealed class AIProviderDefinition(EAIProviderType providerType, AIProviderOptions options)
{
    public string ApiKey => options.ApiKey;

    public AIProviderConfiguration ToConfiguration(AIModelCatalog catalog)
    {
        var openAI = options as OpenAIProviderOptions;
        var allowBuiltInTemplates = AIProviderMetadataPolicy.AllowsBuiltInTemplates(providerType, options.BaseUrl);
        var models = options.Models?.ToArray()
            ?? options.SupportedModels?.Where(static name => !string.IsNullOrWhiteSpace(name))
                .Select(name => catalog.GetModel(name.Trim(), allowBuiltInTemplates) ?? new LLMModelInfo { ModelName = name.Trim() }).ToArray()
            ?? [];
        return new AIProviderConfiguration
        {
            ProviderId = options.ProviderId ?? providerType.ToString(), ProviderType = providerType,
            DisplayName = options.DisplayName, BaseUrl = options.BaseUrl, Enabled = options.Enabled,
            IsDefault = options.IsDefault, SystemPrompt = options.SystemPrompt,
            DefaultModel = options.DefaultModel ?? models.OfType<LLMModelInfo>().FirstOrDefault()?.ModelName,
            TimeoutSeconds = options.TimeoutSeconds,
            OpenAIApiMode = openAI?.ApiMode ?? OpenAIProviderApiMode.Chat,
            OpenAIProtocolProfile = openAI?.ProtocolProfile ?? OpenAIProtocolProfile.Auto,
            ResponsesHistoryMode = openAI?.ResponsesHistoryMode ?? OpenAIResponsesHistoryMode.LocalHistory,
            PromptCacheKey = openAI?.PromptCacheKey, PromptCacheRetention = openAI?.PromptCacheRetention,
            Organization = openAI?.Organization, Project = openAI?.Project,
            Models = models.Select(AIModelConfiguration.FromModelInfo).ToArray()
        };
    }
}
