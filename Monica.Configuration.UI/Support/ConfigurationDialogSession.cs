using MudBlazor;

namespace Monica.Configuration.UI.Support;

/// <summary>Owns one native dialog producer, reference, and actual result through repeated retirement.</summary>
/// <remarks>
/// Duplicate open calls join the same operation. Retirement requests native cancellation, closes late references,
/// and drains the actual result even if Close throws. No saved result is manufactured after native cancellation.
/// The owner token must be cancelled from the renderer when native Close requires that dispatcher.
/// </remarks>
internal sealed class ConfigurationDialogSession(
    Func<Task<IDialogReference>> producer, CancellationToken ownerToken = default) : IAsyncDisposable
{
    private readonly object _gate = new();
    private TaskCompletionSource<DialogResult?>? _operation;
    private TaskCompletionSource? _retirement;
    private IDialogReference? _dialog;
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
        DialogResult? outcome = null;
        Exception? failure = null;
        var cancelled = false;
        using (ownerToken.Register(() => { _ = DisposeAsync(); }))
        {
        try
        {
            ownerToken.ThrowIfCancellationRequested();
            var dialog = await producer();
            bool closeLate;
            lock (_gate)
            {
                closeLate = _retired;
                if (!closeLate) _dialog = dialog;
            }

            Exception? closeFailure = null;
            if (closeLate)
            {
                try { dialog.Close(DialogResult.Cancel()); }
                catch (Exception exception) { closeFailure = exception; }
            }

            DialogResult? result;
            try { result = await dialog.Result; }
            catch (Exception exception) when (closeFailure is not null)
            {
                throw new AggregateException(closeFailure, exception);
            }
            if (closeFailure is not null) throw new AggregateException(closeFailure);
            outcome = result;
        }
        catch (OperationCanceledException exception) when (ownerToken.IsCancellationRequested && exception.CancellationToken == ownerToken)
        {
            cancelled = true;
        }
        catch (OperationCanceledException exception)
        {
            failure = new InvalidOperationException("Configuration dialog work was cancelled outside its owner lifetime.", exception);
        }
        catch (Exception exception) { failure = exception; }
        finally { lock (_gate) _dialog = null; }
        }
        if (cancelled) completion.TrySetCanceled(ownerToken);
        else if (failure is not null) completion.TrySetException(failure);
        else completion.TrySetResult(outcome);
    }

    public ValueTask DisposeAsync()
    {
        TaskCompletionSource completion;
        IDialogReference? dialog;
        Task? operation;
        lock (_gate)
        {
            if (_retirement is not null) return new(_retirement.Task);
            _retired = true;
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _retirement = completion;
            dialog = _dialog;
            operation = _operation?.Task;
        }

        _ = CompleteRetirementAsync(dialog, operation, completion);
        return new(completion.Task);
    }

    private async Task CompleteRetirementAsync(IDialogReference? dialog, Task? operation, TaskCompletionSource completion)
    {
        var failures = new List<Exception>();
        if (dialog is not null)
        {
            try { dialog.Close(DialogResult.Cancel()); }
            catch (Exception exception) { failures.Add(exception); }
        }
        if (operation is not null)
        {
            try { await operation; }
            catch (OperationCanceledException exception) when (ownerToken.IsCancellationRequested && exception.CancellationToken == ownerToken) { }
            catch (Exception exception) { failures.Add(exception); }
        }

        if (failures.Count == 0) completion.TrySetResult();
        else completion.TrySetException(new AggregateException("Configuration dialog retirement failed.", failures));
    }
}
