using System.Text.Json.Serialization;
using Monica.AI.Models;

namespace Monica.AI.Chat.Models;

/// <summary>Durable Monica-owned conversation contract, independent of provider SDK session serialization.</summary>
public sealed record ChatSessionSnapshot
{
    private IReadOnlyList<ChatTurnSnapshot> _turns = [];
    private IReadOnlyList<ChatContextMessage> _contextMessages = [];
    private IReadOnlyList<ChatExecutionStep> _executionSteps = [];
    /// <summary>Current contract version; older chat formats are intentionally unsupported.</summary>
    public const int CurrentVersion = 2;
    /// <summary>Serialized contract version.</summary>
    public int Version { get; init; } = CurrentVersion;
    /// <summary>Conversation identity.</summary>
    public required string SessionId { get; init; }
    /// <summary>Display title.</summary>
    public required string Title { get; init; }
    /// <summary>Conversation creation time.</summary>
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>Last durable update.</summary>
    public DateTimeOffset UpdatedAt { get; init; }
    /// <summary>Choices used for the next new turn.</summary>
    public required ChatSessionSettings Settings { get; init; }
    /// <summary>Original transcript and unabridged context for each turn.</summary>
    public required IReadOnlyList<ChatTurnSnapshot> Turns { get => _turns; init => _turns = ChatSnapshotOwnership.Own(value); }
    /// <summary>Current retained request context, including any compaction summary.</summary>
    public IReadOnlyList<ChatContextMessage> ContextMessages { get => _contextMessages; init => _contextMessages = ChatSnapshotOwnership.Own(value); }
    /// <summary>Single ordered source for request usage, tools, timings, and compaction history.</summary>
    public IReadOnlyList<ChatExecutionStep> ExecutionSteps { get => _executionSteps; init => _executionSteps = ChatSnapshotOwnership.Own(value); }
    /// <summary>Last measured and next estimated context occupancy.</summary>
    public ChatContextUsage ContextUsage { get; init; } = new();
    /// <summary>Revision assigned by the configured history provider.</summary>
    public long Revision { get; init; }
    /// <summary>Creates the light catalog projection.</summary>
    public ChatSessionSummary ToSummary() => new()
    {
        SessionId = SessionId, Title = Title, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt, Settings = Settings, Revision = Revision
    };
}

/// <summary>Original transcript and context belonging to one turn.</summary>
public sealed record ChatTurnSnapshot
{
    private IReadOnlyList<ChatMessageSnapshot> _errors = [];
    private IReadOnlyList<ChatContextMessage> _contextMessages = [];
    /// <summary>Stable turn identity.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    /// <summary>Recorded lifecycle status.</summary>
    public ChatExecutionStatus Status { get; init; }
    /// <summary>Submission time.</summary>
    public DateTimeOffset StartedAt { get; init; }
    /// <summary>Completion time, when terminal.</summary>
    public DateTimeOffset? CompletedAt { get; init; }
    /// <summary>Original SDK-independent messages retained for editing/retry after compaction.</summary>
    public IReadOnlyList<ChatContextMessage> ContextMessages { get => _contextMessages; init => _contextMessages = ChatSnapshotOwnership.Own(value); }
    /// <summary>Original user submission.</summary>
    public required ChatMessageSnapshot UserMessage { get; init; }
    /// <summary>Assistant transcript, including partial output after failure or cancellation.</summary>
    public ChatMessageSnapshot? AssistantMessage { get; init; }
    /// <summary>Execution failures following any partial output.</summary>
    public IReadOnlyList<ChatMessageSnapshot> ErrorMessages { get => _errors; init => _errors = ChatSnapshotOwnership.Own(value); }
}

/// <summary>Immutable ordered transcript content. Diagnostics belong exclusively to the execution ledger.</summary>
public sealed record ChatMessageSnapshot
{
    private IReadOnlyList<ChatContentPart> _parts = [];
    /// <summary>Stable message identity.</summary>
    public required string Id { get; init; }
    /// <summary>Message role.</summary>
    public AIChatRole Role { get; init; }
    /// <summary>Normal message or execution failure.</summary>
    public AIChatMessageKind Kind { get; init; }
    /// <summary>Original ordered content.</summary>
    public IReadOnlyList<ChatContentPart> Parts { get => _parts; init => _parts = ChatSnapshotOwnership.Own(value); }
    /// <summary>Plain-text projection for title/preview consumers, not separately persisted.</summary>
    [JsonIgnore]
    public string Content => string.Concat(Parts.Where(part => part.Kind == ChatContentKind.Text).Select(part => part.Text));
    /// <summary>Message creation time.</summary>
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>Model that produced the message, when known.</summary>
    public string? ModelName { get; init; }
    /// <summary>Provider that produced the message, when known.</summary>
    public string? ProviderId { get; init; }
}

internal static class ChatSnapshotOwnership
{
    public static IReadOnlyList<T> Own<T>(IEnumerable<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return Array.AsReadOnly(values.ToArray());
    }
}
