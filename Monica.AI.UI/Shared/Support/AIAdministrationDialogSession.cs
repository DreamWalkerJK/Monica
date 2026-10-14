using Microsoft.AspNetCore.Components;
using Monica.AI.UI.Shared.Components;
using MudBlazor;

namespace Monica.AI.UI.Shared.Support;

/// <summary>Owns native dialog producers and results for one routed administration page.</summary>
/// <remarks>
/// The dialog service is borrowed. Cancellation closes only this session's dialog on the renderer.
/// Async dialog components must retire idempotently: this session drains them after their native result,
/// and the native provider subsequently disposes the rendered component.
/// </remarks>
internal sealed class AIAdministrationDialogSession(IDialogService dialogs, Func<Action, Task> dispatch) : IAsyncDisposable
{
    private readonly AIAdministrationOperationLifetime _lifetime = new();
    private bool _opening;

    internal async Task<DialogResult?> ShowAsync<T>(string title, DialogParameters parameters, DialogOptions? options = null)
        where T : IComponent
    {
        if (_lifetime.IsRetired || _opening) return null;
        _opening = true;
        try
        {
            return await _lifetime.RunAsync(async token =>
            {
                var reference = await dialogs.ShowAsync<T>(title, parameters, options ?? new()
                {
                    FullWidth = true, MaxWidth = MaxWidth.Medium, CloseButton = true, CloseOnEscapeKey = true
                });
                Task closing = Task.CompletedTask;
                using var cancellation = token.Register(() =>
                {
                    // Native dialog results can resume inline while Close is still on the stack.
                    var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    closing = completion.Task;
                    _ = CloseAsync(completion);
                });
                async Task CloseAsync(TaskCompletionSource completion)
                {
                    try
                    {
                        await dispatch(() => reference.Close(DialogResult.Cancel()));
                        completion.TrySetResult();
                    }
                    catch (Exception exception)
                    {
                        // A renderer failure must still release the result producer before retirement observes it.
                        try { reference.Dismiss(DialogResult.Cancel()); }
                        catch (Exception dismissFailure) { exception = new AggregateException(exception, dismissFailure); }
                        completion.TrySetException(exception);
                    }
                }
                try { return await reference.Result; }
                finally
                {
                    // Escape/X can finish the native result before the renderer disposes its component.
                    await Task.WhenAll(closing, RetireDialogAsync(reference.Dialog));
                }
            });
        }
        catch (OperationCanceledException) when (_lifetime.IsRetired) { return null; }
        finally { if (!_lifetime.IsRetired) _opening = false; }
    }

    private static async Task RetireDialogAsync(object? component)
    {
        if (component is IAsyncDisposable owner) await owner.DisposeAsync();
    }

    internal async Task<bool> ConfirmAsync(string title, string description, string confirmText) =>
        await ShowAsync<AIAdministrationConfirmation>(title, new()
        {
            [nameof(AIAdministrationConfirmation.Title)] = title,
            [nameof(AIAdministrationConfirmation.Description)] = description,
            [nameof(AIAdministrationConfirmation.ConfirmText)] = confirmText
        }, new() { FullWidth = true, MaxWidth = MaxWidth.Small, CloseButton = true, CloseOnEscapeKey = true })
        is { Canceled: false, Data: true };

    public ValueTask DisposeAsync() => _lifetime.RetireAsync();
}
