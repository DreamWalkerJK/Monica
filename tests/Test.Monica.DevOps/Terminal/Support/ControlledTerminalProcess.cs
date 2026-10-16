using System.Diagnostics;
using Monica.DevOps.Terminal.Models;
using Monica.DevOps.Terminal.Services.Support;

namespace Test.Monica.DevOps.Terminal.Support;

internal sealed class ControlledTerminalProcessFactory : ITerminalProcessFactory
{
    public ControlledTerminalProcess NextProcess { get; set; } = new();
    public List<ControlledTerminalProcess> Processes { get; } = [];
    public ITerminalProcess Create(ProcessStartInfo startInfo)
    {
        var process = NextProcess;
        NextProcess = new();
        process.StartInfo = startInfo;
        Processes.Add(process);
        return process;
    }
}

internal sealed class ControlledTerminalProcess : ITerminalProcess
{
    private readonly TaskCompletionSource _started = NewCompletion();
    private readonly TaskCompletionSource _waiting = NewCompletion();
    private readonly TaskCompletionSource _killRequested = NewCompletion();
    private readonly TaskCompletionSource _rootExit = NewCompletion();
    private readonly TaskCompletionSource _outputEnd = NewCompletion();
    private readonly TaskCompletionSource _errorEnd = NewCompletion();
    private readonly TaskCompletionSource _disposed = NewCompletion();
    public event Action<string, string?>? OutputReceived;
    public ProcessStartInfo? StartInfo { get; set; }
    public Task Started => _started.Task;
    public Task WaitStarted => _waiting.Task;
    public Task KillRequested => _killRequested.Task;
    public Task Disposed => _disposed.Task;
    public Exception? StartFailure { get; set; }
    public Exception? ReadFailure { get; set; }
    public Exception? KillFailure { get; set; }
    public Exception? DrainFailure { get; set; }
    public int KillCount { get; private set; }
    public int DisposeCount { get; private set; }
    public bool HasExited => _rootExit.Task.IsCompleted;
    public int ExitCode { get; private set; }
    public bool Start()
    {
        if (StartFailure is not null) throw StartFailure;
        _started.TrySetResult();
        return true;
    }
    public void BeginOutputRead()
    {
        if (ReadFailure is not null) throw ReadFailure;
    }
    public async Task WaitForExitAsync(CancellationToken ct)
    {
        _waiting.TrySetResult();
        await _rootExit.Task.WaitAsync(ct);
        await _outputEnd.Task.WaitAsync(ct);
        await _errorEnd.Task.WaitAsync(ct);
        if (DrainFailure is not null) throw DrainFailure;
    }
    public void KillProcessTree()
    {
        KillCount++;
        _killRequested.TrySetResult();
        if (KillFailure is not null) throw KillFailure;
    }
    public void Emit(string text, string stream = TerminalOutputStreams.Stdout) => OutputReceived?.Invoke(stream, text);
    public void CompleteRootExit(int exitCode = 0) { ExitCode = exitCode; _rootExit.TrySetResult(); }
    public void CompleteOutput() { OutputReceived?.Invoke(TerminalOutputStreams.Stdout, null); _outputEnd.TrySetResult(); }
    public void CompleteError() { OutputReceived?.Invoke(TerminalOutputStreams.Stderr, null); _errorEnd.TrySetResult(); }
    public void Complete(int exitCode = 0) { CompleteRootExit(exitCode); CompleteOutput(); CompleteError(); }
    public void Dispose() { DisposeCount++; _disposed.TrySetResult(); }
    private static TaskCompletionSource NewCompletion() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}

internal sealed class ControlledTerminalTimeProvider : TimeProvider
{
    private readonly List<ControlledTimer> _timers = [];
    public TimeSpan LastDueTime { get; private set; }
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        LastDueTime = dueTime;
        var timer = new ControlledTimer(callback, state);
        _timers.Add(timer);
        return timer;
    }
    public void FireTimers()
    {
        foreach (var timer in _timers.ToArray()) timer.Fire();
    }
    private sealed class ControlledTimer(TimerCallback callback, object? state) : ITimer
    {
        private bool _active = true;
        public void Fire() { if (!_active) return; _active = false; callback(state); }
        public bool Change(TimeSpan dueTime, TimeSpan period) { _active = true; return true; }
        public void Dispose() => _active = false;
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}
