using MudBlazor;

namespace Monica.Configuration.UI.Support;

/// <summary>Owns admitted configuration UI work until its actual producer and resource use complete.</summary>
/// <remarks>
/// Retirement rejects new admission and cancels only caller-owned work; it cannot reverse a persisted facade
/// operation. Producers, cancellation, callbacks, native dialog Close, and cleanup run outside the admission gate.
/// Do not await this owner's retirement from an operation admitted by the same owner.
/// Live callers must observe the returned task; completed operations are released. Retirement joins the actual
/// operations still admitted at its boundary and retains their unexpected failures, including foreign cancellation.
/// </remarks>
internal sealed class ConfigurationOperationLifetime
{
    private readonly object _gate = new();
    private readonly CancellationTokenSource _cancellation = new();
    private readonly CancellationToken _token;
    private readonly HashSet<Task> _operations = [];
    private TaskCompletionSource? _retirement;
    private bool _retired;

    internal ConfigurationOperationLifetime() => _token = _cancellation.Token;

    internal bool IsRetired { get { lock (_gate) return _retired; } }

    internal Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> producer)
    {
        TaskCompletionSource<T> completion;
        lock (_gate)
        {
            if (_retired) return Task.FromCanceled<T>(new CancellationToken(canceled: true));
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

    /// <summary>Applies a synchronous state update while retirement cannot intervene.</summary>
    /// <remarks>The update must not call external producers, subscribers, cancellation, or resource cleanup.</remarks>
    internal bool TryApply(Action update)
    {
        lock (_gate)
        {
            if (_retired) return false;
            update();
            return true;
        }
    }

    /// <summary>Retains a native dialog producer and its actual result, including a late reference.</summary>
    /// <remarks>
    /// The actual result is returned even after retirement. A caller must reject late UI adoption, but can reconcile
    /// a completed persistence outcome against its captured drafts. A cancelled dialog result is never a saved result.
    /// </remarks>
    internal Task<DialogResult?> ShowDialogAsync(Func<Task<IDialogReference>> producer) => RunAsync(async token =>
    {
        await using var session = new ConfigurationDialogSession(producer, token);
        return await session.OpenAsync();
    });

    internal ValueTask RetireAsync() => RetireAsync(static () => ValueTask.CompletedTask);

    /// <summary>Shares actual retirement completion, draining all work before releasing owned resources.</summary>
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

    private async Task CompleteAsync<T>(Func<CancellationToken, Task<T>> producer, TaskCompletionSource<T> completion)
    {
        try
        {
            _token.ThrowIfCancellationRequested();
            completion.TrySetResult(await producer(_token));
        }
        catch (OperationCanceledException exception) when (_token.IsCancellationRequested && exception.CancellationToken == _token)
        {
            completion.TrySetCanceled(_token);
        }
        catch (OperationCanceledException exception)
        {
            completion.TrySetException(new InvalidOperationException("Configuration work was cancelled outside its owner lifetime.", exception));
        }
        catch (Exception exception) { completion.TrySetException(exception); }
        finally { lock (_gate) _operations.Remove(completion.Task); }
    }

    private async Task CompleteRetirementAsync(Task[] operations, Func<ValueTask> cleanup, TaskCompletionSource completion)
    {
        var failures = new List<Exception>();
        // Native dialog retirement callbacks require the caller's renderer context.
        try { _cancellation.Cancel(); }
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
        else completion.TrySetException(new AggregateException("Configuration UI retirement failed.", failures));
    }
}
