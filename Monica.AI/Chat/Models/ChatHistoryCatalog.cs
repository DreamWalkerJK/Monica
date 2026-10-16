using Monica.AI.Models;

namespace Monica.AI.Chat.Models;

/// <summary>Immutable catalog of persisted chat sessions in one partition.</summary>
public sealed record ChatHistoryCatalog
{
    /// <summary>Current serialized contract version.</summary>
    public const int CurrentVersion = 1;

    /// <summary>Contract version used to create this catalog.</summary>
    public int Version { get; init; } = CurrentVersion;

    /// <summary>Optimistic concurrency revision for the whole partition catalog.</summary>
    public long Revision { get; init; }

    /// <summary>All persisted session summaries, including archived conversations, ordered by the provider.</summary>
    public required IReadOnlyList<ChatSessionSummary> Sessions { get; init; }

    /// <summary>Identifier of the last selected active session, when one is persisted. Archived sessions cannot be selected.</summary>
    public string? CurrentSessionId { get; init; }

    /// <summary>Creates an empty catalog.</summary>
    public static ChatHistoryCatalog Empty { get; } = new() { Sessions = [] };
}

/// <summary>Immutable list item describing a persisted chat session.</summary>
public sealed record ChatSessionSummary
{
    /// <summary>Current serialized contract version.</summary>
    public const int CurrentVersion = 1;

    /// <summary>Contract version used to create this summary.</summary>
    public int Version { get; init; } = CurrentVersion;

    /// <summary>Unique session identifier.</summary>
    public required string SessionId { get; init; }

    /// <summary>Display title.</summary>
    public required string Title { get; init; }

    /// <summary>Session creation time.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Last transcript or settings update time.</summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Whether the active conversation is pinned. This catalog-owned preference does not change transcript recency.</summary>
    public bool IsPinned { get; init; }

    /// <summary>
    /// Time the conversation was archived, or <see langword="null"/> while active. Archiving clears its pin and
    /// current selection but retains the original transcript and attachments; restoring leaves it unpinned.
    /// </summary>
    public DateTimeOffset? ArchivedAt { get; init; }

    /// <summary>Read-only settings needed to continue the conversation.</summary>
    public required ChatSessionSettings Settings { get; init; }

    /// <summary>Revision assigned to the persisted session snapshot.</summary>
    public long Revision { get; init; }
}
