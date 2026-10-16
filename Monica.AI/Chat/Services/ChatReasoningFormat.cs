using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Monica.AI.Chat.Services;

/// <summary>Carries the observed reasoning transport through SDK aggregation and Monica snapshots.</summary>
internal static class ChatReasoningFormat
{
    internal const string PROPERTY_KEY = "monica.reasoning.format";
    internal const string NATIVE_FIELD = "reasoning_content";
    internal const string THINK_TAGS = "think_tags";

    internal static string? Read(AIContent content)
        => content.AdditionalProperties?.GetValueOrDefault(PROPERTY_KEY) switch
        {
            string value => value,
            JsonElement { ValueKind: JsonValueKind.String } json => json.GetString(),
            _ => null
        };
}
