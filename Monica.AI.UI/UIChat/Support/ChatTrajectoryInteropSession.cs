using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Monica.AI.UI.UIChat.Support;

/// <summary>Owns one trajectory's normalized interval gestures and optional live scrolling.</summary>
internal sealed class ChatTrajectoryInteropSession(IJSRuntime jsRuntime) : IAsyncDisposable
{
    internal const string MODULE_PATH = "./_content/Monica.AI.UI/UIChat/Components/ChatTrajectory.razor.js";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private readonly object _disposeSync = new();
    private IJSObjectReference? _module, _controller;
    private bool _disposed;
    private Task? _disposeTask;

    internal async Task InitializeAsync(ElementReference root, ElementReference ledger, ElementReference timeline)
    {
        await InvokeAsync<object?>(async () =>
        {
            if (_controller is not null) return null;
            var module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", MODULE_PATH);
            if (_disposed) { await DisposeReferenceAsync(module); return null; }
            try
            {
                var controller = await module.InvokeAsync<IJSObjectReference>("initialize", root, ledger, timeline);
                if (_disposed) { await DisposeControllerAsync(controller); return null; }
                _module = module;
                _controller = controller;
            }
            finally
            {
                // Publish only a complete controller. A failed or late initialization retains no module.
                if (_module != module) await DisposeReferenceAsync(module);
            }
            return null;
        });
    }

    internal Task FollowAsync() => InvokeAsync<object?>(async () =>
    {
        if (_controller is not null) await _controller.InvokeVoidAsync("follow");
        return null;
    });

    internal Task RevealAsync(int rowIndex) => InvokeAsync<object?>(async () =>
    {
        if (_controller is not null) await _controller.InvokeVoidAsync("reveal", rowIndex);
        return null;
    });

    internal Task<double[]?> IntervalAsync(double from, double to) => InvokeAsync(async () =>
        _controller is null ? null : await _controller.InvokeAsync<double[]>("interval", from, to));

    private async Task<T?> InvokeAsync<T>(Func<Task<T?>> action)
    {
        if (_disposed) return default;
        var acquired = false;
        try
        {
            await _gate.WaitAsync(_lifetime.Token);
            acquired = true;
            return _disposed ? default : await action();
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { return default; }
        catch (JSDisconnectedException) { return default; }
        finally { if (acquired) _gate.Release(); }
    }

    public ValueTask DisposeAsync()
    {
        lock (_disposeSync) return new ValueTask(_disposeTask ??= DisposeCoreAsync());
    }

    private async Task DisposeCoreAsync()
    {
        _disposed = true;
        await _lifetime.CancelAsync();
        await _gate.WaitAsync();
        var controller = _controller;
        var module = _module;
        _controller = null; _module = null;
        _gate.Release();
        try { await DisposeControllerAsync(controller); }
        finally
        {
            await DisposeReferenceAsync(module);
            _lifetime.Dispose();
            _gate.Dispose();
        }
    }

    private static async Task DisposeControllerAsync(IJSObjectReference? controller)
    {
        if (controller is null) return;
        try { await controller.InvokeVoidAsync("shutdown"); }
        catch (JSDisconnectedException) { }
        finally { await DisposeReferenceAsync(controller); }
    }

    private static async Task DisposeReferenceAsync(IJSObjectReference? reference)
    {
        if (reference is null) return;
        try { await reference.DisposeAsync(); }
        catch (JSDisconnectedException) { }
    }
}
