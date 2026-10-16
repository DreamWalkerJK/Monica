using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Monica.AI.UI.Shared.Components;
using Monica.AI.UI.Shared.Support;
using MudBlazor;
using NSubstitute;

namespace Test.Monica.AI.UI.Shared.Support;

public sealed class AIAdministrationDialogSessionTests
{
    [Fact]
    public async Task DisposeAsync_WhenDialogProducerReturnsLate_ShouldCloseAndDrainOwnedReference()
    {
        var opened = new TaskCompletionSource<IDialogReference>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dialogs = CreateDialogService(opened.Task);
        var owned = new DialogBoundary();
        var dispatchCount = 0;
        var session = new AIAdministrationDialogSession(dialogs, action =>
        {
            dispatchCount++;
            action();
            return Task.CompletedTask;
        });
        var show = session.ShowAsync<AIAdministrationConfirmation>("Owned confirmation", new());

        var disposal = session.DisposeAsync().AsTask();

        disposal.IsCompleted.Should().BeFalse();
        dispatchCount.Should().Be(0);
        opened.SetResult(owned.Reference);
        var result = await show;
        await disposal;
        await session.DisposeAsync();

        result.Should().NotBeNull();
        result!.Canceled.Should().BeTrue();
        dispatchCount.Should().Be(1);
        owned.Reference.Received(1).Close(Arg.Is<DialogResult>(value => value.Canceled));
        (await session.ShowAsync<AIAdministrationConfirmation>("Retired confirmation", new())).Should().BeNull();
        await dialogs.Received(1).ShowAsync<AIAdministrationConfirmation>(
            Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>());
    }

    [Fact]
    public async Task DisposeAsync_WhenRendererDispatchIsPending_ShouldDrainCloseBeforeReturning()
    {
        var boundary = new DialogBoundary();
        var dialogs = CreateDialogService(Task.FromResult(boundary.Reference));
        var dispatchStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatchRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = new AIAdministrationDialogSession(dialogs, async action =>
        {
            dispatchStarted.SetResult();
            await dispatchRelease.Task;
            action();
        });
        var show = session.ShowAsync<AIAdministrationConfirmation>("Pending confirmation", new());

        var disposal = session.DisposeAsync().AsTask();
        await dispatchStarted.Task;

        disposal.IsCompleted.Should().BeFalse();
        show.IsCompleted.Should().BeFalse();
        boundary.Reference.DidNotReceive().Close(Arg.Any<DialogResult>());
        dispatchRelease.SetResult();
        (await show)!.Canceled.Should().BeTrue();
        await disposal;
        boundary.Reference.Received(1).Close(Arg.Any<DialogResult>());
    }

    [Fact]
    public async Task DisposeAsync_WhenRendererDispatchFaultsAsynchronously_ShouldDismissAndReportFailure()
    {
        var boundary = new DialogBoundary();
        var dialogs = CreateDialogService(Task.FromResult(boundary.Reference));
        var dispatchStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatchCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new InvalidOperationException("Renderer is unavailable.");
        var session = new AIAdministrationDialogSession(dialogs, _ =>
        {
            dispatchStarted.SetResult();
            return dispatchCompletion.Task;
        });
        var show = session.ShowAsync<AIAdministrationConfirmation>("Pending confirmation", new());

        var disposal = session.DisposeAsync().AsTask();
        await dispatchStarted.Task;
        disposal.IsCompleted.Should().BeFalse();
        dispatchCompletion.SetException(failure);

        (await Assert.ThrowsAsync<InvalidOperationException>(() => show)).Should().BeSameAs(failure);
        var retirementFailure = await Assert.ThrowsAsync<AggregateException>(() => disposal);
        retirementFailure.Flatten().InnerExceptions.Should().ContainSingle().Which.Should().BeSameAs(failure);
        boundary.Result.Task.IsCompletedSuccessfully.Should().BeTrue();
        boundary.Reference.Received(1).Dismiss(Arg.Is<DialogResult>(value => value.Canceled));
        boundary.Reference.DidNotReceive().Close(Arg.Any<DialogResult>());
    }

