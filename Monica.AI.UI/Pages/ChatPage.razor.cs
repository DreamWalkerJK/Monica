using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Monica.AI.Chat.Models;
using Monica.AI.UI.Localization;
using Monica.AI.UI.UIChat.State;

namespace Monica.AI.UI.Pages;

/// <summary>The conversation workbench, owning one cancellable page-state lifetime.</summary>
public partial class ChatPage : IAsyncDisposable
{
    /// <summary>Registered workbench route.</summary>
    public const string PAGE_URL = "/ai-chat";
    [Inject] public required ChatPageStateFactory StateFactory { get; set; }
    [Inject] public required IStringLocalizer<AIResource> L { get; set; }
    private ChatPageState PageState { get; set; } = null!;
    private bool _sessionDrawerOpen = true;
    private bool _trajectory;
    private bool _disposed;
    private Task _renderTask = Task.CompletedTask;

    protected override async Task OnInitializedAsync()
    {
        PageState = StateFactory.Create();
        PageState.StateChanged += OnStateChanged;
        await PageState.InitializeAsync();
    }
    private void OnStateChanged()
    {
        if (!_disposed) _renderTask = InvokeAsync(() => { if (!_disposed) StateHasChanged(); });
    }
    private void ToggleSessionDrawer() => _sessionDrawerOpen = !_sessionDrawerOpen;
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
        await PageState.DisposeAsync();
        await _renderTask;
    }
}
