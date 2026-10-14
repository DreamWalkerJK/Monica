using Monica.JobScheduler.Models.Execution;
using Monica.JobScheduler.UI.UIJobScheduler.Executions.State;
using MudBlazor;
using Test.Monica.JobScheduler.UI.Infrastructure;
using Test.Monica.JobScheduler.UI.Support;
using Xunit;

namespace Test.Monica.JobScheduler.UI.UIJobScheduler.Executions.State;

public sealed class JobExecutionsPageStateTests
{
    private static readonly TimeSpan ObservationTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task LoadTableAsync_WhenDraftAndNativeStateChangeDuringAuthorization_ShouldUseEachCopiedQueryAndNewestEvidence()
    {
        var boundary = new ExecutionStateTestBoundary();
        await boundary.SeedAsync(TestContext.Current.CancellationToken);
        await boundary.EnqueueAsync(ExecutionStateTestBoundary.ExactInstance, TestContext.Current.CancellationToken);
        await boundary.EnqueueAsync("another-execution", TestContext.Current.CancellationToken);
        var authorize = Signal<bool>();
        var secondEntered = Signal<bool>();
        var secondRelease = Signal<bool>();
        boundary.Access.Authorize = _ => boundary.Access.Calls == 1 ? authorize.Task : Task.FromResult(true);
        boundary.LoadExecutions = async (query, token) =>
        {
            if (query.SearchText == "another-execution")
            {
                secondEntered.TrySetResult(true);
                await ObserveAsync(secondRelease.Task);
            }
            return await boundary.Store.QueryExecutionsAsync(query, token);
        };
        var state = boundary.CreatePage();
        try
        {
            var admittedAt = boundary.Clock.Now;
            state.SearchText = ExecutionStateTestBoundary.ExactInstance;
            JobExecutionState[] selected = [JobExecutionState.Queued];
            state.SetSelectedStates(selected);
            state.TimeRange = ExecutionTimeRange.LastHour;
            var native = Table();
            var first = state.LoadTableAsync(native, TestContext.Current.CancellationToken);
            selected[0] = JobExecutionState.Failed;
            native.Page = 3;
            native.PageSize = 200;
            native.SortLabel = nameof(JobExecutionSortField.CompletedAtUtc);
            state.SearchText = "another-execution";
            state.TimeRange = ExecutionTimeRange.All;
            var second = state.LoadTableAsync(Table(sort: JobExecutionSortField.AvailableAtUtc, descending: false),
                TestContext.Current.CancellationToken);
            boundary.Clock.Now = admittedAt.AddMinutes(5);
            authorize.TrySetResult(true);
            var firstPage = await ObserveAsync(first);
            await ObserveAsync(secondEntered.Task);

            Assert.Equal(ExecutionStateTestBoundary.ExactInstance, Assert.Single(Items(firstPage)).InstanceId);
            Assert.False(state.HasLoadedSnapshot);
            Assert.Empty(state.Executions);
            secondRelease.TrySetResult(true);
            var secondPage = await ObserveAsync(second);
            Assert.Equal("another-execution", Assert.Single(Items(secondPage)).InstanceId);
            Assert.Equal("another-execution", Assert.Single(state.Executions).InstanceId);
            Assert.True(state.HasLoadedSnapshot);
            Assert.Equal(boundary.Clock.Now, state.ObservedAtUtc);
            Assert.Collection(boundary.Queries,
                query =>
                {
                    Assert.Equal(ExecutionStateTestBoundary.Scope, query.SchedulerScopeKey);
                    Assert.Equal(ExecutionStateTestBoundary.ExactInstance, query.SearchText);
                    Assert.Equal(admittedAt.AddHours(-1), query.CreatedAfterUtc);
                    Assert.NotNull(query.States);
                    Assert.Equal(JobExecutionState.Queued, Assert.Single(query.States));
                    Assert.Equal(1, query.PageNumber);
                    Assert.Equal(10, query.PageSize);
                    Assert.Equal(JobExecutionSortField.CreatedAtUtc, query.SortField);
                },
                query =>
                {
                    Assert.Equal("another-execution", query.SearchText);
                    Assert.Null(query.CreatedAfterUtc);
                    Assert.Equal(JobExecutionSortField.AvailableAtUtc, query.SortField);
                    Assert.False(query.SortDescending);
                });
        }
        finally
        {
            authorize.TrySetResult(true);
            secondRelease.TrySetResult(true);
            await ObserveAsync(state.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task LoadTableAsync_WhenFirstReadFailsThenZeroReadSucceeds_ShouldDistinguishUnknownFromEmpty()
    {
        var boundary = new ExecutionStateTestBoundary();
        await using var state = boundary.CreatePage();
        boundary.LoadExecutions = (_, _) => Task.FromException<global::Monica.JobScheduler.Models.QueryResult<JobExecutionInstance>>(
            new IOException("Ledger unavailable"));
        var failed = await state.LoadTableAsync(Table(), TestContext.Current.CancellationToken);
        Assert.Empty(Items(failed));
        Assert.False(state.HasLoadedSnapshot);
        Assert.True(state.IsQueryFailed);
        Assert.Contains("Ledger unavailable", state.Error);
        Assert.Null(state.LoadedQuery);
        Assert.Null(state.ObservedAtUtc);

        boundary.LoadExecutions = null;
        var empty = await state.LoadTableAsync(Table(), TestContext.Current.CancellationToken);
        Assert.Empty(Items(empty));
        Assert.Equal(0, empty.TotalItems);
        Assert.True(state.HasLoadedSnapshot);
        Assert.False(state.IsQueryFailed);
        Assert.NotNull(state.LoadedQuery);
        Assert.Equal(boundary.Clock.Now, state.ObservedAtUtc);
        Assert.Null(state.Error);
    }

    [Fact]
    public async Task LoadTableAsync_WhenSameIntentRefreshFails_ShouldRetainItsActualBoundsAndClearOnChangedIntent()
    {
        var boundary = new ExecutionStateTestBoundary();
        await boundary.SeedAsync(TestContext.Current.CancellationToken);
        await boundary.EnqueueAsync(ExecutionStateTestBoundary.ExactInstance, TestContext.Current.CancellationToken);
        await using var state = boundary.CreatePage();
        state.TimeRange = ExecutionTimeRange.LastHour;
        await state.LoadTableAsync(Table(), TestContext.Current.CancellationToken);
        var acceptedQuery = state.LoadedQuery;
        Assert.NotNull(acceptedQuery);
        var acceptedLowerBound = Assert.IsType<DateTimeOffset>(acceptedQuery.CreatedAfterUtc);
        var acceptedObservation = state.ObservedAtUtc;
        boundary.Clock.Now = boundary.Clock.Now.AddMinutes(10);
        boundary.LoadExecutions = (_, _) => Task.FromException<global::Monica.JobScheduler.Models.QueryResult<JobExecutionInstance>>(
            new IOException("Refresh failed"));

        var stale = await state.LoadTableAsync(Table(), TestContext.Current.CancellationToken);
        Assert.Single(Items(stale));
        Assert.True(state.HasLoadedSnapshot);
        Assert.True(state.IsQueryFailed);
        Assert.Same(acceptedQuery, state.LoadedQuery);
        Assert.Equal(acceptedObservation, state.ObservedAtUtc);
        Assert.NotNull(state.RequestedQuery);
        Assert.Equal(acceptedLowerBound.AddMinutes(10), state.RequestedQuery.CreatedAfterUtc);
        state.SearchText = "different-query";
        var changed = await state.LoadTableAsync(Table(), TestContext.Current.CancellationToken);
        Assert.Empty(Items(changed));
        Assert.False(state.HasLoadedSnapshot);
        Assert.True(state.IsQueryFailed);
        Assert.Null(state.LoadedQuery);
        Assert.Null(state.ObservedAtUtc);
    }

    [Fact]
    public async Task LoadTableAsync_WhenCustomCalendarDatesAreInvalid_ShouldClearPreviousEvidenceWithoutBackendRead()
    {
        var boundary = new ExecutionStateTestBoundary();
        await boundary.SeedAsync(TestContext.Current.CancellationToken);
        await boundary.EnqueueAsync(ExecutionStateTestBoundary.ExactInstance, TestContext.Current.CancellationToken);
        await using var state = boundary.CreatePage();
        await state.LoadTableAsync(Table(), TestContext.Current.CancellationToken);
        state.TimeRange = ExecutionTimeRange.Custom;
        state.CustomStartDate = new DateTime(2026, 10, 12);
        state.CustomEndDate = new DateTime(2026, 10, 11);
        var invalid = await state.LoadTableAsync(Table(), TestContext.Current.CancellationToken);
        Assert.True(state.HasInvalidCustomRange);
        Assert.Empty(Items(invalid));
        Assert.False(state.HasLoadedSnapshot);
        Assert.False(state.IsQueryFailed);
        Assert.Null(state.ObservedAtUtc);
        Assert.Single(boundary.Queries);
    }

    [Fact]
    public async Task ApplyInitialQuery_WhenStatesContainUndefinedNumbers_ShouldKeepOnlyDefinedStatesAndExactSearch()
    {
        var boundary = new ExecutionStateTestBoundary();
        await using var state = boundary.CreatePage();
        state.ApplyInitialQuery("running,999,Queued,Running,-1", ExecutionStateTestBoundary.ExactInstance);
        await state.LoadTableAsync(Table(sort: (JobExecutionSortField)999), TestContext.Current.CancellationToken);
        var query = Assert.Single(boundary.Queries);
        Assert.Equal(ExecutionStateTestBoundary.ExactInstance, query.SearchText);
        Assert.Equal(new[] { JobExecutionState.Queued, JobExecutionState.Running }, query.States);
        Assert.Equal(JobExecutionSortField.CreatedAtUtc, query.SortField);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoadTableAsync_WhenAuthorizationIsLostOrFaulted_ShouldClearEvidenceAndAvoidReadAndMutation(bool throws)
    {
        var boundary = new ExecutionStateTestBoundary();
        await boundary.SeedAsync(TestContext.Current.CancellationToken);
        await boundary.EnqueueAsync(ExecutionStateTestBoundary.ExactInstance, TestContext.Current.CancellationToken);
        await using var state = boundary.CreatePage();
        await state.LoadTableAsync(Table(), TestContext.Current.CancellationToken);
        boundary.Access.Authorized = false;
        if (throws) boundary.Access.Authorize = _ => throw new IOException("Authorization unavailable");
        var inaccessible = await state.LoadTableAsync(Table(), TestContext.Current.CancellationToken);
        var cancellation = await state.RequestCancellationAsync(ExecutionStateTestBoundary.ExactInstance);

        Assert.Empty(Items(inaccessible));
        Assert.False(state.IsAuthorized);
        Assert.True(state.AccessChecked);
        Assert.False(state.HasLoadedSnapshot);
        Assert.Null(state.ObservedAtUtc);
        Assert.Null(state.LoadedQuery);
        Assert.Single(boundary.Queries);
        Assert.Empty(boundary.CancellationIds);
        Assert.False(cancellation.IsAuthorized);
        Assert.Null(cancellation.Status);
        if (throws)
        {
            Assert.Equal("Authorization unavailable", state.Error);
            Assert.Equal("Authorization unavailable", cancellation.Error);
        }
        else Assert.Null(cancellation.Error);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task LoadTableAsync_WhenASeparateAccessCheckRevokesAnEarlierRead_ShouldNeverRestoreItsRows(bool laterReauthorized, bool lateReadFails)
    {
        var boundary = new ExecutionStateTestBoundary();
        await boundary.SeedAsync(TestContext.Current.CancellationToken);
        await boundary.EnqueueAsync(ExecutionStateTestBoundary.ExactInstance, TestContext.Current.CancellationToken);
        var entered = Signal<bool>();
        var release = Signal<bool>();
        boundary.LoadExecutions = async (query, token) =>
        {
            entered.TrySetResult(true);
            await ObserveAsync(release.Task);
            if (lateReadFails) throw new IOException("Superseded read failed");
            return await boundary.Store.QueryExecutionsAsync(query, token);
        };
        var state = boundary.CreatePage();
        try
        {
            var load = state.LoadTableAsync(Table(), TestContext.Current.CancellationToken);
            await ObserveAsync(entered.Task);
            boundary.Access.Authorized = false;
            Assert.False(await state.ReauthorizeAsync());
            if (laterReauthorized)
            {
                boundary.Access.Authorized = true;
                Assert.True(await state.ReauthorizeAsync());
            }
            release.TrySetResult(true);
            Assert.Empty(Items(await ObserveAsync(load)));
            Assert.Empty(state.Executions);
            Assert.False(state.HasLoadedSnapshot);
            Assert.Null(state.ObservedAtUtc);
        }
        finally
        {
            release.TrySetResult(true);
            await ObserveAsync(state.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task InitializeAsync_WhenAnOlderAccessProducerCompletesAfterAProviderFault_ShouldNotRestoreAuthorization()
    {
        var boundary = new ExecutionStateTestBoundary();
        var release = Signal<bool>();
        boundary.Access.Authorize = _ => boundary.Access.Calls == 1 ? release.Task : throw new IOException("Access fault");
        var state = boundary.CreatePage();
        try
        {
            var initialization = state.InitializeAsync();
            Assert.False(await state.ReauthorizeAsync());
            release.TrySetResult(true);
            await ObserveAsync(initialization);
            Assert.False(state.IsAuthorized);
            Assert.Equal("Access fault", state.Error);
            Assert.False(state.HasLoadedSnapshot);
        }
        finally
        {
            release.TrySetResult(true);
            await ObserveAsync(state.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenInitializationIgnoresCancellationAndAReadIsQueued_ShouldJoinBothBeforeCleanup()
    {
        var boundary = new ExecutionStateTestBoundary();
        var release = Signal<bool>();
        boundary.Access.Authorize = _ => release.Task;
        var state = boundary.CreatePage();
        var initialization = state.InitializeAsync();
        var queued = state.LoadTableAsync(Table(), TestContext.Current.CancellationToken);
        var retirement = state.DisposeAsync().AsTask();
        try
        {
            Assert.True(state.IsDisposed);
            Assert.Same(retirement, state.DisposeAsync().AsTask());
            Assert.False(retirement.IsCompleted);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => state.InitializeAsync());
            release.TrySetResult(true);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ObserveAsync(initialization));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ObserveAsync(queued));
            await ObserveAsync(retirement);
            Assert.Empty(boundary.Queries);
            Assert.False(state.AccessChecked);
        }
        finally
        {
            release.TrySetResult(true);
            await ObserveAsync(retirement);
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenActualReadCompletesLate_ShouldRejectAdoptionAndShareRetirement()
    {
        var boundary = new ExecutionStateTestBoundary();
        await boundary.SeedAsync(TestContext.Current.CancellationToken);
        await boundary.EnqueueAsync(ExecutionStateTestBoundary.ExactInstance, TestContext.Current.CancellationToken);
        var actual = await boundary.Store.QueryExecutionsAsync(new() { SchedulerScopeKey = ExecutionStateTestBoundary.Scope },
            TestContext.Current.CancellationToken);
        var release = Signal<global::Monica.JobScheduler.Models.QueryResult<JobExecutionInstance>>();
        boundary.LoadExecutions = (_, _) => release.Task;
        var state = boundary.CreatePage();
        var load = state.LoadTableAsync(Table(), TestContext.Current.CancellationToken);
        var retirement = state.DisposeAsync().AsTask();
        try
        {
            Assert.False(retirement.IsCompleted);
            release.TrySetResult(actual);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ObserveAsync(load));
            await ObserveAsync(retirement);
            Assert.False(state.HasLoadedSnapshot);
            Assert.Empty(state.Executions);
            Assert.Same(retirement, state.DisposeAsync().AsTask());
        }
        finally
        {
            release.TrySetResult(actual);
            await ObserveAsync(retirement);
        }
    }

    [Fact]
    public async Task LoadTableAsync_WhenSubscriberThrows_ShouldReleaseSerializationForTheNextRead()
    {
        var boundary = new ExecutionStateTestBoundary();
        await using var state = boundary.CreatePage();
        Func<Task> subscriber = () => throw new IOException("Renderer callback failed");
        state.StateChanged += subscriber;
        await Assert.ThrowsAsync<IOException>(() => state.LoadTableAsync(Table(), TestContext.Current.CancellationToken));
        state.StateChanged -= subscriber;
        await ObserveAsync(state.LoadTableAsync(Table(), TestContext.Current.CancellationToken));
        Assert.True(state.HasLoadedSnapshot);
        Assert.False(state.IsLoading);
    }

    [Fact]
    public async Task LoadTableAsync_WhenSubscriberAwaitsAReentrantRead_ShouldNotHoldTheSerializationGateAcrossNotification()
    {
        var boundary = new ExecutionStateTestBoundary();
        await using var state = boundary.CreatePage();
        var reentered = false;
        Func<Task>? subscriber = null;
        subscriber = async () =>
        {
            state.StateChanged -= subscriber;
            await state.LoadTableAsync(Table(), TestContext.Current.CancellationToken);
            reentered = true;
        };
        state.StateChanged += subscriber;
        await ObserveAsync(state.LoadTableAsync(Table(), TestContext.Current.CancellationToken));
        Assert.True(reentered);
        Assert.Equal(2, boundary.Queries.Count);
        Assert.True(state.HasLoadedSnapshot);
        Assert.False(state.IsLoading);
    }

    [Fact]
    public async Task LoadTableAsync_WhenCalendarConversionCannotRepresentAnEnd_ShouldReturnFailedEvidenceInsteadOfZeroSuccess()
    {
        var boundary = new ExecutionStateTestBoundary();
        await using var state = boundary.CreatePage();
        state.TimeRange = ExecutionTimeRange.Custom;
        state.CustomEndDate = DateTime.MaxValue.Date;
        var page = await state.LoadTableAsync(Table(), TestContext.Current.CancellationToken);
        Assert.Empty(Items(page));
        Assert.False(state.HasLoadedSnapshot);
        Assert.True(state.IsQueryFailed);
        Assert.False(state.IsLoading);
        Assert.NotNull(state.Error);
        Assert.Empty(boundary.Queries);
    }

    [Theory]
    [InlineData(3, 8, 23)]
    [InlineData(11, 1, 25)]
    public async Task LoadTableAsync_WhenCustomDayCrossesDaylightSaving_ShouldIncludeTheWholeSchedulerCalendarDay(int month, int day, int hours)
    {
        var boundary = new ExecutionStateTestBoundary(SchedulerTimePresentationTests.CreateDaylightSavingTimeZone());
        await using var state = boundary.CreatePage();
        state.TimeRange = ExecutionTimeRange.Custom;
        state.CustomStartDate = new DateTime(2026, month, day);
        state.CustomEndDate = state.CustomStartDate;
        await state.LoadTableAsync(Table(), TestContext.Current.CancellationToken);
        var query = Assert.Single(boundary.Queries);
        var after = Assert.IsType<DateTimeOffset>(query.CreatedAfterUtc);
        var before = Assert.IsType<DateTimeOffset>(query.CreatedBeforeUtc);
        Assert.Equal(TimeSpan.FromHours(hours), before.AddTicks(1) - after);
        Assert.Equal(TimeSpan.Zero, after.Offset);
        Assert.True(state.HasLoadedSnapshot);
    }

    [Theory]
    [InlineData(3, 1, 23)]
    [InlineData(11, 1, 25)]
    public async Task LoadTableAsync_WhenMidnightIsSkippedOrRepeated_ShouldUseResolvedCalendarBoundaries(int month, int day, int hours)
    {
        var boundary = new ExecutionStateTestBoundary(SchedulerTimePresentationTests.CreateMidnightTransitionTimeZone());
        await using var state = boundary.CreatePage();
        state.TimeRange = ExecutionTimeRange.Custom;
        state.CustomStartDate = new DateTime(2026, month, day, 0, 0, 0, DateTimeKind.Utc);
        state.CustomEndDate = state.CustomStartDate;
        await state.LoadTableAsync(Table(), TestContext.Current.CancellationToken);
        var query = Assert.Single(boundary.Queries);
        var after = Assert.IsType<DateTimeOffset>(query.CreatedAfterUtc);
        var before = Assert.IsType<DateTimeOffset>(query.CreatedBeforeUtc);
        var expectedStart = month == 3
            ? new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero)
            : new DateTimeOffset(2026, 10, 31, 23, 0, 0, TimeSpan.Zero);
        Assert.Equal(expectedStart, query.CreatedAfterUtc);
        Assert.Equal(TimeSpan.FromHours(hours), before.AddTicks(1) - after);
    }

    private static TableState Table(JobExecutionSortField sort = JobExecutionSortField.CreatedAtUtc, bool descending = true) => new()
    {
        PageSize = 10, SortLabel = sort.ToString(), SortDirection = descending ? SortDirection.Descending : SortDirection.Ascending
    };

    private static IEnumerable<JobExecutionInstance> Items(TableData<JobExecutionInstance> page)
    {
        var items = page.Items;
        Assert.NotNull(items);
        return items;
    }

    private static TaskCompletionSource<T> Signal<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static Task ObserveAsync(Task task) => task.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);
    private static Task<T> ObserveAsync<T>(Task<T> task) => task.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);
}