    [Fact]
    public Task DisposeAsync_WhenNativeResultResumesInline_ShouldDrainPendingDispatch() => Task.Run(async () =>
    {
        // DialogReference uses an inline-continuation result source; a renderer context must not mask it.
        var boundary = new DialogBoundary();
        var dialogs = CreateDialogService(Task.FromResult(boundary.Reference));
        var dispatchStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatchRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = new AIAdministrationDialogSession(dialogs, action =>
        {
            action();
            dispatchStarted.SetResult();
            return dispatchRelease.Task;
        });
        var show = session.ShowAsync<AIAdministrationConfirmation>("Inline confirmation", new());

        var disposal = session.DisposeAsync().AsTask();
        await dispatchStarted.Task;

        boundary.Result.Task.IsCompletedSuccessfully.Should().BeTrue();
        show.IsCompleted.Should().BeFalse();
        disposal.IsCompleted.Should().BeFalse();
        dispatchRelease.SetResult();
        (await show)!.Canceled.Should().BeTrue();
        await disposal;
        boundary.Reference.Received(1).Close(Arg.Any<DialogResult>());
    }, TestContext.Current.CancellationToken);

    [Fact]
    public Task DisposeAsync_WhenDispatchThrowsAndDismissResumesInline_ShouldReportReservedCloseFailure() => Task.Run(async () =>
    {
        var boundary = new DialogBoundary();
        var dialogs = CreateDialogService(Task.FromResult(boundary.Reference));
        var failure = new InvalidOperationException("Renderer dispatch failed synchronously.");
        var session = new AIAdministrationDialogSession(dialogs, _ => throw failure);
        var show = session.ShowAsync<AIAdministrationConfirmation>("Inline confirmation", new());

        var disposal = session.DisposeAsync().AsTask();

        (await Assert.ThrowsAsync<InvalidOperationException>(() => show)).Should().BeSameAs(failure);
        var retirementFailure = await Assert.ThrowsAsync<AggregateException>(() => disposal);
        retirementFailure.Flatten().InnerExceptions.Should().ContainSingle().Which.Should().BeSameAs(failure);
        boundary.Result.Task.IsCompletedSuccessfully.Should().BeTrue();
        boundary.Reference.Received(1).Dismiss(Arg.Is<DialogResult>(value => value.Canceled));
    }, TestContext.Current.CancellationToken);

    [Fact]
    public Task DisposeAsync_WhenDispatchClosesInlineThenFaults_ShouldRetainCloseFailure() => Task.Run(async () =>
    {
        var boundary = new DialogBoundary();
        var dialogs = CreateDialogService(Task.FromResult(boundary.Reference));
        var failure = new InvalidOperationException("Renderer failed after closing the dialog.");
        var session = new AIAdministrationDialogSession(dialogs, action =>
        {
            action();
            throw failure;
        });
        var show = session.ShowAsync<AIAdministrationConfirmation>("Inline confirmation", new());

        var disposal = session.DisposeAsync().AsTask();

        (await Assert.ThrowsAsync<InvalidOperationException>(() => show)).Should().BeSameAs(failure);
        var retirementFailure = await Assert.ThrowsAsync<AggregateException>(() => disposal);
        retirementFailure.Flatten().InnerExceptions.Should().ContainSingle().Which.Should().BeSameAs(failure);
        boundary.Reference.Received(1).Close(Arg.Any<DialogResult>());
        boundary.Reference.Received(1).Dismiss(Arg.Any<DialogResult>());
    }, TestContext.Current.CancellationToken);

    [Fact]
    public async Task ShowAsync_WhenNativeResultPrecedesInstanceDisposal_ShouldDrainInstanceBeforeReleasingResult()
    {
        var boundary = new DialogBoundary();
        var instance = new DialogInstance();
        boundary.Reference.Dialog.Returns(instance);
        var dialogs = CreateDialogService(Task.FromResult(boundary.Reference));
        var session = new AIAdministrationDialogSession(dialogs, action =>
        {
            action();
            return Task.CompletedTask;
        });
        var show = session.ShowAsync<AIAdministrationConfirmation>("Owned instance", new());

        boundary.Result.SetResult(DialogResult.Ok(true));
        await instance.DisposalStarted.Task;
        var disposal = session.DisposeAsync().AsTask();

        show.IsCompleted.Should().BeFalse();
        disposal.IsCompleted.Should().BeFalse();
        instance.DisposeCount.Should().Be(1);
        instance.DisposalCompletion.SetResult();
        (await show)!.Data.Should().Be(true);
        await disposal;
        await session.DisposeAsync();
        instance.DisposeCount.Should().Be(1);
    }

