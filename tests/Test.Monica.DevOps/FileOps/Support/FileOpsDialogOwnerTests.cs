using Monica.DevOps.FileOps.Dialogs;
using Monica.DevOps.FileOps.Support;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Test.Monica.DevOps.FileOps.Support;

public sealed class FileOpsDialogOwnerTests
{
    private static readonly TimeSpan ObservationTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task DisposeAsync_WhenShowIsPending_ShouldCloseLateReferenceDrainResultAndRejectFurtherAdmission()
    {
        var opening = Signal<IDialogReference>();
        var nativeResult = new TaskCompletionSource<DialogResult?>();
        var reference = Substitute.For<IDialogReference>();
        reference.Result.Returns(nativeResult.Task);
        reference.When(dialog => dialog.Close(Arg.Any<DialogResult>())).Do(call => nativeResult.SetResult(call.Arg<DialogResult>()));
        var dialogs = Substitute.For<IDialogService>();
        dialogs.ShowAsync<FileOpsCreateDirectoryDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(opening.Task);
        var owner = new FileOpsDialogOwner(dialogs, action => { action(); return Task.CompletedTask; });
        var showing = owner.ShowAsync<FileOpsCreateDirectoryDialog>("Create", new DialogParameters());
        var retirement = owner.DisposeAsync().AsTask();
        try
        {
            Assert.False(retirement.IsCompleted);
            Assert.Same(retirement, owner.DisposeAsync().AsTask());
            Assert.Null(await owner.ShowAsync<FileOpsCreateDirectoryDialog>("Rejected", new DialogParameters()));
            opening.SetResult(reference);
            await ObserveAsync(retirement);
            Assert.Null(await ObserveAsync(showing));
            Assert.True((await nativeResult.Task)!.Canceled);
            reference.Received(1).Close(Arg.Any<DialogResult>());
            _ = dialogs.Received(1).ShowAsync<FileOpsCreateDirectoryDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>());
        }
        finally
        {
            opening.TrySetResult(reference);
            await ObserveAsync(owner.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenCloseCompletesNativeResultInlineButDispatchIsPending_ShouldJoinReservedCloseDispatch()
    {
        // Native Result permits synchronous continuations; using RunContinuationsAsynchronously would hide this race.
        var nativeResult = new TaskCompletionSource<DialogResult?>();
        var dispatched = Signal<bool>();
        var reference = Substitute.For<IDialogReference>();
        reference.Result.Returns(nativeResult.Task);
        reference.When(dialog => dialog.Close(Arg.Any<DialogResult>())).Do(call => nativeResult.SetResult(call.Arg<DialogResult>()));
        var dialogs = Substitute.For<IDialogService>();
        dialogs.ShowAsync<FileOpsBatchDeleteConfirmDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult(reference));
        var owner = new FileOpsDialogOwner(dialogs, action => { action(); return dispatched.Task; });
        var showing = owner.ShowAsync<FileOpsBatchDeleteConfirmDialog>("Delete", new DialogParameters());
        var retirement = owner.DisposeAsync().AsTask();
        try
        {
            Assert.True(nativeResult.Task.IsCompleted);
            Assert.False(showing.IsCompleted);
            Assert.False(retirement.IsCompleted);
            dispatched.SetResult(true);
            await ObserveAsync(retirement);
            Assert.Null(await ObserveAsync(showing));
            reference.Received(1).Close(Arg.Any<DialogResult>());
        }
        finally
        {
            dispatched.TrySetResult(true);
            await ObserveAsync(owner.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenCloseCompletesNativeResultInlineThenDispatchFails_ShouldObserveFailureWithoutSecondClose()
    {
        var nativeResult = new TaskCompletionSource<DialogResult?>();
        var dispatched = Signal<bool>();
        var reference = Substitute.For<IDialogReference>();
        reference.Result.Returns(nativeResult.Task);
        reference.When(dialog => dialog.Close(Arg.Any<DialogResult>())).Do(call => nativeResult.SetResult(call.Arg<DialogResult>()));
        reference.Dismiss(Arg.Any<DialogResult>()).Returns(call => nativeResult.TrySetResult(call.Arg<DialogResult>()));
        var dialogs = Substitute.For<IDialogService>();
        dialogs.ShowAsync<FileOpsBatchDeleteConfirmDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult(reference));
        var owner = new FileOpsDialogOwner(dialogs, action => { action(); return dispatched.Task; });
        var showing = owner.ShowAsync<FileOpsBatchDeleteConfirmDialog>("Delete", new DialogParameters());
        var retirement = owner.DisposeAsync().AsTask();
        Assert.True(nativeResult.Task.IsCompleted);
        Assert.False(showing.IsCompleted);
        Assert.False(retirement.IsCompleted);
        dispatched.SetException(new InvalidOperationException("Close dispatch failed"));

        var showFailure = await Assert.ThrowsAnyAsync<Exception>(() => ObserveAsync(showing));
        var retireFailure = await Assert.ThrowsAnyAsync<Exception>(() => ObserveAsync(retirement));
        Assert.Contains("Close dispatch failed", showFailure.ToString());
        Assert.Contains("Close dispatch failed", retireFailure.ToString());
        Assert.Same(retirement, owner.DisposeAsync().AsTask());
        reference.Received(1).Close(Arg.Any<DialogResult>());
        reference.Received(1).Dismiss(Arg.Any<DialogResult>());
    }

    [Fact]
    public async Task ShowAsync_WhenNativeCancelCompletes_ShouldReturnCanceledResultWithoutOwnerClosingAgain()
    {
        var nativeResult = new TaskCompletionSource<DialogResult?>();
        var reference = Substitute.For<IDialogReference>();
        reference.Result.Returns(nativeResult.Task);
        var dialogs = Substitute.For<IDialogService>();
        dialogs.ShowAsync<FileOpsCreateDirectoryDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult(reference));
        await using var owner = new FileOpsDialogOwner(dialogs, action => { action(); return Task.CompletedTask; });
        var showing = owner.ShowAsync<FileOpsCreateDirectoryDialog>("Create", new DialogParameters());
        Assert.Null(await owner.ShowAsync<FileOpsCreateDirectoryDialog>("Overlap", new DialogParameters()));
        nativeResult.SetResult(DialogResult.Cancel());

        Assert.True((await ObserveAsync(showing))!.Canceled);
        reference.DidNotReceive().Close(Arg.Any<DialogResult>());
    }

    private static TaskCompletionSource<T> Signal<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static Task ObserveAsync(Task task) => task.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);
    private static Task<T> ObserveAsync<T>(Task<T> task) => task.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);
}
