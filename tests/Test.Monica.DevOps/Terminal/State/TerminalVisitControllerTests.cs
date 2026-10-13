using Monica.DevOps.Terminal.State;
using Monica.UI.Shell.Support;
using Test.Monica.DevOps.Terminal.Support;
using Xunit;

namespace Test.Monica.DevOps.Terminal.State;

public sealed class TerminalVisitControllerTests
{
    private const string OWNER = "11111111111111111111111111111111";

    [Fact]
    public async Task InitializeAsync_WhenOwnerReadFinishesAfterRetirement_ShouldDrainReadWithoutCreatingSession()
    {
        await using var fixture = new TerminalRuntimeFixture();
        var storage = new ControlledStorage { ReadGate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var controller = CreateController(fixture, storage);
        var initialization = controller.InitializeAsync();
        await WaitAsync(storage.ReadEntered.Task);
        var retirement = controller.DisposeAsync().AsTask();
        try
        {
            Assert.False(retirement.IsCompleted);
            Assert.Empty(fixture.SessionService.GetSessions(OWNER));
        }
        finally { storage.ReadGate.TrySetResult(OWNER); }
        await WaitAsync(initialization);
        await WaitAsync(retirement);
        Assert.False(controller.IsInitialized);
        Assert.Null(controller.Environment);
        Assert.Empty(fixture.SessionService.GetSessions(OWNER));
        Assert.Equal(0, storage.DisposeCount);
    }

    [Fact]
    public async Task InitializeAsync_WhenOwnerWriteFails_ShouldRemainUnavailableAndRetryWithOnePollOwner()
    {
        await using var fixture = new TerminalRuntimeFixture();
        var storage = new ControlledStorage { Value = null, WriteFailure = BrowserStorageWriteFailureKind.StorageUnavailable };
        var clock = new ControlledClock();
        await using var controller = CreateController(fixture, storage, clock: clock);
        await controller.InitializeAsync();
        Assert.False(controller.IsInitialized);
        Assert.False(controller.CanRun);
        Assert.Equal(BrowserStorageWriteFailureKind.StorageUnavailable, controller.OwnerStorageFailure);
        Assert.Empty(fixture.SessionService.GetSessions(storage.LastWrittenOwner!));
        Assert.Equal(0, clock.CreatedTimers);

        storage.WriteFailure = BrowserStorageWriteFailureKind.None;
        await controller.InitializeAsync();
        await controller.InitializeAsync();
        Assert.True(controller.IsInitialized);
        Assert.Null(controller.InitializationFailure);
        Assert.Single(controller.Sessions);
        Assert.Equal(1, clock.CreatedTimers);
        Assert.Equal(1, clock.ActiveTimers);
        Assert.Equal(0, storage.DisposeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisposeAsync_WhenOwnerReadFailsLate_ShouldObserveFailureWithoutAdmittingBackendWork(bool canceled)
    {
        await using var fixture = new TerminalRuntimeFixture();
        var storage = new ControlledStorage { ReadGate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var controller = CreateController(fixture, storage);
        var initialization = controller.InitializeAsync();
        await WaitAsync(storage.ReadEntered.Task);
        var retirement = controller.DisposeAsync().AsTask();
        Exception failure = canceled ? new OperationCanceledException("Unrelated late owner-read cancellation.") :
            new InvalidOperationException("Controlled late owner-read failure.");
        try { Assert.False(retirement.IsCompleted); }
        finally { storage.ReadGate.TrySetException(failure); }
        if (canceled) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => initialization);
        else await Assert.ThrowsAsync<InvalidOperationException>(() => initialization);
        var aggregate = await Assert.ThrowsAsync<AggregateException>(() => retirement);
        Assert.Contains(aggregate.InnerExceptions, error => error.Message == failure.Message);
        Assert.False(controller.IsInitialized);
        Assert.Empty(fixture.SessionService.GetSessions(OWNER));
        Assert.Equal(0, storage.DisposeCount);
    }

    [Fact]
    public async Task StartCommandAsync_WhenPublicationIsPending_ShouldFreezeTargetAndPreserveNewerDraft()
    {
        await using var fixture = new TerminalRuntimeFixture();
        var original = fixture.CreateSession(OWNER);
        var other = fixture.CreateSession(OWNER);
        var entered = Signal();
        var release = Signal();
        Func<Task>? publication = null;
        await using var controller = CreateController(fixture, new(), () => publication?.Invoke() ?? Task.CompletedTask);
        await controller.InitializeAsync();
        await controller.SelectSessionAsync(original.Id);
        controller.TakeFocusIntent();
        controller.SetDraft("  frozen command  ");
        publication = async () => { entered.TrySetResult(); await release.Task; };
        var start = controller.StartCommandAsync();
        await WaitAsync(entered.Task);
        try
        {
            controller.SetDraft("newer unsent draft");
            await controller.SelectSessionAsync(other.Id);
            await controller.CreateSessionAsync();
            await controller.ClearOutputAsync();
            Assert.Equal(original.Id, controller.SelectedSessionId);
            Assert.Empty(fixture.Factory.Processes);
        }
        finally { publication = null; release.TrySetResult(); }
        await WaitAsync(start);
        var process = Assert.Single(fixture.Factory.Processes);
        await WaitAsync(process.Started);
        Assert.Equal("frozen command", fixture.SessionService.GetSession(OWNER, original.Id).ActiveCommand?.Command);
        Assert.Null(fixture.SessionService.GetSession(OWNER, other.Id).ActiveCommand);
        Assert.Equal("newer unsent draft", controller.Draft);
        process.Complete();
        await WaitForIdleAsync(fixture, original.Id);
        await controller.RefreshAsync();
        Assert.Null(controller.TakeFocusIntent());
    }

    [Fact]
    public async Task DisposeAsync_WhenCommandIsRunning_ShouldRetireVisitAndLetNewVisitObserveAndCancelRealRun()
    {
        await using var fixture = new TerminalRuntimeFixture();
        var session = fixture.CreateSession(OWNER);
        var first = CreateController(fixture, new());
        await first.InitializeAsync();
        first.SetDraft("retained host command");
        await first.StartCommandAsync();
        var process = Assert.Single(fixture.Factory.Processes);
        await WaitAsync(process.Started);
        process.Emit("output from the host-owned run");
        await UntilAsync(() => fixture.SessionService.GetSession(OWNER, session.Id).Output.Any(x => x.Text == "output from the host-owned run"));
        await WaitAsync(first.DisposeAsync().AsTask());
        Assert.Equal(0, process.KillCount);
        Assert.True(fixture.SessionService.GetSession(OWNER, session.Id).IsRunning);

        await using var second = CreateController(fixture, new());
        await second.InitializeAsync();
        Assert.True(second.IsRunning);
        Assert.True(second.CanCancel);
        second.SetDraft("must stay unsent");
        await second.StartCommandAsync();
        Assert.Single(fixture.Factory.Processes);
        Assert.Equal("must stay unsent", second.Draft);
        Assert.Contains(second.ActiveSession!.Output, x => x.Text == "output from the host-owned run");
        await second.CancelCommandAsync();
        await WaitAsync(process.KillRequested);
        Assert.True(second.IsRunning);
        Assert.True(second.ActiveSession!.ActiveCommand!.CancellationRequested);
        process.CompleteRootExit();
        await second.RefreshAsync();
        Assert.True(second.IsRunning);
        process.CompleteOutput();
        await second.RefreshAsync();
        Assert.True(second.IsRunning);
        process.CompleteError();
        await WaitForIdleAsync(fixture, session.Id);
        await second.RefreshAsync();
        Assert.False(second.IsRunning);
        Assert.True(second.ActiveSession!.LastResult!.Canceled);
        Assert.Equal(-1, second.ActiveSession.LastResult.ExitCode);
        Assert.Null(second.TakeFocusIntent());
    }

    [Fact]
    public async Task RefreshAsync_WhenOwnedCommandCompletes_ShouldOfferCurrentFocusOnceButNotForForeignCompletion()
    {
        await using var fixture = new TerminalRuntimeFixture();
        var session = fixture.CreateSession(OWNER);
        await using var controller = CreateController(fixture, new());
        await controller.InitializeAsync();
        controller.SetDraft("owned command");
        await controller.StartCommandAsync();
        var ownedProcess = Assert.Single(fixture.Factory.Processes);
        await WaitAsync(ownedProcess.Started);
        ownedProcess.Complete();
        await WaitForIdleAsync(fixture, session.Id);
        await controller.RefreshAsync();
        var intent = Assert.IsType<TerminalFocusIntent>(controller.TakeFocusIntent());
        Assert.True(controller.IsFocusIntentCurrent(intent));
        Assert.Null(controller.TakeFocusIntent());
        controller.SetDraft("later draft");
        Assert.False(controller.IsFocusIntentCurrent(intent));

        fixture.SessionService.StartCommand(OWNER, session.Id, new() { Command = "other visit command" });
        var foreignProcess = fixture.Factory.Processes.Last();
        await WaitAsync(foreignProcess.Started);
        await controller.RefreshAsync();
        Assert.True(controller.IsRunning);
        foreignProcess.Complete();
        await WaitForIdleAsync(fixture, session.Id);
        await controller.RefreshAsync();
        Assert.Equal("later draft", controller.Draft);
        Assert.Null(controller.TakeFocusIntent());
    }

    [Fact]
    public async Task StartCommandAsync_WhenAnotherVisitWonAdmission_ShouldRetainRejectedDraftAndAcceptedEvidence()
    {
        await using var fixture = new TerminalRuntimeFixture();
        var session = fixture.CreateSession(OWNER);
        await using var controller = CreateController(fixture, new());
        await controller.InitializeAsync();
        fixture.SessionService.StartCommand(OWNER, session.Id, new() { Command = "foreign admitted command" });
        var process = Assert.Single(fixture.Factory.Processes);
        await WaitAsync(process.Started);
        controller.SetDraft("rejected local draft");
        await controller.StartCommandAsync();
        Assert.NotNull(controller.OperationFailure);
        Assert.Equal("rejected local draft", controller.Draft);
        Assert.Single(fixture.Factory.Processes);
        await controller.RefreshAsync();
        Assert.True(controller.IsRunning);
        Assert.Equal("foreign admitted command", controller.ActiveSession?.ActiveCommand?.Command);
        process.Complete();
        await WaitForIdleAsync(fixture, session.Id);
    }

    [Fact]
    public async Task StartCommandAsync_WhenProducerFailsAfterAcceptance_ShouldRetainPreviousCompletedResult()
    {
        await using var fixture = new TerminalRuntimeFixture();
        var session = fixture.CreateSession(OWNER);
        await using var controller = CreateController(fixture, new());
        await controller.InitializeAsync();
        controller.SetDraft("completed command");
        await controller.StartCommandAsync();
        var firstProcess = Assert.Single(fixture.Factory.Processes);
        await WaitAsync(firstProcess.Started);
        firstProcess.Complete(7);
        await WaitForIdleAsync(fixture, session.Id);
        await controller.RefreshAsync();
        var previous = controller.ActiveSession!.LastResult;
        controller.TakeFocusIntent();

        fixture.Factory.NextProcess.StartFailure = new InvalidOperationException("Controlled process start failure.");
        controller.SetDraft("accepted but failed command");
        await controller.StartCommandAsync();
        await WaitForIdleAsync(fixture, session.Id);
        await controller.RefreshAsync();
        Assert.Equal(previous, controller.ActiveSession!.LastResult);
        Assert.Equal("accepted but failed command", controller.ActiveSession.LastExecutionFailure?.Command);
        Assert.NotEqual(previous!.CommandId, controller.ActiveSession.LastExecutionFailure?.CommandId);
        Assert.Null(controller.OperationFailure);
        Assert.Empty(controller.Draft);
    }

    [Fact]
    public async Task RefreshAsync_WhenRealServiceRetires_ShouldRetainPreviousEvidenceAndRejectedDraft()
    {
        await using var fixture = new TerminalRuntimeFixture();
        fixture.CreateSession(OWNER);
        await using var controller = CreateController(fixture, new());
        await controller.InitializeAsync();
        var accepted = controller.Sessions;
        await fixture.SessionService.DisposeAsync();
        await controller.RefreshAsync();
        Assert.NotNull(controller.ReadFailure);
        Assert.Same(accepted, controller.Sessions);
        Assert.True(controller.IsInitialized);
        controller.SetDraft("unadmitted draft");
        await controller.StartCommandAsync();
        Assert.NotNull(controller.OperationFailure);
        Assert.Equal("unadmitted draft", controller.Draft);
        Assert.Empty(fixture.Factory.Processes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task DisposeAsync_WhenPollPublicationIgnoresCancellation_ShouldJoinActualPublicationAndPreventAnotherTick(int faultKind)
    {
        await using var fixture = new TerminalRuntimeFixture();
        fixture.CreateSession(OWNER);
        var clock = new ControlledClock();
        Func<Task>? publication = null;
        var controller = CreateController(fixture, new(), () => publication?.Invoke() ?? Task.CompletedTask, clock);
        await controller.InitializeAsync();
        var entered = Signal();
        var release = Signal();
        Exception failure = faultKind == 2 ? new OperationCanceledException("Unrelated late publication cancellation.") :
            new InvalidOperationException("Controlled late publication failure.");
        publication = async () =>
        {
            entered.TrySetResult();
            await release.Task;
            if (faultKind != 0) throw failure;
        };
        clock.Tick();
        await WaitAsync(entered.Task);
        var retirement = controller.DisposeAsync().AsTask();
        try
        {
            Assert.False(retirement.IsCompleted);
            Assert.Same(retirement, controller.DisposeAsync().AsTask());
            Assert.True(controller.IsRetired);
        }
        finally { release.TrySetResult(); }
        await WaitAsync(retirement);
        Assert.Equal(0, clock.ActiveTimers);
        Assert.Equal(0, clock.Tick());
        Assert.Empty(fixture.Factory.Processes);
        if (faultKind != 0) Assert.Same(failure, controller.NotificationError);
        else Assert.Null(controller.NotificationError);
    }

    [Fact]
    public async Task InitializeAsync_WhenPublicationPrefixStartsRetirement_ShouldJoinItsReservedOperationWithoutBackendAdmission()
    {
        await using var fixture = new TerminalRuntimeFixture();
        var release = Signal();
        Task? retirement = null;
        TerminalVisitController? controller = null;
        controller = CreateController(fixture, new(), () =>
        {
            retirement = controller!.DisposeAsync().AsTask();
            return release.Task;
        });
        var initialization = controller.InitializeAsync();
        try
        {
            Assert.NotNull(retirement);
            Assert.False(retirement.IsCompleted);
            Assert.Same(retirement, controller.DisposeAsync().AsTask());
            Assert.Null(controller.Environment);
            Assert.Empty(fixture.SessionService.GetSessions(OWNER));
        }
        finally { release.TrySetResult(); }
        await WaitAsync(initialization);
        await WaitAsync(retirement);
        Assert.False(controller.IsInitialized);
        Assert.Empty(fixture.SessionService.GetSessions(OWNER));
    }

    [Fact]
    public async Task DisposeAsync_WhenNativePresentationFailsLate_ShouldObserveFailureAndDrainBeforeRelease()
    {
        await using var fixture = new TerminalRuntimeFixture();
        var controller = CreateController(fixture, new());
        var entered = Signal();
        var release = Signal();
        var failure = new InvalidOperationException("Controlled late native focus failure.");
        var presentation = controller.RunPresentationAsync(async () =>
        {
            entered.TrySetResult();
            await release.Task;
            throw failure;
        });
        await WaitAsync(entered.Task);
        var retirement = controller.DisposeAsync().AsTask();
        try { Assert.False(retirement.IsCompleted); }
        finally { release.TrySetResult(); }
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => presentation));
        var aggregate = await Assert.ThrowsAsync<AggregateException>(() => retirement);
        Assert.Contains(failure, aggregate.InnerExceptions);
        var calledAfterRetirement = false;
        await controller.RunPresentationAsync(() => { calledAfterRetirement = true; return Task.CompletedTask; });
        Assert.False(calledAfterRetirement);
    }

    private static TerminalVisitController CreateController(TerminalRuntimeFixture fixture, ControlledStorage storage,
        Func<Task>? publish = null, TimeProvider? clock = null) =>
        new(fixture.Facade, storage, work => work(), publish ?? (() => Task.CompletedTask), clock ?? new ControlledClock());

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static Task WaitAsync(Task task) => task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    private static Task WaitForIdleAsync(TerminalRuntimeFixture fixture, string id) =>
        fixture.WaitForIdleAsync(OWNER, id, TestContext.Current.CancellationToken);

    // Yield probes observe actual producer completion; held process/EOF/publication gates arrange the races.
    private static async Task UntilAsync(Func<bool> condition)
    {
        using var watchdog = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        watchdog.CancelAfter(TimeSpan.FromSeconds(10));
        while (!condition())
        {
            watchdog.Token.ThrowIfCancellationRequested();
            await Task.Yield();
        }
    }

    private sealed class ControlledStorage : IBrowserStorage
    {
        internal string? Value { get; set; } = OWNER;
        internal TaskCompletionSource<string?>? ReadGate { get; init; }
        internal TaskCompletionSource ReadEntered { get; } = Signal();
        internal BrowserStorageWriteFailureKind WriteFailure { get; set; }
        internal string? LastWrittenOwner { get; private set; }
        internal int DisposeCount { get; private set; }
        public async Task<T> GetAsync<T>(string key, T defaultValue, BrowserStorageType storageType = BrowserStorageType.Local)
        {
            ReadEntered.TrySetResult();
            var value = ReadGate is null ? Value : await ReadGate.Task;
            return value is null ? defaultValue : (T)(object)value;
        }
        public Task<BrowserStorageWriteResult> TrySetAsync<T>(string key, T value, BrowserStorageType storageType = BrowserStorageType.Local)
        {
            LastWrittenOwner = value as string;
            if (WriteFailure == BrowserStorageWriteFailureKind.None) Value = LastWrittenOwner;
            return Task.FromResult(new BrowserStorageWriteResult(WriteFailure));
        }
        public Task SetAsync<T>(string key, T value, BrowserStorageType storageType = BrowserStorageType.Local) => throw new NotSupportedException();
        public Task RemoveAsync(string key, BrowserStorageType storageType = BrowserStorageType.Local) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> GetKeysAsync(string category, BrowserStorageType storageType = BrowserStorageType.Local) => throw new NotSupportedException();
        public Task<int> ClearCategoryAsync(string category, BrowserStorageType storageType = BrowserStorageType.Local) => throw new NotSupportedException();
        public ValueTask DisposeAsync() { DisposeCount++; return ValueTask.CompletedTask; }
    }

    private sealed class ControlledClock : TimeProvider
    {
        private readonly object _sync = new();
        private readonly List<ControlledTimer> _timers = [];
        internal int CreatedTimers { get { lock (_sync) return _timers.Count; } }
        internal int ActiveTimers { get { lock (_sync) return _timers.Count(x => x.IsActive); } }
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 13, 0, 0, 0, TimeSpan.Zero);
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ControlledTimer(callback, state, dueTime);
            lock (_sync) _timers.Add(timer);
            return timer;
        }
        internal int Tick()
        {
            ControlledTimer[] timers;
            lock (_sync) timers = _timers.ToArray();
            return timers.Count(x => x.Fire());
        }
        private sealed class ControlledTimer(TimerCallback callback, object? state, TimeSpan dueTime) : ITimer
        {
            private readonly object _sync = new();
            private bool _active = dueTime != Timeout.InfiniteTimeSpan;
            private bool _disposed;
            internal bool IsActive { get { lock (_sync) return _active && !_disposed; } }
            internal bool Fire()
            {
                lock (_sync)
                {
                    if (!_active || _disposed) return false;
                    _active = false;
                }
                callback(state);
                return true;
            }
            public bool Change(TimeSpan value, TimeSpan period)
            {
                lock (_sync)
                {
                    ObjectDisposedException.ThrowIf(_disposed, this);
                    _active = value != Timeout.InfiniteTimeSpan;
                    return true;
                }
            }
            public void Dispose() { lock (_sync) { _disposed = true; _active = false; } }
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }
}
