using Monica.DevOps.K8S.Dialogs;
using Monica.DevOps.K8S.Support;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Test.Monica.DevOps.K8S.Support;

public sealed class K8SDialogOwnerTests
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
        dialogs.ShowAsync<K8SDescribeDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(opening.Task);
        var owner = new K8SDialogOwner(dialogs, action => { action(); return Task.CompletedTask; });
        var showing = owner.ShowAsync<K8SDescribeDialog>("Create", new DialogParameters());
        var retirement = owner.DisposeAsync().AsTask();
        try
        {
            Assert.False(retirement.IsCompleted);
            Assert.Same(retirement, owner.DisposeAsync().AsTask());
            Assert.Null(await owner.ShowAsync<K8SDescribeDialog>("Rejected", new DialogParameters()));
            opening.SetResult(reference);
            await ObserveAsync(retirement);
            Assert.Null(await ObserveAsync(showing));
            Assert.True((await nativeResult.Task)!.Canceled);
            reference.Received(1).Close(Arg.Any<DialogResult>());
            _ = dialogs.Received(1).ShowAsync<K8SDescribeDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>());
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
        dialogs.ShowAsync<K8SRestartConfirmDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult(reference));
        var owner = new K8SDialogOwner(dialogs, action => { action(); return dispatched.Task; });
        var showing = owner.ShowAsync<K8SRestartConfirmDialog>("Delete", new DialogParameters());
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
        dialogs.ShowAsync<K8SRestartConfirmDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult(reference));
        var owner = new K8SDialogOwner(dialogs, action => { action(); return dispatched.Task; });
        var showing = owner.ShowAsync<K8SRestartConfirmDialog>("Delete", new DialogParameters());
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
        dialogs.ShowAsync<K8SDescribeDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult(reference));
        await using var owner = new K8SDialogOwner(dialogs, action => { action(); return Task.CompletedTask; });
        var showing = owner.ShowAsync<K8SDescribeDialog>("Create", new DialogParameters());
        Assert.Null(await owner.ShowAsync<K8SDescribeDialog>("Overlap", new DialogParameters()));
        nativeResult.SetResult(DialogResult.Cancel());

        Assert.True((await ObserveAsync(showing))!.Canceled);
        reference.DidNotReceive().Close(Arg.Any<DialogResult>());
    }

    [Fact]
    public async Task ShowAsync_WhenNativeResultCompletes_ShouldDrainAcquiredInspectorBeforeReturning()
    {
        var nativeResult = new TaskCompletionSource<DialogResult?>();
        var component = new PendingInspector();
        var reference = Substitute.For<IDialogReference>();
        reference.Result.Returns(nativeResult.Task);
        reference.Dialog.Returns(component);
        var dialogs = Substitute.For<IDialogService>();
        dialogs.ShowAsync<K8SDescribeDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult(reference));
        await using var owner = new K8SDialogOwner(dialogs, action => { action(); return Task.CompletedTask; });
        var showing = owner.ShowAsync<K8SDescribeDialog>("Inspect", new DialogParameters());
        try
        {
            nativeResult.SetResult(DialogResult.Cancel());
            await ObserveAsync(component.RetirementStarted.Task);
            Assert.False(showing.IsCompleted);
            Assert.Null(await owner.ShowAsync<K8SPodDescribeDialog>("Overlapping", new DialogParameters()));
            component.Drained.SetResult(true);
            Assert.True((await ObserveAsync(showing))!.Canceled);
            reference.DidNotReceive().Close(Arg.Any<DialogResult>());
        }
        finally { component.Drained.TrySetResult(true); }
    }

    [Fact]
    public async Task DisposeAsync_WhenInspectorRetirementIsPending_ShouldAwaitActualChildDrainage()
    {
        var nativeResult = new TaskCompletionSource<DialogResult?>();
        var component = new PendingInspector();
        var reference = Substitute.For<IDialogReference>();
        reference.Result.Returns(nativeResult.Task);
        reference.Dialog.Returns(component);
        reference.When(dialog => dialog.Close(Arg.Any<DialogResult>())).Do(call => nativeResult.TrySetResult(call.Arg<DialogResult>()));
        var dialogs = Substitute.For<IDialogService>();
        dialogs.ShowAsync<K8SDescribeDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult(reference));
        var owner = new K8SDialogOwner(dialogs, action => { action(); return Task.CompletedTask; });
        var showing = owner.ShowAsync<K8SDescribeDialog>("Inspect", new DialogParameters());
        var retirement = owner.DisposeAsync().AsTask();
        try
        {
            await ObserveAsync(component.RetirementStarted.Task);
            Assert.False(showing.IsCompleted);
            Assert.False(retirement.IsCompleted);
            Assert.Same(retirement, owner.DisposeAsync().AsTask());
            component.Drained.SetResult(true);
            await ObserveAsync(retirement);
            Assert.Null(await ObserveAsync(showing));
        }
        finally
        {
            component.Drained.TrySetResult(true);
            await ObserveAsync(owner.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenNativeProviderHasUnsubscribed_ShouldDismissAfterCloseDispatch()
    {
        var nativeService = new DialogService();
        var reference = new DialogReference(Guid.NewGuid(), nativeService);
        var dialogs = Substitute.For<IDialogService>();
        dialogs.ShowAsync<K8SRestartConfirmDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult<IDialogReference>(reference));
        var owner = new K8SDialogOwner(dialogs, action => { action(); return Task.CompletedTask; });
        var showing = owner.ShowAsync<K8SRestartConfirmDialog>("Restart", new DialogParameters());

        await ObserveAsync(owner.DisposeAsync().AsTask());

        Assert.True((await reference.Result)!.Canceled);
        Assert.Null(await ObserveAsync(showing));
    }

    private sealed class PendingInspector : IAsyncDisposable
    {
        public TaskCompletionSource<bool> RetirementStarted { get; } = Signal<bool>();
        public TaskCompletionSource<bool> Drained { get; } = Signal<bool>();
        public ValueTask DisposeAsync()
        {
            RetirementStarted.TrySetResult(true);
            return new(Drained.Task);
        }
    }

    private static TaskCompletionSource<T> Signal<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static Task ObserveAsync(Task task) => task.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);
    private static Task<T> ObserveAsync<T>(Task<T> task) => task.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);
}
