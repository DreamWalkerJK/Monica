using Monica.AI.Models;
using Monica.AI.UI.UIChat.Support;
using Monica.AI.UI.UIProvider.Components;
using Monica.Core.Results;
using MudBlazor;

namespace Monica.AI.UI.UIChat.State;

public sealed partial class ChatPageState
{
    /// <summary>Atomically selects a provider and model for subsequent requests.</summary>
    public async Task SelectModelAsync((string ProviderId, string ModelName) selection)
    {
        if (IsHistoryBusy || HasHistoryConflict || _disposed) return;
        RefreshProviders();
        var provider = ChatProviderResolver.FindProvider(Providers, selection.ProviderId);
        if (provider is not { IsValid: true } || !ChatProviderResolver.GetChatModels(provider).Any(model => model.ModelName == selection.ModelName)) return;
        if (CurrentSession is { } session && !await SaveSettingsAsync(session.Settings with { ProviderId = selection.ProviderId, ModelName = selection.ModelName, ReasoningLevel = null })) return;
        if (_disposed) return;
        DefaultProviderId = selection.ProviderId;
        DefaultModelName = selection.ModelName;
        NotifyStateChanged();
    }

    /// <summary>Persists a model-defined reasoning level for the next request.</summary>
    public async Task ChangeReasoningAsync(string? level)
    {
        if (CurrentSession is { } session && !IsSending && !IsUploading && !HasHistoryConflict && !_disposed)
            await SaveSettingsAsync(session.Settings with { ReasoningLevel = level });
    }

    /// <summary>Updates automatic compaction for this conversation.</summary>
    public async Task SetAutomaticCompactionAsync(bool enabled)
    {
        if (CurrentSession is { } session && !IsSending)
            await SaveSettingsAsync(session.Settings with { AutomaticCompaction = enabled });
    }

    /// <summary>Updates the occupancy threshold and complete recent turns retained during compaction.</summary>
    public async Task SetCompactionPreferencesAsync((double Threshold, int RetainedTurns, int? OutputBudget) preferences)
    {
        if (CurrentSession is { } session && !IsSending && !_disposed)
            await SaveSettingsAsync(session.Settings with { CompactionThreshold = preferences.Threshold, RetainedTurns = preferences.RetainedTurns, MaxOutputTokens = preferences.OutputBudget });
    }

    /// <summary>Edits conversation instructions; changes affect the next request.</summary>
    public async Task OpenPromptSettingsAsync()
    {
        if (CurrentSession is not { } session || IsHistoryBusy || _disposed) return;
        var parameters = new DialogParameters
        {
            [nameof(ProviderSystemPromptDialog.ProviderName)] = CurrentProviderName,
            [nameof(ProviderSystemPromptDialog.SystemPrompt)] = session.SystemPrompt,
            [nameof(ProviderSystemPromptDialog.IsReadOnly)] = false
        };
        var dialog = await dialogService.ShowAsync<ProviderSystemPromptDialog>(
            localizer["Provider:Settings:SystemPrompt"], parameters,
            new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true });
        var result = await dialog.Result;
        if (_disposed || IsHistoryBusy || CurrentSession != session || result is not { Canceled: false }) return;
        await SaveSettingsAsync(session.Settings with { SystemPrompt = result.Data as string });
    }

    private async Task<bool> SaveSettingsAsync(ChatSessionSettings settings)
    {
        if (_disposed || IsHistoryBusy || HasHistoryConflict || CurrentSession is not { } session) return false;
        var result = chatFacade.UpdateSettings(session, settings);
        if (result.IsFailed(out var error)) { SetError(error.Message); return false; }
        var saved = await workspace.SaveSessionAsync(session, _lifetime.Token);
        NotifyStateChanged();
        return saved;
    }
}
