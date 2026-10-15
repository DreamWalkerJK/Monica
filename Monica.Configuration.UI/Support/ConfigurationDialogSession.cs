using MudBlazor;

namespace Monica.Configuration.UI.Support;

/// <summary>Owns one actual native dialog producer, result, close dispatch and acquired component retirement.</summary>
/// <remarks>
/// Duplicate opens join the same operation. Retirement rejects new opens, closes late references and joins
/// their actual result and captured async component. Actual results remain available after retirement for
/// persistence reconciliation; callers must separately reject retired UI adoption. Services remain borrowed.
/// Supply a renderer dispatcher when closure can originate outside it; without one, all calls and owner-token
/// cancellation must run on the renderer. Components must expose idempotent asynchronous retirement.
/// </remarks>
internal sealed class ConfigurationDialogSession(
    Func<Task<IDialogReference>> producer,
    CancellationToken ownerToken = default,
    Func<Action, Task>? dispatch = null) : IAsyncDisposable
{
    private readonly object _gate = new();
    private TaskCompletionSource<DialogResult?>? _operation;
    private TaskCompletionSource? _retirement;
    private DialogLease? _lease;
    private bool _retired;

    internal Task<DialogResult?> OpenAsync()
    {
        TaskCompletionSource<DialogResult?> completion;
        lock (_gate)
        {
            if (_operation is not null) return _operation.Task;
            if (_retired) return Task.FromResult<DialogResult?>(null);
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _operation = completion;
        }

        _ = CompleteOpenAsync(completion);
        return completion.Task;
    }

    private async Task CompleteOpenAsync(TaskCompletionSource<DialogResult?> completion)
    {
        DialogLease? lease = null;
        DialogResult? outcome = null;
        var failures = new List<Exception>();
        var cancelled = false;
        using (ownerToken.Register(() => { _ = DisposeAsync(); }))
        {
            try
            {
                ownerToken.ThrowIfCancellationRequested();
                var reference = await producer();
                // Capture the acquired component before observing a result that may complete inline.
                lease = new(reference, reference.Dialog as IAsyncDisposable);
                bool closeLate;
                lock (_gate) { _lease = lease; closeLate = _retired; }
                if (closeLate) _ = CloseAsync(lease);
                outcome = await reference.Result;
            }
            catch (OperationCanceledException exception) when
                (ownerToken.IsCancellationRequested && exception.CancellationToken == ownerToken)
            {
                cancelled = true;
            }
            catch (OperationCanceledException exception)
            {
                failures.Add(new InvalidOperationException(
                    "Configuration dialog work was cancelled outside its owner lifetime.", exception));
            }
            catch (Exception exception) { failures.Add(exception); }

            if (lease is not null)
            {
                Task componentRetirement = Task.CompletedTask;
                try
                {
                    // Native Result does not wait for the renderer's eventual component disposal.
                    if (lease.Component is not null) componentRetirement = lease.Component.DisposeAsync().AsTask();
                }
                catch (Exception exception) { failures.Add(exception); }

                Task closing;
                lock (_gate) closing = lease.Closing ?? Task.CompletedTask;
                try { await closing; }
                catch (Exception exception) { failures.Add(exception); }
                try { await componentRetirement; }
                catch (Exception exception) { failures.Add(exception); }
            }
            lock (_gate) { if (ReferenceEquals(_lease, lease)) _lease = null; }
        }

        if (failures.Count == 1) completion.TrySetException(failures[0]);
        else if (failures.Count > 1) completion.TrySetException(new AggregateException("Configuration dialog drainage failed.", failures));
        else if (cancelled) completion.TrySetCanceled(ownerToken);
        else completion.TrySetResult(outcome);
    }

    private Task CloseAsync(DialogLease lease)
    {
        TaskCompletionSource completion;
        lock (_gate)
        {
            if (lease.Closing is not null) return lease.Closing;
        }
        // A naturally completed result needs drainage, not another native Close request.
        if (lease.Reference.Result.IsCompleted) return Task.CompletedTask;
        lock (_gate)
        {
            if (lease.Closing is not null) return lease.Closing;
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            lease.Closing = completion.Task;
        }

        _ = DispatchCloseAsync();
        return completion.Task;

        async Task DispatchCloseAsync()
        {
            try
            {
                if (dispatch is null) lease.Reference.Close(DialogResult.Cancel());
                else await dispatch(() => lease.Reference.Close(DialogResult.Cancel()));
                // Close raises the service event. A retired provider may no longer complete Result.
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

    public ValueTask DisposeAsync()
    {
        TaskCompletionSource completion;
        DialogLease? lease;
        Task? operation;
        lock (_gate)
        {
            if (_retirement is not null) return new(_retirement.Task);
            _retired = true;
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _retirement = completion;
            lease = _lease;
            operation = _operation?.Task;
        }

        _ = CompleteRetirementAsync(lease, operation, completion);
        return new(completion.Task);
    }

    private async Task CompleteRetirementAsync(DialogLease? lease, Task? operation, TaskCompletionSource completion)
    {
        var failures = new List<Exception>();
        // Start resource retirement before waiting for the operation that needs its native result.
        var closing = lease is null ? Task.CompletedTask : CloseAsync(lease);
        try { await closing; }
        catch (Exception exception) { failures.Add(exception); }
        if (operation is not null)
        {
            try { await operation; }
            catch (OperationCanceledException exception) when
                (ownerToken.IsCancellationRequested && exception.CancellationToken == ownerToken) { }
            catch (Exception exception) { failures.Add(exception); }
        }

        if (failures.Count == 0) completion.TrySetResult();
        else completion.TrySetException(new AggregateException("Configuration dialog retirement failed.", failures));
    }

    private sealed class DialogLease(IDialogReference reference, IAsyncDisposable? component)
    {
        internal IDialogReference Reference { get; } = reference;
        internal IAsyncDisposable? Component { get; } = component;
        internal Task? Closing { get; set; }
    }
}
