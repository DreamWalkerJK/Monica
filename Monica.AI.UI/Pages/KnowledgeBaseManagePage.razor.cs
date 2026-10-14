using Microsoft.AspNetCore.Components;
using Monica.AI.UI.UIKnowledgeBase.State;

namespace Monica.AI.UI.Pages;

public partial class KnowledgeBaseManagePage : IAsyncDisposable
{
    public const string PAGE_URL = "/ai/knowledge-bases";

    [Inject]
    private KnowledgeBaseManagePageStateFactory StateFactory { get; set; } = null!;
    private KnowledgeBaseManagePageState PageState { get; set; } = null!;
    private bool _retired;

    protected override async Task OnInitializedAsync()
    {
        PageState = StateFactory.Create(action => InvokeAsync(action));
        PageState.StateChanged += HandleStateChanged;
        await PageState.InitializeAsync();
    }

    private Task HandleStateChanged() => InvokeAsync(() => { if (!_retired) StateHasChanged(); });

    public async ValueTask DisposeAsync()
    {
        _retired = true;
        var focusRetirement = _dossierFocus.DisposeAsync().AsTask();
        if (PageState is null) { await focusRetirement; return; }
        PageState.StateChanged -= HandleStateChanged;
        var stateRetirement = PageState.DisposeAsync().AsTask();
        await Task.WhenAll(focusRetirement, stateRetirement);
    }
}
