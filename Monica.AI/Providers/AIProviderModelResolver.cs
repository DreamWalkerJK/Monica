using Monica.AI.Models;
using Monica.AI.Services;

namespace Monica.AI.Providers;

internal static class AIProviderModelResolver
{
    public static ProviderModelResolution ResolveModels(
        AIModelCatalog catalog,
        AIProviderOptions options)
    {
        if (options.Models is { } configuredModels)
        {
            var models = configuredModels.Select(CloneMutableMetadata).ToArray();
            return new ProviderModelResolution(models,
                options.DefaultModel ?? models.OfType<LLMModelInfo>().FirstOrDefault()?.ModelName,
                options.Enabled && models.Length > 0);
        }

        var supportedModels = options.SupportedModels
            ?.Where(modelName => !string.IsNullOrWhiteSpace(modelName))
            .Select(modelName => modelName!.Trim())
            .ToList()
            ?? [];

        if (supportedModels.Count == 0)
        {
            return new ProviderModelResolution(
                [],
                null,
                false);
        }

        var allowBuiltInTemplates = AIProviderMetadataPolicy.AllowsBuiltInTemplates(options);
        var result = supportedModels.Select(modelName => CloneMutableMetadata(
            catalog.GetModel(modelName, allowBuiltInTemplates) ?? new LLMModelInfo { ModelName = modelName })).ToArray();

        var defaultModel = options.DefaultModel ?? result.OfType<LLMModelInfo>().FirstOrDefault()?.ModelName;
        var isValid = options.Enabled && result.Length > 0;

        return new ProviderModelResolution(
            result,
            defaultModel,
            isValid);
    }

    private static AIModelInfo CloneMutableMetadata(AIModelInfo model) => model is EmbeddingModelInfo embedding
        ? new EmbeddingModelInfo
        {
            ModelName = embedding.ModelName, DisplayName = embedding.DisplayName, Description = embedding.Description,
            Dimensions = embedding.Dimensions, MaxInputTokens = embedding.MaxInputTokens,
            CostPerMillionTokens = embedding.CostPerMillionTokens
        }
        : model;
}

internal sealed record ProviderModelResolution(
    IReadOnlyList<AIModelInfo> Models,
    string? DefaultModel,
    bool IsValid);
