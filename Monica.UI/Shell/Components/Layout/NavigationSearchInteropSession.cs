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
    private volatile bool _disposed;

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
        catch (OperationCanceledException) when (_disposed)
        {
            // Retirement can cancel an admitted browser call while teardown waits for it.
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
        catch (OperationCanceledException) when (_disposed)
        {
            // Observe the actual import or factory completion; retirement cancellation creates no reference.
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
                try { await initialization; }
                catch (OperationCanceledException) when (_disposed)
                {
                    // A previously canceled initialization must not become a second teardown failure.
                }
            }
        }
        finally
        {
            await _operations.WaitAsync();
            try
            {
                var observer = _observer;
                var callbackReference = _callbackReference;
                _observer = null;
                _callbackReference = null;
                try
                {
                    await DisposeObserverAsync(observer);
                }
                finally
                {
                    callbackReference?.Dispose();
                    await _module.DisposeAsync();
                }
            }
            finally { _operations.Release(); }
        }
    }

    private async ValueTask DisposeObserverAsync(IJSObjectReference? observer)
    {
        if (observer is null)
        {
            return;
        }

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
        catch (OperationCanceledException) when (_disposed)
        {
            // The retiring circuit canceled the observer's final callback drain.
        }
        finally
        {
            try { await observer.DisposeAsync(); }
            catch (JSDisconnectedException) { }
            catch (JSException) { }
            catch (OperationCanceledException) when (_disposed) { }
        }
    }
}
