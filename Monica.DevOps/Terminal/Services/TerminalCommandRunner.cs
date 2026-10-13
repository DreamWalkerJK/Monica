using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monica.DevOps.Terminal.Models;
using Monica.DevOps.Terminal.Services.Support;
using Monica.Modules;

namespace Monica.DevOps.Terminal.Services;

/// <summary>Executes a fresh non-interactive local shell and joins its output observers before returning.</summary>
public sealed class TerminalCommandRunner
{
    private readonly TerminalEnvironmentDetector _environmentDetector;
    private readonly ModuleTerminalOption _options;
    private readonly ILogger<TerminalCommandRunner> _logger;
    private readonly ITerminalProcessFactory _processFactory;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the system-process runner; dependencies are borrowed.</summary>
    public TerminalCommandRunner(TerminalEnvironmentDetector environmentDetector,
        IOptions<ModuleTerminalOption> options, ILogger<TerminalCommandRunner> logger)
        : this(environmentDetector, options, logger, new SystemTerminalProcessFactory()) { }

    internal TerminalCommandRunner(TerminalEnvironmentDetector environmentDetector,
        IOptions<ModuleTerminalOption> options, ILogger<TerminalCommandRunner> logger,
        ITerminalProcessFactory processFactory, TimeProvider? timeProvider = null)
    {
        _environmentDetector = environmentDetector;
        _options = options.Value;
        _logger = logger;
        _processFactory = processFactory;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Executes one command, preserving plaintext output and observing every callback.</summary>
    /// <remarks>Cancellation requests tree termination, then joins the associated process, output readers and
    /// callback consumer with uncanceled cleanup. This does not guarantee all descendants have exited.
    /// Callback failures propagate after drainage. A callback that never completes prevents completion.</remarks>
    /// <param name="command">Command text.</param>
    /// <param name="workingDirectory">Process starting directory.</param>
    /// <param name="timeoutSeconds">Optional timeout clamped to the configured range.</param>
    /// <param name="outputCallback">Optional serial async observer, invoked outside capture locks.</param>
    /// <param name="ct">Explicit execution cancellation, not a UI visit lifetime.</param>
    /// <returns>Result after actual drainage; cancellation and timeout retain exit -1.</returns>
    public async Task<TerminalCommandResult> ExecuteAsync(string command, string workingDirectory,
        int? timeoutSeconds, Func<TerminalOutputLine, CancellationToken, ValueTask>? outputCallback = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
        var timeout = Math.Clamp(timeoutSeconds ?? _options.DefaultTimeoutSeconds, 1, _options.MaxTimeoutSeconds);
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout), _timeProvider);
        using var executionCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        var result = new TerminalCommandResult { Command = command, WorkingDirectory = workingDirectory };
        var startedAt = Stopwatch.GetTimestamp();
        var captureLock = new object();
        var queue = Channel.CreateUnbounded<TerminalOutputLine>(new UnboundedChannelOptions
            { SingleReader = true, AllowSynchronousContinuations = false });
        var accepting = true;
        var failures = new List<Exception>();
        using var process = _processFactory.Create(CreateStartInfo(_environmentDetector.Detect(), command, workingDirectory));
        var consumer = ConsumeAsync();
        process.OutputReceived += Capture;
        var started = false;
        var drained = false;
        try
        {
            _logger.LogInformation("Executing terminal command in {WorkingDirectory}: {Command}", workingDirectory, command);
            started = process.Start();
            if (!started) throw new InvalidOperationException("Failed to start the terminal command process.");
            process.BeginOutputRead();
            await process.WaitForExitAsync(executionCts.Token).ConfigureAwait(false);
            drained = true;
            result.ExitCode = process.ExitCode;
        }
        catch (OperationCanceledException) when (executionCts.IsCancellationRequested)
        {
            result.Canceled = true;
            result.TimedOut = timeoutCts.IsCancellationRequested;
            result.ExitCode = -1;
        }
        catch (Exception exception) { failures.Add(exception); }
        finally
        {
            if (started && !drained)
            {
                try { if (!process.HasExited) process.KillProcessTree(); }
                catch (InvalidOperationException exception)
                {
                    try { if (!process.HasExited) failures.Add(exception); }
                    catch (Exception stateFailure) { failures.Add(exception); failures.Add(stateFailure); }
                }
                catch (Exception exception) { failures.Add(exception); }
                // A canceled wait proves neither exit nor EOF. Failed kill still retains ownership here.
                try { await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false); }
                catch (Exception exception) { failures.Add(exception); }
            }
            if (result.Canceled)
                Capture(TerminalOutputStreams.System, result.TimedOut
                    ? $"Command timed out after {timeout} second(s)." : "Command canceled.");
            lock (captureLock)
            {
                accepting = false;
                queue.Writer.TryComplete();
            }
            process.OutputReceived -= Capture;
            var observerFailure = await consumer.ConfigureAwait(false);
            if (observerFailure is not null) failures.Add(observerFailure);
            result.Duration = Stopwatch.GetElapsedTime(startedAt);
            TrimOutput(result.Output);
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException("Terminal execution or drainage failed.", failures);
        return result;

        void Capture(string stream, string? text)
        {
            if (text is null) return;
            lock (captureLock)
            {
                if (!accepting) return;
                var line = new TerminalOutputLine { Stream = stream, Text = text };
                result.Output.Add(line);
                queue.Writer.TryWrite(line);
            }
        }

        async Task<Exception?> ConsumeAsync()
        {
            Exception? failure = null;
            await foreach (var line in queue.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                if (outputCallback is null) continue;
                try { await outputCallback(line, ct).ConfigureAwait(false); }
                catch (OperationCanceledException exception) when (ct.IsCancellationRequested && exception.CancellationToken == ct) { }
                catch (Exception exception) { failure ??= exception; }
            }
            return failure;
        }
    }

    private static ProcessStartInfo CreateStartInfo(TerminalEnvironmentInfo environment, string command, string directory)
    {
        var info = new ProcessStartInfo { FileName = environment.ShellPath, WorkingDirectory = directory,
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var argument in environment.ShellArguments) info.ArgumentList.Add(argument);
        info.ArgumentList.Add(command);
        return info;
    }

    private void TrimOutput(List<TerminalOutputLine> output)
    {
        var maximum = Math.Max(0, _options.MaxOutputLines);
        if (maximum == 0) { output.Clear(); return; }
        if (output.Count <= maximum) return;
        var removed = output.Count - maximum + 1;
        output.RemoveRange(0, removed);
        output.Insert(0, new TerminalOutputLine { Text = $"Output truncated. Removed {removed} earlier line(s)." });
    }
}
