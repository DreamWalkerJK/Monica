using Monica.Core.Results;
using Monica.Markdown.Facades;
using Monica.Markdown.Models;

namespace Monica.Markdown.UIMarkdown.State;

/// <summary>Owns a group dialog's rescan and catalog read without disposing the borrowed facade.</summary>
internal sealed class MarkdownGroupSwitcherState(MarkdownFacade facade, List<MarkdownDocumentGroup> groups)
    : IAsyncDisposable
{
    private readonly MarkdownViewerOperationLifetime _lifetime = new();
    private int _refreshing;

    internal List<MarkdownDocumentGroup> Groups { get; private set; } = groups;
    internal string? ErrorMessage { get; private set; }
    internal bool IsRefreshing => Volatile.Read(ref _refreshing) != 0;
    internal bool IsRetired => _lifetime.IsRetired;

    internal Task RefreshAsync()
    {
        if (_lifetime.IsRetired || Interlocked.CompareExchange(ref _refreshing, 1, 0) != 0)
            return Task.CompletedTask;
        return _lifetime.RunAsync(async token =>
        {
            try
            {
                token.ThrowIfCancellationRequested();
                _lifetime.TryApply(() => ErrorMessage = null);
                var refresh = await facade.RefreshAllAsync();
                token.ThrowIfCancellationRequested();
                if (refresh.IsFailed(out var refreshError))
                {
                    _lifetime.TryApply(() => ErrorMessage = refreshError);
                    return;
                }

                var response = await facade.GetAllDocumentGroupsAsync();
                token.ThrowIfCancellationRequested();
                if (response.IsFailed(out var error, out var refreshedGroups))
                    _lifetime.TryApply(() => ErrorMessage = error);
                else
                    _lifetime.TryApply(() => Groups = refreshedGroups);
            }
            finally { Interlocked.Exchange(ref _refreshing, 0); }
        });
    }

    public ValueTask DisposeAsync() => _lifetime.RetireAsync();
}
