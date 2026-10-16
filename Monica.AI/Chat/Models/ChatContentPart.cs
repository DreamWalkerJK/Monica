using System.Text.Json;
using System.Text.Json.Serialization;
using Monica.AI.Models;

namespace Monica.AI.Chat.Models;

/// <summary>The meaning of one ordered message part, independent of provider SDK types.</summary>
public enum ChatContentKind
{
    /// <summary>Visible message text.</summary>
    Text,
    /// <summary>Reasoning text exposed by the model.</summary>
    Reasoning,
    /// <summary>A durable attachment reference resolved inside its conversation partition.</summary>
    Attachment,
    /// <summary>A model-requested function invocation.</summary>
    ToolCall,
    /// <summary>The result of a function invocation.</summary>
    ToolResult
}

/// <summary>One ordered text, reasoning, attachment, or tool part in a durable message.</summary>
public sealed record ChatContentPart
{
    private JsonElement? _data;
    /// <summary>Part discriminator.</summary>
    public ChatContentKind Kind { get; init; }
    /// <summary>Text, reasoning, or display-ready tool result.</summary>
    public string? Text { get; init; }
    /// <summary>Durable attachment metadata; binary content is stored separately.</summary>
    public ChatAttachmentReference? Attachment { get; init; }
    /// <summary>Tool invocation identity, shared by call and result parts.</summary>
    public string? CallId { get; init; }
    /// <summary>Invoked tool name.</summary>
    public string? ToolName { get; init; }
    /// <summary>JSON tool arguments or results; credentials are redacted in inspection snapshots.</summary>
    public JsonElement? Data { get => _data; init => _data = value?.Clone(); }
    /// <summary>Failure description for an unsuccessful tool result.</summary>
    public string? Error { get; init; }
    /// <summary>Provider-owned encrypted reasoning or signature needed for replay; never exposed in request inspectors.</summary>
    public string? ProtectedData { get; init; }
    /// <summary>Original reasoning item identity needed by the OpenAI Responses protocol; hidden from inspectors.</summary>
    public string? ReasoningItemId { get; init; }
    /// <summary>
    /// Observed reasoning transport used for replay, such as a native field or leading think tags.
    /// Null leaves transport unspecified; applications must not infer it from a model name.
    /// </summary>
    public string? ReasoningFormat { get; init; }
    /// <summary>Provider that produced this reasoning. Replay to another provider omits provider-specific reasoning.</summary>
    public string? OriginProviderId { get; init; }
    /// <summary>Model that produced provider-specific reasoning state.</summary>
    public string? OriginModelName { get; init; }

    /// <summary>Creates a text part.</summary>
    public static ChatContentPart FromText(string text) => new() { Kind = ChatContentKind.Text, Text = text };
    /// <summary>Creates an attachment part from previously uploaded metadata.</summary>
    public static ChatContentPart FromAttachment(ChatAttachmentReference attachment)
        => new() { Kind = ChatContentKind.Attachment, Attachment = attachment };
}

/// <summary>SDK-independent request history, including tool call/result pairs and attachment references.</summary>
public sealed record ChatContextMessage
{
    private IReadOnlyList<ChatContentPart> _parts = [];
    /// <summary>Conversation turn that owns this message; summaries have no turn.</summary>
    public string? TurnId { get; init; }
    /// <summary>Message role.</summary>
    public AIChatRole Role { get; init; }
    /// <summary>Ordered message contents.</summary>
    public IReadOnlyList<ChatContentPart> Parts { get => _parts; init => _parts = ChatSnapshotOwnership.Own(value); }
    /// <summary>Whether this message summarizes a compacted portion of the conversation.</summary>
    public bool IsSummary { get; init; }
}

/// <summary>A user submission containing text and already persisted attachment references in display order.</summary>
public sealed record ChatUserInput
{
    private IReadOnlyList<ChatContentPart> _parts = [];
    /// <summary>Ordered user input; only text and attachment parts are accepted.</summary>
    public required IReadOnlyList<ChatContentPart> Parts { get => _parts; init => _parts = ChatSnapshotOwnership.Own(value); }
    /// <summary>Creates a plain-text submission.</summary>
    public static ChatUserInput FromText(string text) => new() { Parts = [ChatContentPart.FromText(text)] };
    /// <summary>Combined text for titles and transcript previews.</summary>
    [JsonIgnore]
    public string Text => string.Concat(Parts.Where(part => part.Kind == ChatContentKind.Text).Select(part => part.Text));
}
