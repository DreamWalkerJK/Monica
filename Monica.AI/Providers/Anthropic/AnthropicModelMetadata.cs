using System.Text.Json;
using Anthropic.Models.Models;
using Monica.AI.Configuration.Models;

namespace Monica.AI.Providers.Anthropic;

/// <summary>Maps endpoint evidence without requiring newer optional metadata from compatible servers.</summary>
internal static class AnthropicModelMetadata
{
    public static AIModelConfiguration Read(ModelInfo model)
    {
        var raw = model.RawData;
        var capabilities = raw.GetValueOrDefault("capabilities");
        var effort = GetProperty(capabilities, "effort");
        var supportsReasoning = ReadSupport(capabilities, "thinking");
        var reasoningLevels = new[] { "low", "medium", "high", "xhigh", "max" }
            .Where(level => ReadSupport(effort, level) == true)
            .Select(static level => new AIReasoningLevel { Id = level, ProviderValue = level })
            .ToArray();
        return new AIModelConfiguration
        {
            ModelName = model.ID,
            DisplayName = raw.GetValueOrDefault("display_name") is { ValueKind: JsonValueKind.String } displayName
                ? displayName.GetString()
                : null,
            ContextWindow = ReadPositiveInt(raw.GetValueOrDefault("max_input_tokens")),
            MaxOutputTokens = ReadPositiveInt(raw.GetValueOrDefault("max_tokens")),
            SupportsImage = ReadSupport(capabilities, "image_input"),
            SupportsDocuments = ReadSupport(capabilities, "pdf_input"),
            SupportsReasoning = supportsReasoning,
            ReasoningLevels = supportsReasoning == false ? [] : reasoningLevels
        };
    }

    private static JsonElement GetProperty(JsonElement parent, string name) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out var value) ? value : default;

    private static bool? ReadSupport(JsonElement parent, string name) =>
        GetProperty(GetProperty(parent, name), "supported").ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };

    private static int? ReadPositiveInt(JsonElement value) =>
        value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) && number > 0 ? number : null;
}
