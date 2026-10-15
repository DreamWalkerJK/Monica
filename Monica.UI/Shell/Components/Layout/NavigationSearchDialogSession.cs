using Monica.UI.Shell.Support;

namespace Monica.UI.Shell.Components.Layout;

/// <summary>Owns one search dialog's catalog revisions, access evaluations, and retirement.</summary>
internal sealed class NavigationSearchDialogSession : IAsyncDisposable
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<NavigationSearchEntry>>> _createEntries;
    private readonly Func<NavigationSearchEntry, CancellationToken, Task<bool>> _authorizeEntry;
    private readonly Func<Action, Task> _dispatch;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly object _sync = new();
    private readonly HashSet<Task> _pending = [];
    private readonly HashSet<long> _activatingRevisions = [];
    private Snapshot? _snapshot;
    private long _revision;
    private bool _retired;
    private Task? _disposeTask;

    internal NavigationSearchDialogSession(
        Func<CancellationToken, Task<IReadOnlyList<NavigationSearchEntry>>> createEntries,
        Func<NavigationSearchEntry, CancellationToken, Task<bool>> authorizeEntry,
        Func<Action, Task> dispatch)
    {
        _createEntries = createEntries;
        _authorizeEntry = authorizeEntry;
        _dispatch = dispatch;
        LifetimeToken = _lifetime.Token;
    }

    /// <summary>Gets the cached token while retirement retains and drains its actual producers.</summary>
    internal CancellationToken LifetimeToken { get; }

    internal sealed record Snapshot(long Revision, IReadOnlyList<NavigationSearchEntry> Entries);

    internal enum ActivationResult
    {
        Rejected,
        Denied,
        Opened
    }

    /// <summary>Invalidates displayed entries immediately, before an authentication event reaches the renderer.</summary>
    internal long InvalidateAuthentication()
    {
        lock (_sync)
        {
            if (!_retired)
            {
                _snapshot = null;
                ++_revision;
            }

            return _revision;
        }
    }

    internal bool IsCurrent(long revision)
    {
        lock (_sync)
        {
            return !_retired && revision == _revision;
        }
    }

    /// <summary>Retains the actual catalog producer; only the latest live revision can publish its snapshot.</summary>
    internal Task<Snapshot?> RefreshAsync()
    {
        TaskCompletionSource<Snapshot?> completion;
        long revision;
        lock (_sync)
        {
            if (_retired)
            {
                return Task.FromResult<Snapshot?>(null);
            }

            revision = ++_revision;
            _snapshot = null;
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending.Add(completion.Task);
        }

        _ = RefreshCoreAsync(revision, completion);
        return completion.Task;
    }

    /// <summary>
    /// Rechecks an exact displayed entry, then commits its native action on the renderer only while that revision
    /// remains current. The completion includes policy work, renderer dispatch, and the synchronous open callback.
    /// </summary>
    internal Task<ActivationResult> ActivateAsync(
        NavigationSearchEntry entry,
        long revision,
        Action<NavigationSearchEntry> open)
    {
        TaskCompletionSource<ActivationResult> completion;
        lock (_sync)
        {
            if (!IsCurrentEntry(entry, revision) || !_activatingRevisions.Add(revision))
            {
                return Task.FromResult(ActivationResult.Rejected);
            }

            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending.Add(completion.Task);
        }

        _ = ActivateCoreAsync(entry, revision, open, completion);
        return completion.Task;
    }

    private async Task RefreshCoreAsync(long revision, TaskCompletionSource<Snapshot?> completion)
    {
        try
        {
            var entries = await _createEntries(LifetimeToken);
            Snapshot? snapshot = null;
            lock (_sync)
            {
                if (!_retired && revision == _revision)
                {
                    snapshot = _snapshot = new Snapshot(revision, entries);
                }
            }

            completion.TrySetResult(snapshot);
        }
        catch (OperationCanceledException) when (LifetimeToken.IsCancellationRequested)
        {
            completion.TrySetResult(null);
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
        finally
        {
            lock (_sync) _pending.Remove(completion.Task);
        }
    }

    private async Task ActivateCoreAsync(
        NavigationSearchEntry entry,
        long revision,
        Action<NavigationSearchEntry> open,
        TaskCompletionSource<ActivationResult> completion)
    {
        try
        {
            var authorized = await _authorizeEntry(entry, LifetimeToken);
            lock (_sync)
            {
                if (!IsCurrentEntry(entry, revision))
                {
                    completion.TrySetResult(ActivationResult.Rejected);
                    return;
                }
            }

            var result = ActivationResult.Rejected;
            await _dispatch(() =>
            {
                lock (_sync)
                {
                    if (!IsCurrentEntry(entry, revision)) return;
                    result = authorized ? ActivationResult.Opened : ActivationResult.Denied;
                    if (authorized)
                    {
                        // The accepted action consumes this snapshot before native Close can re-enter its owner.
                        _snapshot = null;
                        ++_revision;
                    }
                }

                if (result == ActivationResult.Opened) open(entry);
            });
            completion.TrySetResult(result);
        }
        catch (OperationCanceledException) when (LifetimeToken.IsCancellationRequested)
        {
            completion.TrySetResult(ActivationResult.Rejected);
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
        finally
        {
            lock (_sync)
            {
                _activatingRevisions.Remove(revision);
                _pending.Remove(completion.Task);
            }
        }
    }

    private bool IsCurrentEntry(NavigationSearchEntry entry, long revision) =>
        !_retired && _snapshot is { } snapshot && snapshot.Revision == revision &&
        snapshot.Entries.Any(current => ReferenceEquals(current, entry));

    /// <summary>Seals admission, cancels policies, and drains every retained producer before releasing its token source.</summary>
    public ValueTask DisposeAsync()
    {
        TaskCompletionSource completion;
        Task[] pending;
        lock (_sync)
        {
            if (_disposeTask is { } disposal) return new ValueTask(disposal);
            _retired = true;
            _snapshot = null;
            ++_revision;
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _disposeTask = completion.Task;
            pending = _pending.ToArray();
        }

        _ = DisposeCoreAsync(pending, completion);
        return new ValueTask(completion.Task);
    }

    private async Task DisposeCoreAsync(Task[] pending, TaskCompletionSource completion)
    {
        try
        {
            Task cancellation = Task.CompletedTask;
            try { _lifetime.Cancel(); }
            catch (Exception exception) { cancellation = Task.FromException(exception); }
            try { await Task.WhenAll(pending.Append(cancellation)); }
            finally { _lifetime.Dispose(); }
            completion.TrySetResult();
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
    }
}
