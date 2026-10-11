using AwesomeAssertions;
using Monica.AI.UI.UIRAG.Support;
using MudBlazor;
using NSubstitute;

namespace Test.Monica.AI.UI.UIRAG.Support;

public sealed class RetrievalWorkbenchLifetimeTests
{
    [Fact]
    public async Task DisposeAsync_WhenTokenlessProducerIsPending_ShouldCancelAndDrainItBeforeReturning()
    {
        var lifetime = new RetrievalWorkbenchLifetime();
        var release = Completion();
        var cancelled = Completion();
        using var cancellation = lifetime.Token.Register(() => cancelled.SetResult());
        var producer = lifetime.RunAsync(() => release.Task);

        var retirement = lifetime.DisposeAsync().AsTask();
        await cancelled.Task;

        lifetime.Retired.Should().BeTrue();
        lifetime.Token.IsCancellationRequested.Should().BeTrue();
        retirement.IsCompleted.Should().BeFalse();
        lifetime.DisposeAsync().AsTask().Should().BeSameAs(retirement);
        release.SetResult();
        await producer;
        await retirement;
        var admittedAfterRetirement = false;
        await lifetime.RunAsync(() => { admittedAfterRetirement = true; return Task.CompletedTask; });
        admittedAfterRetirement.Should().BeFalse();
    }

    [Fact]
    public async Task DisposeAsync_WhenProducerFaultsLate_ShouldDrainAndReportItsActualFailure()
    {
        var lifetime = new RetrievalWorkbenchLifetime();
        var release = Completion();
        var failure = new InvalidOperationException("Tokenless retrieval failed.");
        var producer = lifetime.RunAsync(() => release.Task);
        var retirement = lifetime.DisposeAsync().AsTask();

        release.SetException(failure);

        (await Assert.ThrowsAsync<InvalidOperationException>(() => producer)).Should().BeSameAs(failure);
        var retirementFailure = await Assert.ThrowsAsync<AggregateException>(() => retirement);
        retirementFailure.Flatten().InnerExceptions.Should().ContainSingle().Which.Should().BeSameAs(failure);
    }

    [Fact]
    public async Task DisposeAsync_WhenDialogAcquisitionReturnsLate_ShouldCloseAndDrainItsOwnedComponent()
    {
        var lifetime = new RetrievalWorkbenchLifetime();
        var acquired = new TaskCompletionSource<IDialogReference>(TaskCreationOptions.RunContinuationsAsynchronously);
        var component = new OwnedComponent();
        var boundary = new DialogBoundary(component);
        var ownershipAccepted = true;
        var producer = lifetime.RunAsync(async () => { ownershipAccepted = lifetime.OwnDialog(await acquired.Task); });

        var retirement = lifetime.DisposeAsync().AsTask();
        retirement.IsCompleted.Should().BeFalse();
        acquired.SetResult(boundary.Reference);
        await producer;
        await component.Started.Task;

        ownershipAccepted.Should().BeFalse();
        (await boundary.Result.Task)!.Canceled.Should().BeTrue();
        retirement.IsCompleted.Should().BeFalse();
        component.DisposeCount.Should().Be(1);
        component.Release.SetResult();
        await retirement;
        await lifetime.DisposeAsync();
        component.DisposeCount.Should().Be(1);
        boundary.Reference.Received(1).Close(Arg.Is<DialogResult>(result => result.Canceled));
    }

    [Fact]
    public async Task DisposeAsync_WhenPageReleasedNativeResult_ShouldStillDrainPendingComponentDisposal()
    {
        var lifetime = new RetrievalWorkbenchLifetime();
        var component = new OwnedComponent();
        var boundary = new DialogBoundary(component);
        lifetime.OwnDialog(boundary.Reference).Should().BeTrue();
        boundary.Result.SetResult(DialogResult.Ok(true));
        await component.Started.Task;
        lifetime.ReleaseDialog(boundary.Reference);

        var retirement = lifetime.DisposeAsync().AsTask();

        retirement.IsCompleted.Should().BeFalse();
        boundary.Reference.DidNotReceive().Close(Arg.Any<DialogResult>());
        component.Release.SetResult();
        await retirement;
        component.DisposeCount.Should().Be(1);
    }

