using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Monica.UI.Shell.Support;

namespace Monica.UI.Shell.Components.Layout;

/// <summary>Owns one search observer, its module, and its .NET callback until all queued callbacks finish.</summary>
internal sealed class NavigationSearchInteropSession<TTarget>(IJSRuntime jsRuntime, TTarget callbackTarget) : IAsyncDisposable
    where TTarget : class
{
    private readonly JsModuleSession _module = new(jsRuntime, "./_content/Monica.UI/js/navigation-search.js");
    private readonly object _disposeSync = new();
    private readonly SemaphoreSlim _operations = new(1, 1);
    private IJSObjectReference? _observer;
    private DotNetObjectReference<TTarget>? _callbackReference;
    private Task? _initializeTask;
    private Task? _disposeTask;
    private bool _disposed;

    internal Task InitializeAsync(string factory, params object?[] arguments)
    {
        lock (_disposeSync)
        {
            return _disposed ? Task.CompletedTask : _initializeTask ??= InitializeCoreAsync(factory, arguments);
        }
    }

    internal async ValueTask ScrollSelectionAsync(ElementReference root, int selectedIndex)
    {
        await _operations.WaitAsync();
        try
        {
            if (!_disposed)
            {
                await _module.InvokeVoidAsync("scrollSelection", root, selectedIndex);
            }
        }
        finally
        {
            _operations.Release();
        }
    }

    private async Task InitializeCoreAsync(string factory, object?[] arguments)
    {
        _callbackReference = DotNetObjectReference.Create(callbackTarget);
        try
        {
            _observer = await _module.InvokeAsync<IJSObjectReference>(factory, [.. arguments, _callbackReference]);
        }
        catch (JSDisconnectedException)
        {
            // The browser no longer has a search surface to observe.
        }
    }

    public ValueTask DisposeAsync()
    {
        lock (_disposeSync)
        {
            _disposed = true;
            return new ValueTask(_disposeTask ??= DisposeCoreAsync());
        }
    }

    private async Task DisposeCoreAsync()
    {
        try
        {
            if (_initializeTask is { } initialization)
            {
                await initialization;
            }

            if (_observer is { } observer)
            {
                try
                {
                    await observer.InvokeVoidAsync("dispose");
                }
                catch (JSDisconnectedException)
                {
                    // Disconnect releases the observer's browser context.
                }
                catch (JSException)
                {
                    // A destroyed browser context no longer owns live listeners.
                }
                finally
                {
                    try { await observer.DisposeAsync(); }
                    catch (JSDisconnectedException) { }
                    catch (JSException) { }
                }
            }
        }
        finally
        {
            _callbackReference?.Dispose();
            await _operations.WaitAsync();
            try { await _module.DisposeAsync(); }
            finally { _operations.Release(); }
        }
    }
}