    [Fact]
    public async Task DisposeAsync_WhenDialogInstanceDisposalFails_ShouldDrainAndReportInstanceFailure()
    {
        var boundary = new DialogBoundary();
        var instance = new DialogInstance();
        boundary.Reference.Dialog.Returns(instance);
        var dialogs = CreateDialogService(Task.FromResult(boundary.Reference));
        var failure = new InvalidOperationException("Dialog instance disposal failed.");
        var session = new AIAdministrationDialogSession(dialogs, action =>
        {
            action();
            return Task.CompletedTask;
        });
        var show = session.ShowAsync<AIAdministrationConfirmation>("Owned instance", new());

        var disposal = session.DisposeAsync().AsTask();
        await instance.DisposalStarted.Task;
        disposal.IsCompleted.Should().BeFalse();
        instance.DisposalCompletion.SetException(failure);

        (await Assert.ThrowsAsync<InvalidOperationException>(() => show)).Should().BeSameAs(failure);
        var retirementFailure = await Assert.ThrowsAsync<AggregateException>(() => disposal);
        retirementFailure.Flatten().InnerExceptions.Should().ContainSingle().Which.Should().BeSameAs(failure);
        instance.DisposeCount.Should().Be(1);
    }

    [Fact]
    public async Task ShowAsync_WhenOneDialogIsPending_ShouldRejectReentryAndAllowNextAfterResult()
    {
        var boundary = new DialogBoundary();
        var dialogs = CreateDialogService(Task.FromResult(boundary.Reference));
        await using var session = new AIAdministrationDialogSession(dialogs, action =>
        {
            action();
            return Task.CompletedTask;
        });
        var show = session.ShowAsync<AIAdministrationConfirmation>("First confirmation", new());

        (await session.ShowAsync<AIAdministrationConfirmation>("Reentered confirmation", new())).Should().BeNull();
        boundary.Result.SetResult(DialogResult.Ok(true));
        (await show)!.Data.Should().Be(true);

        (await session.ShowAsync<AIAdministrationConfirmation>("Next confirmation", new()))!.Data.Should().Be(true);
        await dialogs.Received(2).ShowAsync<AIAdministrationConfirmation>(
            Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>());
    }

    [Theory]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    [InlineData(false, null, false)]
    [InlineData(false, true, true)]
    public async Task ConfirmAsync_WhenResultArrives_ShouldRequireUncanceledExplicitTrueResult(
        bool canceled, bool? confirmed, bool expected)
    {
        var boundary = new DialogBoundary();
        var dialogs = CreateDialogService(Task.FromResult(boundary.Reference));
        await using var session = new AIAdministrationDialogSession(dialogs, action =>
        {
            action();
            return Task.CompletedTask;
        });
        boundary.Result.SetResult(canceled ? DialogResult.Cancel() : DialogResult.Ok(confirmed));

        (await session.ConfirmAsync("Remove provider", "Remove saved provider settings.", "Remove")).Should().Be(expected);
    }

    private static IDialogService CreateDialogService(Task<IDialogReference> opened)
    {
        var dialogs = Substitute.For<IDialogService>();
        dialogs.ShowAsync<AIAdministrationConfirmation>(
            Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>()).Returns(opened);
        return dialogs;
    }

    private sealed class DialogBoundary
    {
        internal IDialogReference Reference { get; } = Substitute.For<IDialogReference>();
        // Match MudBlazor's native DialogReference completion semantics, including synchronous continuations.
        internal TaskCompletionSource<DialogResult?> Result { get; } = new();

        internal DialogBoundary()
        {
            Reference.Result.Returns(Result.Task);
            Reference.When(reference => reference.Close(Arg.Any<DialogResult>()))
                .Do(call => Result.TrySetResult(call.Arg<DialogResult>()));
            Reference.Dismiss(Arg.Any<DialogResult>()).Returns(call => Result.TrySetResult(call.Arg<DialogResult>()));
        }
    }

    private sealed class DialogInstance : IComponent, IAsyncDisposable
    {
        internal TaskCompletionSource DisposalStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource DisposalCompletion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal int DisposeCount { get; private set; }

        public void Attach(RenderHandle renderHandle) => throw new NotSupportedException("This boundary is never rendered.");
        public Task SetParametersAsync(ParameterView parameters) => throw new NotSupportedException("This boundary is never rendered.");

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            DisposalStarted.TrySetResult();
            return new(DisposalCompletion.Task);
        }
    }
}
