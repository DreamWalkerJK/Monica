using Microsoft.Extensions.Logging;
using Monica.DevOps.Terminal.Models;
using Monica.DevOps.Terminal.Services;
using Monica.Modules;
using Test.Monica.DevOps.Terminal.Support;
using Xunit;

namespace Test.Monica.DevOps.Terminal.Services;

public sealed class TerminalSessionServiceTests
{
    [Fact]
    public async Task StartCommand_WhenFailureLoggerReentersRetirementAndThrows_ShouldJoinFinalProducerAndObserveBothFailures()
    {
        var logger = new ControlledTerminalLogger();
        var fixture = new TerminalRuntimeFixture(logger: logger);
        Task? retirement = null;
        Task? repeatedRetirement = null;
        var wasRunningDuringLogging = false;
        var retirementWasPendingDuringLogging = false;
        var loggerReached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = fixture.CreateSession();
        var process = fixture.Factory.NextProcess;
        process.StartFailure = new InvalidOperationException("controlled execution failure");
        logger.OnLog = () =>
        {
            wasRunningDuringLogging = fixture.SessionService.GetSession("owner", session.Id).IsRunning;
            retirement = fixture.SessionService.DisposeAsync().AsTask();
            repeatedRetirement = fixture.SessionService.DisposeAsync().AsTask();
            retirementWasPendingDuringLogging = !retirement.IsCompleted;
            loggerReached.TrySetResult();
            throw new IOException("controlled logger failure");
        };
        try
        {
            var acceptance = fixture.SessionService.StartCommand("owner", session.Id, new() { Command = "harmless" });
            Assert.NotEmpty(acceptance.CommandId);
            // Even start failure joins the runner's asynchronous output consumer before failure logging.
            await loggerReached.Task.WaitAsync(TestContext.Current.CancellationToken);
            Assert.True(wasRunningDuringLogging);
            Assert.True(retirementWasPendingDuringLogging);
            Assert.Same(retirement, repeatedRetirement);
            var failure = await Assert.ThrowsAsync<AggregateException>(() => retirement!);
            var failures = failure.Flatten().InnerExceptions;
            Assert.Contains(failures, exception => exception.Message == "controlled execution failure");
            Assert.Contains(failures, exception => exception.Message == "controlled logger failure");
            Assert.Equal(1, logger.Calls);
            Assert.Equal(1, process.DisposeCount);
        }
        finally
        {
            process.Complete();
            try { await fixture.DisposeAsync(); }
            catch (AggregateException) when (retirement is { IsFaulted: true }) { }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteCommandAsync_WhenInitialObserverFails_ShouldReleaseAdmissionAndAllowNextCommand(bool asynchronously)
    {
        await using var fixture = new TerminalRuntimeFixture();
        var session = fixture.CreateSession();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var execution = fixture.SessionService.ExecuteCommandAsync("owner", session.Id,
            new() { Command = "harmless" }, async (_, _) =>
            {
                if (asynchronously) await release.Task;
                throw new InvalidOperationException("input observer failed");
            }, TestContext.Current.CancellationToken);
        if (asynchronously) Assert.True(fixture.SessionService.GetSession("owner", session.Id).IsRunning);
        release.TrySetResult();
        await Assert.ThrowsAsync<InvalidOperationException>(() => execution);
        var failed = fixture.SessionService.GetSession("owner", session.Id);
        Assert.False(failed.IsRunning);
        Assert.Equal("input observer failed", failed.LastExecutionFailure!.Message);
        Assert.Equal(new[] { "harmless" }, failed.History);
        Assert.Empty(fixture.Factory.Processes);
        var next = await fixture.SessionService.ExecuteCommandAsync("owner", session.Id, new() { Command = "cd ." }, ct: TestContext.Current.CancellationToken);
        Assert.Equal(0, next.ExitCode);
        Assert.Null(fixture.SessionService.GetSession("owner", session.Id).LastExecutionFailure);
    }

    [Fact]
    public async Task CancelCommandAsync_WhenProcessHasNotDrained_ShouldKeepAdmissionAndRejectStaleCommand()
    {
        await using var fixture = new TerminalRuntimeFixture();
        var session = fixture.CreateSession();
        var process = fixture.Factory.NextProcess;
        var acceptance = fixture.SessionService.StartCommand("owner", session.Id, new() { Command = "first" });
        var canceled = await fixture.SessionService.CancelCommandAsync("owner", session.Id, acceptance.CommandId);
        Assert.True(canceled.IsRunning);
        Assert.True(canceled.ActiveCommand!.CancellationRequested);
        await process.KillRequested.WaitAsync(TestContext.Current.CancellationToken);
        Assert.Throws<InvalidOperationException>(() => fixture.SessionService.StartCommand("owner", session.Id, new() { Command = "second" }));
        process.CompleteRootExit();
        process.CompleteOutput();
        Assert.True(fixture.SessionService.GetSession("owner", session.Id).IsRunning);
        process.CompleteError();
        var complete = await fixture.WaitForIdleAsync("owner", session.Id, TestContext.Current.CancellationToken);
        Assert.True(complete.LastResult!.Canceled);
        var secondProcess = fixture.Factory.NextProcess;
        var second = fixture.SessionService.StartCommand("owner", session.Id, new() { Command = "second" });
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.SessionService.CancelCommandAsync("owner", session.Id, acceptance.CommandId));
        Assert.NotEqual(acceptance.CommandId, second.CommandId);
        Assert.Equal(0, secondProcess.KillCount);
        secondProcess.Complete();
    }

    [Fact]
    public async Task CancelCommandAsync_WhenTokenCallbackRemainsPending_ShouldJoinRequestBeforeReleasingCtsAndAdmission()
    {
        await using var fixture = new TerminalRuntimeFixture();
        var session = fixture.CreateSession();
        var process = fixture.Factory.NextProcess;
        var registered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        CancellationTokenRegistration registration = default;
        var execution = fixture.SessionService.ExecuteCommandAsync("owner", session.Id, new() { Command = "harmless" }, (line, token) =>
        {
            if (line.Stream == TerminalOutputStreams.Input)
            {
                registration = token.Register(() => { callbackEntered.TrySetResult(); release.Wait(TestContext.Current.CancellationToken); });
                registered.TrySetResult();
            }
            return ValueTask.CompletedTask;
        }, TestContext.Current.CancellationToken);
        try
        {
            await registered.Task.WaitAsync(TestContext.Current.CancellationToken);
            var commandId = fixture.SessionService.GetSession("owner", session.Id).ActiveCommand!.CommandId;
            var request = fixture.SessionService.CancelCommandAsync("owner", session.Id, commandId);
            await callbackEntered.Task.WaitAsync(TestContext.Current.CancellationToken);
            process.Complete();
            Assert.False(request.IsCompleted);
            Assert.False(execution.IsCompleted);
            Assert.True(fixture.SessionService.GetSession("owner", session.Id).IsRunning);
            release.Set();
            await request;
            await execution;
            Assert.False(fixture.SessionService.GetSession("owner", session.Id).IsRunning);
        }
        finally { release.Set(); process.Complete(); registration.Dispose(); }
    }

    [Fact]
    public async Task GetSession_WhenLaterCommandOrCallerMutatesResult_ShouldRetainDetachedPriorCompletion()
    {
        await using var fixture = new TerminalRuntimeFixture();
        var session = fixture.CreateSession();
        var result = await fixture.SessionService.ExecuteCommandAsync("owner", session.Id, new() { Command = "cd ." }, ct: TestContext.Current.CancellationToken);
        var retained = fixture.SessionService.GetSession("owner", session.Id);
        result.Output.Clear();
        result.Command = "caller mutation";
        var process = fixture.Factory.NextProcess;
        fixture.SessionService.StartCommand("owner", session.Id, new() { Command = "next" });
        var running = fixture.SessionService.GetSession("owner", session.Id);
        Assert.True(running.IsRunning);
        Assert.Equal("cd .", running.LastResult!.Command);
        Assert.Single(running.LastResult.Output);
        Assert.False(retained.IsRunning);
        Assert.Equal(new[] { "cd ." }, retained.History);
        process.Complete();
    }

    [Fact]
    public async Task ClearOutput_WhenCdAndHistoryRetained_ShouldClearOnlyTranscriptResultAndFailure()
    {
        await using var fixture = new TerminalRuntimeFixture(new ModuleTerminalOption { MaxHistoryEntries = 2, MaxOutputLines = 2 });
        var session = fixture.CreateSession();
        var child = Path.Combine(fixture.Root, "child");
        Directory.CreateDirectory(child);
        await fixture.SessionService.ExecuteCommandAsync("owner", session.Id, new() { Command = "cd child" }, ct: TestContext.Current.CancellationToken);
        await fixture.SessionService.ExecuteCommandAsync("owner", session.Id, new() { Command = "cd ." }, ct: TestContext.Current.CancellationToken);
        await fixture.SessionService.ExecuteCommandAsync("owner", session.Id, new() { Command = "cd ." }, ct: TestContext.Current.CancellationToken);
        var retained = fixture.SessionService.GetSession("owner", session.Id);
        Assert.Equal(child, retained.WorkingDirectory);
        Assert.Equal(new[] { "cd .", "cd ." }, retained.History);
        Assert.Equal(2, retained.Output.Length);
        Assert.Contains(retained.Output, line => line.Text.Contains("truncated", StringComparison.Ordinal));
        fixture.SessionService.ClearOutput("owner", session.Id);
        var cleared = fixture.SessionService.GetSession("owner", session.Id);
        Assert.Equal(child, cleared.WorkingDirectory);
        Assert.Equal(retained.History.ToArray(), cleared.History.ToArray());
        Assert.Empty(cleared.Output);
        Assert.Null(cleared.LastResult);
        Assert.Null(cleared.LastExecutionFailure);
        Assert.Equal(2, retained.Output.Length);
    }

    private sealed class ControlledTerminalLogger : ILogger<TerminalSessionService>
    {
        public Action OnLog { get; set; } = () => { };
        public int Calls { get; private set; }
        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Calls++;
            OnLog();
        }
    }
}
