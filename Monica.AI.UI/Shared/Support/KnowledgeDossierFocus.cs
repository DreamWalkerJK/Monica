using Microsoft.JSInterop;

namespace Monica.AI.UI.Shared.Support;

/// <summary>Defers compact catalog focus until its selected dossier has rendered and owns the native focus call.</summary>
internal sealed class KnowledgeDossierFocus : IAsyncDisposable
{
    private readonly AIAdministrationOperationLifetime _lifetime = new();
    private (string Id, long Generation)? _pending;

    internal void Request(string id, long generation)
    {
        _lifetime.TryApply(() => _pending = (id, generation));
    }

    internal async Task AfterRenderAsync(Func<string, long, bool> isCurrent, Func<Task> focus)
    {
        if (_lifetime.IsRetired || _pending is not { } request) return;
        _pending = null;
        try
        {
            await _lifetime.RunAsync(async token =>
            {
                if (token.IsCancellationRequested || !isCurrent(request.Id, request.Generation)) return;
                try { await focus(); }
                catch (JSDisconnectedException) { }
                catch (ObjectDisposedException) when (_lifetime.IsRetired) { }
            });
        }
        catch (OperationCanceledException) when (_lifetime.IsRetired) { }
    }

    public ValueTask DisposeAsync() => _lifetime.RetireAsync();
}
