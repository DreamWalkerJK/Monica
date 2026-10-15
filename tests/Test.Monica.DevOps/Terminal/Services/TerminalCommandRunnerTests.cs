using Monica.DevOps.Terminal.Models;
using Test.Monica.DevOps.Terminal.Support;
using Xunit;

namespace Test.Monica.DevOps.Terminal.Services;

public sealed class TerminalCommandRunnerTests
{
    [Fact]
    public async Task ExecuteAsync_WhenTimeoutAndCallerCancellationMeet_ShouldPreserveTimeoutPrecedenceAfterDrainage()
    {
        var clock = new ControlledTerminalTimeProvider();
        await using var fixture = new TerminalRuntimeFixture(timeProvider: clock);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var process = fixture.Factory.NextProcess;
        var execution = fixture.Runner.ExecuteAsync("harmless", fixture.Root, 5000, ct: cancellation.Token);
        Assert.Equal(TimeSpan.FromSeconds(900), clock.LastDueTime);
        clock.FireTimers();
        cancellation.Cancel();
        await process.KillRequested.WaitAsync(TestContext.Current.CancellationToken);
        Assert.False(execution.IsCompleted);
        process.Emit("last timeout output");
        process.Complete();
        var result = await execution;
        Assert.True(result.TimedOut);
        Assert.True(result.Canceled);
        Assert.Equal(-1, result.ExitCode);
        Assert.Contains(result.Output, line => line.Text == "last timeout output");
        Assert.Contains(result.Output, line => line.Text.Contains("timed out", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WhenObserverFailsLate_ShouldJoinObserverAndDisposeAfterExitAndBothEofs()
    {
        await using var fixture = new TerminalRuntimeFixture();
        var process = fixture.Factory.NextProcess;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var execution = fixture.Runner.ExecuteAsync("harmless", fixture.Root, null, async (_, _) =>
        {
            entered.TrySetResult();
            await release.Task;
            throw new InvalidOperationException("late observer failure");
        }, TestContext.Current.CancellationToken);
        try
        {
            process.Emit("last output");
            await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
            process.CompleteRootExit();
            process.CompleteOutput();
            Assert.False(execution.IsCompleted);
            process.CompleteError();
            Assert.False(execution.IsCompleted);
            Assert.Equal(0, process.DisposeCount);
            release.TrySetResult();
            var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => execution);
            Assert.Equal("late observer failure", failure.Message);
            Assert.Equal(1, process.DisposeCount);
        }
        finally { release.TrySetResult(); process.Complete(); }
    }

    [Fact]
    public async Task ExecuteAsync_WhenSynchronousObserverThrows_ShouldKeepDrainingLaterFramesAndObserveFailure()
    {
        await using var fixture = new TerminalRuntimeFixture();
        var process = fixture.Factory.NextProcess;
        var observed = new List<string>();
        var execution = fixture.Runner.ExecuteAsync("harmless", fixture.Root, null, (line, _) =>
        {
            observed.Add(line.Text);
            throw new InvalidOperationException("synchronous observer failure");
        }, TestContext.Current.CancellationToken);
        process.Emit("first");
        process.Emit("second", TerminalOutputStreams.Stderr);
        process.Complete();
        await Assert.ThrowsAsync<InvalidOperationException>(() => execution);
        Assert.Equal(new[] { "first", "second" }, observed);
        Assert.Equal(1, process.DisposeCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCanceled_ShouldWaitForRootBothEofsAndObserverBeforeReturning()
    {
        await using var fixture = new TerminalRuntimeFixture();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var process = fixture.Factory.NextProcess;
        var observerEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var execution = fixture.Runner.ExecuteAsync("harmless", fixture.Root, null, async (line, _) =>
        {
            if (line.Stream != TerminalOutputStreams.Stdout) return;
            observerEntered.TrySetResult();
            await release.Task;
        }, cancellation.Token);
        try
        {
            process.Emit("before cancellation");
            await observerEntered.Task.WaitAsync(TestContext.Current.CancellationToken);
            cancellation.Cancel();
            await process.KillRequested.WaitAsync(TestContext.Current.CancellationToken);
            Assert.False(execution.IsCompleted);
            process.CompleteRootExit();
            process.Emit("late captured line", TerminalOutputStreams.Stderr);
            process.CompleteOutput();
            Assert.False(execution.IsCompleted);
            process.CompleteError();
            Assert.False(execution.IsCompleted);
            release.TrySetResult();
            var result = await execution;
            Assert.True(result.Canceled);
            Assert.False(result.TimedOut);
            Assert.Equal(-1, result.ExitCode);
            Assert.Contains(result.Output, line => line.Text == "late captured line");
            Assert.Contains(result.Output, line => line.Text == "Command canceled.");
            Assert.Equal(1, process.DisposeCount);
        }
        finally { release.TrySetResult(); process.Complete(); }
    }

    [Fact]
    public async Task ExecuteAsync_WhenKillFails_ShouldRetainProcessUntilActualExitAndReportFailure()
    {
        await using var fixture = new TerminalRuntimeFixture();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var process = fixture.Factory.NextProcess;
        process.KillFailure = new InvalidOperationException("kill denied");
        var execution = fixture.Runner.ExecuteAsync("harmless", fixture.Root, null, ct: cancellation.Token);
        cancellation.Cancel();
        await process.KillRequested.WaitAsync(TestContext.Current.CancellationToken);
        Assert.False(execution.IsCompleted);
        Assert.Equal(0, process.DisposeCount);
        process.Complete();
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => execution);
        Assert.Equal("kill denied", failure.Message);
        Assert.Equal(1, process.DisposeCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenReaderInitializationFails_ShouldKillAndJoinStartedProcess()
    {
        await using var fixture = new TerminalRuntimeFixture();
        var process = fixture.Factory.NextProcess;
        process.ReadFailure = new InvalidOperationException("reader failed");
        var execution = fixture.Runner.ExecuteAsync("harmless", fixture.Root, null, ct: TestContext.Current.CancellationToken);
        await process.KillRequested.WaitAsync(TestContext.Current.CancellationToken);
        Assert.False(execution.IsCompleted);
        process.Complete();
        await Assert.ThrowsAsync<InvalidOperationException>(() => execution);
        Assert.Equal(1, process.DisposeCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOutputEnds_ShouldPreserveFramesAndUseRealFreshShellConfiguration()
    {
        await using var fixture = new TerminalRuntimeFixture();
        var process = fixture.Factory.NextProcess;
        var execution = fixture.Runner.ExecuteAsync("harmless", fixture.Root, null, ct: TestContext.Current.CancellationToken);
        process.Emit("");
        process.Emit("  whitespace  ");
        process.Emit("error stream", TerminalOutputStreams.Stderr);
        process.Complete(7);
        var result = await execution;
        Assert.Equal(7, result.ExitCode);
        Assert.Equal(new[] { "", "  whitespace  ", "error stream" }, result.Output.Select(line => line.Text));
        var info = Assert.IsType<System.Diagnostics.ProcessStartInfo>(process.StartInfo);
        Assert.Equal(fixture.Root, info.WorkingDirectory);
        Assert.Equal(fixture.Detector.Detect().ShellPath, info.FileName);
        Assert.Equal("harmless", info.ArgumentList.Last());
        Assert.False(info.UseShellExecute);
        Assert.True(info.RedirectStandardOutput);
        Assert.True(info.RedirectStandardError);
        Assert.False(info.RedirectStandardInput);
        Assert.True(info.CreateNoWindow);
    }
}