    [Fact]
    public Task DisposeAsync_WhenNativeCloseCompletesInlineThenThrows_ShouldRetainFailureAndCloseOtherDialogs() => Task.Run(async () =>
    {
        var lifetime = new RetrievalWorkbenchLifetime();
        var first = new DialogBoundary();
        var second = new DialogBoundary();
        var failure = new InvalidOperationException("Native close failed after publishing its result.");
        first.Reference.When(reference => reference.Close(Arg.Any<DialogResult>())).Do(call =>
        {
            first.Result.TrySetResult(call.Arg<DialogResult>());
            throw failure;
        });
        lifetime.OwnDialog(first.Reference).Should().BeTrue();
        lifetime.OwnDialog(second.Reference).Should().BeTrue();

        var retirement = lifetime.DisposeAsync().AsTask();

        var retirementFailure = await Assert.ThrowsAsync<AggregateException>(() => retirement);
        retirementFailure.Flatten().InnerExceptions.Should().ContainSingle().Which.Should().BeSameAs(failure);
        (await second.Result.Task)!.Canceled.Should().BeTrue();
        first.Reference.Received(1).Dismiss(Arg.Is<DialogResult>(result => result.Canceled));
        second.Reference.Received(1).Close(Arg.Is<DialogResult>(result => result.Canceled));
        (await Assert.ThrowsAsync<AggregateException>(() => lifetime.DisposeAsync().AsTask())).Should().BeSameAs(retirementFailure);
    }, TestContext.Current.CancellationToken);

    [Fact]
    public async Task DisposeAsync_WhenCancellationCallbackFails_ShouldStillCloseDialogAndDrainProducer()
    {
        var lifetime = new RetrievalWorkbenchLifetime();
        var boundary = new DialogBoundary();
        lifetime.OwnDialog(boundary.Reference).Should().BeTrue();
        var release = Completion();
        var cancelled = Completion();
        var failure = new InvalidOperationException("Cancellation subscriber failed.");
        using var cancellation = lifetime.Token.Register(() => { cancelled.SetResult(); throw failure; });
        var producer = lifetime.RunAsync(() => release.Task);

        var retirement = lifetime.DisposeAsync().AsTask();
        await cancelled.Task;

        (await boundary.Result.Task)!.Canceled.Should().BeTrue();
        retirement.IsCompleted.Should().BeFalse();
        release.SetResult();
        await producer;
        var retirementFailure = await Assert.ThrowsAsync<AggregateException>(() => retirement);
        retirementFailure.Flatten().InnerExceptions.Should().ContainSingle().Which.Should().BeSameAs(failure);
    }

    [Fact]
    public async Task DisposeAsync_WhenNativeCloseCallsRegistryFromAnotherThread_ShouldAllowCallbackAndShareRetirement()
    {
        var lifetime = new RetrievalWorkbenchLifetime();
        var boundary = new DialogBoundary();
        Task? reenteredRetirement = null;
        boundary.Reference.When(reference => reference.Close(Arg.Any<DialogResult>())).Do(call =>
        {
            // Native callbacks may dispatch work that re-enters this owner from another thread.
            Task.Run(() => lifetime.ReleaseDialog(boundary.Reference))
                .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).GetAwaiter().GetResult();
            reenteredRetirement = lifetime.DisposeAsync().AsTask();
            boundary.Result.TrySetResult(call.Arg<DialogResult>());
        });
        lifetime.OwnDialog(boundary.Reference).Should().BeTrue();

        var retirement = lifetime.DisposeAsync().AsTask();
        await retirement;

        reenteredRetirement.Should().BeSameAs(retirement);
        boundary.Reference.Received(1).Close(Arg.Any<DialogResult>());
    }

    private static TaskCompletionSource Completion() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class DialogBoundary
    {
        // Match MudBlazor's inline native result continuations; no renderer masks re-entry.
        internal TaskCompletionSource<DialogResult?> Result { get; } = new();
        internal IDialogReference Reference { get; } = Substitute.For<IDialogReference>();

        internal DialogBoundary(object? component = null)
        {
            Reference.Result.Returns(Result.Task);
            Reference.Dialog.Returns(component);
            Reference.When(reference => reference.Close(Arg.Any<DialogResult>()))
                .Do(call => Result.TrySetResult(call.Arg<DialogResult>()));
            Reference.Dismiss(Arg.Any<DialogResult>()).Returns(call => Result.TrySetResult(call.Arg<DialogResult>()));
        }
    }

    private sealed class OwnedComponent : IAsyncDisposable
    {
        internal TaskCompletionSource Started { get; } = Completion();
        internal TaskCompletionSource Release { get; } = Completion();
        internal int DisposeCount { get; private set; }
        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            Started.TrySetResult();
            return new(Release.Task);
        }
    }
}
