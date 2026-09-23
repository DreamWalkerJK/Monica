using Monica.AI.Chat.Models;

namespace Monica.AI.Chat.Abstractions;

/// <summary>Persists immutable attachments within the same partition as their conversation.</summary>
/// <remarks>
/// Implementations must enforce configured size/type limits, use opaque identifiers, and return
/// canonical metadata. Supplied streams remain caller-owned. Cancellation before commit must not
/// publish partial attachments. Missing reads throw <see cref="FileNotFoundException"/>.
/// </remarks>
public interface IChatAttachmentStore
{
    /// <summary>
    /// Validates and durably stores one attachment before returning its reference. The conversation must already be
    /// persisted and active when attachment publication occurs, including when its state changes during the upload.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The conversation is absent or was permanently deleted.</exception>
    /// <exception cref="InvalidOperationException">The conversation was archived before the attachment could be published.</exception>
    Task<ChatAttachmentReference> SaveAsync(ChatHistoryPartition partition, string sessionId,
        string fileName, string mediaType, Stream content, CancellationToken ct = default);

    /// <summary>Reads canonical bytes and metadata from one conversation's partition.</summary>
    Task<ChatAttachmentData> ReadAsync(ChatHistoryPartition partition, string sessionId,
        string attachmentId, CancellationToken ct = default);

    /// <summary>Removes an uncommitted draft attachment. Conversation-owned references must be retained.</summary>
    Task DeleteAsync(ChatHistoryPartition partition, string sessionId,
        string attachmentId, CancellationToken ct = default);
}
