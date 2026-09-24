using Monica.AI.Chat.Abstractions;
using Monica.AI.Chat.Models;
using Monica.Core.Extensions;
using Monica.Core.Results;

namespace Monica.AI.Chat.Facades;

/// <summary>Provides attachment upload, preview, and draft removal for the current trusted partition.</summary>
public sealed class ChatAttachmentFacade(
    IChatAttachmentStore store,
    IChatHistoryPartitionResolver partitions)
{
    /// <summary>
    /// Stores a supported image or document before it is attached to a message. The caller owns the
    /// stream and should dispose it after this method completes. Size and document limits are host-configured.
    /// The conversation must be persisted and active; an upload completing after archival or deletion is rejected.
    /// </summary>
    public async Task<Res<ChatAttachmentReference>> UploadAsync(string sessionId, string fileName,
        string mediaType, Stream content, CancellationToken ct = default)
    {
        try
        {
            return Res.Ok(await store.SaveAsync(await partitions.ResolveAsync(ct), sessionId,
                fileName, mediaType, content, ct));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Res.Fail(ex.GetMessageRecursively());
        }
    }

    /// <summary>Reads a conversation attachment for a preview or inspector within the current partition.</summary>
    public async Task<Res<ChatAttachmentData>> ReadAsync(string sessionId, string attachmentId, CancellationToken ct = default)
    {
        try
        {
            return Res.Ok(await store.ReadAsync(await partitions.ResolveAsync(ct), sessionId, attachmentId, ct));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Res.Fail(ex.GetMessageRecursively());
        }
    }

    /// <summary>Removes a staged attachment that has not been committed to a conversation.</summary>
    public async Task<Res> DeleteAsync(string sessionId, string attachmentId, CancellationToken ct = default)
    {
        try
        {
            await store.DeleteAsync(await partitions.ResolveAsync(ct), sessionId, attachmentId, ct);
            return Res.Ok();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Res.Fail(ex.GetMessageRecursively());
        }
    }
}
