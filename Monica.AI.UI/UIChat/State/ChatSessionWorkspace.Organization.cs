namespace Monica.AI.UI.UIChat.State;

public sealed partial class ChatSessionWorkspace
{
    /// <summary>Persists the pin independently of the conversation snapshot.</summary>
    public async Task<bool> SetPinnedAsync(string sessionId, bool isPinned, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsConflicted) return false;
        var result = await historyFacade.SetPinnedAsync(sessionId, isPinned, Revision, ct);
        if (!TryApplyWriteResult(result, out _)) return false;
        var index = _sessions.FindIndex(item => item.SessionId == sessionId);
        if (index >= 0) _sessions[index] = _sessions[index] with { IsPinned = isPinned };
        SortSummaries();
        StateChanged?.Invoke();
        return true;
    }

    /// <summary>Archives or restores an atomic selection without deleting transcripts or attachments.</summary>
    public async Task<bool> SetArchivedAsync(IReadOnlyList<string> sessionIds, bool isArchived, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsConflicted) return false;
        var result = await historyFacade.SetArchivedAsync(sessionIds, isArchived, Revision, ct);
        if (!TryApplyWriteResult(result, out _)) return false;
        var ids = sessionIds.ToHashSet(StringComparer.Ordinal);
        var archivedAt = DateTimeOffset.UtcNow;
        for (var index = 0; index < _sessions.Count; index++)
        {
            if (ids.Contains(_sessions[index].SessionId) && (_sessions[index].ArchivedAt is not null) != isArchived)
                _sessions[index] = _sessions[index] with { IsPinned = false, ArchivedAt = isArchived ? archivedAt : null };
        }
        SortSummaries();
        if (isArchived)
        {
            foreach (var id in ids)
                if (_loadedSessions.Remove(id, out var session)) await session.DisposeAsync();
            if (CurrentSessionId is { } current && ids.Contains(current))
            {
                CurrentSessionId = Sessions.FirstOrDefault()?.SessionId;
                if (CurrentSessionId is not null) await LoadSessionAsync(CurrentSessionId, ct);
                await PersistCurrentSelectionAsync(ct);
            }
        }
        StateChanged?.Invoke();
        return true;
    }

    /// <summary>Permanently removes only archived conversations accepted at the current catalog revision.</summary>
    public async Task<bool> DeleteArchivedSessionsAsync(IReadOnlyList<string> sessionIds, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsConflicted) return false;
        var result = await historyFacade.DeleteArchivedSessionsAsync(sessionIds, Revision, ct);
        if (!TryApplyWriteResult(result, out _)) return false;
        await ApplyPrunedSessionsAsync(sessionIds);
        StateChanged?.Invoke();
        return true;
    }
}
