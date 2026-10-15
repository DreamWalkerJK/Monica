namespace Monica.JobScheduler.UI.UIJobScheduler.Support;

/// <summary>Owns admitted scheduler operations and the resources they use until actual completion.</summary>
/// <remarks>
/// Completion is reserved before invoking a producer. Retirement rejects new work, cancels admitted operations,
/// and drains them before cleanup. Producers, cancellation, notifications, and cleanup run outside the admission
/// gate. Consumers may retain serialization gates, which they must release only during cleanup.
/// </remarks>
internal sealed class SchedulerOperationLifetime
{
    private readonly object _gate = new();
    private readonly CancellationTokenSource _cancellation = new();
    private readonly CancellationToken _token;
    private readonly HashSet<Task> _operations = [];
    private TaskCompletionSource? _retirement;
    private bool _retired;

    internal SchedulerOperationLifetime() => _token = _cancellation.Token;

    internal bool IsRetired
    {
        get { lock (_gate) return _retired; }
    }

    /// <summary>Admits work before calling its producer; retired admission is canceled without starting it.</summary>
    internal Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> operation)
    {
        TaskCompletionSource<T> completion;
        lock (_gate)
        {
            if (_retired) return Task.FromCanceled<T>(new CancellationToken(canceled: true));
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _operations.Add(completion.Task);
        }

        _ = CompleteOperationAsync(operation, completion);
        return completion.Task;
    }

    internal Task RunAsync(Func<CancellationToken, Task> operation) => RunAsync(async token =>
    {
        await operation(token);
        return true;
    });

    /// <summary>Applies only synchronous owner-state changes while retirement cannot intervene.</summary>
    /// <remarks>The update must not call producers, notify subscribers, cancel work, or release resources.</remarks>
    internal bool TryApply(Action update)
    {
        lock (_gate)
        {
            if (_retired) return false;
            update();
            return true;
        }
    }

    internal Func<Task>[] SnapshotHandlers(Func<Task>? changed)
    {
        lock (_gate)
        {
            return _retired ? [] : changed?.GetInvocationList().Cast<Func<Task>>().ToArray() ?? [];
        }
    }

    /// <summary>Shares retirement completion, including cancellation faults and every admitted operation.</summary>
    internal ValueTask RetireAsync(Action cleanup) => RetireAsync(() =>
    {
        cleanup();
        return ValueTask.CompletedTask;
    });

    internal ValueTask RetireAsync(Func<ValueTask> cleanup)
    {
        TaskCompletionSource completion;
        Task[] operations;
        lock (_gate)
        {
            if (_retirement is not null) return new(_retirement.Task);
            _retired = true;
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _retirement = completion;
            operations = _operations.ToArray();
        }

        _ = CompleteRetirementAsync(operations, cleanup, completion);
        return new(completion.Task);
    }

    private async Task CompleteOperationAsync<T>(
        Func<CancellationToken, Task<T>> operation, TaskCompletionSource<T> completion)
    {
        try
        {
            _token.ThrowIfCancellationRequested();
            completion.TrySetResult(await operation(_token));
        }
        catch (OperationCanceledException exception)
        {
            completion.TrySetCanceled(exception.CancellationToken);
        }
        catch (Exception exception) { completion.TrySetException(exception); }
        finally { lock (_gate) _operations.Remove(completion.Task); }
    }

    private async Task CompleteRetirementAsync(
        Task[] operations, Func<ValueTask> cleanup, TaskCompletionSource completion)
    {
        var failures = new List<Exception>();
        try { await _cancellation.CancelAsync(); }
        catch (Exception exception) { failures.Add(exception); }
        foreach (var operation in operations)
        {
            try { await operation; }
            catch (OperationCanceledException) when (_token.IsCancellationRequested) { }
            catch (Exception exception) { failures.Add(exception); }
        }
        try { await cleanup(); }
        catch (Exception exception) { failures.Add(exception); }
        try { _cancellation.Dispose(); }
        catch (Exception exception) { failures.Add(exception); }

        if (failures.Count == 0) completion.TrySetResult();
        else completion.TrySetException(new AggregateException("Scheduler operation retirement failed.", failures));
    }
}
