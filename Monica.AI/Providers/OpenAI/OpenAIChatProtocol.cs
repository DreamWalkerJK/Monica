using System.ClientModel.Primitives;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using Monica.AI.Chat.Services;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Monica.AI.Providers.OpenAI;

/// <summary>Preserves observed compatible-Chat reasoning fields and DeepSeek cache counters across the SDK adapter.</summary>
internal static class OpenAIChatProtocol
{
    internal static void MarkNativeReasoning(IEnumerable<AIContent> contents)
    {
        foreach (var content in contents.OfType<TextReasoningContent>())
            (content.AdditionalProperties ??= [])[ChatReasoningFormat.PROPERTY_KEY] = ChatReasoningFormat.NATIVE_FIELD;
    }

    internal static IEnumerable<ChatMessage> RestoreReasoning(IEnumerable<ChatMessage> messages)
    {
        foreach (var message in messages)
        {
            var reasoning = message.Contents.OfType<TextReasoningContent>()
                .Where(content => content.ProtectedData is null && ChatReasoningFormat.Read(content)
                    is ChatReasoningFormat.NATIVE_FIELD or ChatReasoningFormat.THINK_TAGS).ToArray();
            if (message.Role != ChatRole.Assistant || reasoning.Length == 0)
            {
                yield return message;
                continue;
            }

            // Convert with the SDK so tool calls and content retain the SDK's exact wire representation.
            // A private clone prevents changing a borrowed RawRepresentation or the durable transcript.
            var restored = message.Clone();
            restored.RawRepresentation = null;
            restored.Contents = RestoreTaggedText(message.Contents);
            var raw = (AssistantChatMessage)new[] { restored }.AsOpenAIChatMessages().Single();
            var native = reasoning.Where(content => ChatReasoningFormat.Read(content) == ChatReasoningFormat.NATIVE_FIELD).ToArray();
            if (native.Length > 0)
            {
                var payload = JsonNode.Parse(ModelReaderWriter.Write(raw).ToString())!.AsObject();
                payload["reasoning_content"] = string.Concat(native.Select(static content => content.Text));
                raw = ModelReaderWriter.Read<AssistantChatMessage>(BinaryData.FromString(payload.ToJsonString()));
            }
            restored.RawRepresentation = raw;
            yield return restored;
        }
    }

    private static List<AIContent> RestoreTaggedText(IList<AIContent> contents)
    {
        var restored = new List<AIContent>();
        StringBuilder? tagged = null;
        foreach (var content in contents)
        {
            // Native fields and zero-width aggregation boundaries do not interrupt the visible text
            // channel, so interleaved native deltas must not split a single tagged block on replay.
            if (content is TextReasoningContent { ProtectedData: null } native
                && ChatReasoningFormat.Read(native) == ChatReasoningFormat.NATIVE_FIELD
                || content is TextContent { Text.Length: 0 } empty && empty.Annotations is not { Count: > 0 }) continue;
            if (content is TextReasoningContent { ProtectedData: null } reasoning
                && ChatReasoningFormat.Read(reasoning) == ChatReasoningFormat.THINK_TAGS)
                (tagged ??= new StringBuilder()).Append(reasoning.Text);
            else
            {
                FlushTagged();
                restored.Add(content);
            }
        }
        FlushTagged();
        return restored;

        void FlushTagged()
        {
            if (tagged is null) return;
            restored.Add(new TextContent($"<think>{tagged}</think>"));
            tagged = null;
        }
    }

    internal static void NormalizeUsage(UsageDetails? usage, ChatTokenUsage? raw)
    {
        if (usage is null || raw is null) return;
        using var payload = JsonDocument.Parse(ModelReaderWriter.Write(raw).ToString());
        var root = payload.RootElement;
        if (TryReadCount(root, "prompt_cache_hit_tokens", out var hit))
        {
            usage.CachedInputTokenCount ??= hit;
            (usage.AdditionalCounts ??= [])["prompt_cache_hit_tokens"] = hit;
        }
        if (TryReadCount(root, "prompt_cache_miss_tokens", out var miss))
        {
            (usage.AdditionalCounts ??= [])["prompt_cache_miss_tokens"] = miss;
        }
    }

    private static bool TryReadCount(JsonElement payload, string property, out long value)
    {
        value = 0;
        return payload.TryGetProperty(property, out var count) && count.ValueKind == JsonValueKind.Number
            && count.TryGetInt64(out value) && value >= 0;
    }
}
