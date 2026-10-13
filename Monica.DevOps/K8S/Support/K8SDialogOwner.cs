using MudBlazor;

namespace Monica.DevOps.K8S.Support;

/// <summary>Owns native dialog references, results and renderer close dispatches for one K8S visit.</summary>
/// <remarks>Call on the component dispatcher and supply its <c>InvokeAsync</c> delegate. The dialog service is
/// borrowed. Start this owner's retirement before draining work which awaits a dialog result.</remarks>
internal sealed class K8SDialogOwner(IDialogService dialogs, Func<Action, Task> dispatch) : IAsyncDisposable
{
    private readonly object _sync = new();
    private readonly HashSet<Task> _users = [];
    private DialogLease? _active;
    private Task? _retirement;
    private bool _retired;

    /// <summary>Opens one native dialog and observes its actual reference, result and any close dispatch.</summary>
    /// <remarks>Returns null after retirement or when another dialog is already owned. Native cancellation
    /// remains a canceled result. A reference produced after retirement is closed and drained.</remarks>
    public Task<DialogResult?> ShowAsync<TDialog>(string title, DialogParameters parameters, DialogOptions? options = null)
        where TDialog : Microsoft.AspNetCore.Components.IComponent
    {
        TaskCompletionSource<DialogResult?> completion;
        lock (_sync)
        {
            if (_retired || _users.Count != 0) return Task.FromResult<DialogResult?>(null);
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _users.Add(completion.Task);
        }
        _ = RunAsync();
        return completion.Task;

        async Task RunAsync()
        {
            DialogLease? lease = null;
            DialogResult? result = null;
            Exception? failure = null;
            try
            {
                var reference = await dialogs.ShowAsync<TDialog>(title, parameters, options);
                lease = new(reference);
                bool retired;
                lock (_sync) { _active = lease; retired = _retired; }
                if (retired) await CloseAsync(lease);
                result = await reference.Result;
                Task closing;
                lock (_sync) { closing = lease.Closing ?? Task.CompletedTask; retired = _retired; }
                await closing;
                // Native Escape/Close completes Result before the renderer disposes the inspector.
                // Its actual nested reads and child dialogs must finish before this result escapes.
                if (reference.Dialog is IAsyncDisposable component) await component.DisposeAsync();
                if (retired) result = null;
            }
            catch (Exception exception)
            {
                failure = exception;
                if (lease is not null)
                {
                    try { await CloseAsync(lease); }
                    catch (Exception closeFailure) { failure = new AggregateException(exception, closeFailure); }
                    try
                    {
                        if (lease.Reference.Dialog is IAsyncDisposable component) await component.DisposeAsync();
                    }
                    catch (Exception retireFailure) { failure = new AggregateException(failure, retireFailure); }
                }
            }
            finally
            {
                lock (_sync)
                {
                    if (ReferenceEquals(_active, lease)) _active = null;
                    _users.Remove(completion.Task);
                    if (failure is null) completion.TrySetResult(_retired ? null : result);
                    else completion.TrySetException(failure);
                }
            }
        }
    }

    private Task CloseAsync(DialogLease lease)
    {
        TaskCompletionSource completion;
        lock (_sync)
        {
            if (lease.Closing is not null) return lease.Closing;
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            // Close can synchronously complete Result. Publish the drainable dispatch first.
            lease.Closing = completion.Task;
        }
        _ = DispatchAsync();
        return completion.Task;

        async Task DispatchAsync()
        {
            try
            {
                await dispatch(() => lease.Reference.Close(DialogResult.Cancel()));
                // MudBlazor Close only raises a provider event. A retired provider may have unsubscribed.
                if (!lease.Reference.Result.IsCompleted) lease.Reference.Dismiss(DialogResult.Cancel());
                completion.TrySetResult();
            }
            catch (Exception exception)
            {
                try { lease.Reference.Dismiss(DialogResult.Cancel()); }
                catch (Exception dismissFailure) { exception = new AggregateException(exception, dismissFailure); }
                completion.TrySetException(exception);
            }
        }
    }

    /// <summary>Stops admission, closes acquired dialogs, and joins late references and actual results exactly once.</summary>
    public ValueTask DisposeAsync()
    {
        TaskCompletionSource completion;
        Task[] users;
        DialogLease? active;
        lock (_sync)
        {
            if (_retirement is not null) return new(_retirement);
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _retirement = completion.Task;
            _retired = true;
            users = _users.ToArray();
            active = _active;
        }
        _ = DrainAsync();
        return new(completion.Task);

        async Task DrainAsync()
        {
            try
            {
                var closing = active is null ? Task.CompletedTask : CloseAsync(active);
                await Task.WhenAll(users.Append(closing));
                completion.TrySetResult();
            }
            catch (Exception exception) { completion.TrySetException(exception); }
        }
    }

    private sealed class DialogLease(IDialogReference reference)
    {
        public IDialogReference Reference { get; } = reference;
        public Task? Closing { get; set; }
    }
}
