using Monica.JobScheduler.Models.Execution;
using Monica.JobScheduler.UI.UIJobScheduler.Support;
using Test.Monica.JobScheduler.UI.Infrastructure;
using Xunit;

namespace Test.Monica.JobScheduler.UI.UIJobScheduler.Executions.State;

public sealed class ExecutionDetailStateTests
{
    private static readonly TimeSpan ObservationTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task InitializeAndCancellation_WhenIdentityHasWhitespaceUnicodeAndReservedCharacters_ShouldUseExactTargetAndFullHistory()
    {
        var boundary = new ExecutionStateTestBoundary();
        await boundary.SeedAsync(TestContext.Current.CancellationToken);
        await boundary.EnqueueAsync(ExecutionStateTestBoundary.ExactInstance, TestContext.Current.CancellationToken);
        var peerId = ExecutionStateTestBoundary.ExactInstance.Trim();
        await boundary.EnqueueAsync(peerId, TestContext.Current.CancellationToken);
        await using var state = boundary.CreateDetail();
        await state.InitializeAsync();
        Assert.Equal(ExecutionStateTestBoundary.ExactInstance, state.InstanceId);
        Assert.NotNull(state.Execution);
        Assert.NotEmpty(state.Execution.History);
        var cancellation = await state.RequestCancellationAsync();

        Assert.True(cancellation.IsAuthorized);
        Assert.Equal(JobCancellationStatus.Cancelled, cancellation.Status);
        Assert.Equal(JobExecutionState.Cancelled, state.Execution.State);
        Assert.Contains(state.Execution.History, entry => entry.NewState == JobExecutionState.Cancelled);
        Assert.Equal(ExecutionStateTestBoundary.ExactInstance, Assert.Single(boundary.CancellationIds));
        Assert.All(boundary.DetailIds, id => Assert.Equal(ExecutionStateTestBoundary.ExactInstance, id));
        Assert.Equal(3, boundary.Access.Calls);
        var peer = await boundary.Store.GetExecutionAsync(ExecutionStateTestBoundary.Scope, peerId,
            TestContext.Current.CancellationToken);
        Assert.NotNull(peer);
        Assert.Equal(JobExecutionState.Queued, peer.State);
    }

    [Fact]
    public async Task RequestCancellationAsync_WhenExecutionHasActiveLease_ShouldKeepRunningUntilWorkerCompletesAndReportActualOutcomes()
    {
        var boundary = new ExecutionStateTestBoundary();
        await boundary.SeedAsync(TestContext.Current.CancellationToken);
        await boundary.EnqueueAsync(ExecutionStateTestBoundary.ExactInstance, TestContext.Current.CancellationToken);
        var lease = Assert.Single(await boundary.ClaimAsync(TestContext.Current.CancellationToken));
        await using var state = boundary.CreateDetail();
        await state.InitializeAsync();
        var requested = await state.RequestCancellationAsync();
        Assert.Equal(JobCancellationStatus.CancellationRequested, requested.Status);
        Assert.NotNull(state.Execution);
        Assert.Equal(JobExecutionState.Running, state.Execution.State);
        Assert.NotNull(state.Execution.CancellationRequestedAtUtc);
        Assert.Null(state.Execution.CompletedAtUtc);
        var historyCount = state.Execution.History.Count;
        var repeated = await state.RequestCancellationAsync();
        Assert.Equal(JobCancellationStatus.CancellationRequested, repeated.Status);
        Assert.Equal(historyCount, state.Execution.History.Count);

        await boundary.Store.CompleteAttemptAsync(new()
        {
            LeaseKey = lease.LeaseKey, Outcome = JobAttemptOutcome.Cancelled
        }, TestContext.Current.CancellationToken);
        var terminal = await state.RequestCancellationAsync();
        Assert.Equal(JobCancellationStatus.AlreadyTerminal, terminal.Status);
        Assert.Equal(JobExecutionState.Cancelled, state.Execution.State);
        Assert.NotNull(state.Execution.CompletedAtUtc);
    }

    [Fact]
    public async Task InitializeAsync_WhenExactTargetIsMissingOrFirstReadFails_ShouldDistinguishConfirmedMissingFromUnknown()
    {
        var boundary = new ExecutionStateTestBoundary();
        await using var missing = boundary.CreateDetail(" missing/记录?exact=%25 ");
        await missing.InitializeAsync();
        Assert.True(missing.HasLoadedSnapshot);
        Assert.True(missing.IsNotFound);
        Assert.False(missing.IsQueryFailed);
        Assert.Null(missing.Execution);
        Assert.NotNull(missing.ObservedAtUtc);
        Assert.Equal(JobCancellationStatus.NotFound, (await missing.RequestCancellationAsync()).Status);

        boundary.LoadDetail = (_, _) => throw new IOException("Detail unavailable");
        await using var failed = boundary.CreateDetail();
        await failed.InitializeAsync();
        Assert.False(failed.HasLoadedSnapshot);
        Assert.False(failed.IsNotFound);
        Assert.True(failed.IsQueryFailed);
        Assert.Contains("Detail unavailable", failed.Error);
        Assert.Null(failed.ObservedAtUtc);
    }

