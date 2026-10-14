using Microsoft.AspNetCore.Components;
using Monica.Markdown.Models;
using Monica.Markdown.UIMarkdown.State;
using MudBlazor;

namespace Monica.Markdown.UIMarkdown.Dialogs;

/// <summary>A native group selector whose refresh work is owned by this dialog instance.</summary>
public partial class DocumentGroupSwitcherDialog : IAsyncDisposable
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    /// <summary>Gets or sets the catalog snapshot shown when the dialog opens.</summary>
    [Parameter, EditorRequired] public List<MarkdownDocumentGroup> Groups { get; set; } = [];

    /// <summary>Gets or sets the exact selected group key; the default is no selection.</summary>
    [Parameter] public string? SelectedGroupKey { get; set; }

    private MarkdownGroupSwitcherState _refreshState = null!;
    private string? _searchText;
    private bool _refreshing => _refreshState.IsRefreshing;
    private List<MarkdownDocumentGroup> FilteredGroups => Groups.Where(MatchesSearch).ToList();
    private string? RefreshError => _refreshState.ErrorMessage;

    protected override void OnInitialized() => _refreshState = new(MarkdownFacade, Groups);

    private Task SelectGroupAsync(string groupKey)
    {
        if (!_refreshState.IsRetired && !_refreshing) MudDialog.Close(DialogResult.Ok(groupKey));
        return Task.CompletedTask;
    }

    private async Task RefreshGroupsAsync()
    {
        await _refreshState.RefreshAsync();
        if (_refreshState.IsRetired) return;
        Groups = _refreshState.Groups;
        if (RefreshError is { } error) Snackbar.Add(error, Severity.Error);
    }

    private bool MatchesSearch(MarkdownDocumentGroup group)
    {
        if (string.IsNullOrWhiteSpace(_searchText)) return true;
        var searchText = _searchText.Trim();
        return group.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase)
               || (!string.IsNullOrWhiteSpace(group.Description)
                   && group.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase))
               || group.BasePath.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }

    private string GetEmptyStateText() => string.IsNullOrWhiteSpace(_searchText)
        ? L["MarkdownViewer:States:NoKnowledgeBases"]
        : L["MarkdownViewer:States:NoMatchingKnowledgeBases"];

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _refreshState is null ? ValueTask.CompletedTask : _refreshState.DisposeAsync();
}
