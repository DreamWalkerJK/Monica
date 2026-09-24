using Monica.AI.Chat.Models;

namespace Monica.AI.Chat.Abstractions;

/// <summary>
/// Persists complete chat session snapshots inside explicitly resolved partitions.
/// </summary>
/// <remarks>
/// Implementations must apply mutations atomically with the catalog and reject stale expected
/// revision values by returning a conflict result. Every non-success result must expose a
/// provider-neutral <see cref="ChatHistoryWriteFailureReason"/>; warning text is diagnostic only.
/// Implementations own serialized snapshot security, retention, and disposal of external resources.
/// </remarks>
public interface IChatHistoryProvider
{
    /// <summary>Loads active and archived session summaries and the current active selection for a partition.</summary>
    Task<ChatHistoryCatalog> GetCatalogAsync(
        ChatHistoryPartition partition,
        CancellationToken ct = default);

    /// <summary>Loads one complete session snapshot, or <see langword="null"/> when absent.</summary>
    Task<ChatSessionSnapshot?> LoadSessionAsync(
        ChatHistoryPartition partition,
        string sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Saves one complete snapshot when the catalog revision and the snapshot's previous session revision match.
    /// New sessions must have snapshot revision zero; stale or deleted sessions must return a conflict instead of
    /// overwriting newer content or recreating a deleted conversation. Archived sessions must also return a conflict;
    /// active saves must preserve catalog-owned pin and archive metadata.
    /// </summary>
    Task<ChatHistoryWriteResult> SaveSessionAsync(
        ChatHistoryPartition partition,
        ChatSessionSnapshot snapshot,
        long expectedRevision,
        CancellationToken ct = default);

    /// <summary>Changes an active conversation's pin without changing its snapshot revision or transcript recency.</summary>
    /// <exception cref="KeyNotFoundException">The conversation does not exist in this partition.</exception>
    /// <exception cref="InvalidOperationException">The conversation is archived.</exception>
    Task<ChatHistoryWriteResult> SetPinnedAsync(
        ChatHistoryPartition partition,
        string sessionId,
        bool isPinned,
        long expectedRevision,
        CancellationToken ct = default);

    /// <summary>
    /// Atomically archives or restores all specified conversations. Archiving clears their pins and any matching
    /// current selection; restoring does not select or pin them. Already matching states are left unchanged.
    /// </summary>
    /// <param name="partition">Trusted user/workspace partition.</param>
    /// <param name="sessionIds">Nonempty set of conversation IDs. Every ID must exist before any mutation is published.</param>
    /// <param name="isArchived">Whether to archive the conversations or restore them to the active list.</param>
    /// <param name="expectedRevision">Required current catalog revision.</param>
    /// <param name="ct">Cancellation for acquiring storage and publishing the catalog.</param>
    /// <exception cref="KeyNotFoundException">At least one conversation does not exist in this partition.</exception>
    Task<ChatHistoryWriteResult> SetArchivedAsync(
        ChatHistoryPartition partition,
        IReadOnlyList<string> sessionIds,
        bool isArchived,
        long expectedRevision,
        CancellationToken ct = default);

    /// <summary>
    /// Permanently deletes a nonempty set of archived conversations and their retained attachments. All IDs must
    /// resolve to archived conversations before the catalog mutation is published. Providers must report any
    /// residual-file cleanup warning and retain enough information to retry cleanup after a restart.
    /// </summary>
    /// <exception cref="KeyNotFoundException">At least one conversation does not exist in this partition.</exception>
    /// <exception cref="InvalidOperationException">At least one conversation is active.</exception>
    Task<ChatHistoryWriteResult> DeleteArchivedSessionsAsync(
        ChatHistoryPartition partition,
        IReadOnlyList<string> sessionIds,
        long expectedRevision,
        CancellationToken ct = default);

    /// <summary>Deletes one session when the catalog revision matches.</summary>
    Task<ChatHistoryWriteResult> DeleteSessionAsync(
        ChatHistoryPartition partition,
        string sessionId,
        long expectedRevision,
        CancellationToken ct = default);

    /// <summary>Clears every persisted session and current selection in a partition.</summary>
    Task<ChatHistoryWriteResult> ClearAsync(
        ChatHistoryPartition partition,
        long expectedRevision,
        CancellationToken ct = default);

    /// <summary>Changes the current active-session selection when the catalog revision matches.</summary>
    /// <exception cref="InvalidOperationException">The requested conversation is archived.</exception>
    Task<ChatHistoryWriteResult> SetCurrentSessionAsync(
        ChatHistoryPartition partition,
        string? sessionId,
        long expectedRevision,
        CancellationToken ct = default);
}