    [Fact]
    public async Task RefreshAsync_WhenReadFailsThenAuthorizationFaults_ShouldRetainSameIdentityEvidenceOnlyWhileAccessible()
    {
        var boundary = new ExecutionStateTestBoundary();
        await boundary.SeedAsync(TestContext.Current.CancellationToken);
        await boundary.EnqueueAsync(ExecutionStateTestBoundary.ExactInstance, TestContext.Current.CancellationToken);
        await using var state = boundary.CreateDetail();
        await state.InitializeAsync();
        var evidence = state.Execution;
        var observation = state.ObservedAtUtc;
        boundary.LoadDetail = (_, _) => throw new IOException("Refresh unavailable");
        await state.RefreshAsync();
        Assert.True(state.HasLoadedSnapshot);
        Assert.True(state.IsQueryFailed);
        Assert.Same(evidence, state.Execution);
        Assert.Equal(observation, state.ObservedAtUtc);

        boundary.Access.Authorize = _ => throw new IOException("Authorization unavailable");
        Assert.False(await state.ReauthorizeAsync());
        var mutation = await state.RequestCancellationAsync();
        Assert.False(state.IsAuthorized);
        Assert.True(state.AccessChecked);
        Assert.False(state.HasLoadedSnapshot);
        Assert.False(state.IsNotFound);
        Assert.Null(state.Execution);
        Assert.Null(state.ObservedAtUtc);
        Assert.Equal("Authorization unavailable", state.Error);
        Assert.False(mutation.IsAuthorized);
        Assert.Equal("Authorization unavailable", mutation.Error);
        Assert.Empty(boundary.CancellationIds);
    }

    [Fact]
    public async Task RequestCancellationAsync_WhenPostMutationRefreshLosesAccess_ShouldPreserveDurableOutcomeAndClearDetailEvidence()
    {
        var boundary = new ExecutionStateTestBoundary();
        await boundary.SeedAsync(TestContext.Current.CancellationToken);
        await boundary.EnqueueAsync(ExecutionStateTestBoundary.ExactInstance, TestContext.Current.CancellationToken);
        await using var state = boundary.CreateDetail();
        await state.InitializeAsync();
        boundary.Access.Authorize = _ => Task.FromResult(boundary.Access.Calls < 3);
        var cancellation = await state.RequestCancellationAsync();
        Assert.True(cancellation.IsAuthorized);
        Assert.Equal(JobCancellationStatus.Cancelled, cancellation.Status);
        Assert.False(state.IsAuthorized);
        Assert.Null(state.Execution);
        Assert.False(state.HasLoadedSnapshot);
        Assert.Single(boundary.DetailIds);
        var durable = await boundary.Store.GetExecutionAsync(ExecutionStateTestBoundary.Scope,
            ExecutionStateTestBoundary.ExactInstance, TestContext.Current.CancellationToken);
        Assert.NotNull(durable);
        Assert.Equal(JobExecutionState.Cancelled, durable.State);
    }

    [Fact]
    public async Task RequestCancellationAsync_WhenAccessChangesAfterPreConfirmationCheck_ShouldReauthorizeBeforeMutation()
    {
        var boundary = new ExecutionStateTestBoundary();
        await using var state = boundary.CreateDetail();
        Assert.True(await state.ReauthorizeAsync());
        boundary.Access.Authorized = false;
        var cancellation = await state.RequestCancellationAsync();
        Assert.False(cancellation.IsAuthorized);
        Assert.Null(cancellation.Status);
        Assert.Empty(boundary.CancellationIds);
        Assert.Equal(2, boundary.Access.Calls);
    }

