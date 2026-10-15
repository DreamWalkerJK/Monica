using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Monica.DevOps.Terminal.Support;

/// <summary>Owns scrolling, workspace observation, and their actual browser producers for one transcript.</summary>
internal sealed class TerminalScrollInteropSession(
    IJSRuntime runtime, Func<bool, bool, bool, Task>? stateChanged = null) : IAsyncDisposable
{
    internal const string MODULE_PATH = "./_content/Monica.DevOps/Terminal/Pages/UITerminalPage.razor.js";
    private readonly object _sync = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly HashSet<Task> _users = [];
    private IJSObjectReference? _module;
    private IJSObjectReference? _controller;
    private DotNetObjectReference<TerminalScrollInteropSession>? _receiver;
    private Task? _retirement;
    private bool _retired;

    /// <summary>Updates the rendered output and its nearest workspace; only an explicit reveal resets a paused reader to the end.</summary>
    internal Task<bool> UpdateAsync(ElementReference outputRoot, bool revealLatest = false) =>
        AdmitAsync(async () =>
        {
            await _gate.WaitAsync();
            try
            {
                if (IsRetired()) return false;
                _module ??= await runtime.InvokeAsync<IJSObjectReference>("import", MODULE_PATH);
                if (IsRetired()) return false;
                if (_controller is null)
                {
                    _receiver ??= DotNetObjectReference.Create(this);
                    _controller = await _module.InvokeAsync<IJSObjectReference>(
                        "createTerminalScroller", outputRoot, _receiver);
                }
                if (IsRetired()) return false;
                return await _controller.InvokeAsync<bool>("update", revealLatest);
            }
            finally { _gate.Release(); }
        });

    /// <summary>Publishes instance-owned resize/scroll evidence and keeps its actual callback observable.</summary>
    [JSInvokable]
    public Task ReceiveStateAsync(bool compact, bool unseen, bool flow) => AdmitAsync(async () =>
    {
        if (stateChanged is not null) await stateChanged(compact, unseen, flow);
        return true;
    });

    private Task<bool> AdmitAsync(Func<Task<bool>> operation)
    {
        TaskCompletionSource<bool> completion;
        lock (_sync)
        {
            if (_retired) return Task.FromResult(false);
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _users.Add(completion.Task);
        }
        _ = RunAsync();
        return completion.Task;

        async Task RunAsync()
        {
            Exception? failure = null;
            var result = false;
            try { result = await operation(); }
            catch (JSDisconnectedException) { }
            catch (Exception exception) { failure = exception; }
            lock (_sync)
            {
                _users.Remove(completion.Task);
                if (failure is null) completion.TrySetResult(!_retired && result);
                else completion.TrySetException(failure);
            }
        }
    }

    private bool IsRetired() { lock (_sync) return _retired; }

    /// <summary>Stops admission, drains late imports/controllers/users, and independently releases every reference.</summary>
    public ValueTask DisposeAsync()
    {
        TaskCompletionSource completion;
        Task[] users;
        lock (_sync)
        {
            if (_retirement is not null) return new(_retirement);
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _retirement = completion.Task;
            _retired = true;
            users = _users.ToArray();
        }
        _ = DrainAsync();
        return new(completion.Task);

        async Task DrainAsync()
        {
            List<Exception> failures = [];
            async Task ObserveAsync(Func<Task> action)
            {
                try { await action(); }
                catch (JSDisconnectedException) { }
                catch (Exception exception) { failures.Add(exception); }
            }
            await ObserveAsync(() => Task.WhenAll(users));
            if (_controller is not null)
            {
                await ObserveAsync(() => _controller.InvokeVoidAsync("shutdown").AsTask());
                await ObserveAsync(() => _controller.DisposeAsync().AsTask());
                _controller = null;
            }
            if (_module is not null)
            {
                await ObserveAsync(() => _module.DisposeAsync().AsTask());
                _module = null;
            }
            _receiver?.Dispose();
            _receiver = null;
            _gate.Dispose();
            if (failures.Count == 0) completion.TrySetResult();
            else completion.TrySetException(failures.Count == 1 ? failures[0] : new AggregateException(failures));
        }
    }
}
