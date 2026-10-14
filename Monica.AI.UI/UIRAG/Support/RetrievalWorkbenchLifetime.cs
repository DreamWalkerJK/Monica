using Monica.AI.UI.Shared.Support;
using MudBlazor;

namespace Monica.AI.UI.UIRAG.Support;

/// <summary>Retires route-owned producers and closes and drains their acquired native dialogs.</summary>
/// <remarks>
/// Acquire dialogs only from admitted operations. The native dialog service is borrowed; each acquired
/// reference and its disposable component belong to this route. Cancellation and native close callbacks
/// run outside the registry lock. Producers must not await their own route's retirement.
/// </remarks>
internal sealed class RetrievalWorkbenchLifetime : IAsyncDisposable
{
    private readonly AIAdministrationOperationLifetime _operations = new();
    private readonly object _dialogGate = new();
    private readonly Dictionary<IDialogReference, DialogLease> _dialogs = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<Task> _dialogOperations = [];
    private TaskCompletionSource? _retirement;

    public bool Retired { get { lock (_dialogGate) return _retirement is not null; } }
    public CancellationToken Token => _operations.Token;

    public async Task RunAsync(Func<Task> action)
    {
        if (Retired) return;
        try { await _operations.RunAsync(_ => action()); }
        catch (OperationCanceledException) when (Retired) { }
    }

    public bool OwnDialog(IDialogReference dialog)
    {
        DialogLease lease;
        bool close;
        lock (_dialogGate)
        {
            if (_dialogs.ContainsKey(dialog)) return _retirement is null;
            lease = new(dialog);
            _dialogs.Add(dialog, lease);
            _dialogOperations.Add(lease.Completion.Task);
            close = _retirement is not null;
            if (close) lease.CloseCompletion = NewCompletion();
        }
        _ = ObserveDialogAsync(lease);
        if (close) CloseDialog(lease);
        return !close;
    }

    public void ReleaseDialog(IDialogReference dialog)
    {
        // The page has observed the native result. Drain ownership continues until component disposal.
        lock (_dialogGate) _dialogs.Remove(dialog);
    }

    public ValueTask DisposeAsync()
    {
        TaskCompletionSource completion;
        DialogLease[] dialogs;
        lock (_dialogGate)
        {
            if (_retirement is not null) return new(_retirement.Task);
            completion = NewCompletion();
            _retirement = completion;
            dialogs = _dialogs.Values.ToArray();
            foreach (var dialog in dialogs) dialog.CloseCompletion = NewCompletion();
        }

        // Start cancellation and every native close before awaiting any producer or dialog drain.
        var operations = _operations.RetireAsync().AsTask();
        foreach (var dialog in dialogs) CloseDialog(dialog);
        _ = CompleteRetirementAsync(operations, completion);
        return new(completion.Task);
    }

    private async Task CompleteRetirementAsync(Task operations, TaskCompletionSource completion)
    {
        var failures = new List<Exception>();
        try { await operations; }
        catch (Exception exception) { failures.Add(exception); }
        // All late acquisitions from admitted producers have now entered the dialog registry.
        Task[] dialogs;
        lock (_dialogGate) dialogs = _dialogOperations.ToArray();
        foreach (var dialog in dialogs)
        {
            try { await dialog; }
            catch (Exception exception) { failures.Add(exception); }
        }
        if (failures.Count == 0) completion.TrySetResult();
        else completion.TrySetException(new AggregateException("Retrieval workbench retirement failed.", failures));
    }

    private static void CloseDialog(DialogLease lease)
    {
        try
        {
            lease.Reference.Close(DialogResult.Cancel());
            lease.CloseCompletion!.TrySetResult();
        }
        catch (Exception exception)
        {
            // Release the native result even if the provider's close notification fails.
            try { lease.Reference.Dismiss(DialogResult.Cancel()); }
            catch (Exception dismissFailure) { exception = new AggregateException(exception, dismissFailure); }
            lease.CloseCompletion!.TrySetException(exception);
        }
    }

    private async Task ObserveDialogAsync(DialogLease lease)
    {
        var failures = new List<Exception>();
        try { await lease.Reference.Result; }
        catch (Exception exception) { failures.Add(exception); }
        try
        {
            if (lease.Reference.Dialog is IAsyncDisposable component) await component.DisposeAsync();
        }
        catch (Exception exception) { failures.Add(exception); }
        Task? close;
        lock (_dialogGate)
        {
            _dialogs.Remove(lease.Reference);
            close = lease.CloseCompletion?.Task;
        }
        // Native result continuations may run inline inside Close; retain that callback until it returns.
        if (close is not null)
        {
            try { await close; }
            catch (Exception exception) { failures.Add(exception); }
        }
        if (failures.Count == 0)
        {
            lease.Completion.TrySetResult();
            lock (_dialogGate) _dialogOperations.Remove(lease.Completion.Task);
        }
        else lease.Completion.TrySetException(new AggregateException("Owned dialog retirement failed.", failures));
    }

    private static TaskCompletionSource NewCompletion() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class DialogLease(IDialogReference reference)
    {
        internal IDialogReference Reference { get; } = reference;
        internal TaskCompletionSource Completion { get; } = NewCompletion();
        internal TaskCompletionSource? CloseCompletion { get; set; }
    }
}
