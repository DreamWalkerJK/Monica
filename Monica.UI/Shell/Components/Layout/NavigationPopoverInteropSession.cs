using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Monica.UI.Shell.Components.Layout;

/// <summary>Owns one navigation menu's pointer observer and serializes its browser calls with teardown.</summary>
internal sealed class NavigationPopoverInteropSession(
    IJSRuntime jsRuntime,
    NavigationPopover callbackTarget) : IAsyncDisposable
{
    private const string MODULE_PATH = "./_content/Monica.UI/js/navigation-popover.js";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly object _disposeSync = new();
    private IJSObjectReference? _module;
    private IJSObjectReference? _observer;
    private DotNetObjectReference<NavigationPopover>? _callbackReference;
    private Task? _disposeTask;
    private bool _disposed;

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
        try
        {
            await _gate.WaitAsync(_lifetimeCancellation.Token);
            acquired = true;
            if (_disposed)
            {
                return;
            }

            if (_observer is null)
            {
                importedModule = await jsRuntime.InvokeAsync<IJSObjectReference>("import", MODULE_PATH);
                if (_disposed)
                {
                    return;
                }

                createdCallbackReference = DotNetObjectReference.Create(callbackTarget);
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
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
            // Component teardown ended an operation waiting for the serialized observer.
        }
        catch (JSDisconnectedException)
        {
            // A disconnected circuit no longer has a navigation menu to coordinate.
        }
        finally
        {
            await DisposeOwnedReferencesAsync(createdObserver, createdCallbackReference, importedModule);
            if (acquired)
            {
                _gate.Release();
            }
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        lock (_disposeSync)
        {
            return new ValueTask(_disposeTask ??= DisposeCoreAsync());
        }
    }

    private async Task DisposeCoreAsync()
    {
        _disposed = true;
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

        await DisposeOwnedReferencesAsync(observer, callbackReference, module);
        _lifetimeCancellation.Dispose();
    }

    private static async ValueTask DisposeOwnedReferencesAsync(
        IJSObjectReference? observer,
        DotNetObjectReference<NavigationPopover>? callbackReference,
        IJSObjectReference? module)
    {
        try
        {
            if (observer is not null)
            {
                try
                {
                    // This stops event producers and drains callbacks without changing the DOM.
                    await observer.InvokeVoidAsync("dispose");
                }
                catch (JSDisconnectedException)
                {
                    // The browser context already released its listeners.
                }
                catch (JSException)
                {
                    // A destroyed browser context cannot retain a usable observer.
                }

                await DisposeReferenceAsync(observer);
            }
        }
        finally
        {
            callbackReference?.Dispose();
            await DisposeReferenceAsync(module);
        }
    }

    private static async ValueTask DisposeReferenceAsync(IJSObjectReference? reference)
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
    }
}
