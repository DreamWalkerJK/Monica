using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Monica.UI.Shell.Support;

namespace Monica.UI.Shell.Components.Layout;

/// <summary>Owns one search observer, its module, and its .NET callback and drains admitted work on retirement.</summary>
/// <remarks>
/// Retirement does not cancel browser calls. Cancellation surfaced by initialization, selection, or observer
/// cleanup remains observable. Independent initialization and cleanup failures are reported together in execution order.
/// </remarks>
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
        TaskCompletionSource completion;
        lock (_disposeSync)
        {
            if (_disposed) return Task.CompletedTask;
            if (_initializeTask is { } initialization) return initialization;
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _initializeTask = completion.Task;
        }

        _ = CompleteAsync(completion, () => InitializeCoreAsync(factory, arguments));
        return completion.Task;
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
        TaskCompletionSource completion;
        Task? initialization;
        lock (_disposeSync)
        {
            if (_disposeTask is { } disposal) return new ValueTask(disposal);
            _disposed = true;
            initialization = _initializeTask;
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _disposeTask = completion.Task;
        }

        _ = CompleteAsync(completion, () => DisposeCoreAsync(initialization));
        return new ValueTask(completion.Task);
    }

    private async Task DisposeCoreAsync(Task? initialization)
    {
        List<Exception> failures = [];
        if (initialization is not null)
        {
            try { await initialization; }
            catch (Exception exception) { failures.Add(exception); }
        }

        await _operations.WaitAsync();
        try
        {
            var observer = _observer;
            var callbackReference = _callbackReference;
            _observer = null;
            _callbackReference = null;
            await DisposeObserverAsync(observer, failures);
            try { callbackReference?.Dispose(); }
            catch (Exception exception) { failures.Add(exception); }
            try { await _module.DisposeAsync(); }
            catch (Exception exception) { failures.Add(exception); }
        }
        finally { _operations.Release(); }

        // Cleanup must not replace an earlier producer or observer-drain failure.
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException("Search interop retirement failed.", failures);
    }

    private static async ValueTask DisposeObserverAsync(IJSObjectReference? observer, List<Exception> failures)
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
        catch (Exception exception) { failures.Add(exception); }

        try { await observer.DisposeAsync(); }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
        catch (Exception exception) { failures.Add(exception); }
    }

    private static async Task CompleteAsync(TaskCompletionSource completion, Func<Task> operation)
    {
        try
        {
            await operation();
            completion.TrySetResult();
        }
        catch (OperationCanceledException exception)
        {
            completion.TrySetCanceled(exception.CancellationToken);
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
    }
}
