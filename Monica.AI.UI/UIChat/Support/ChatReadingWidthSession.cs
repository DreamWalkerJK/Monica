using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Monica.UI.Shell.Support;

namespace Monica.AI.UI.UIChat.Support;

/// <summary>Owns one chat surface's resize controller and browser-wide reading-width preference.</summary>
internal sealed class ChatReadingWidthSession(IJSRuntime jsRuntime, IBrowserStorage storage) : IAsyncDisposable
{
    internal const string MODULE_PATH = "./_content/Monica.AI.UI/UIChat/Components/ChatContainer.razor.js";
    internal const string STORAGE_KEY = "ai:chat:reading-width";
    internal const int DEFAULT_WIDTH = 960;
    private IJSObjectReference? _module;
    private IJSObjectReference? _controller;
    private DotNetObjectReference<ChatReadingWidthSession>? _callback;
    private Task? _initialization;
    private Task? _disposal;
    private Task _save = Task.CompletedTask;
    private bool _disposed;

    internal Task InitializeAsync(ElementReference root) => _disposed ? Task.CompletedTask : _initialization ??= InitializeCoreAsync(root);

    private async Task InitializeCoreAsync(ElementReference root)
    {
        try
        {
            var width = await storage.GetAsync(STORAGE_KEY, DEFAULT_WIDTH);
            if (_disposed) return;
            // Imports create owned resources, so always observe them even when disposal starts meanwhile.
            _module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", MODULE_PATH);
            if (_disposed) return;
            _callback = DotNetObjectReference.Create(this);
            _controller = await _module.InvokeAsync<IJSObjectReference>("createReadingWidth", root, _callback, Math.Clamp(width, 560, 1600));
        }
        catch (JSDisconnectedException) { }
    }

    /// <summary>Persists only completed user gestures; viewport clamping never changes the saved preference.</summary>
    [JSInvokable]
    public Task CommitWidthAsync(int width)
    {
        if (_disposed) return Task.CompletedTask;
        return _save = storage.SetAsync(STORAGE_KEY, Math.Clamp(width, 560, 1600));
    }

    public ValueTask DisposeAsync() => new(_disposal ??= DisposeCoreAsync());

    private async Task DisposeCoreAsync()
    {
        _disposed = true;
        try
        {
            if (_initialization is not null) await _initialization;
            // Stop callback producers without touching rendered DOM, then drain the browser's queued callback.
            if (_controller is not null) await _controller.InvokeVoidAsync("shutdown");
            await _save;
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
            await DisposeReferenceAsync(controller);
            await DisposeReferenceAsync(module);
        }
    }

    private static async ValueTask DisposeReferenceAsync(IJSObjectReference? reference)
    {
        if (reference is null) return;
        try { await reference.DisposeAsync(); }
        catch (JSDisconnectedException) { }
    }
}
