using System.Text.Json;
using Microsoft.Extensions.AI;
using Monica.AI.Chat.Abstractions;
using Monica.AI.Chat.Models;
using Monica.AI.Models;
using Monica.AI.Services.Support;

namespace Monica.AI.Chat.Services;

/// <summary>Translates stable conversation content at the SDK boundary.</summary>
internal sealed class ChatContentMapper(IChatAttachmentStore attachments)
{
    internal const string TURN_ID_KEY = "monica.chat.turn";
    private const string ATTACHMENT_KEY = "monica.chat.attachment";
    internal const string SUMMARY_KEY = "monica.chat.summary";
    private const string REASONING_ORIGIN_KEY = "monica.chat.reasoning_origin";

    internal async Task<ChatMessage> MaterializeAsync(ChatSession session, ChatContextMessage source,
        LLMModelInfo? model, CancellationToken ct, ChatSessionSettings? requestSettings = null)
    {
        var contents = new List<AIContent>();
        foreach (var part in source.Parts)
        {
            AIContent? content = part.Kind switch
            {
                ChatContentKind.Text => new TextContent(part.Text ?? string.Empty),
                ChatContentKind.Reasoning => MaterializeReasoning(requestSettings ?? session.Settings, part),
                ChatContentKind.ToolCall => new FunctionCallContent(part.CallId!, part.ToolName!, ReadArguments(part.Data)),
                ChatContentKind.ToolResult => new FunctionResultContent(part.CallId!, part.Data is { } value ? value.Clone() : part.Text),
                ChatContentKind.Attachment => await MaterializeAttachmentAsync(session, part.Attachment!, model, ct),
                _ => throw new InvalidDataException($"Unsupported content kind '{part.Kind}'.")
            };
            if (content is not null) contents.Add(content);
        }

        return new ChatMessage(source.Role.ToChatRole(), contents)
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                [TURN_ID_KEY] = source.TurnId,
                [SUMMARY_KEY] = source.IsSummary
            }
        };
    }

    private static AIContent? MaterializeReasoning(ChatSessionSettings settings, ChatContentPart part)
    {
        // Signatures and encrypted content are meaningful only to their original provider/model.
        // Original visible reasoning is replayed as reasoning content, never invented assistant prose.
        if (part.OriginProviderId is { } provider && provider != settings.ProviderId
            || part.OriginModelName is { } name && name != settings.ModelName)
            return null;
        var content = new TextReasoningContent(part.Text ?? string.Empty)
        {
            ProtectedData = part.ProtectedData,
            AdditionalProperties = new AdditionalPropertiesDictionary { [REASONING_ORIGIN_KEY] = part }
        };
        if (part.ReasoningItemId is { } itemId) content.AdditionalProperties["reasoningItemId"] = itemId;
        if (part.ReasoningFormat is { } format) content.AdditionalProperties[ChatReasoningFormat.PROPERTY_KEY] = format;
        return content;
    }

    private async Task<AIContent> MaterializeAttachmentAsync(ChatSession session, ChatAttachmentReference supplied,
        LLMModelInfo? model, CancellationToken ct)
    {
        var partition = session.Partition ?? throw new InvalidOperationException("The conversation has no resolved ownership partition.");
        var attachment = await attachments.ReadAsync(partition, session.SessionId, supplied.Id, ct);
        if (attachment.Reference != supplied)
            throw new InvalidDataException("Attachment metadata does not match the stored attachment.");

        AIContent content;
        if (supplied.Kind == ChatAttachmentKind.Image)
        {
            if (model?.SupportsImage != true)
                throw new NotSupportedException("Image input must be enabled for the selected model before images can be sent.");
            content = new DataContent(attachment.Data, supplied.MediaType) { Name = supplied.FileName };
        }
        else if (!string.IsNullOrWhiteSpace(attachment.ExtractedText))
        {
            content = new TextContent($"<document name=\"{System.Net.WebUtility.HtmlEncode(supplied.FileName)}\">\n{attachment.ExtractedText}\n</document>");
        }
        else
        {
            if (!string.Equals(supplied.MediaType, "application/pdf", StringComparison.OrdinalIgnoreCase)
                || model?.SupportsDocuments != true)
                throw new NotSupportedException($"'{supplied.FileName}' has no extracted text and this model does not have native document input enabled.");
            content = new DataContent(attachment.Data, supplied.MediaType) { Name = supplied.FileName };
        }

        content.AdditionalProperties = new AdditionalPropertiesDictionary { [ATTACHMENT_KEY] = supplied };
        return content;
    }

    internal static ChatContextMessage Capture(ChatMessage message, string? defaultTurnId = null, bool redact = false)
    {
        var parts = message.Contents.Select(content => Capture(content, redact)).OfType<ChatContentPart>().ToArray();
        return new ChatContextMessage
        {
            TurnId = message.AdditionalProperties?.TryGetValue(TURN_ID_KEY, out var turn) == true ? turn as string : defaultTurnId,
            IsSummary = message.AdditionalProperties?.TryGetValue(SUMMARY_KEY, out var summary) == true && summary is true,
            Role = AIChatRoleExtensions.FromChatRole(message.Role),
            Parts = parts
        };
    }

    internal static ChatContentPart? Capture(AIContent content, bool redact = false)
    {
        if (content.AdditionalProperties?.TryGetValue(ATTACHMENT_KEY, out var attachment) == true
            && attachment is ChatAttachmentReference reference)
        {
            return ChatContentPart.FromAttachment(reference) with
            {
                Text = content is TextContent extracted ? Redact(extracted.Text, redact) : null
            };
        }

        return content switch
        {
            TextContent text => ChatContentPart.FromText(Redact(text.Text, redact) ?? string.Empty),
            TextReasoningContent reasoning => CaptureReasoning(reasoning, redact),
            FunctionCallContent call => new ChatContentPart
            {
                Kind = ChatContentKind.ToolCall, CallId = call.CallId, ToolName = call.Name,
                Data = Serialize(call.Arguments, redact)
            },
            FunctionResultContent result => new ChatContentPart
            {
                Kind = ChatContentKind.ToolResult, CallId = result.CallId,
                Data = Serialize(result.Result, redact), Text = Redact(ToolCallContentSerializer.SerializeResult(result.Result), redact),
                Error = Redact(result.Exception?.Message, redact)
            },
            DataContent data => new ChatContentPart { Kind = ChatContentKind.Text, Text = $"[Binary content: {data.MediaType}]" },
            _ => null
        };
    }

    private static ChatContentPart CaptureReasoning(TextReasoningContent content, bool redact)
    {
        var origin = content.AdditionalProperties?.TryGetValue(REASONING_ORIGIN_KEY, out var value) == true
            ? value as ChatContentPart : null;
        var itemId = content.AdditionalProperties?.TryGetValue("reasoningItemId", out var item) == true ? item switch
        {
            string text => text,
            JsonElement { ValueKind: JsonValueKind.String } json => json.GetString(),
            _ => null
        } : null;
        return new ChatContentPart
        {
            Kind = ChatContentKind.Reasoning, Text = Redact(content.Text, redact),
            ProtectedData = redact ? null : content.ProtectedData,
            ReasoningItemId = redact ? null : itemId,
            ReasoningFormat = ChatReasoningFormat.Read(content),
            OriginProviderId = origin?.OriginProviderId, OriginModelName = origin?.OriginModelName
        };
    }

    internal static IReadOnlyList<ChatContextMessage> KeepCompleteToolPairs(IEnumerable<ChatContextMessage> messages)
    {
        var values = messages.ToArray();
        var calls = values.SelectMany(message => message.Parts).Where(part => part.Kind == ChatContentKind.ToolCall).Select(part => part.CallId).ToHashSet();
        var results = values.SelectMany(message => message.Parts).Where(part => part.Kind == ChatContentKind.ToolResult).Select(part => part.CallId).ToHashSet();
        return values.Select(message => message with
            {
                Parts = message.Parts.Where(part => part.Kind switch
                {
                    ChatContentKind.ToolCall => results.Contains(part.CallId),
                    ChatContentKind.ToolResult => calls.Contains(part.CallId),
                    _ => true
                }).ToArray()
            })
            .Where(message => message.Parts.Count > 0).ToArray();
    }

    private static IDictionary<string, object?>? ReadArguments(JsonElement? data)
        => data is { ValueKind: JsonValueKind.Object } value
            ? value.EnumerateObject().ToDictionary(property => property.Name, property => (object?)property.Value.Clone()) : null;

    private static JsonElement? Serialize(object? value, bool redact)
    {
        if (value is null) return null;
        try
        {
            var json = JsonSerializer.SerializeToElement(value);
            return redact ? ChatInspectionRedactor.Redact(json) : json;
        }
        catch (NotSupportedException)
        {
            return JsonSerializer.SerializeToElement(Redact(value.ToString(), redact));
        }
    }

    private static string? Redact(string? value, bool redact) => redact ? ChatInspectionRedactor.Redact(value) : value;
}
