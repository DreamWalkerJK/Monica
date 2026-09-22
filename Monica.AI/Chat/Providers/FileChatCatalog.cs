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

    public ChatHistoryCatalog ToCatalog() => new()
    {
        Revision = Revision,
        CurrentSessionId = CurrentSessionId,
        Sessions = Sessions.Values.Select(entry => entry.Summary).OrderByDescending(item => item.UpdatedAt).ToArray()
    };

    public static async Task<FileChatCatalog> ReadAsync(
        AIFileStore files, ChatHistoryPartition partition, CancellationToken ct)
    {
        var catalog = await files.ReadAsync<FileChatCatalog>(ChatStoragePaths.Manifest(partition), ct) ?? new();
        if (catalog.Revision < 0 || catalog.Sessions is null
            || catalog.CurrentSessionId is { } selected && !catalog.Sessions.ContainsKey(selected)
            || catalog.Sessions.Any(item => item.Value is null || item.Value.Summary is null
                || item.Key != item.Value.Summary.SessionId || item.Value.Summary.Revision > catalog.Revision
                || !Guid.TryParseExact(item.Value.FileId, "N", out _)))
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
