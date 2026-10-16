using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using Monica.AI.Chat.Models;
using Monica.AI.UI.Localization;
using Monica.AI.UI.UIChat.State;
using Monica.AI.UI.UIChat.Support;

namespace Monica.AI.UI.Pages;

/// <summary>The conversation workbench, owning one cancellable page-state lifetime.</summary>
public partial class ChatPage : IAsyncDisposable
{
    /// <summary>Registered workbench route.</summary>
    public const string PAGE_URL = "/ai-chat";
    [Inject] public required ChatPageStateFactory StateFactory { get; set; }
    [Inject] public required IStringLocalizer<AIResource> L { get; set; }
    [Inject] private IJSRuntime JS { get; set; } = null!;
    private ChatPageState PageState { get; set; } = null!;
    private bool _sessionDrawerOpen;
    private bool _compactHistory = true;
    private bool _wideHistoryOpen = true;
    private ChatWorkbenchLayout _layout = new(0, 0, 0, false);
    private ChatLayoutInteropSession? _layoutSession;
    private ElementReference _shellElement;
    private ElementReference _mainElement;
    private bool _trajectory;
    private bool _archiveOpen;
    private bool _disposed;
    private Task _renderTask = Task.CompletedTask;

    protected override async Task OnInitializedAsync()
    {
        PageState = StateFactory.Create();
        PageState.SetDialogDispatcher(action => InvokeAsync(action));
        PageState.StateChanged += OnStateChanged;
        await PageState.InitializeAsync();
    }
    private void OnStateChanged()
    {
        if (!_disposed) _renderTask = InvokeAsync(() => { if (!_disposed) StateHasChanged(); });
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_disposed)
        {
            _layoutSession = new ChatLayoutInteropSession(JS, OnLayoutChangedAsync);
            await _layoutSession.InitializeAsync(_shellElement, _mainElement);
        }
    }

    private Task OnLayoutChangedAsync(ChatWorkbenchLayout layout) => InvokeAsync(() =>
    {
        if (_disposed) return;
        var compact = layout.OwnerWidth < 1180;
        if (!_layout.IsObserved || compact != _compactHistory)
        {
            if (_layout.IsObserved && !_compactHistory) _wideHistoryOpen = _sessionDrawerOpen;
            _compactHistory = compact;
            _sessionDrawerOpen = !compact && _wideHistoryOpen;
        }
        _layout = layout;
        StateHasChanged();
    });

    private void SetSessionDrawerOpen(bool open)
    {
        _sessionDrawerOpen = open;
        if (!_compactHistory) _wideHistoryOpen = open;
    }

    private void ToggleSessionDrawer() => SetSessionDrawerOpen(!_sessionDrawerOpen);
    private void CloseSessionDrawer() => SetSessionDrawerOpen(false);
    private void HandleHistoryKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs args)
    {
        if (args.Key == "Escape" && _compactHistory && _sessionDrawerOpen) CloseSessionDrawer();
    }

    private async Task CreateSessionAsync()
    {
        var previousSession = PageState.CurrentSessionId;
        await PageState.CreateNewSessionAsync();
        if (_compactHistory && PageState.CurrentSessionId is { } current && current != previousSession) CloseSessionDrawer();
    }

    private async Task SelectSessionAsync(string sessionId)
    {
        await PageState.SelectSessionAsync(sessionId);
        if (_compactHistory && PageState.CurrentSessionId == sessionId) CloseSessionDrawer();
    }
    private void InspectStep(ChatExecutionStep step)
    {
        PageState.Inspect(step);
        _trajectory = true;
    }
    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        PageState.StateChanged -= OnStateChanged;
        try
        {
            if (_layoutSession is not null) await _layoutSession.DisposeAsync();
        }
        finally
        {
            try { await PageState.DisposeAsync(); }
            finally { await _renderTask; }
        }
    }
}
