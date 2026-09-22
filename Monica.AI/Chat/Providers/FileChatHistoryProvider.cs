using Microsoft.Extensions.Logging;
using Monica.AI.Chat.Abstractions;
using Monica.AI.Chat.Models;
using Monica.AI.Chat.Services;
using Monica.AI.Storage.Providers;

namespace Monica.AI.Chat.Providers;

/// <summary>Publishes immutable session snapshots through one atomic, revisioned partition catalog.</summary>
internal sealed class FileChatHistoryProvider(
    AIFileStore files, IChatAttachmentStore attachments, ILogger<FileChatHistoryProvider> logger)
    : IChatHistoryProvider
{
    public Task<ChatHistoryCatalog> GetCatalogAsync(ChatHistoryPartition partition, CancellationToken ct = default)
        => files.WithLockAsync(ChatStoragePaths.Manifest(partition), async token =>
            (await FileChatCatalog.ReadAsync(files, partition, token)).ToCatalog(), ct);

    public Task<ChatSessionSnapshot?> LoadSessionAsync(
        ChatHistoryPartition partition, string sessionId, CancellationToken ct = default)
        => files.WithLockAsync<ChatSessionSnapshot?>(ChatStoragePaths.Manifest(partition), async token =>
        {
            var manifest = await FileChatCatalog.ReadAsync(files, partition, token);
            return await manifest.LoadSessionAsync(files, partition, sessionId, token);
        }, ct);

    public Task<ChatHistoryWriteResult> SaveSessionAsync(
        ChatHistoryPartition partition, ChatSessionSnapshot snapshot, long expectedRevision, CancellationToken ct = default)
        => MutateAsync(partition, expectedRevision, async (manifest, token) =>
        {
            ChatSessionSnapshotValidator.Validate(snapshot);
            foreach (var reference in ChatSnapshotAttachments.Enumerate(snapshot).Distinct())
            {
                var attachment = await attachments.ReadAsync(partition, snapshot.SessionId, reference.Id, token);
                if (attachment.Reference != reference
                    || attachment.Data.LongLength != reference.Size)
                {
                    throw new InvalidDataException("The conversation refers to a missing or inconsistent attachment.");
                }
            }

            var committed = snapshot with { Revision = manifest.Revision + 1 };
            var fileId = Guid.NewGuid().ToString("N");
            // A crash before catalog publication can leave only an unreferenced snapshot, never a torn session.
            await files.WriteAsync(FileChatCatalog.SnapshotPath(partition, snapshot.SessionId, fileId), committed, token);
            manifest.Sessions[snapshot.SessionId] = new FileChatSnapshotEntry(fileId, committed.ToSummary());
            return () => CleanupUnreferencedSnapshots(partition, snapshot.SessionId, fileId);
        }, ct, manifest => manifest.Sessions.TryGetValue(snapshot.SessionId, out var existing)
            ? existing.Summary.Revision == snapshot.Revision : snapshot.Revision == 0);

    public Task<ChatHistoryWriteResult> DeleteSessionAsync(
        ChatHistoryPartition partition, string sessionId, long expectedRevision, CancellationToken ct = default)
        => MutateAsync(partition, expectedRevision, (manifest, _) =>
        {
            manifest.Sessions.Remove(sessionId);
            if (manifest.CurrentSessionId == sessionId)
            {
                manifest.CurrentSessionId = null;
            }

            return Task.FromResult<Action?>(() => CleanupDirectory(ChatStoragePaths.Session(partition, sessionId)));
        }, ct);

    public Task<ChatHistoryWriteResult> ClearAsync(
        ChatHistoryPartition partition, long expectedRevision, CancellationToken ct = default)
        => MutateAsync(partition, expectedRevision, (manifest, _) =>
        {
            manifest.Sessions.Clear();
            manifest.CurrentSessionId = null;
            return Task.FromResult<Action?>(() => CleanupDirectory(Path.Combine(ChatStoragePaths.Partition(partition), "sessions")));
        }, ct);

    public Task<ChatHistoryWriteResult> SetCurrentSessionAsync(
        ChatHistoryPartition partition, string? sessionId, long expectedRevision, CancellationToken ct = default)
        => MutateAsync(partition, expectedRevision, (manifest, _) =>
        {
            if (sessionId is not null && !manifest.Sessions.ContainsKey(sessionId))
            {
                throw new KeyNotFoundException("The selected conversation does not exist in the current partition.");
            }

            manifest.CurrentSessionId = sessionId;
            return Task.FromResult<Action?>(null);
        }, ct);

    private Task<ChatHistoryWriteResult> MutateAsync(
        ChatHistoryPartition partition,
        long expectedRevision,
        Func<FileChatCatalog, CancellationToken, Task<Action?>> mutate,
        CancellationToken ct,
        Func<FileChatCatalog, bool>? sessionRevisionMatches = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(expectedRevision);
        return files.WithLockAsync(ChatStoragePaths.Manifest(partition), async token =>
        {
            var manifest = await FileChatCatalog.ReadAsync(files, partition, token);
            if (manifest.Revision != expectedRevision || sessionRevisionMatches?.Invoke(manifest) == false)
            {
                return new ChatHistoryWriteResult
                {
                    Status = ChatHistoryWriteStatus.Conflict,
                    Revision = manifest.Revision,
                    FailureReason = ChatHistoryWriteFailureReason.Conflict
                };
            }

            var cleanup = await mutate(manifest, token);
            manifest.Revision++;
            await files.WriteAsync(ChatStoragePaths.Manifest(partition), manifest, token);
            cleanup?.Invoke();
            return new ChatHistoryWriteResult { Status = ChatHistoryWriteStatus.Succeeded, Revision = manifest.Revision };
        }, ct);
    }

    private void CleanupUnreferencedSnapshots(ChatHistoryPartition partition, string sessionId, string retainedFileId)
    {
        var directory = files.GetPath(ChatStoragePaths.Session(partition, sessionId));
        var retainedPath = files.GetPath(FileChatCatalog.SnapshotPath(partition, sessionId, retainedFileId));
        try
        {
            foreach (var path in Directory.EnumerateFiles(directory, "snapshot-*.json"))
            {
                if (!string.Equals(path, retainedPath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(path);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Could not remove an unreferenced chat snapshot.");
        }
    }

    private void CleanupDirectory(string relativePath)
    {
        var path = files.GetPath(relativePath);
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex, "A deleted conversation has residual files requiring cleanup.");
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "A deleted conversation has residual files requiring cleanup.");
        }
    }

}
