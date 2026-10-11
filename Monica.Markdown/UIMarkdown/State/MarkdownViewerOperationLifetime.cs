namespace Monica.Markdown.UIMarkdown.State;

/// <summary>Owns admitted viewer producers until cancellation and actual completion have drained.</summary>
/// <remarks>
/// Producers and cancellation callbacks run outside the admission lock. An admitted producer must not
/// await retirement of its own lifetime. Collaborators passed to producers remain borrowed.
/// </remarks>
internal sealed class MarkdownViewerOperationLifetime
{
    private readonly Lock _gate = new();
    private readonly CancellationTokenSource _cancellation = new();
    private readonly HashSet<Task> _operations = [];
    private TaskCompletionSource? _retirement;

    internal MarkdownViewerOperationLifetime() => Token = _cancellation.Token;

    internal CancellationToken Token { get; }
    internal bool IsRetired { get { lock (_gate) return _retirement is not null; } }

    internal Task RunAsync(Func<CancellationToken, Task> producer)
    {
        TryRunAsync(producer, out var operation);
        return operation;
    }

    internal bool TryRunAsync(Func<CancellationToken, Task> producer, out Task operation)
    {
        TaskCompletionSource completion;
        lock (_gate)
        {
            if (_retirement is not null)
            {
                operation = Task.CompletedTask;
                return false;
            }
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _operations.Add(completion.Task);
        }

        _ = CompleteAsync(producer, completion);
        operation = completion.Task;
        return true;
    }

    /// <summary>Applies synchronous state only; callbacks and producers must run outside this lock.</summary>
    internal bool TryApply(Action update)
    {
        lock (_gate)
        {
            if (_retirement is not null) return false;
            update();
            return true;
        }
    }

    internal ValueTask RetireAsync()
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

        _ = CompleteRetirementAsync(operations, completion);
        return new(completion.Task);
    }

    private async Task CompleteAsync(Func<CancellationToken, Task> producer, TaskCompletionSource completion)
    {
        // Admission and a caller's ownership fields must be published before synchronous producers run.
        await Task.Yield();
        try
        {
            await producer(Token);
            completion.TrySetResult();
        }
        catch (OperationCanceledException) when (Token.IsCancellationRequested) { completion.TrySetResult(); }
        catch (Exception exception) { completion.TrySetException(exception); }
        finally { lock (_gate) _operations.Remove(completion.Task); }
    }

    private async Task CompleteRetirementAsync(Task[] operations, TaskCompletionSource completion)
    {
        var failures = new List<Exception>();
        try { await _cancellation.CancelAsync(); }
        catch (Exception exception) { failures.Add(exception); }
        foreach (var operation in operations)
        {
            try { await operation; }
            catch (Exception exception) { failures.Add(exception); }
        }
        _cancellation.Dispose();
        if (failures.Count == 0) completion.TrySetResult();
        else completion.TrySetException(new AggregateException("Markdown viewer retirement failed.", failures));
    }
}
