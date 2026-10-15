using Monica.JobScheduler.UI.UIJobScheduler.Executions.State;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Test.Monica.JobScheduler.UI.UIJobScheduler.Executions.State;

public sealed class ExecutionDialogSessionTests
{
    private static readonly TimeSpan ObservationTimeout = TimeSpan.FromSeconds(10);
    private const string InstanceId = "owner:上海/job?revision=2026#execution%25";

    [Fact]
    public async Task OpenAsync_WhenNativeResultCompletes_ShouldRefreshOnceWithExactIdentity()
    {
        var dialog = new DialogBoundary();
        string? openedIdentity = null;
        var refreshCalls = 0;
        var session = new ExecutionDialogSession(identity =>
        {
            openedIdentity = identity;
            return Task.FromResult(dialog.Reference);
        }, () =>
        {
            refreshCalls++;
            return Task.CompletedTask;
        });

        try
        {
            var operation = session.OpenAsync(InstanceId);
            Assert.Equal(InstanceId, openedIdentity);
            Assert.False(operation.IsCompleted);
            Assert.Equal(0, refreshCalls);

            dialog.Result.TrySetResult(DialogResult.Ok(true));
            await ObserveAsync(operation);

            Assert.Equal(1, refreshCalls);
            Assert.Equal(0, dialog.CloseCalls);
        }
        finally
        {
            dialog.Result.TrySetResult(DialogResult.Cancel());
            await ObserveAsync(session.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenProducerIsPending_ShouldRejectDuplicatesAndDrainLateNativeResult()
    {
        var produced = NewCompletion<IDialogReference>();
        var closeRequested = NewSignal();
        var dialog = new DialogBoundary(() => closeRequested.TrySetResult());
        var openCalls = 0;
        var refreshCalls = 0;
        string? openedIdentity = null;
        var session = new ExecutionDialogSession(identity =>
        {
            openCalls++;
            openedIdentity = identity;
            return produced.Task;
        }, () =>
        {
            refreshCalls++;
            return Task.CompletedTask;
        });

        try
        {
            var operation = session.OpenAsync(InstanceId);
            await ObserveAsync(session.OpenAsync("other/execution"));
            Assert.Equal(1, openCalls);
            Assert.Equal(InstanceId, openedIdentity);

            var retirement = session.DisposeAsync().AsTask();
            Assert.Same(retirement, session.DisposeAsync().AsTask());
            Assert.False(retirement.IsCompleted);
            await ObserveAsync(session.OpenAsync("retired/execution"));
            Assert.Equal(1, openCalls);

            produced.TrySetResult(dialog.Reference);
            await ObserveAsync(closeRequested.Task);
            Assert.Equal(1, dialog.CloseCalls);
            Assert.True(dialog.CloseResult!.Canceled);
            Assert.False(operation.IsCompleted);
            Assert.False(retirement.IsCompleted);

            dialog.Result.TrySetResult(DialogResult.Cancel());
            await ObserveAsync(Task.WhenAll(operation, retirement));
            Assert.Equal(0, refreshCalls);
        }
        finally
        {
            produced.TrySetResult(dialog.Reference);
            dialog.Result.TrySetResult(DialogResult.Cancel());
            await ObserveAsync(session.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenAnotherSessionIsActive_ShouldLeaveItsNativeOwnershipIndependent()
    {
        var firstDialog = new DialogBoundary();
        var secondDialog = new DialogBoundary();
        var firstRefreshes = 0;
        var secondRefreshes = 0;
        var first = new ExecutionDialogSession(_ => Task.FromResult(firstDialog.Reference), () =>
        {
            firstRefreshes++;
            return Task.CompletedTask;
        });
        var second = new ExecutionDialogSession(_ => Task.FromResult(secondDialog.Reference), () =>
        {
            secondRefreshes++;
            return Task.CompletedTask;
        });

        try
        {
            var firstOperation = first.OpenAsync("first/execution");
            var secondOperation = second.OpenAsync("second/execution");
            var firstRetirement = first.DisposeAsync().AsTask();
            Assert.Equal(1, firstDialog.CloseCalls);
            Assert.Equal(0, secondDialog.CloseCalls);

            firstDialog.Result.TrySetResult(DialogResult.Cancel());
            await ObserveAsync(Task.WhenAll(firstOperation, firstRetirement));
            Assert.False(secondOperation.IsCompleted);
            Assert.Equal(0, firstRefreshes);

            secondDialog.Result.TrySetResult(DialogResult.Ok(true));
            await ObserveAsync(secondOperation);
            Assert.Equal(1, secondRefreshes);
            Assert.Equal(0, secondDialog.CloseCalls);
        }
        finally
        {
            firstDialog.Result.TrySetResult(DialogResult.Cancel());
            secondDialog.Result.TrySetResult(DialogResult.Cancel());
            await ObserveAsync(Task.WhenAll(first.DisposeAsync().AsTask(), second.DisposeAsync().AsTask()));
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenCloseReentersRetirement_ShouldReuseTheReservedCompletion()
    {
        ExecutionDialogSession? session = null;
        Task? reenteredRetirement = null;
        var refreshCalls = 0;
        var dialog = new DialogBoundary(() => reenteredRetirement = session!.DisposeAsync().AsTask());
        session = new ExecutionDialogSession(_ => Task.FromResult(dialog.Reference), () =>
        {
            refreshCalls++;
            return Task.CompletedTask;
        });

        try
        {
            var operation = session.OpenAsync(InstanceId);
            var retirement = session.DisposeAsync().AsTask();
            Assert.Same(retirement, reenteredRetirement);
            Assert.Same(retirement, session.DisposeAsync().AsTask());
            Assert.Equal(1, dialog.CloseCalls);
            Assert.False(retirement.IsCompleted);

            dialog.Result.TrySetResult(DialogResult.Cancel());
            await ObserveAsync(Task.WhenAll(operation, retirement));
            Assert.Equal(0, refreshCalls);
        }
        finally
        {
            dialog.Result.TrySetResult(DialogResult.Cancel());
            await ObserveAsync(session.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenCloseAndResultFail_ShouldDrainActualResultAndPreserveBothFailures()
    {
        var closeFailure = new InvalidOperationException("The native close request failed.");
        var resultFailure = new IOException("The native result failed later.");
        var dialog = new DialogBoundary(() => throw closeFailure);
        var refreshCalls = 0;
        var session = new ExecutionDialogSession(_ => Task.FromResult(dialog.Reference), () =>
        {
            refreshCalls++;
            return Task.CompletedTask;
        });
        var operation = session.OpenAsync(InstanceId);

        try
        {
            var retirement = session.DisposeAsync().AsTask();
            Assert.Equal(1, dialog.CloseCalls);
            Assert.False(operation.IsCompleted);
            Assert.False(retirement.IsCompleted);

            dialog.Result.TrySetException(resultFailure);
            var actualResultFailure = await Assert.ThrowsAsync<IOException>(() => ObserveAsync(operation));
            Assert.Same(resultFailure, actualResultFailure);
            var failure = await Assert.ThrowsAsync<AggregateException>(() => ObserveAsync(retirement));
            var flattened = failure.Flatten().InnerExceptions;
            Assert.Contains(closeFailure, flattened);
            Assert.Contains(resultFailure, flattened);
            Assert.Equal(2, flattened.Count);
            Assert.Equal(0, refreshCalls);
            Assert.Same(retirement, session.DisposeAsync().AsTask());
        }
        finally
        {
            dialog.Result.TrySetException(resultFailure);
            await Assert.ThrowsAsync<IOException>(() => ObserveAsync(operation));
            await Assert.ThrowsAsync<AggregateException>(() => ObserveAsync(session.DisposeAsync().AsTask()));
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenRefreshWasAlreadyAdmitted_ShouldDrainItAndRejectNewInspectors()
    {
        var refreshEntered = NewSignal();
        var releaseRefresh = NewSignal();
        var dialog = new DialogBoundary();
        var openCalls = 0;
        var refreshCalls = 0;
        var session = new ExecutionDialogSession(_ =>
        {
            openCalls++;
            return Task.FromResult(dialog.Reference);
        }, async () =>
        {
            refreshCalls++;
            refreshEntered.TrySetResult();
            await ObserveAsync(releaseRefresh.Task);
        });

        try
        {
            var operation = session.OpenAsync(InstanceId);
            dialog.Result.TrySetResult(DialogResult.Ok(true));
            await ObserveAsync(refreshEntered.Task);

            var retirement = session.DisposeAsync().AsTask();
            Assert.False(operation.IsCompleted);
            Assert.False(retirement.IsCompleted);
            await ObserveAsync(session.OpenAsync("retired/execution"));
            Assert.Equal(1, openCalls);

            releaseRefresh.TrySetResult();
            await ObserveAsync(Task.WhenAll(operation, retirement));
            Assert.Equal(1, refreshCalls);
        }
        finally
        {
            dialog.Result.TrySetResult(DialogResult.Cancel());
            releaseRefresh.TrySetResult();
            await ObserveAsync(session.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task OpenAsync_WhenExternalCallbacksCoordinateWithAnotherThread_ShouldNotHoldAdmissionGate()
    {
        ExecutionDialogSession? session = null;
        var readers = new List<Task>();
        var observations = new List<Task>();
        var observerSignals = new List<ManualResetEventSlim>();
        var callbacks = 0;
        var openCalls = 0;
        var refreshCalls = 0;

        void CoordinateWithAdmission()
        {
            var observed = NewSignal();
            var observerSignal = new ManualResetEventSlim();
            observations.Add(observed.Task);
            observerSignals.Add(observerSignal);
            var reader = Task.Run(async () =>
            {
                try
                {
                    await ObserveAsync(session!.OpenAsync("reentrant/execution"));
                    observed.TrySetResult();
                }
                catch (Exception exception)
                {
                    observed.TrySetException(exception);
                }
                finally
                {
                    observerSignal.Set();
                }
            }, TestContext.Current.CancellationToken);
            readers.Add(reader);
            // The synchronous callback cannot wait for another admission while holding the owner's gate.
            if (!observerSignal.Wait(ObservationTimeout, TestContext.Current.CancellationToken))
                throw new TimeoutException("The external callback could not coordinate with another admission.");
            callbacks++;
        }

        var firstDialog = new DialogBoundary();
        var secondDialog = new DialogBoundary(CoordinateWithAdmission);
        session = new ExecutionDialogSession(_ =>
        {
            CoordinateWithAdmission();
            openCalls++;
            return Task.FromResult(openCalls == 1 ? firstDialog.Reference : secondDialog.Reference);
        }, () =>
        {
            CoordinateWithAdmission();
            refreshCalls++;
            return Task.CompletedTask;
        });

        try
        {
            var firstOperation = session.OpenAsync(InstanceId);
            firstDialog.Result.TrySetResult(DialogResult.Ok(true));
            await ObserveAsync(firstOperation);

            var secondOperation = session.OpenAsync("second/execution");
            var retirement = session.DisposeAsync().AsTask();
            Assert.Equal(1, secondDialog.CloseCalls);
            Assert.False(retirement.IsCompleted);
            secondDialog.Result.TrySetResult(DialogResult.Cancel());
            await ObserveAsync(Task.WhenAll(secondOperation, retirement));

            Assert.Equal(2, openCalls);
            Assert.Equal(1, refreshCalls);
            Assert.Equal(4, callbacks);
            await ObserveAsync(Task.WhenAll(observations));
        }
        finally
        {
            firstDialog.Result.TrySetResult(DialogResult.Cancel());
            secondDialog.Result.TrySetResult(DialogResult.Cancel());
            try
            {
                await ObserveAsync(session.DisposeAsync().AsTask());
            }
            finally
            {
                try
                {
                    await ObserveAsync(Task.WhenAll(readers));
                    await ObserveAsync(Task.WhenAll(observations));
                }
                finally
                {
                    foreach (var signal in observerSignals) signal.Dispose();
                }
            }
        }
    }

    private static Task ObserveAsync(Task task) => task.WaitAsync(
        ObservationTimeout,
        TestContext.Current.CancellationToken);

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TaskCompletionSource<T> NewCompletion<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class DialogBoundary
    {
        internal DialogBoundary(Action? onClose = null)
        {
            Reference.Result.Returns(Result.Task);
            Reference.When(reference => reference.Close(Arg.Any<DialogResult?>())).Do(call =>
            {
                CloseCalls++;
                CloseResult = call.Arg<DialogResult?>();
                onClose?.Invoke();
            });
        }

        internal IDialogReference Reference { get; } = Substitute.For<IDialogReference>();
        internal TaskCompletionSource<DialogResult?> Result { get; } = NewCompletion<DialogResult?>();
        internal int CloseCalls { get; private set; }
        internal DialogResult? CloseResult { get; private set; }
    }
}
