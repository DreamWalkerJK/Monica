using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Monica.UI.Shell.Components.Layout;

/// <summary>Owns one navigation menu's pointer observer and serializes its browser calls with teardown.</summary>
internal sealed class NavigationPopoverInteropSession : IAsyncDisposable
{
    private const string MODULE_PATH = "./_content/Monica.UI/js/navigation-popover.js";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly CancellationToken _lifetimeToken;
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationPopover _callbackTarget;
    private readonly object _disposeSync = new();
    private IJSObjectReference? _module;
    private IJSObjectReference? _observer;
    private DotNetObjectReference<NavigationPopover>? _callbackReference;
    private Task? _disposeTask;
    private volatile bool _disposed;

    internal NavigationPopoverInteropSession(IJSRuntime jsRuntime, NavigationPopover callbackTarget)
    {
        _jsRuntime = jsRuntime;
        _callbackTarget = callbackTarget;
        _lifetimeToken = _lifetimeCancellation.Token;
    }

    /// <summary>Connects the rendered trigger to its portaled popup and publishes the current interaction mode.</summary>
    internal async Task SynchronizeAsync(
        ElementReference activator,
        string popupId,
        bool enabled,
        bool open,
        bool openedByHover,
        bool focusFirst,
        bool restoreFocus,
        long pointerVersion)
    {
        if (_disposed || (!enabled && _observer is null))
        {
            return;
        }

        var acquired = false;
        IJSObjectReference? importedModule = null;
        IJSObjectReference? createdObserver = null;
        DotNetObjectReference<NavigationPopover>? createdCallbackReference = null;
        Exception? operationFailure = null;
        try
        {
            await _gate.WaitAsync(_lifetimeToken);
            acquired = true;
            if (_disposed)
            {
                return;
            }

            if (_observer is null)
            {
                importedModule = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", MODULE_PATH);
                if (_disposed)
                {
                    return;
                }

                createdCallbackReference = DotNetObjectReference.Create(_callbackTarget);
                createdObserver = await importedModule.InvokeAsync<IJSObjectReference>(
                    "createNavigationPopoverObserver", activator, popupId, createdCallbackReference);
                if (_disposed)
                {
                    return;
                }

                _module = importedModule;
                _observer = createdObserver;
                _callbackReference = createdCallbackReference;
                importedModule = null;
                createdObserver = null;
                createdCallbackReference = null;
            }

            await _observer.InvokeVoidAsync(
                "synchronize", enabled, open, openedByHover, focusFirst, restoreFocus, pointerVersion);
        }
        catch (OperationCanceledException) when (_lifetimeToken.IsCancellationRequested)
        {
            // Component teardown ended an operation waiting for the serialized observer.
        }
        catch (JSDisconnectedException)
        {
            // A disconnected circuit no longer has a navigation menu to coordinate.
        }
        catch (Exception exception) { operationFailure = exception; }
        finally
        {
            try
            {
                await DisposeOwnedReferencesAsync(createdObserver, createdCallbackReference, importedModule);
            }
            catch (Exception cleanupFailure) when (operationFailure is not null)
            {
                throw new AggregateException("Navigation synchronization and cleanup failed.", operationFailure, cleanupFailure);
            }
            finally
            {
                if (acquired) _gate.Release();
            }
        }
        if (operationFailure is not null) ExceptionDispatchInfo.Capture(operationFailure).Throw();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        TaskCompletionSource completion;
        lock (_disposeSync)
        {
            if (_disposeTask is not null) return new(_disposeTask);
            _disposed = true;
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _disposeTask = completion.Task;
        }
        _ = CompleteDisposalAsync(completion);
        return new(completion.Task);
    }

    private async Task CompleteDisposalAsync(TaskCompletionSource completion)
    {
        try { await DisposeCoreAsync(); completion.TrySetResult(); }
        catch (Exception exception) { completion.TrySetException(exception); }
    }

    private async Task DisposeCoreAsync()
    {
        _lifetimeCancellation.Cancel();
        await _gate.WaitAsync();
        IJSObjectReference? observer;
        IJSObjectReference? module;
        DotNetObjectReference<NavigationPopover>? callbackReference;
        try
        {
            observer = _observer;
            module = _module;
            callbackReference = _callbackReference;
            _observer = null;
            _module = null;
            _callbackReference = null;
        }
        finally
        {
            _gate.Release();
        }

        try { await DisposeOwnedReferencesAsync(observer, callbackReference, module); }
        finally { _lifetimeCancellation.Dispose(); }
    }

    private async ValueTask DisposeOwnedReferencesAsync(
        IJSObjectReference? observer,
        DotNetObjectReference<NavigationPopover>? callbackReference,
        IJSObjectReference? module)
    {
        var failures = new List<Exception>();
        if (observer is not null)
        {
            try
            {
                // This stops event producers and drains callbacks without changing the DOM.
                await observer.InvokeVoidAsync("dispose");
            }
            catch (JSDisconnectedException) { }
            catch (JSException) { }
            catch (OperationCanceledException) when (_disposed) { }
            catch (Exception exception) { failures.Add(exception); }
            try { await DisposeReferenceAsync(observer); }
            catch (Exception exception) { failures.Add(exception); }
        }

        try { callbackReference?.Dispose(); }
        catch (Exception exception) { failures.Add(exception); }
        try { await DisposeReferenceAsync(module); }
        catch (Exception exception) { failures.Add(exception); }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException("Navigation observer cleanup failed.", failures);
    }

    private async ValueTask DisposeReferenceAsync(IJSObjectReference? reference)
    {
        if (reference is null)
        {
            return;
        }

        try
        {
            await reference.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit already owns browser cleanup.
        }
        catch (JSException)
        {
            // The browser context was destroyed while releasing the reference.
        }
        catch (OperationCanceledException) when (_disposed)
        {
            // Circuit retirement can cancel release after admitted browser users finish.
        }
    }
}
