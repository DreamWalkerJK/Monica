using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Monica.DevOps.Terminal.Models;
using Monica.DevOps.Terminal.Services.Support;
using Monica.Modules;

namespace Monica.DevOps.Terminal.Services;

/// <summary>Owns host-memory sessions and admitted commands independently of UI visit lifetimes.</summary>
/// <remarks>DI owns async disposal. Owner keys partition state, not authorization. Disposal requests
/// cancellation and joins actual command/output/callback work, including commands of closed sessions.</remarks>
public sealed class TerminalSessionService(TerminalCommandRunner commandRunner,
    TerminalEnvironmentDetector environmentDetector, IOptions<ModuleTerminalOption> options,
    ILogger<TerminalSessionService>? logger = null) : IAsyncDisposable
{
    private readonly object _lifetimeLock = new();
    private readonly ConcurrentDictionary<string, OwnerState> _owners = [];
    private readonly HashSet<RunningCommand> _runs = [];
    private readonly ModuleTerminalOption _options = options.Value;
    private readonly ILogger<TerminalSessionService> _logger = logger ?? NullLogger<TerminalSessionService>.Instance;
    private bool _retired;
    private Task? _disposal;
    private int MaxHistory => Math.Max(0, _options.MaxHistoryEntries);
    private int MaxOutput => Math.Max(0, _options.MaxOutputLines);

    /// <summary>Gets detached immutable host environment evidence.</summary>
    public TerminalEnvironmentSnapshot GetEnvironment() => TerminalEnvironmentSnapshot.From(environmentDetector.Detect());
    /// <summary>Gets immutable owner-scoped snapshots sorted by latest activity.</summary>
    public IReadOnlyList<TerminalSessionSnapshot> GetSessions(string ownerKey) => GetOwner(ownerKey).Sessions.Values
        .Select(session => session.Snapshot()).OrderByDescending(session => session.LastActivityAt).ToList();
    /// <summary>Gets a detached active-session snapshot, or throws when the owner has no such session.</summary>
    public TerminalSessionSnapshot GetSession(string ownerKey, string sessionId) => FindSession(ownerKey, sessionId).Snapshot();
    /// <summary>Creates an owner-scoped session; invalid directories fall back to the host default.</summary>
    public TerminalSessionSnapshot CreateSession(string ownerKey, TerminalSessionCreateRequest? request = null)
    {
        var owner = GetOwner(ownerKey);
        var name = string.IsNullOrWhiteSpace(request?.Name)
            ? $"Terminal {Interlocked.Increment(ref owner.CreatedCount)}" : request.Name.Trim();
        var session = new SessionState(name, NormalizeDirectory(request?.WorkingDirectory));
        lock (_lifetimeLock)
        {
            ObjectDisposedException.ThrowIf(_retired, this);
            owner.Sessions.TryAdd(session.Id, session);
        }
        return session.Snapshot();
    }
    /// <summary>Accepts a command that continues after a UI visit leaves; acceptance is not completion.</summary>
    public TerminalCommandAcceptance StartCommand(string ownerKey, string sessionId, TerminalCommandRequest request)
    {
        var (session, run) = Admit(ownerKey, sessionId, request, null, CancellationToken.None);
        return new(run.Id, session.Snapshot());
    }
    /// <summary>Admits and awaits the same host-owned producer used by StartCommand.</summary>
    /// <remarks>The caller token explicitly cancels execution. Output observers are serialized, awaited and
    /// drained; observer faults propagate after cleanup. They must not await their own command's completion.</remarks>
    public async Task<TerminalCommandResult> ExecuteCommandAsync(string ownerKey, string sessionId,
        TerminalCommandRequest request, Func<TerminalOutputLine, CancellationToken, ValueTask>? outputCallback = null,
        CancellationToken ct = default)
    {
        var (_, run) = Admit(ownerKey, sessionId, request, outputCallback, ct);
        var outcome = await run.Completion.ConfigureAwait(false);
        outcome.Failure?.Throw();
        return outcome.Result!;
    }
    /// <summary>Requests cancellation only for the captured command, returning current detached evidence.</summary>
    /// <remarks>Request completion joins token callbacks, not process/output drainage. A stale command fails;
    /// completion after validation safely turns cancellation of that captured run into a no-op.</remarks>
    public async Task<TerminalSessionSnapshot> CancelCommandAsync(string ownerKey, string sessionId, string expectedCommandId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedCommandId);
        var session = FindSession(ownerKey, sessionId);
        var run = session.CaptureRun(expectedCommandId);
        var request = run.RequestCancellationAsync();
        session.MarkCancellation(run);
        await request.ConfigureAwait(false);
        return session.Snapshot();
    }
    /// <summary>Removes a session and requests active cancellation; missing sessions succeed idempotently.</summary>
    /// <remarks>The receipt does not prove process exit. The host retains and drains the removed session's run.</remarks>
    public async Task CloseSessionAsync(string ownerKey, string sessionId)
    {
        var owner = GetOwner(ownerKey);
        if (!owner.Sessions.TryRemove(sessionId, out var session)) return;
        var run = session.Close();
        if (run is not null) await run.RequestCancellationAsync().ConfigureAwait(false);
    }
    /// <summary>Clears retained transcript, latest result and failed-attempt evidence, preserving history and directory.</summary>
    public void ClearOutput(string ownerKey, string sessionId) => FindSession(ownerKey, sessionId).Clear();

    private (SessionState Session, RunningCommand Run) Admit(string ownerKey, string sessionId,
        TerminalCommandRequest request, Func<TerminalOutputLine, CancellationToken, ValueTask>? callback, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var command = request.Command.Trim();
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        var session = FindSession(ownerKey, sessionId);
        RunningCommand run;
        lock (_lifetimeLock)
        {
            ObjectDisposedException.ThrowIf(_retired, this);
            run = session.Begin(command, request.TimeoutSeconds, ct);
            _runs.Add(run);
        }
        // Reserve and publish observation before external producer code can reenter host retirement.
        run.PublishObservation(ObserveProducerAsync(session, run));
        run.PublishProducer(ProduceAsync(session, run, callback));
        return (session, run);
    }

    private async Task<ExecutionOutcome> ProduceAsync(SessionState session, RunningCommand run,
        Func<TerminalOutputLine, CancellationToken, ValueTask>? callback)
    {
        TerminalCommandResult? result = null;
        Exception? failure = null;
        try
        {
            var input = session.RecordInput(run.Command, MaxHistory, MaxOutput);
            if (callback is not null)
                await callback(input, run.Token).ConfigureAwait(false);
            result = ApplyChangeDirectory(session, run);
            if (result is not null)
            {
                foreach (var line in result.Output) session.AddOutput(line, MaxOutput);
            }
            else
            {
                result = await commandRunner.ExecuteAsync(run.Command, run.Directory, run.TimeoutSeconds,
                    async (line, token) =>
                    {
                        session.AddOutput(line, MaxOutput);
                        if (callback is not null) await callback(line, token).ConfigureAwait(false);
                    }, run.Token).ConfigureAwait(false);
            }
        }
        catch (Exception exception) { failure = exception; }
        finally
        {
            try { await run.FinishAsync().ConfigureAwait(false); }
            catch (Exception exception) { failure = failure is null ? exception : new AggregateException(failure, exception); }
        }
        if (failure is not null)
        {
            try { _logger.LogError(failure, "Terminal command {CommandId} failed in session {SessionId}", run.Id, session.Id); }
            catch (Exception loggingFailure) { failure = new AggregateException(failure, loggingFailure); }
        }
        return new(result, failure is null ? null : ExceptionDispatchInfo.Capture(failure));
    }

    private async Task<ExecutionOutcome> ObserveProducerAsync(SessionState session, RunningCommand run)
    {
        ExecutionOutcome outcome;
        try { outcome = await run.ProducerCompletion.ConfigureAwait(false); }
        catch (Exception failure) { outcome = new(null, ExceptionDispatchInfo.Capture(failure)); }
        try { session.Complete(run, outcome.Result, outcome.Failure?.SourceException); }
        finally { lock (_lifetimeLock) _runs.Remove(run); }
        return outcome;
    }

    /// <summary>Stops server admission, requests cancellation and joins every actual producer before releasing state.</summary>
    public ValueTask DisposeAsync()
    {
        TaskCompletionSource reserved;
        RunningCommand[] runs;
        Task completion;
        lock (_lifetimeLock)
        {
            if (_disposal is not null) return new(_disposal);
            _retired = true;
            runs = [.. _runs];
            reserved = new(TaskCreationOptions.RunContinuationsAsynchronously);
            completion = _disposal = reserved.Task;
        }
        _ = DisposeCoreAsync(runs, reserved);
        return new(completion);
    }

    private async Task DisposeCoreAsync(RunningCommand[] runs, TaskCompletionSource completion)
    {
        var failures = new List<Exception>();
        var cancellations = runs.Select(run => run.RequestCancellationAsync()).ToArray();
        foreach (var request in cancellations)
        {
            try { await request.ConfigureAwait(false); }
            catch (Exception exception) { failures.Add(exception); }
        }
        foreach (var run in runs)
        {
            try
            {
                var outcome = await run.Completion.ConfigureAwait(false);
                if (outcome.Failure is not null) failures.Add(outcome.Failure.SourceException);
            }
            catch (Exception exception) { failures.Add(exception); }
        }
        _owners.Clear();
        if (failures.Count == 0) completion.TrySetResult();
        else completion.TrySetException(new AggregateException("Terminal host retirement failed.", failures));
    }

    private OwnerState GetOwner(string ownerKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerKey);
        lock (_lifetimeLock)
        {
            ObjectDisposedException.ThrowIf(_retired, this);
            return _owners.GetOrAdd(ownerKey.Trim(), static _ => new());
        }
    }
    private SessionState FindSession(string ownerKey, string sessionId) => GetOwner(ownerKey).Sessions.TryGetValue(sessionId, out var session)
        ? session : throw new KeyNotFoundException($"Terminal session '{sessionId}' was not found.");
    private string NormalizeDirectory(string? directory)
    {
        var fallback = environmentDetector.Detect().DefaultWorkingDirectory;
        if (string.IsNullOrWhiteSpace(directory)) return fallback;
        try { var full = Path.GetFullPath(directory.Trim().Trim('"')); return Directory.Exists(full) ? full : fallback; }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException) { return fallback; }
    }
    private static TerminalCommandResult? ApplyChangeDirectory(SessionState session, RunningCommand run)
    {
        string target;
        if (string.Equals(run.Command, "cd", StringComparison.OrdinalIgnoreCase))
            target = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        else if (run.Command.StartsWith("cd ", StringComparison.OrdinalIgnoreCase)) target = run.Command[3..].Trim().Trim('"', '\'');
        else return null;
        if (string.IsNullOrWhiteSpace(target) || target == "~") target = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        else if (target.StartsWith("~/", StringComparison.Ordinal)) target = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), target[2..]);
        var directory = Path.GetFullPath(Path.IsPathRooted(target) ? target : Path.Combine(run.Directory, target));
        var result = new TerminalCommandResult { Command = run.Command, WorkingDirectory = run.Directory };
        if (!Directory.Exists(directory))
        {
            result.ExitCode = 1;
            result.Output.Add(new TerminalOutputLine { Stream = TerminalOutputStreams.Stderr, Text = $"Directory not found: {directory}" });
        }
        else
        {
            session.SetDirectory(directory);
            result.WorkingDirectory = directory;
            result.Output.Add(new TerminalOutputLine { Text = $"Working directory changed to {directory}" });
        }
        return result;
    }

    private sealed class OwnerState
    {
        public readonly ConcurrentDictionary<string, SessionState> Sessions = [];
        public int CreatedCount;
    }
    private sealed record ExecutionOutcome(TerminalCommandResult? Result, ExceptionDispatchInfo? Failure);

    private sealed class RunningCommand
    {
        private readonly object _sync = new();
        private readonly CancellationTokenSource _cancellation;
        private readonly TaskCompletionSource<Task<ExecutionOutcome>> _producer = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<Task<ExecutionOutcome>> _observation = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Task<ExecutionOutcome> _producerCompletion;
        private readonly Task<ExecutionOutcome> _completion;
        private Task? _request;
        private bool _finishing;
        private int _requested;
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public RunningCommand(string command, string directory, int? timeoutSeconds, CancellationToken ct)
        {
            Command = command;
            Directory = directory;
            TimeoutSeconds = timeoutSeconds;
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
            Token = _cancellation.Token;
            _producerCompletion = _producer.Task.Unwrap();
            _completion = _observation.Task.Unwrap();
        }
        public string Command { get; }
        public string Directory { get; }
        public int? TimeoutSeconds { get; }
        public CancellationToken Token { get; }
        public bool CancellationRequested => Volatile.Read(ref _requested) != 0;
        public Task<ExecutionOutcome> Completion => _completion;
        public Task<ExecutionOutcome> ProducerCompletion => _producerCompletion;
        public void PublishProducer(Task<ExecutionOutcome> producer) => _producer.SetResult(producer);
        public void PublishObservation(Task<ExecutionOutcome> observation) => _observation.SetResult(observation);
        public Task RequestCancellationAsync()
        {
            TaskCompletionSource reservation;
            lock (_sync)
            {
                if (_request is not null) return _request;
                if (_finishing) return Task.CompletedTask;
                Volatile.Write(ref _requested, 1);
                reservation = new(TaskCreationOptions.RunContinuationsAsynchronously);
                _request = reservation.Task;
            }
            _ = CancelCoreAsync(reservation);
            return reservation.Task;
        }
        private async Task CancelCoreAsync(TaskCompletionSource reservation)
        {
            try { await _cancellation.CancelAsync().ConfigureAwait(false); reservation.TrySetResult(); }
            catch (Exception exception) { reservation.TrySetException(exception); }
        }
        public async Task FinishAsync()
        {
            Task? request;
            lock (_sync) { _finishing = true; request = _request; }
            try { if (request is not null) await request.ConfigureAwait(false); }
            finally { _cancellation.Dispose(); }
        }
    }

    private sealed class SessionState(string name, string directory)
    {
        private readonly object _sync = new();
        private readonly List<string> _history = [];
        private readonly List<TerminalOutputLine> _output = [];
        private string _directory = directory;
        private DateTimeOffset _activity = DateTimeOffset.Now;
        private long _revision;
        private RunningCommand? _run;
        private TerminalCommandResultSnapshot? _result;
        private TerminalExecutionFailureSnapshot? _failure;
        private bool _closed;
        public string Id { get; } = Guid.NewGuid().ToString("N");
        private DateTimeOffset CreatedAt { get; } = DateTimeOffset.Now;
        public TerminalSessionSnapshot Snapshot()
        {
            lock (_sync) return new(Id, name, _directory, CreatedAt, _activity, _revision,
                [.. _history], [.. _output], _run is null ? null : new(_run.Id, _run.Command, _run.Directory, _run.CancellationRequested), _result, _failure);
        }
        public RunningCommand Begin(string command, int? timeout, CancellationToken ct)
        {
            lock (_sync)
            {
                if (_closed) throw new InvalidOperationException("This terminal session is closed.");
                if (_run is not null) throw new InvalidOperationException("A command is already running in this terminal session.");
                _run = new(command, _directory, timeout, ct);
                _failure = null;
                Touch();
                return _run;
            }
        }
        public RunningCommand CaptureRun(string id)
        {
            lock (_sync)
            {
                if (_run is null || _run.Id != id) throw new InvalidOperationException("The requested terminal command is no longer active.");
                return _run;
            }
        }
        public void MarkCancellation(RunningCommand run) { lock (_sync) { if (ReferenceEquals(_run, run)) Touch(); } }
        public RunningCommand? Close() { lock (_sync) { _closed = true; return _run; } }
        public TerminalOutputLine RecordInput(string command, int maxHistory, int maxOutput)
        {
            var input = new TerminalOutputLine { Stream = TerminalOutputStreams.Input, Text = command };
            lock (_sync)
            {
                if (maxHistory > 0)
                {
                    _history.Add(command);
                    if (_history.Count > maxHistory) _history.RemoveRange(0, _history.Count - maxHistory);
                }
                AddOutputCore(input, maxOutput);
                Touch();
                return input;
            }
        }
        public void AddOutput(TerminalOutputLine line, int maximum) { lock (_sync) { AddOutputCore(line, maximum); Touch(); } }
        private void AddOutputCore(TerminalOutputLine line, int maximum)
        {
            if (maximum <= 0) return;
            _output.Add(line);
            if (_output.Count <= maximum) return;
            var removed = _output.Count - maximum + 1;
            _output.RemoveRange(0, removed);
            _output.Insert(0, new TerminalOutputLine { Text = $"Session output truncated. Removed {removed} earlier line(s)." });
        }
        public void SetDirectory(string directory) { lock (_sync) { _directory = directory; Touch(); } }
        public void Clear() { lock (_sync) { _output.Clear(); _result = null; _failure = null; Touch(); } }
        public void Complete(RunningCommand run, TerminalCommandResult? result, Exception? failure)
        {
            lock (_sync)
            {
                if (failure is not null)
                    _failure = new(run.Id, run.Command, run.Directory, failure.Message, DateTimeOffset.Now);
                else if (result is not null)
                    _result = new(run.Id, result.Command, result.WorkingDirectory, result.ExitCode, result.Canceled, result.TimedOut, result.Duration, [.. result.Output]);
                if (ReferenceEquals(_run, run)) _run = null;
                Touch();
            }
        }
        private void Touch() { _activity = DateTimeOffset.Now; _revision++; }
    }
}
