using Microsoft.JSInterop;
using Monica.Framework.UI.UILogging.Models;

namespace Monica.Framework.UI.UILogging.Support;

/// <summary>Owns browser transfers for one page, including late imports and cancellation of file fetches.</summary>
internal sealed class LogBrowserTransfer(IJSRuntime runtime) : IAsyncDisposable
{
    private readonly object _sync = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly HashSet<Task> _operations = [];
    private readonly string _owner = Guid.NewGuid().ToString("N");
    private IJSObjectReference? _module;
    private Task? _disposeTask;
    private bool _disposed;

    public Task DownloadAsync(string url) => Admit(module => module.InvokeVoidAsync("downloadFile", url, _owner).AsTask());
    public Task ExportAsync(LogExportResult export) => Admit(module => module.InvokeVoidAsync("downloadContent", export.FileName, export.ContentType, export.Content).AsTask());

    private Task Admit(Func<IJSObjectReference, Task> action)
    {
        lock (_sync)
        {
            if (_disposed) return Task.CompletedTask;
            var task = TransferAsync(action);
            _operations.Add(task);
            _ = ObserveAsync(task);
            return task;
        }
    }

    private async Task ObserveAsync(Task task)
    {
        try { await task.ConfigureAwait(false); }
        catch { /* The caller observes transfer failures. */ }
        finally { lock (_sync) _operations.Remove(task); }
    }

    private async Task TransferAsync(Func<IJSObjectReference, Task> action)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            lock (_sync) if (_disposed) return;
            if (_module is null)
            {
                var imported = await runtime.InvokeAsync<IJSObjectReference>("import", "./_content/Monica.Framework.UI/js/logging-ui.js");
                bool retired;
                lock (_sync)
                {
                    retired = _disposed;
                    if (!retired) _module = imported;
                }
                if (retired) { await ReleaseAsync(imported).ConfigureAwait(false); return; }
            }
            lock (_sync) if (_disposed) return;
            var ownedModule = _module;
            if (ownedModule is not null) await action(ownedModule).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public ValueTask DisposeAsync()
    {
        lock (_sync)
        {
            _disposed = true;
            return new ValueTask(_disposeTask ??= RetireAsync());
        }
    }

    private async Task RetireAsync()
    {
        // Teardown cancels fetches only; it does not mutate the DOM.
        if (_module is { } module)
        {
            try { await module.InvokeVoidAsync("cancelDownloads", _owner); }
            catch (Exception error) when (error is JSDisconnectedException or TaskCanceledException) { }
        }
        Task[] admitted;
        lock (_sync) admitted = _operations.ToArray();
        try { await Task.WhenAll(admitted).ConfigureAwait(false); }
        catch { /* Admitted callers already own failures. */ }
        if (_module is { } owned)
        {
            try { await owned.InvokeVoidAsync("releaseDownloadOwner", _owner); }
            catch (Exception error) when (error is JSDisconnectedException or TaskCanceledException) { }
            await ReleaseAsync(owned).ConfigureAwait(false);
        }
        _module = null;
        _gate.Dispose();
    }

    private static async Task ReleaseAsync(IJSObjectReference module)
    {
        try { await module.DisposeAsync(); }
        catch (Exception error) when (error is JSDisconnectedException or TaskCanceledException) { }
    }

    public static string FileUrl(string relativePath) => "logging-ui/files/" +
        string.Join('/', relativePath.Replace('\\', '/').Split('/').Select(Uri.EscapeDataString));
}