    [Fact]
    public async Task RequestCancellationAsync_WhenPostMutationReadSubscriberAwaitsAnotherCancellation_ShouldReleaseMutationAndReadGatesBeforeNotifying()
    {
        var boundary = new ExecutionStateTestBoundary();
        await boundary.SeedAsync(TestContext.Current.CancellationToken);
        await boundary.EnqueueAsync(ExecutionStateTestBoundary.ExactInstance, TestContext.Current.CancellationToken);
        await using var state = boundary.CreateDetail();
        await state.InitializeAsync();
        JobCancellationStatus? nestedStatus = null;
        Func<Task>? subscriber = null;
        subscriber = async () =>
        {
            if (!state.IsLoading || !state.IsMutating) return;
            state.StateChanged -= subscriber;
            nestedStatus = (await state.RequestCancellationAsync()).Status;
        };
        state.StateChanged += subscriber;
        var cancellation = await state.RequestCancellationAsync().WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);
        Assert.Equal(JobCancellationStatus.Cancelled, cancellation.Status);
        Assert.Equal(JobCancellationStatus.AlreadyTerminal, nestedStatus);
        Assert.False(state.IsMutating);
        Assert.NotNull(state.Execution);
        Assert.Equal(JobExecutionState.Cancelled, state.Execution.State);
    }

    [Fact]
    public async Task RefreshAsync_WhenEarlierAuthorizedReadCompletesAfterRevocationAndNewAuthorization_ShouldRejectItsOldAccessEvidence()
    {
        var boundary = new ExecutionStateTestBoundary();
        await boundary.SeedAsync(TestContext.Current.CancellationToken);
        var execution = await boundary.EnqueueAsync(ExecutionStateTestBoundary.ExactInstance, TestContext.Current.CancellationToken);
        var release = Signal<JobExecutionInstance?>();
        boundary.LoadDetail = (_, _) => release.Task;
        var state = boundary.CreateDetail();
        try
        {
            var load = state.InitializeAsync();
            Assert.Single(boundary.DetailIds);
            boundary.Access.Authorized = false;
            Assert.False(await state.ReauthorizeAsync());
            boundary.Access.Authorized = true;
            Assert.True(await state.ReauthorizeAsync());
            release.TrySetResult(execution);
            await ObserveAsync(load);
            Assert.Null(state.Execution);
            Assert.False(state.HasLoadedSnapshot);
            Assert.Null(state.ObservedAtUtc);
        }
        finally
        {
            release.TrySetResult(execution);
            await ObserveAsync(state.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenActualDetailReadCompletesLateAndAnotherReadIsQueued_ShouldDrainAndRejectBothAdmissions()
    {
        var boundary = new ExecutionStateTestBoundary();
        await boundary.SeedAsync(TestContext.Current.CancellationToken);
        var execution = await boundary.EnqueueAsync(ExecutionStateTestBoundary.ExactInstance, TestContext.Current.CancellationToken);
        var release = Signal<JobExecutionInstance?>();
        boundary.LoadDetail = (_, _) => release.Task;
        var state = boundary.CreateDetail();
        var first = state.InitializeAsync();
        var queued = state.RefreshAsync();
        var retirement = state.DisposeAsync().AsTask();
        try
        {
            Assert.Same(retirement, state.DisposeAsync().AsTask());
            Assert.False(retirement.IsCompleted);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => state.RequestCancellationAsync());
            release.TrySetResult(execution);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ObserveAsync(first));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ObserveAsync(queued));
            await ObserveAsync(retirement);
            Assert.Single(boundary.DetailIds);
            Assert.Null(state.Execution);
            Assert.False(state.HasLoadedSnapshot);
        }
        finally
        {
            release.TrySetResult(execution);
            await ObserveAsync(retirement);
        }
    }

    [Fact]
    public async Task RequestCancellationAsync_WhenRetiredDuringIgnoringAuthorization_ShouldPropagateCancellationWithoutMutationOrFeedbackOutcome()
    {
        var boundary = new ExecutionStateTestBoundary();
        var release = Signal<bool>();
        boundary.Access.Authorize = _ => release.Task;
        var state = boundary.CreateDetail();
        var mutation = state.RequestCancellationAsync();
        var retirement = state.DisposeAsync().AsTask();
        try
        {
            Assert.False(retirement.IsCompleted);
            release.TrySetResult(true);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ObserveAsync(mutation));
            await ObserveAsync(retirement);
            Assert.Empty(boundary.CancellationIds);
        }
        finally
        {
            release.TrySetResult(true);
            await ObserveAsync(retirement);
        }
    }

    [Fact]
    public async Task RequestCancellationAsync_WhenActualMutationReturnsAfterRetirement_ShouldDrainItsActualResultWithoutAdoptingLateDetail()
    {
        var boundary = new ExecutionStateTestBoundary();
        await boundary.SeedAsync(TestContext.Current.CancellationToken);
        await boundary.EnqueueAsync(ExecutionStateTestBoundary.ExactInstance, TestContext.Current.CancellationToken);
        var release = Signal<bool>();
        var entered = Signal<bool>();
        boundary.CancelExecution = async (id, token) =>
        {
            var actual = await boundary.Store.RequestCancellationAsync(ExecutionStateTestBoundary.Scope, id,
                cancellationToken: token);
            entered.TrySetResult(true);
            await ObserveAsync(release.Task);
            return actual;
        };
        var state = boundary.CreateDetail();
        var mutation = state.RequestCancellationAsync();
        await ObserveAsync(entered.Task);
        var retirement = state.DisposeAsync().AsTask();
        try
        {
            Assert.False(retirement.IsCompleted);
            release.TrySetResult(true);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ObserveAsync(mutation));
            await ObserveAsync(retirement);
            Assert.Empty(boundary.DetailIds);
            Assert.False(state.HasLoadedSnapshot);
            var actual = await boundary.Store.GetExecutionAsync(ExecutionStateTestBoundary.Scope,
                ExecutionStateTestBoundary.ExactInstance, TestContext.Current.CancellationToken);
            Assert.NotNull(actual);
            Assert.Equal(JobExecutionState.Cancelled, actual.State);
        }
        finally
        {
            release.TrySetResult(true);
            await ObserveAsync(retirement);
        }
    }

    [Fact]
    public async Task InitializeAsync_WhenSubscriberStartsRetirement_ShouldObserveCancellationAndAvoidCallbackLockInversion()
    {
        var boundary = new ExecutionStateTestBoundary();
        var state = boundary.CreateDetail();
        Task? retirement = null;
        state.StateChanged += () =>
        {
            retirement = state.DisposeAsync().AsTask();
            return Task.CompletedTask;
        };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => state.InitializeAsync());
        Assert.NotNull(retirement);
        await ObserveAsync(retirement);
        Assert.Same(retirement, state.DisposeAsync().AsTask());
        Assert.Empty(boundary.DetailIds);
    }

    [Fact]
    public async Task InitializeAsync_WhenAnotherSessionRetires_ShouldKeepIndependentIdentityAndEvidence()
    {
        var boundary = new ExecutionStateTestBoundary();
        await boundary.SeedAsync(TestContext.Current.CancellationToken);
        await boundary.EnqueueAsync(ExecutionStateTestBoundary.ExactInstance, TestContext.Current.CancellationToken);
        var retired = boundary.CreateDetail("another-execution");
        await retired.DisposeAsync();
        await using var live = boundary.CreateDetail();
        await live.InitializeAsync();
        Assert.False(live.IsDisposed);
        Assert.True(live.HasLoadedSnapshot);
        Assert.NotNull(live.Execution);
        Assert.Equal(ExecutionStateTestBoundary.ExactInstance, live.Execution.InstanceId);
    }

    [Fact]
    public async Task GetExecutionDuration_WhenRealStoreQueuesRetry_ShouldNotTreatRetainedAttemptStartAsActiveWork()
    {
        var boundary = new ExecutionStateTestBoundary();
        await boundary.SeedAsync(TestContext.Current.CancellationToken);
        await boundary.EnqueueAsync(ExecutionStateTestBoundary.ExactInstance, TestContext.Current.CancellationToken);
        var lease = Assert.Single(await boundary.ClaimAsync(TestContext.Current.CancellationToken));
        boundary.Clock.Now = boundary.Clock.Now.AddSeconds(10);
        await boundary.Store.CompleteAttemptAsync(new()
        {
            LeaseKey = lease.LeaseKey, Outcome = JobAttemptOutcome.Failed, RetryDelay = TimeSpan.FromMinutes(1)
        }, TestContext.Current.CancellationToken);
        await using var state = boundary.CreateDetail();
        await state.InitializeAsync();
        Assert.NotNull(state.Execution);
        Assert.Equal(JobExecutionState.Queued, state.Execution.State);
        Assert.NotNull(state.Execution.StartedAtUtc);
        Assert.Null(state.Execution.CompletedAtUtc);
        Assert.Equal(1, state.Execution.ExecutionAttempt);
        Assert.Equal(1, state.Execution.RetryAttempt);
        Assert.Null(SchedulerTimePresentation.GetExecutionDuration(state.Execution, boundary.Clock.Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateDetailState_WhenIdentifierIsBlank_ShouldRejectIt(string id)
    {
        var boundary = new ExecutionStateTestBoundary();
        Assert.Throws<ArgumentException>(() => boundary.CreateDetail(id));
    }

    private static TaskCompletionSource<T> Signal<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static Task ObserveAsync(Task task) => task.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);
}
