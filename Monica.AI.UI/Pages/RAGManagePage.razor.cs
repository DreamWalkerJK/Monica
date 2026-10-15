using Microsoft.AspNetCore.Components;
using Monica.AI.UI.UIRAG.State;

namespace Monica.AI.UI.Pages;

public partial class RAGManagePage : IAsyncDisposable
{
    public const string PAGE_URL = "/ai/rag/manage";

    [Inject]
    private RAGManagePageStateFactory StateFactory { get; set; } = null!;
    public RAGManagePageState PageState { get; private set; } = null!;
    private bool _retired;

    protected override async Task OnInitializedAsync()
    {
        PageState = StateFactory.Create(action => InvokeAsync(action));
        PageState.StateChanged += OnStateChanged;
        await PageState.InitializeAsync();
    }

    private Task OnStateChanged() => InvokeAsync(() => { if (!_retired) StateHasChanged(); });

    public async ValueTask DisposeAsync()
    {
        _retired = true;
        var focusRetirement = _dossierFocus.DisposeAsync().AsTask();
        if (PageState is null) { await focusRetirement; return; }
        PageState.StateChanged -= OnStateChanged;
        var stateRetirement = PageState.DisposeAsync().AsTask();
        await Task.WhenAll(focusRetirement, stateRetirement);
    }
}
