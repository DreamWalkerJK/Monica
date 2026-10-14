namespace Monica.AI.UI.Shared.Support;

/// <summary>Owns actual administration operations until retirement has cancelled and drained them.</summary>
/// <remarks>
/// Producers and cleanup run outside the admission lock. A caller must observe each returned task and must
/// not await retirement from one of its own admitted operations. Initiating retirement and returning is safe;
/// awaiting one's own drainage is not. Cancellation cannot undo a durable write.
/// </remarks>
internal sealed class AIAdministrationOperationLifetime
{
    private readonly object _gate = new();
    private readonly CancellationTokenSource _cancellation = new();
    private readonly HashSet<Task> _operations = [];
    private TaskCompletionSource? _retirement;

    internal AIAdministrationOperationLifetime() => Token = _cancellation.Token;

    internal CancellationToken Token { get; }
    internal bool IsRetired { get { lock (_gate) return _retirement is not null; } }

    internal Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> producer)
    {
        TaskCompletionSource<T> completion;
        lock (_gate)
        {
            if (_retirement is not null) return Task.FromCanceled<T>(new CancellationToken(true));
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _operations.Add(completion.Task);
        }

        _ = CompleteAsync(producer, completion);
        return completion.Task;
    }

    internal Task RunAsync(Func<CancellationToken, Task> producer) => RunAsync(async token =>
    {
        await producer(token);
        return true;
    });

    /// <summary>Applies only synchronous owner state changes; do not invoke producers or callbacks here.</summary>
    internal bool TryApply(Action update)
    {
        lock (_gate)
        {
            if (_retirement is not null) return false;
            update();
            return true;
        }
    }

    internal ValueTask RetireAsync() => RetireAsync(static () => ValueTask.CompletedTask);

    internal ValueTask RetireAsync(Func<ValueTask> cleanup)
    {
        TaskCompletionSource completion;
        Task[] operations;
        lock (_gate)
        {
            if (_retirement is not null) return new(_retirement.Task);
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _retirement = completion;
            operations = _operations.ToArray();
        }

        _ = CompleteRetirementAsync(operations, cleanup, completion);
        return new(completion.Task);
    }

    private async Task CompleteAsync<T>(Func<CancellationToken, Task<T>> producer, TaskCompletionSource<T> completion)
    {
        try
        {
            Token.ThrowIfCancellationRequested();
            completion.TrySetResult(await producer(Token));
        }
        catch (OperationCanceledException exception) { completion.TrySetCanceled(exception.CancellationToken); }
        catch (Exception exception) { completion.TrySetException(exception); }
        finally { lock (_gate) _operations.Remove(completion.Task); }
    }

    private async Task CompleteRetirementAsync(Task[] operations, Func<ValueTask> cleanup, TaskCompletionSource completion)
    {
        var failures = new List<Exception>();
        try { await _cancellation.CancelAsync(); }
        catch (Exception exception) { failures.Add(exception); }
        foreach (var operation in operations)
        {
            try { await operation; }
            catch (OperationCanceledException) when (Token.IsCancellationRequested) { }
            catch (Exception exception) { failures.Add(exception); }
        }
        try { await cleanup(); }
        catch (Exception exception) { failures.Add(exception); }
        _cancellation.Dispose();
        if (failures.Count == 0) completion.TrySetResult();
        else completion.TrySetException(new AggregateException("AI administration retirement failed.", failures));
    }
}
