using Monica.JobScheduler.Models;
using Monica.JobScheduler.UI.UIJobScheduler.Support;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Test.Monica.JobScheduler.UI.Support;

public sealed class SchedulerDialogSessionTests
{
    private static readonly TimeSpan ObservationTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task OpenAsync_WhenNativeResultCompletes_ShouldPassExactFrozenRequestAndActualResultOnce()
    {
        var request = new PolicyRequest(new JobId(" 运营/华东?revision=%25 ", " 作业/#导出?x=1 "), "revision-1");
        var native = new DialogBoundary();
        PolicyRequest? produced = null;
        PolicyRequest? accepted = null;
        DialogResult? acceptedResult = null;
        var callbacks = 0;
        var session = new SchedulerDialogSession<PolicyRequest>(target =>
        {
            produced = target;
            return Task.FromResult(native.Reference);
        }, (target, result) =>
        {
            accepted = target;
            acceptedResult = result;
            callbacks++;
            return Task.CompletedTask;
        });
        try
        {
            var operation = session.OpenAsync(request);
            await ObserveAsync(session.OpenAsync(request with { Revision = "revision-2" }));
            var result = DialogResult.Ok("persisted-policy");
            native.Result.TrySetResult(result);
            await ObserveAsync(operation);
            Assert.Same(request, produced);
            Assert.Same(request, accepted);
            Assert.Same(result, acceptedResult);
            Assert.Equal(1, callbacks);
            Assert.Equal(0, native.CloseCalls);
        }
        finally
        {
            native.Result.TrySetResult(DialogResult.Cancel());
            await ObserveAsync(session.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenProducerCompletesLate_ShouldCloseAndDrainItsActualResultWithoutCallback()
    {
        var produced = Signal<IDialogReference>();
        var native = new DialogBoundary();
        var callbacks = 0;
        var session = new SchedulerDialogSession<int>(_ => produced.Task, (_, _) =>
        {
            callbacks++;
            return Task.CompletedTask;
        });
        var operation = session.OpenAsync(42);
        var retirement = session.DisposeAsync().AsTask();
        try
        {
            Assert.Same(retirement, session.DisposeAsync().AsTask());
            produced.TrySetResult(native.Reference);
            await ObserveAsync(native.Closed.Task);
            Assert.Equal(1, native.CloseCalls);
            Assert.False(retirement.IsCompleted);
            native.Result.TrySetResult(DialogResult.Cancel());
            await ObserveAsync(Task.WhenAll(operation, retirement));
            Assert.Equal(0, callbacks);
            await ObserveAsync(session.OpenAsync(99));
            Assert.Equal(1, native.CloseCalls);
        }
        finally
        {
            produced.TrySetResult(native.Reference);
            native.Result.TrySetResult(DialogResult.Cancel());
            await ObserveAsync(session.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenCompletionCallbackIsAlreadyRunning_ShouldDrainItAndRejectDuplicateAdmission()
    {
        var native = new DialogBoundary();
        var entered = Signal<bool>();
        var release = Signal<bool>();
        var producers = 0;
        var session = new SchedulerDialogSession<string>(_ =>
        {
            producers++;
            return Task.FromResult(native.Reference);
        }, async (_, _) =>
        {
            entered.TrySetResult(true);
            await ObserveAsync(release.Task);
        });
        try
        {
            var operation = session.OpenAsync("exact target");
            native.Result.TrySetResult(DialogResult.Cancel());
            await ObserveAsync(entered.Task);
            var retirement = session.DisposeAsync().AsTask();
            Assert.False(retirement.IsCompleted);
            await ObserveAsync(session.OpenAsync("another target"));
            Assert.Equal(1, producers);
            release.TrySetResult(true);
            await ObserveAsync(Task.WhenAll(operation, retirement));
        }
        finally
        {
            native.Result.TrySetResult(DialogResult.Cancel());
            release.TrySetResult(true);
            await ObserveAsync(session.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenLateCloseThrows_ShouldStillDrainActualResultAndPreserveBothFailures()
    {
        var produced = Signal<IDialogReference>();
        var native = new DialogBoundary();
        var closeFailure = new IOException("Late native Close failed.");
        var resultFailure = new InvalidOperationException("Actual native Result failed.");
        native.Reference.When(reference => reference.Close(Arg.Any<DialogResult?>())).Do(_ =>
        {
            native.Closed.TrySetResult(true);
            throw closeFailure;
        });
        var callbacks = 0;
        var session = new SchedulerDialogSession<int>(_ => produced.Task, (_, _) =>
        {
            callbacks++;
            return Task.CompletedTask;
        });
        var operation = session.OpenAsync(42);
        var retirement = session.DisposeAsync().AsTask();
        try
        {
            produced.TrySetResult(native.Reference);
            await ObserveAsync(native.Closed.Task);
            Assert.False(operation.IsCompleted);
            Assert.False(retirement.IsCompleted);
            native.Result.TrySetException(resultFailure);
            var failure = await Assert.ThrowsAsync<AggregateException>(() => ObserveAsync(operation));
            Assert.Contains(closeFailure, failure.Flatten().InnerExceptions);
            Assert.Contains(resultFailure, failure.Flatten().InnerExceptions);
            var retirementFailure = await Assert.ThrowsAsync<AggregateException>(() => ObserveAsync(retirement));
            Assert.Contains(closeFailure, retirementFailure.Flatten().InnerExceptions);
            Assert.Contains(resultFailure, retirementFailure.Flatten().InnerExceptions);
            Assert.Same(retirement, session.DisposeAsync().AsTask());
            Assert.Equal(0, callbacks);
        }
        finally
        {
            produced.TrySetResult(native.Reference);
            native.Result.TrySetException(resultFailure);
            await Assert.ThrowsAsync<AggregateException>(() => ObserveAsync(operation));
            await Assert.ThrowsAsync<AggregateException>(() => ObserveAsync(session.DisposeAsync().AsTask()));
        }
    }

    private sealed record PolicyRequest(JobId JobId, string Revision);
    private static Task ObserveAsync(Task task) => task.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);
    private static TaskCompletionSource<T> Signal<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class DialogBoundary
    {
        internal DialogBoundary()
        {
            Reference.Result.Returns(Result.Task);
            Reference.When(reference => reference.Close(Arg.Any<DialogResult?>())).Do(_ =>
            {
                CloseCalls++;
                Closed.TrySetResult(true);
            });
        }
        internal IDialogReference Reference { get; } = Substitute.For<IDialogReference>();
        internal TaskCompletionSource<DialogResult?> Result { get; } = Signal<DialogResult?>();
        internal TaskCompletionSource<bool> Closed { get; } = Signal<bool>();
        internal int CloseCalls { get; private set; }
    }
}
