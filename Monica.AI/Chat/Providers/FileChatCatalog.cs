using Monica.AI.Chat.Models;
using Monica.AI.Chat.Services;
using Monica.AI.Storage.Providers;

namespace Monica.AI.Chat.Providers;

/// <summary>Publication boundary shared by file history and attachment retention.</summary>
/// <remarks>Read or mutate this document only while holding its partition's file lock.</remarks>
internal sealed class FileChatCatalog
{
    public long Revision { get; set; }
    public string? CurrentSessionId { get; set; }
    public Dictionary<string, FileChatSnapshotEntry> Sessions { get; set; } = new(StringComparer.Ordinal);

    // Removed sessions stay queued until a later successful catalog mutation records cleanup completion.
    // Catalog reads may retry deletion, but must not create a new user-visible revision just for cleanup.
    public HashSet<string> PendingDeletionSessionIds { get; set; } = new(StringComparer.Ordinal);

    public ChatHistoryCatalog ToCatalog() => new()
    {
        Revision = Revision,
        CurrentSessionId = CurrentSessionId,
        Sessions = Sessions.Values.Select(entry => entry.Summary)
            .OrderByDescending(item => item.IsPinned)
            .ThenByDescending(item => item.UpdatedAt)
            .ThenBy(item => item.SessionId, StringComparer.Ordinal).ToArray()
    };

    public void SetPinned(string sessionId, bool isPinned)
    {
        var entry = RequireActiveSession(sessionId);
        Sessions[sessionId] = entry with { Summary = entry.Summary with { IsPinned = isPinned } };
    }

    public void SetArchived(IReadOnlyList<string> sessionIds, bool isArchived)
    {
        var entries = RequireSessions(sessionIds);
        var archivedAt = DateTimeOffset.UtcNow;
        foreach (var (sessionId, entry) in entries)
        {
            if ((entry.Summary.ArchivedAt is not null) == isArchived) continue;
            Sessions[sessionId] = entry with
            {
                Summary = entry.Summary with { IsPinned = false, ArchivedAt = isArchived ? archivedAt : null }
            };
            if (isArchived && CurrentSessionId == sessionId) CurrentSessionId = null;
        }
    }

    public void DeleteSessions(IReadOnlyList<string> sessionIds, bool archivedOnly)
    {
        var entries = RequireSessions(sessionIds);
        if (archivedOnly && entries.Any(item => item.Value.Summary.ArchivedAt is null))
        {
            throw new InvalidOperationException("Only archived conversations can be permanently deleted together.");
        }

        foreach (var (sessionId, _) in entries)
        {
            Sessions.Remove(sessionId);
            PendingDeletionSessionIds.Add(sessionId);
            if (CurrentSessionId == sessionId) CurrentSessionId = null;
        }
    }

    public FileChatSnapshotEntry RequireActiveSession(string sessionId)
    {
        var entry = RequireSession(sessionId);
        if (entry.Summary.ArchivedAt is not null)
        {
            throw new InvalidOperationException("The conversation is archived. Restore it before continuing.");
        }

        return entry;
    }

    private Dictionary<string, FileChatSnapshotEntry> RequireSessions(IReadOnlyList<string> sessionIds)
    {
        ArgumentNullException.ThrowIfNull(sessionIds);
        if (sessionIds.Count == 0) throw new ArgumentException("Choose at least one conversation.", nameof(sessionIds));
        return sessionIds.Distinct(StringComparer.Ordinal).ToDictionary(id => id, RequireSession, StringComparer.Ordinal);
    }

    private FileChatSnapshotEntry RequireSession(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return Sessions.TryGetValue(sessionId, out var entry)
            ? entry
            : throw new KeyNotFoundException("The conversation does not exist in the current partition.");
    }

    public static async Task<FileChatCatalog> ReadAsync(
        AIFileStore files, ChatHistoryPartition partition, CancellationToken ct)
    {
        var catalog = await files.ReadAsync<FileChatCatalog>(ChatStoragePaths.Manifest(partition), ct) ?? new();
        if (catalog.Revision < 0 || catalog.Sessions is null || catalog.PendingDeletionSessionIds is null
            || catalog.PendingDeletionSessionIds.Any(id => string.IsNullOrWhiteSpace(id) || catalog.Sessions.ContainsKey(id))
            || catalog.Sessions.Any(item => item.Value is null || item.Value.Summary is null
                || item.Key != item.Value.Summary.SessionId || item.Value.Summary.Revision > catalog.Revision
                || item.Value.Summary is { IsPinned: true, ArchivedAt: not null }
                || !Guid.TryParseExact(item.Value.FileId, "N", out _))
            || catalog.CurrentSessionId is { } selected
                && (!catalog.Sessions.TryGetValue(selected, out var current) || current.Summary.ArchivedAt is not null))
        {
            throw new InvalidDataException("The chat catalog is invalid.");
        }

        return catalog;
    }

    public async Task<ChatSessionSnapshot?> LoadSessionAsync(
        AIFileStore files, ChatHistoryPartition partition, string sessionId, CancellationToken ct)
    {
        if (!Sessions.TryGetValue(sessionId, out var entry)) return null;
        var snapshot = await files.ReadAsync<ChatSessionSnapshot>(SnapshotPath(partition, sessionId, entry.FileId), ct)
                       ?? throw new InvalidDataException("The catalog references a missing chat snapshot.");
        ChatSessionSnapshotValidator.Validate(snapshot, sessionId);
        if (snapshot.Revision != entry.Summary.Revision)
        {
            throw new InvalidDataException("The chat snapshot revision does not match the catalog.");
        }

        return snapshot;
    }

    public static string SnapshotPath(ChatHistoryPartition partition, string sessionId, string fileId)
    {
        if (!Guid.TryParseExact(fileId, "N", out _))
        {
            throw new InvalidDataException("The chat catalog contains an invalid snapshot identifier.");
        }

        return Path.Combine(ChatStoragePaths.Session(partition, sessionId), $"snapshot-{fileId}.json");
    }
}

internal sealed record FileChatSnapshotEntry(string FileId, ChatSessionSummary Summary);
