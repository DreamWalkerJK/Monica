using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Monica.Markdown.UIMarkdown.State;

/// <summary>Owns native acquisition, close dispatch, result, and async component work for viewer dialogs.</summary>
/// <remarks>The dialog service and renderer are borrowed. Dialog components must retire idempotently.</remarks>
internal sealed class MarkdownViewerDialogSession(IDialogService dialogs, Func<Action, Task> dispatch)
    : IAsyncDisposable
{
    private readonly MarkdownViewerOperationLifetime _lifetime = new();
    private int _opening;

    internal async Task<DialogResult?> ShowAsync<T>(string title, DialogParameters parameters, DialogOptions options)
        where T : IComponent
    {
        if (_lifetime.IsRetired || Interlocked.CompareExchange(ref _opening, 1, 0) != 0) return null;
        DialogResult? result = null;
        try
        {
            await _lifetime.RunAsync(async token =>
            {
                token.ThrowIfCancellationRequested();
                var reference = await dialogs.ShowAsync<T>(title, parameters, options);
                Task closing = Task.CompletedTask;
                using var cancellation = token.Register(() =>
                {
                    // Close can complete Result inline; publish its task before invoking the renderer.
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
                        try { reference.Dismiss(DialogResult.Cancel()); }
                        catch (Exception dismissFailure) { exception = new AggregateException(exception, dismissFailure); }
                        completion.TrySetException(exception);
                    }
                }

                try { result = await reference.Result; }
                finally
                {
                    await cancellation.DisposeAsync();
                    await Task.WhenAll(closing, RetireComponentAsync(reference.Dialog));
                }
            });
            return result;
        }
        finally { Interlocked.Exchange(ref _opening, 0); }
    }

    private static async Task RetireComponentAsync(object? component)
    {
        if (component is IAsyncDisposable owner) await owner.DisposeAsync();
    }

    public ValueTask DisposeAsync() => _lifetime.RetireAsync();
}
