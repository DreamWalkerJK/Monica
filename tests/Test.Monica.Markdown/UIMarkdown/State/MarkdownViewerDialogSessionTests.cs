using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Monica.Markdown.UIMarkdown.State;
using MudBlazor;
using NSubstitute;

namespace Test.Monica.Markdown.UIMarkdown.State;

public sealed class MarkdownViewerDialogSessionTests
{
    [Fact]
    public async Task DisposeAsync_WhenAcquisitionFinishesLate_ShouldCloseAndDrainRendererAndOwnedComponent()
    {
        var dialogs = Substitute.For<IDialogService>();
        var acquisitionStarted = Signal();
        var acquired = new TaskCompletionSource<IDialogReference>(TaskCreationOptions.RunContinuationsAsynchronously);
        dialogs.ShowAsync<TestDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(_ => { acquisitionStarted.TrySetResult(); return acquired.Task; });
        var result = new TaskCompletionSource<DialogResult?>();
        var reference = Substitute.For<IDialogReference>();
        reference.Result.Returns(result.Task);
        var component = new ControlledDialogOwner();
        reference.Dialog.Returns(component);
        reference.When(dialog => dialog.Close(Arg.Any<DialogResult>()))
            .Do(call => result.TrySetResult(call.Arg<DialogResult>()));
        var dispatchStarted = Signal();
        var dispatchCompletion = Signal();
        var closeCount = 0;
        var session = new MarkdownViewerDialogSession(dialogs, async action =>
        {
            dispatchStarted.TrySetResult();
            await dispatchCompletion.Task;
            Interlocked.Increment(ref closeCount);
            action();
        });

        var show = session.ShowAsync<TestDialog>("Search", new(), new());
        await acquisitionStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        var firstDisposal = session.DisposeAsync().AsTask();
        var secondDisposal = session.DisposeAsync().AsTask();
        acquired.TrySetResult(reference);
        await dispatchStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        firstDisposal.IsCompleted.Should().BeFalse();
        secondDisposal.IsCompleted.Should().BeFalse();
        show.IsCompleted.Should().BeFalse();
        dispatchCompletion.TrySetResult();
        await component.Started.Task.WaitAsync(TestContext.Current.CancellationToken);
        firstDisposal.IsCompleted.Should().BeFalse();
        component.Completion.TrySetResult();
        await Task.WhenAll(show, firstDisposal, secondDisposal);

        (await show).Should().NotBeNull();
        (await show)!.Canceled.Should().BeTrue();
        closeCount.Should().Be(1);
        component.DisposalCount.Should().Be(1);
        await session.ShowAsync<TestDialog>("Late", new(), new());
        _ = dialogs.Received(1).ShowAsync<TestDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>());
    }

    [Fact]
    public async Task ShowAsync_WhenNativeResultArrivesBeforeComponentRetirement_ShouldDrainTokenlessWork()
    {
        var dialogs = Substitute.For<IDialogService>();
        var result = new TaskCompletionSource<DialogResult?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reference = Substitute.For<IDialogReference>();
        reference.Result.Returns(result.Task);
        var component = new ControlledDialogOwner();
        reference.Dialog.Returns(component);
        dialogs.ShowAsync<TestDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult(reference));
        var session = new MarkdownViewerDialogSession(dialogs, action => { action(); return Task.CompletedTask; });

        var show = session.ShowAsync<TestDialog>("Groups", new(), new());
        result.TrySetResult(DialogResult.Ok("docs"));
        await component.Started.Task.WaitAsync(TestContext.Current.CancellationToken);
        show.IsCompleted.Should().BeFalse();
        var retirement = session.DisposeAsync().AsTask();
        retirement.IsCompleted.Should().BeFalse();
        component.Completion.TrySetResult();
        await Task.WhenAll(show, retirement);
        (await show)!.Data.Should().Be("docs");
    }

    [Fact]
    public async Task DisposeAsync_WhenRendererFails_ShouldDismissResultAndPropagateSameRetirementFailure()
    {
        var dialogs = Substitute.For<IDialogService>();
        var acquisition = new TaskCompletionSource<IDialogReference>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = Signal();
        dialogs.ShowAsync<TestDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(_ => { started.TrySetResult(); return acquisition.Task; });
        var reference = Substitute.For<IDialogReference>();
        var result = new TaskCompletionSource<DialogResult?>(TaskCreationOptions.RunContinuationsAsynchronously);
        reference.Result.Returns(result.Task);
        reference.Dismiss(Arg.Any<DialogResult>()).Returns(call => result.TrySetResult(call.Arg<DialogResult>()));
        var session = new MarkdownViewerDialogSession(dialogs,
            _ => Task.FromException(new InvalidOperationException("Renderer retired.")));
        var show = session.ShowAsync<TestDialog>("Search", new(), new());
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);
        var first = session.DisposeAsync().AsTask();
        var second = session.DisposeAsync().AsTask();
        acquisition.TrySetResult(reference);

        await ((Func<Task>)(() => show)).Should().ThrowAsync<InvalidOperationException>();
        await ((Func<Task>)(() => first)).Should().ThrowAsync<AggregateException>();
        await ((Func<Task>)(() => second)).Should().ThrowAsync<AggregateException>();
        result.Task.IsCompletedSuccessfully.Should().BeTrue();
    }

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private sealed class TestDialog : ComponentBase { }

    private sealed class ControlledDialogOwner : IAsyncDisposable
    {
        internal TaskCompletionSource Started { get; } = Signal();
        internal TaskCompletionSource Completion { get; } = Signal();
        internal int DisposalCount { get; private set; }
        public ValueTask DisposeAsync()
        {
            DisposalCount++;
            Started.TrySetResult();
            return new(Completion.Task);
        }
    }
}
