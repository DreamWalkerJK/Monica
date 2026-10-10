using MudBlazor;

namespace Monica.JobScheduler.UI.UIJobScheduler.Executions.State;

/// <summary>Owns one recent-execution inspector producer, native result, and completion callback.</summary>
/// <remarks>
/// Admission and retirement reserve shared completion under a gate; native dialog work and callbacks
/// run outside it. Retirement closes a produced reference or rejects a late reference, then observes
/// the actual result and all admitted work. Callers must also reject refresh on a retired page owner.
/// </remarks>
internal sealed class ExecutionDialogSession(
    Func<string, Task<IDialogReference>> openDialog,
    Func<Task> refresh) : IAsyncDisposable
{
    private readonly object _gate = new();
    private TaskCompletionSource? _operation;
    private TaskCompletionSource? _retirement;
    private IDialogReference? _dialog;
    private bool _retired;

    /// <summary>Admits a single inspector using the exact clicked identity; duplicate or retired admission is ignored.</summary>
    internal Task OpenAsync(string instanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        TaskCompletionSource completion;
        lock (_gate)
        {
            if (_retired || _operation is not null) return Task.CompletedTask;
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _operation = completion;
        }

        _ = CompleteOpenAsync(instanceId, completion);
        return completion.Task;
    }

    private async Task CompleteOpenAsync(string instanceId, TaskCompletionSource completion)
    {
        IDialogReference? dialog = null;
        Exception? failure = null;
        try
        {
            dialog = await openDialog(instanceId);
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

            // A failed close request does not release ownership of the actual native result.
            try { await dialog.Result; }
            catch (Exception exception) when (closeFailure is not null)
            {
                throw new AggregateException(closeFailure, exception);
            }
            if (closeFailure is not null) throw new AggregateException(closeFailure);

            bool canRefresh;
            lock (_gate) canRefresh = !_retired;
            if (canRefresh) await refresh();
        }
        catch (Exception exception) { failure = exception; }
        finally
        {
            lock (_gate)
            {
                if (ReferenceEquals(_dialog, dialog)) _dialog = null;
                // Continuations are asynchronous, so completion cannot invoke consumer work under the gate.
                if (failure is null) completion.TrySetResult();
                else completion.TrySetException(failure);
                _operation = null;
            }
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        TaskCompletionSource completion;
        IDialogReference? dialog;
        Task? operation;
        lock (_gate)
        {
            if (_retirement is not null) return new ValueTask(_retirement.Task);
            _retired = true;
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _retirement = completion;
            dialog = _dialog;
            operation = _operation?.Task;
        }

        _ = CompleteRetirementAsync(dialog, operation, completion);
        return new ValueTask(completion.Task);
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
        else completion.TrySetException(new AggregateException("Execution inspector retirement failed.", failures));
    }
}
