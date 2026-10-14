using MudBlazor;

namespace Monica.JobScheduler.UI.UIJobScheduler.Support;

/// <summary>Owns a scheduler dialog producer, its actual native result, and the completion callback.</summary>
/// <remarks>
/// A request must contain the exact target captured before admission. Duplicate admission is ignored. Retirement
/// closes an accepted or late-produced reference and drains its actual result, even when requesting Close fails.
/// Producers, Close, and callbacks run outside the gate. The callback must also guard its page or route owner
/// before updating UI or refreshing data.
/// </remarks>
internal sealed class SchedulerDialogSession<TRequest>(
    Func<TRequest, Task<IDialogReference>> openDialog,
    Func<TRequest, DialogResult?, Task> completed) : IAsyncDisposable
{
    private readonly object _gate = new();
    private TaskCompletionSource? _operation;
    private TaskCompletionSource? _retirement;
    private IDialogReference? _dialog;
    private bool _retired;

    internal Task OpenAsync(TRequest request)
    {
        TaskCompletionSource completion;
        lock (_gate)
        {
            if (_retired || _operation is not null) return Task.CompletedTask;
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _operation = completion;
        }

        _ = CompleteOpenAsync(request, completion);
        return completion.Task;
    }

    private async Task CompleteOpenAsync(TRequest request, TaskCompletionSource completion)
    {
        IDialogReference? dialog = null;
        Exception? failure = null;
        try
        {
            dialog = await openDialog(request);
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

            bool canComplete;
            lock (_gate) canComplete = !_retired;
            if (canComplete) await completed(request, result);
        }
        catch (Exception exception) { failure = exception; }
        finally
        {
            lock (_gate)
            {
                if (ReferenceEquals(_dialog, dialog)) _dialog = null;
                if (failure is null) completion.TrySetResult();
                else completion.TrySetException(failure);
                _operation = null;
            }
        }
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

    private static async Task CompleteRetirementAsync(
        IDialogReference? dialog, Task? operation, TaskCompletionSource completion)
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
            catch (Exception exception) { failures.Add(exception); }
        }

        if (failures.Count == 0) completion.TrySetResult();
        else completion.TrySetException(new AggregateException("Scheduler dialog retirement failed.", failures));
    }
}
