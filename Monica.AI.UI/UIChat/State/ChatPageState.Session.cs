using Monica.AI.Models;
using Monica.Core.Results;
using MudBlazor;

namespace Monica.AI.UI.UIChat.State;

public sealed partial class ChatPageState
{
    private async Task EnsureSessionExistsAsync()
    {
        if (workspace.CurrentSession is not null) return;
        if (workspace.Sessions.Count > 0)
            _ = await workspace.SelectSessionAsync(workspace.Sessions[0].SessionId, _lifetime.Token);
        else if (CurrentProviderId is not null)
            _ = await TryCreateSessionAsync();
    }

    /// <summary>Creates and selects a durable conversation.</summary>
    public async Task CreateNewSessionAsync()
    {
        if (IsSending || IsUploading || _disposed) return;
        await DiscardAttachmentsAsync();
        _ = await TryCreateSessionAsync();
        InspectedStep = null;
        NotifyStateChanged();
    }

    /// <summary>Selects a conversation within the current identity partition.</summary>
    public async Task SelectSessionAsync(string id)
    {
        if (IsSending || IsUploading || _disposed) return;
        await DiscardAttachmentsAsync();
        _ = await workspace.SelectSessionAsync(id, _lifetime.Token);
        InspectedStep = null;
        ErrorMessage = null;
        NotifyStateChanged();
    }

    /// <summary>Deletes one conversation and its durable content.</summary>
    public async Task DeleteSessionAsync(string id)
    {
        if (IsSending || IsUploading || _disposed) return;
        if (id == CurrentSessionId) await DiscardAttachmentsAsync();
        if (!await workspace.RemoveSessionAsync(id, _lifetime.Token)) return;
        await EnsureSessionExistsAsync();
        NotifyStateChanged();
    }

    /// <summary>Confirms deletion of every conversation in the current identity partition.</summary>
    public async Task ClearSessionsAsync()
    {
        if (IsSending || IsUploading || _disposed || workspace.Sessions.Count == 0) return;
        var confirmed = await dialogService.ShowMessageBoxAsync(
            localizer["Chat:History:Clear:Title"], localizer["Chat:History:Clear:Message"],
            yesText: localizer["Chat:History:Clear:Confirm"], noText: localizer["Common:Actions:Cancel"]);
        if (_disposed || confirmed != true) return;
        await DiscardAttachmentsAsync();
        if (await workspace.ClearAsync(_lifetime.Token)) await EnsureSessionExistsAsync();
        NotifyStateChanged();
    }

    /// <summary>Explicitly discards conflicting in-memory edits and reloads durable history.</summary>
    public async Task ReloadHistoryAsync()
    {
        if (IsSending || IsUploading || _disposed) return;
        var confirmed = await dialogService.ShowMessageBoxAsync(localizer["Workbench:ReloadHistory"],
            localizer["Workbench:ReloadHistoryConfirm"], yesText: localizer["Workbench:ReloadHistory"],
            noText: localizer["Common:Actions:Cancel"]);
        if (_disposed || confirmed != true) return;
        await DiscardAttachmentsAsync();
        await workspace.ReloadAsync(_lifetime.Token);
        InspectedStep = null;
        NotifyStateChanged();
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
