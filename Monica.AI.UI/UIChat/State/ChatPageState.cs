using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Monica.AI.AgentCapabilities.Models;
using Monica.AI.Chat.Facades;
using Monica.AI.Chat.Models;
using Monica.AI.Facades;
using Monica.AI.KnowledgeBase.Facades;
using Monica.AI.Models;
using Monica.AI.UI.Localization;
using Monica.AI.UI.UIChat.Components;
using Monica.AI.UI.UIChat.Models;
using Monica.AI.UI.UIChat.Support;
using Monica.AI.UI.Shared.Support;
using Microsoft.AspNetCore.Components;
using Monica.Modules;
using Monica.Core.Results;
using MudBlazor;
using KnowledgeBaseModel = Monica.AI.KnowledgeBase.Models.KnowledgeBase;

namespace Monica.AI.UI.UIChat.State;

/// <summary>
/// Component-owned orchestration for one workbench visit. The circuit workspace owns restored
/// sessions; this owner cancels and drains its own requests before it is disposed.
/// </summary>
public sealed partial class ChatPageState(
    ChatFacade chatFacade,
    ChatSessionWorkspace workspace,
    AgentCapabilityFacade capabilityFacade,
    ChatAttachmentFacade attachmentFacade,
    IOptions<ModuleAIUIOption> options,
    IOptions<ModuleAIOption> aiOptions,
    ISnackbar snackbar,
    IDialogService dialogService,
    KnowledgeBaseFacade knowledgeBaseFacade,
    IStringLocalizer<AIResource> localizer) : IAsyncDisposable
{
    private readonly CancellationTokenSource _lifetime = new();
    private readonly AIAdministrationOperationLifetime _operations = new();
    private Func<Action, Task>? _dialogDispatch;
    private readonly ModuleAIUIOption _options = options.Value;
    private readonly List<ChatAttachmentReference> _attachments = [];
    private CancellationTokenSource? _requestCancellation;
    private ChatSession? _operationSession;
    private Task? _initialization;
    private Task? _disposeTask;
    private bool _disposed;
    private bool _attached;

    /// <summary>Signals presentation changes while this owner is alive.</summary>
    public event Action? StateChanged;
    /// <summary>Whether history navigation is enabled by the host.</summary>
    public bool ShowSessionList => _options.ShowSessionList;
    /// <summary>Whether provider selection is enabled by the host.</summary>
    public bool ShowProviderSelector => _options.ShowProviderSelector;
    /// <summary>Whether rich Markdown rendering is enabled.</summary>
    public bool EnableMarkdown => _options.EnableMarkdown;
    /// <summary>Whether transcript tail following is enabled.</summary>
    public bool EnableAutoScroll => _options.EnableAutoScroll;
    /// <summary>Durable conversation catalog for the current identity partition.</summary>
    public IReadOnlyList<ChatSessionSummary> Sessions => workspace.Sessions;
    /// <summary>Archived conversations available for restoration or permanent removal.</summary>
    public IReadOnlyList<ChatSessionSummary> ArchivedSessions => workspace.ArchivedSessions;
    /// <summary>Whether a catalog action is currently being persisted.</summary>
    public bool IsHistoryUpdating { get; private set; }
    /// <summary>Whether conversation navigation or organization must wait for the current operation.</summary>
    public bool IsHistoryBusy => IsHistoryLoading || IsHistoryUpdating || IsSending || IsUploading || IsCommandRunning;
    /// <summary>Whether a settings, dialog, or staged-attachment command owns this visit.</summary>
    public bool IsCommandRunning { get; private set; }
    /// <summary>Whether input commands are unavailable independently of generation/upload and their Stop action.</summary>
    public bool IsInputBlocked => IsHistoryLoading || IsHistoryUpdating || IsCommandRunning || HasHistoryConflict || HasHistoryLoadFailure || _disposed;
    /// <summary>Selected conversation identifier.</summary>
    public string? CurrentSessionId => workspace.CurrentSessionId;
    /// <summary>Selected durable conversation, shared by Chat and Trajectory.</summary>
    public ChatSession? CurrentSession => workspace.CurrentSession;
    /// <summary>Whether history is still loading.</summary>
    public bool IsHistoryLoading => workspace.IsLoading || _initialization is { IsCompleted: false };
    /// <summary>Current selectable providers.</summary>
    public IReadOnlyList<AIProviderInfo> Providers { get; private set; } = [];
    /// <summary>Provider selected before a conversation is created.</summary>
    public string? DefaultProviderId { get; private set; }
    /// <summary>Model selected before a conversation is created.</summary>
    public string? DefaultModelName { get; private set; }
    /// <summary>Effective selected provider.</summary>
    public string? CurrentProviderId => CurrentSession?.ProviderId ?? DefaultProviderId;
    /// <summary>Effective selected model.</summary>
    public string? CurrentModelName => CurrentSession?.ModelName ?? DefaultModelName;
    /// <summary>Display name of the selected provider.</summary>
    public string CurrentProviderName => CurrentProvider?.DisplayName ?? CurrentProviderId ?? string.Empty;
    /// <summary>Selectable chat models for the selected provider.</summary>
    public IReadOnlyList<AIModelInfo> CurrentProviderModels => ChatProviderResolver.GetChatModels(CurrentProvider);
    /// <summary>Selected model capability metadata; null means unavailable.</summary>
    public LLMModelInfo? CurrentModel => ChatProviderResolver.ResolveLLMModel(CurrentProviderModels, CurrentModelName);
    /// <summary>Whether a request, upload, or compaction is running.</summary>
    public bool IsSending { get; private set; }
    /// <summary>Whether attachment upload is running.</summary>
    public bool IsUploading { get; private set; }
    /// <summary>Whether local edits conflict with a newer durable conversation revision.</summary>
    public bool HasHistoryConflict => workspace.IsConflicted;
    /// <summary>Whether history must be explicitly reloaded after an unreadable catalog.</summary>
    public bool HasHistoryLoadFailure => workspace.HasLoadFailure;
    /// <summary>Last actionable error for this page visit.</summary>
    public string? ErrorMessage { get; private set; }
    /// <summary>Current attachments that have been persisted but not submitted.</summary>
    public IReadOnlyList<ChatAttachmentReference> Attachments => _attachments;
    /// <summary>Available knowledge bases for tool context.</summary>
    public IReadOnlyList<KnowledgeBaseModel> KnowledgeBases { get; private set; } = [];
    /// <summary>Selected knowledge bases for subsequent requests.</summary>
    public List<string> SelectedKnowledgeBaseIds { get; private set; } = [];
    /// <summary>Skill and MCP reference completion candidates.</summary>
    public IReadOnlyList<AgentCapabilityReferenceCandidate> CapabilityCandidates { get; private set; } = [];
    /// <summary>Step selected for the shared trajectory inspector.</summary>
    public ChatExecutionStep? InspectedStep { get; private set; }
    private AIProviderInfo? CurrentProvider => ChatProviderResolver.FindProvider(Providers, CurrentProviderId);

    /// <summary>Initializes server-owned data without browser interop.</summary>
    public Task InitializeAsync() => _initialization ??= _operations.RunAsync(_ => InitializeCoreAsync());

    /// <summary>Binds the owning component's renderer dispatcher before initialization. The dispatcher is borrowed.</summary>
    public void SetDialogDispatcher(Func<Action, Task> dispatch)
    {
        ArgumentNullException.ThrowIfNull(dispatch);
        if (_initialization is not null || _disposed) throw new InvalidOperationException("Bind the renderer before initializing the chat visit.");
        _dialogDispatch = dispatch;
    }

    private Task<bool> RunCommandAsync(Func<Task<bool>> command, bool allowConflict = false)
    {
        if (_disposed || IsHistoryBusy || (HasHistoryConflict || HasHistoryLoadFailure) && !allowConflict) return Task.FromResult(false);
        IsCommandRunning = true;
        return _operations.RunAsync(async _ =>
        {
            NotifyStateChanged();
            try { return await command(); }
            catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { return false; }
            catch (Exception exception) { SetError(exception.Message); return false; }
            finally { IsCommandRunning = false; NotifyStateChanged(); }
        });
    }

    private async Task<DialogResult?> ShowDialogAsync<T>(string title, DialogParameters parameters,
        DialogOptions options, CancellationToken token) where T : IComponent
    {
        token.ThrowIfCancellationRequested();
        var dispatch = _dialogDispatch ?? throw new InvalidOperationException("The chat visit requires its renderer dispatcher for native dialogs.");
        // ShowAsync itself cannot be canceled. Adopt and close a late reference before draining it.
        var reference = await dialogService.ShowAsync<T>(title, parameters, options);
        Task closing = Task.CompletedTask;
        using var cancellation = token.Register(() =>
        {
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            closing = completion.Task;
            _ = CloseAsync(completion);
        });
        async Task CloseAsync(TaskCompletionSource completion)
        {
            try { await dispatch(() => reference.Close(DialogResult.Cancel())); completion.TrySetResult(); }
            catch (Exception exception)
            {
                try { reference.Dismiss(DialogResult.Cancel()); }
                catch (Exception failure) { exception = new AggregateException(exception, failure); }
                completion.TrySetException(exception);
            }
        }
        try { return await reference.Result; }
        finally
        {
            if (reference.Dialog is IAsyncDisposable owner) await Task.WhenAll(closing, owner.DisposeAsync().AsTask());
            else await closing;
        }
    }

    private async Task InitializeCoreAsync()
    {
        workspace.StateChanged += NotifyStateChanged;
        workspace.WarningRaised += OnWorkspaceWarning;
        _attached = true;
        try
        {
            RefreshProviders();
            SelectedKnowledgeBaseIds = [.. _options.DefaultKnowledgeBaseIds];
            await workspace.InitializeAsync(_lifetime.Token);
            if (_disposed) return;
            await LoadKnowledgeBasesAsync();
            if (_disposed) return;
            await LoadCapabilityCandidatesAsync();
            if (_disposed) return;
            await EnsureSessionExistsAsync();
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception exception) { SetError(exception.Message); }
        finally { NotifyStateChanged(); }
    }

    /// <summary>Refreshes selectable provider metadata after operator settings changes.</summary>
    public void RefreshProviders()
    {
        Providers = ChatProviderResolver.GetChatProviders(chatFacade.GetProviders());
        var provider = ChatProviderResolver.GetPreferredChatProvider(Providers, chatFacade.GetDefaultProvider()?.ProviderId);
        DefaultProviderId ??= provider?.ProviderId;
        DefaultModelName ??= ChatProviderResolver.GetPreferredChatModel(provider, provider?.DefaultModel);
    }

    /// <summary>Opens the execution record in the Trajectory inspector.</summary>
    public void Inspect(ChatExecutionStep step) { InspectedStep = step; NotifyStateChanged(); }
    /// <summary>Dismisses the latest operation error.</summary>
    public void DismissError() { ErrorMessage = null; NotifyStateChanged(); }
    private void SetError(string? message)
    {
        if (_disposed) return;
        ErrorMessage = message ?? localizer["Error:Generic"];
        NotifyStateChanged();
    }
    private void NotifyStateChanged() { if (!_disposed) StateChanged?.Invoke(); }
    private void OnWorkspaceWarning(ChatHistoryWorkspaceWarning warning)
    {
        if (_disposed) return;
        var message = warning.Kind switch
        {
            ChatHistoryWorkspaceWarningKind.RevisionConflict => localizer["Chat:History:Warnings:RevisionConflict"],
            ChatHistoryWorkspaceWarningKind.SessionUnavailable => localizer["Chat:History:Warnings:SessionUnavailable"],
            ChatHistoryWorkspaceWarningKind.PersistenceWarning => localizer["Chat:History:Warnings:PersistenceWarning", warning.Details ?? string.Empty],
            _ => localizer["Chat:History:Warnings:StorageUnavailable"]
        };
        snackbar.Add(message, Severity.Warning);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => new(_disposeTask ??= DisposeCoreAsync());
    private async Task DisposeCoreAsync()
    {
        _disposed = true;
        if (_attached)
        {
            workspace.StateChanged -= NotifyStateChanged;
            workspace.WarningRaised -= OnWorkspaceWarning;
            _attached = false;
        }
        StateChanged = null;
        try { await Task.WhenAll(_lifetime.CancelAsync(), _operations.RetireAsync().AsTask()); }
        finally { _requestCancellation?.Dispose(); _lifetime.Dispose(); }
    }
}
