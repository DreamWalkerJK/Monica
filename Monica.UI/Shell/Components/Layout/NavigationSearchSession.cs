using System.Runtime.ExceptionServices;
using Monica.UI.Shell.Support;
using MudBlazor;

namespace Monica.UI.Shell.Components.Layout;

/// <summary>Owns one launcher's admitted search callback or native dialog through its actual result and child retirement.</summary>
internal sealed class NavigationSearchSession(
    Func<Task<NavigationSearchSession.DialogLease>> showDialog,
    Func<Action, Task> dispatch,
    Action<NavigationSearchEntry> navigate) : IAsyncDisposable
{
    private readonly object _sync = new();
    private readonly List<Task> _ownedOperations = [];
    private TaskCompletionSource? _operation;
    private TaskCompletionSource? _request;
    private TaskCompletionSource? _closing;
    private TaskCompletionSource? _retirement;
    private DialogLease? _dialog;
    private bool _retired;

    /// <summary>Captures the acquired NavigationSearchDialog before its native result can precede renderer disposal.</summary>
    internal sealed record DialogLease(IDialogReference Reference, IAsyncDisposable? Content);

    /// <summary>
    /// Acknowledges a built-in request after its native opening is captured, or a custom request after its callback
    /// completes. A still-owned dialog or child drain rejects reopening without extending that public acknowledgement.
    /// </summary>
    internal Task RequestAsync(Func<Task>? customSearch = null)
    {
        TaskCompletionSource completion;
        TaskCompletionSource request;
        lock (_sync)
        {
            if (_retired) return Task.CompletedTask;
            if (_operation is { Task.IsCompleted: false }) return _request!.Task;
            _ownedOperations.RemoveAll(static operation => operation.IsCompletedSuccessfully);
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            request = customSearch is null
                ? new(TaskCreationOptions.RunContinuationsAsynchronously)
                : completion;
            _operation = completion;
            _request = request;
            _ownedOperations.Add(completion.Task);
            _closing = null;
        }

        _ = CompleteOperationAsync(customSearch, request, completion);
        return request.Task;
    }

    private async Task CompleteOperationAsync(
        Func<Task>? customSearch,
        TaskCompletionSource request,
        TaskCompletionSource completion)
    {
        try
        {
            if (customSearch is not null) await customSearch();
            else await RunDialogAsync(request);
            completion.TrySetResult();
            request.TrySetResult();
        }
        catch (OperationCanceledException exception)
        {
            completion.TrySetCanceled(exception.CancellationToken);
            request.TrySetCanceled(exception.CancellationToken);
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
            request.TrySetException(exception);
        }
    }

    private async Task RunDialogAsync(TaskCompletionSource request)
    {
        var lease = await showDialog();
        bool retired;
        lock (_sync)
        {
            _dialog = lease;
            retired = _retired;
        }

        var nativeResult = lease.Reference.Result;
        var close = retired || lease.Content is null ? CloseAsync(lease.Reference) : Task.CompletedTask;
        request.TrySetResult();
        DialogResult? result = null;
        Exception? failure = null;
        try
        {
            // This is the native producer, not a cancellable wrapper around it.
            try { result = await nativeResult; }
            catch (Exception exception) { failure = exception; }

            // Start child retirement as soon as Result settles, even if its close dispatch is still draining.
            var child = lease.Content is { } content ? RetireContentAsync(content) : Task.CompletedTask;
            try { await Task.WhenAll(close, child); }
            catch (Exception exception)
            {
                failure = failure is null ? exception : new AggregateException(failure, exception);
            }
        }
        finally
        {
            lock (_sync)
            {
                if (_dialog == lease) _dialog = null;
            }
        }

        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
        if (lease.Content is not null && result is { Canceled: false, Data: NavigationSearchEntry entry })
        {
            await dispatch(() =>
            {
                lock (_sync)
                {
                    if (_retired) return;
                }

                navigate(entry);
            });
        }
    }

    private static async Task RetireContentAsync(IAsyncDisposable content) => await content.DisposeAsync();

    private Task CloseAsync(IDialogReference reference)
    {
        TaskCompletionSource completion;
        lock (_sync)
        {
            if (_closing is not null) return _closing.Task;
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _closing = completion;
        }

        _ = CompleteCloseAsync(reference, completion);
        return completion.Task;
    }

    private async Task CompleteCloseAsync(IDialogReference reference, TaskCompletionSource completion)
    {
        try
        {
            try
            {
                await dispatch(() =>
                {
                    if (!reference.Result.IsCompleted) reference.Close(DialogResult.Cancel());
                });
            }
            finally
            {
                // A live v9 provider completes Result synchronously during Close. A retired provider is unsubscribed.
                if (!reference.Result.IsCompleted) reference.Dismiss(DialogResult.Cancel());
            }

            completion.TrySetResult();
        }
        catch (OperationCanceledException exception)
        {
            completion.TrySetCanceled(exception.CancellationToken);
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
    }

    /// <summary>Seals admission/navigation, closes current or late references, and shares one complete retirement.</summary>
    public ValueTask DisposeAsync()
    {
        TaskCompletionSource completion;
        DialogLease? lease;
        Task[] operations;
        lock (_sync)
        {
            if (_retirement is not null) return new ValueTask(_retirement.Task);
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _retirement = completion;
            _retired = true;
            lease = _dialog;
            operations = _ownedOperations.ToArray();
        }

        _ = CompleteRetirementAsync(lease, operations, completion);
        return new ValueTask(completion.Task);
    }

    private async Task CompleteRetirementAsync(DialogLease? lease, Task[] operations, TaskCompletionSource completion)
    {
        try
        {
            var close = lease is null ? Task.CompletedTask : CloseAsync(lease.Reference);
            await Task.WhenAll(operations.Append(close));
            completion.TrySetResult();
        }
        catch (OperationCanceledException exception)
        {
            completion.TrySetCanceled(exception.CancellationToken);
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
    }
}
