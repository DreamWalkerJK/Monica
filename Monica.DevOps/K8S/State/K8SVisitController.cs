namespace Monica.DevOps.K8S.State;

/// <summary>Owns actual producers and publication for one explicitly created routed visit.</summary>
internal abstract class K8SVisitController : IAsyncDisposable
{
    private readonly object _sync = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly HashSet<Task> _users = [];
    private Task? _retirement;
    private volatile bool _retired;
    private readonly CancellationToken _token;
    private readonly Func<Task> _publish;

    protected K8SVisitController(Func<Task> publish)
    {
        _publish = publish;
        _token = _lifetime.Token;
    }
    internal bool IsRetired => _retired;
    internal bool IsBusy { get; private set; }
    internal CancellationToken CancellationToken => _token;

    protected Task RunAsync(Func<CancellationToken, Task> action) => TrackAsync(action, exclusive: true);
    protected Task RunPresentationAsync(Func<CancellationToken, Task> action) => TrackAsync(action, exclusive: false);

    internal Task WhenIdleAsync()
    {
        lock (_sync) return Task.WhenAll(_users.ToArray());
    }

    private Task TrackAsync(Func<CancellationToken, Task> action, bool exclusive)
    {
        TaskCompletionSource completion;
        CancellationToken token;
        lock (_sync)
        {
            if (_retired || exclusive && IsBusy) return Task.CompletedTask;
            if (exclusive) IsBusy = true;
            token = _token;
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _users.Add(completion.Task);
        }
        _ = CompleteAsync();
        return completion.Task;

        async Task CompleteAsync()
        {
            Exception? failure = null;
            try
            {
                if (exclusive) await PublishAsync();
                token.ThrowIfCancellationRequested();
                await action(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception exception) { failure = exception; }
            finally
            {
                if (exclusive) CompleteOperation();
                if (exclusive) IsBusy = false;
                try { if (exclusive) await PublishAsync(); }
                catch (Exception exception) { failure = failure is null ? exception : new AggregateException(failure, exception); }
                lock (_sync)
                {
                    _users.Remove(completion.Task);
                    if (failure is null) completion.TrySetResult();
                    else completion.TrySetException(failure);
                }
            }
        }
    }

    protected Task PublishAsync() => _retired ? Task.CompletedTask : _publish();

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
            var failures = new List<Exception>();
            try
            {
                try { _lifetime.Cancel(); }
                catch (Exception exception) { failures.Add(exception); }
                foreach (var task in users)
                {
                    try { await task; }
                    catch (OperationCanceledException) { }
                    catch (Exception exception) { failures.Add(exception); }
                }
                try { RetireEvidence(); }
                catch (Exception exception) { failures.Add(exception); }
            }
            finally { _lifetime.Dispose(); }
            if (failures.Count == 0) completion.TrySetResult();
            else completion.TrySetException(new AggregateException(failures));
        }
    }

    protected virtual void RetireEvidence() { }
    protected virtual void CompleteOperation() { }
}
