using MudBlazor;
using Microsoft.AspNetCore.Components;

namespace Monica.AI.UI.Shared.Support;

/// <summary>Owns a knowledge page's producers, selection generation, renderer dispatch and native dialogs.</summary>
internal sealed class KnowledgePageSession(Func<string?> selectedId, IDialogService dialogs, Func<Action, Task> dispatch)
    : IAsyncDisposable
{
    private readonly AIAdministrationOperationLifetime _lifetime = new();
    private readonly AIAdministrationDialogSession _dialogs = new(dialogs, dispatch);
    private readonly AsyncLocal<Origin?> _origin = new();
    private readonly List<Task> _background = [];
    private long _generation;
    private TaskCompletionSource? _retirement;
    internal bool IsRetired => _lifetime.IsRetired;
    internal CancellationToken Token => _lifetime.Token;
    internal long Generation => _generation;
    internal bool CanPublish => !IsRetired && (_origin.Value is not { } origin ||
        origin.Generation == _generation && string.Equals(origin.Id, selectedId(), StringComparison.OrdinalIgnoreCase));

    internal void AdvanceSelection()
    {
        _generation++;
        _origin.Value = new(selectedId(), _generation);
    }

    internal void CheckCurrent()
    {
        Token.ThrowIfCancellationRequested();
        if (_origin.Value is { } origin && (origin.Generation != _generation
            || !string.Equals(origin.Id, selectedId(), StringComparison.OrdinalIgnoreCase)))
            throw new SelectionSupersededException();
    }

    internal async Task RunAsync(Func<Task> producer, bool selectionBound = true)
    {
        var origin = selectionBound ? new Origin(selectedId(), _generation) : null;
        try
        {
            await _lifetime.RunAsync(async _ =>
            {
                _origin.Value = origin;
                try
                {
                    CheckCurrent();
                    await producer();
                }
                catch (SelectionSupersededException) { }
            });
        }
        catch (OperationCanceledException) when (IsRetired) { }
    }

    internal void StartBackground(Func<Task> producer, bool selectionBound = true)
    {
        if (IsRetired) return;
        var task = RunAsync(producer, selectionBound);
        lock (_background)
        {
            _background.RemoveAll(completed => completed.IsCompletedSuccessfully || completed.IsCanceled);
            _background.Add(task);
        }
    }

    internal Task PublishAsync(Action update) => RunAsync(async () =>
    {
        await dispatch(() => { CheckCurrent(); update(); });
    });

    internal Task<DialogResult?> ShowAsync<T>(string title, DialogParameters parameters, DialogOptions? options = null)
        where T : IComponent => _dialogs.ShowAsync<T>(title, parameters, options);

    internal async Task<bool?> ShowMessageBoxAsync(string title, string message, string yesText, string? cancelText = null)
    {
        CheckCurrent();
        var confirmed = await _dialogs.ConfirmAsync(title, message, yesText);
        CheckCurrent();
        return confirmed;
    }

    public ValueTask DisposeAsync()
    {
        TaskCompletionSource completion;
        lock (_background)
        {
            if (_retirement is not null) return new(_retirement.Task);
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _retirement = completion;
        }
        _ = CompleteRetirementAsync(completion);
        return new(completion.Task);
    }

    private async Task CompleteRetirementAsync(TaskCompletionSource completion)
    {
        try { await DisposeCoreAsync(); completion.TrySetResult(); }
        catch (Exception exception) { completion.TrySetException(exception); }
    }

    private async Task DisposeCoreAsync()
    {
        // Start both retirements before waiting: admitted commands may be awaiting a native dialog result.
        var operations = _lifetime.RetireAsync().AsTask();
        var nativeDialogs = _dialogs.DisposeAsync().AsTask();
        var failures = new List<Exception>();
        foreach (var task in new[] { operations, nativeDialogs })
        {
            try { await task; }
            catch (Exception exception) { failures.Add(exception); }
        }
        Task[] background;
        lock (_background) background = _background.ToArray();
        foreach (var task in background)
        {
            try { await task; }
            catch (Exception exception) { failures.Add(exception); }
        }
        if (failures.Count > 0) throw new AggregateException("Knowledge page retirement failed.", failures);
    }

    private sealed record Origin(string? Id, long Generation);
    private sealed class SelectionSupersededException() : OperationCanceledException(new CancellationToken(true)) { }
}
