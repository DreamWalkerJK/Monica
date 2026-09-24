using System.Text.Json;
using Monica.AI.Configuration.Models;

namespace Monica.AI.Providers.OpenAI;

/// <summary>Reads explicit metadata exposed by an enriched OpenAI-compatible model-list endpoint.</summary>
internal static class OpenAIModelMetadata
{
    internal static IReadOnlyDictionary<string, AIModelConfiguration> Read(BinaryData response)
    {
        using var document = JsonDocument.Parse(response.ToMemory());
        if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            return new Dictionary<string, AIModelConfiguration>();
        return data.EnumerateArray().Where(static item => Text(item, "id") is not null)
            .Select(ReadModel).DistinctBy(static model => model.ModelName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static model => model.ModelName, StringComparer.OrdinalIgnoreCase);
    }

    private static AIModelConfiguration ReadModel(JsonElement model)
    {
        var capabilities = Object(model, "capabilities");
        var architecture = Object(model, "architecture");
        var limits = Object(model, "limits");
        var reasoning = Object(capabilities, "reasoning");
        var levels = Strings(model, "reasoning_efforts") ?? Strings(reasoning, "efforts");
        var supportsReasoning = Boolean(model, "supports_reasoning") ?? Boolean(capabilities, "reasoning", "thinking")
            ?? Boolean(reasoning, "supported") ?? (levels is { Length: > 0 } ? true : SupportedParameter(model, "reasoning_effort"));
        return new AIModelConfiguration
        {
            ModelName = Text(model, "id")!, DisplayName = Text(model, "name") ?? Text(model, "display_name"),
            Description = Text(model, "description"),
            Kind = Text(model, "type") == "embedding" ? AIModelKind.Embedding : AIModelKind.Chat,
            ContextWindow = Positive(model, "context_length", "context_window", "max_input_tokens")
                ?? Positive(limits, "context", "max_input_tokens"),
            MaxOutputTokens = Positive(model, "max_output_tokens", "max_completion_tokens") ?? Positive(limits, "max_output_tokens"),
            SupportsImage = Boolean(model, "supports_vision", "supports_images") ?? Boolean(capabilities, "vision", "image_input")
                ?? SupportedModality(model, architecture, "image"),
            SupportsDocuments = Boolean(model, "supports_documents") ?? Boolean(capabilities, "document_input", "pdf_input")
                ?? SupportedModality(model, architecture, "file"),
            SupportsTools = Boolean(model, "supports_tools", "supports_function_calling") ?? Boolean(capabilities, "tools", "function_calling")
                ?? SupportedParameter(model, "tools"),
            SupportsReasoning = supportsReasoning,
            // Some compatible servers publish a common effort list even for models that explicitly disable reasoning.
            ReasoningLevels = supportsReasoning == false ? [] : levels?.Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(static value => new AIReasoningLevel { Id = value, ProviderValue = value }).ToArray() ?? [],
            EmbeddingDimensions = Positive(model, "dimensions", "embedding_dimensions")
        };
    }

    private static JsonElement Object(JsonElement parent, string key) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.Object ? value : default;

    private static string? Text(JsonElement parent, string key) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static int? Positive(JsonElement parent, params string[] keys)
    {
        foreach (var key in keys)
            if (parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(key, out var value)
                && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var count) && count > 0) return count;
        return null;
    }

    private static bool? Boolean(JsonElement parent, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(key, out var value)) continue;
            if (value.ValueKind is JsonValueKind.True or JsonValueKind.False) return value.GetBoolean();
            if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("supported", out var supported)
                && supported.ValueKind is JsonValueKind.True or JsonValueKind.False) return supported.GetBoolean();
        }
        return null;
    }

    private static string[]? Strings(JsonElement parent, string key) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Where(static item => item.ValueKind == JsonValueKind.String)
                .Select(static item => item.GetString()!).Where(static item => !string.IsNullOrWhiteSpace(item)).ToArray() : null;

    // A listed supported input/parameter is evidence; an omitted one remains unknown.
    private static bool? SupportedModality(JsonElement model, JsonElement architecture, string value) =>
        (Strings(model, "input_modalities") ?? Strings(architecture, "input_modalities"))?.Contains(value, StringComparer.OrdinalIgnoreCase) == true ? true : null;

    private static bool? SupportedParameter(JsonElement model, string value) =>
        Strings(model, "supported_parameters")?.Contains(value, StringComparer.OrdinalIgnoreCase) == true ? true : null;
}
