using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Monica.AI.UI.UIChat.Support;

/// <summary>Owns the page's size observer and drains its callbacks before retiring the callback target.</summary>
internal sealed class ChatLayoutInteropSession(IJSRuntime jsRuntime, Func<ChatWorkbenchLayout, Task> changed) : IAsyncDisposable
{
    private IJSObjectReference? _module;
    private IJSObjectReference? _controller;
    private DotNetObjectReference<ChatLayoutInteropSession>? _callback;
    private Task? _initialization;
    private Task? _disposal;
    private bool _disposed;

    internal Task InitializeAsync(ElementReference root, ElementReference main) =>
        _disposed ? Task.CompletedTask : _initialization ??= InitializeCoreAsync(root, main);

    private async Task InitializeCoreAsync(ElementReference root, ElementReference main)
    {
        try
        {
            _module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Monica.AI.UI/Pages/ChatPage.razor.js");
            if (_disposed) return;
            _callback = DotNetObjectReference.Create(this);
            _controller = await _module.InvokeAsync<IJSObjectReference>("createLayoutObserver", root, main, _callback);
        }
        catch (JSDisconnectedException) { }
    }

    [JSInvokable]
    public Task ReportLayoutAsync(int ownerWidth, int mainWidth, int height) => _disposed
        ? Task.CompletedTask
        : changed(new ChatWorkbenchLayout(ownerWidth, mainWidth, height));

    public ValueTask DisposeAsync() => new(_disposal ??= DisposeCoreAsync());

    private async Task DisposeCoreAsync()
    {
        _disposed = true;
        try
        {
            if (_initialization is not null) await _initialization;
            if (_controller is not null) await _controller.InvokeVoidAsync("shutdown");
        }
        catch (JSDisconnectedException) { }
        finally
        {
            _callback?.Dispose();
            _callback = null;
            var controller = _controller;
            var module = _module;
            _controller = null;
            _module = null;
            try { await DisposeReferenceAsync(controller); }
            finally { await DisposeReferenceAsync(module); }
        }
    }

    private static async ValueTask DisposeReferenceAsync(IJSObjectReference? reference)
    {
        if (reference is null) return;
        try { await reference.DisposeAsync(); }
        catch (JSDisconnectedException) { }
    }
}
