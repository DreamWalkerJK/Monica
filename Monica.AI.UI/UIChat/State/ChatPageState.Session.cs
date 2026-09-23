using Monica.AI.Models;
using Monica.Core.Results;

namespace Monica.AI.UI.UIChat.State;

public sealed partial class ChatPageState
{
    private async Task EnsureSessionExistsAsync()
    {
        if (workspace.CurrentSession is not null) return;
        if (workspace.Sessions.FirstOrDefault() is { } next)
            _ = await workspace.SelectSessionAsync(next.SessionId, _lifetime.Token);
        else if (CurrentProviderId is not null)
            _ = await TryCreateSessionAsync();
    }

    /// <summary>Creates and selects a durable conversation.</summary>
    public async Task CreateNewSessionAsync() => _ = await RunHistoryOperationAsync(async () =>
    {
        await DiscardAttachmentsAsync();
        _ = await TryCreateSessionAsync();
        InspectedStep = null;
        return true;
    });

    /// <summary>Selects a conversation within the current identity partition.</summary>
    public async Task SelectSessionAsync(string id) => _ = await RunHistoryOperationAsync(async () =>
    {
        await DiscardAttachmentsAsync();
        _ = await workspace.SelectSessionAsync(id, _lifetime.Token);
        InspectedStep = null;
        ErrorMessage = null;
        return true;
    });

    /// <summary>Toggles a conversation pin without altering its transcript.</summary>
    public async Task TogglePinAsync(string id) => _ = await RunHistoryOperationAsync(async () =>
    {
        var summary = Sessions.FirstOrDefault(item => item.SessionId == id);
        return summary is not null && await workspace.SetPinnedAsync(id, !summary.IsPinned, _lifetime.Token);
    });

    /// <summary>Archives a conversation while retaining its transcript and attachments.</summary>
    public async Task ArchiveSessionAsync(string id) => _ = await RunHistoryOperationAsync(async () =>
    {
        if (id == CurrentSessionId) await DiscardAttachmentsAsync();
        if (!await workspace.SetArchivedAsync([id], true, _lifetime.Token)) return false;
        InspectedStep = null;
        return true;
    });

    /// <summary>Restores selected archived conversations without changing the current selection.</summary>
    public Task<bool> RestoreArchivedSessionsAsync(IReadOnlyList<string> ids)
        => RunHistoryOperationAsync(() => workspace.SetArchivedAsync(ids, false, _lifetime.Token));

    /// <summary>Permanently removes the selection after the archive manager obtains explicit confirmation.</summary>
    public Task<bool> DeleteArchivedSessionsAsync(IReadOnlyList<string> ids)
        => RunHistoryOperationAsync(() => workspace.DeleteArchivedSessionsAsync(ids, _lifetime.Token));

    /// <summary>Explicitly discards conflicting in-memory edits and reloads durable history.</summary>
    public async Task ReloadHistoryAsync()
    {
        if (IsHistoryBusy || _disposed) return;
        var confirmed = await dialogService.ShowMessageBoxAsync(localizer["Workbench:ReloadHistory"],
            localizer["Workbench:ReloadHistoryConfirm"], yesText: localizer["Workbench:ReloadHistory"],
            noText: localizer["Common:Actions:Cancel"]);
        if (_disposed || IsHistoryBusy || confirmed != true) return;
        _ = await RunHistoryOperationAsync(async () =>
        {
            await DiscardAttachmentsAsync();
            await workspace.ReloadAsync(_lifetime.Token);
            InspectedStep = null;
            return true;
        }, allowConflict: true);
    }

    private Task<bool> RunHistoryOperationAsync(Func<Task<bool>> action, bool allowConflict = false)
    {
        if (_disposed || IsHistoryBusy || HasHistoryConflict && !allowConflict) return Task.FromResult(false);
        _historyOperation = RunHistoryOperationCoreAsync(action);
        return _historyOperation;
    }

    private async Task<bool> RunHistoryOperationCoreAsync(Func<Task<bool>> action)
    {
        IsHistoryUpdating = true;
        NotifyStateChanged();
        try { return await action(); }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { return false; }
        catch (Exception exception) { SetError(exception.Message); return false; }
        finally { IsHistoryUpdating = false; NotifyStateChanged(); }
    }

    private async Task<ChatSession?> TryCreateSessionAsync()
    {
        RefreshProviders();
        if (string.IsNullOrWhiteSpace(CurrentProviderId))
        {
            SetError(localizer["Provider:NoChatProvider"]);
            return null;
        }
        var result = await chatFacade.CreateSessionAsync(CurrentProviderId, CurrentModelName,
            systemPrompt: _options.DefaultSystemPrompt, runtimeContext: BuildRuntimeContext(SelectedKnowledgeBaseIds), ct: _lifetime.Token);
        if (result.IsFailed(out var error, out var session)) { SetError(error.Message); return null; }
        if (_disposed) { await session.DisposeAsync(); return null; }
        await workspace.AddSessionAsync(session, _lifetime.Token);
        ErrorMessage = null;
        return session;
    }
}
