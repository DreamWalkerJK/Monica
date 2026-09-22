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
    private readonly ModuleAIUIOption _options = options.Value;
    private readonly List<ChatAttachmentReference> _attachments = [];
    private CancellationTokenSource? _requestCancellation;
    private Task? _activeOperation;
    private Task? _uploadOperation;
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
    public Task InitializeAsync() => _initialization ??= InitializeCoreAsync();

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
        await _lifetime.CancelAsync();
        if (_requestCancellation is not null) await _requestCancellation.CancelAsync();
        try
        {
            if (_initialization is not null) await _initialization;
            if (_activeOperation is not null) await _activeOperation;
            if (_uploadOperation is not null) await _uploadOperation;
        }
        catch (OperationCanceledException) { }
        _requestCancellation?.Dispose();
        _lifetime.Dispose();
    }
}
