using Monica.Profiling.ExecutionTiming.Abstractions;
using Monica.Profiling.ExecutionTiming.Models;

namespace Test.Monica.Profiling.UIExecutionTiming.Support;

/// <summary>
/// Holds or fails a query boundary while every successful read still delegates to the real host coordinator.
/// </summary>
internal sealed class ControlledTimingQuery(IExecutionTimingQuery coordinator, TimingQueryControl control)
    : IExecutionTimingQuery
{
    public IReadOnlyDictionary<string, ExecutionTimingStatistics> GetStatistics()
    {
        using var read = control.EnterStatisticsRead();
        return coordinator.GetStatistics();
    }

    public ExecutionTimingStatistics? GetStatistics(string operationKey) => coordinator.GetStatistics(operationKey);

    public IReadOnlyDictionary<Guid, RunningExecutionTimingInfo> GetRunningOperations()
    {
        using var read = control.EnterRunningRead();
        return coordinator.GetRunningOperations();
    }

    public void Reset(string operationKey) => coordinator.Reset(operationKey);
}

internal sealed class TimingQueryControl
{
    private readonly object _sync = new();
    private TimingReadGate? _nextStatisticsGate;
    private TimingReadGate? _nextRunningGate;
    private string? _statisticsFailure;
    private string? _runningFailure;
    private int _activeReads;

    public int StatisticsReads { get; private set; }
    public int RunningReads { get; private set; }
    public int MaximumConcurrentReads { get; private set; }

    public TimingReadGate HoldNextStatisticsRead()
    {
        lock (_sync)
        {
            if (_nextStatisticsGate is not null)
            {
                throw new InvalidOperationException("A read gate is already armed.");
            }

            return _nextStatisticsGate = new TimingReadGate();
        }
    }

    public TimingReadGate HoldNextRunningRead()
    {
        lock (_sync)
        {
            if (_nextRunningGate is not null)
            {
                throw new InvalidOperationException("A read gate is already armed.");
            }

            return _nextRunningGate = new TimingReadGate();
        }
    }

    public void FailStatistics(string message)
    {
        lock (_sync)
        {
            _statisticsFailure = message;
        }
    }

    public void FailRunning(string message)
    {
        lock (_sync)
        {
            _runningFailure = message;
        }
    }

    public void ClearFailures()
    {
        lock (_sync)
        {
            _statisticsFailure = null;
            _runningFailure = null;
        }
    }

    public IDisposable EnterStatisticsRead() => EnterRead(statistics: true);
    public IDisposable EnterRunningRead() => EnterRead(statistics: false);

    private IDisposable EnterRead(bool statistics)
    {
        TimingReadGate? gate;
        string? failure;
        lock (_sync)
        {
            _activeReads++;
            MaximumConcurrentReads = Math.Max(MaximumConcurrentReads, _activeReads);
            if (statistics)
            {
                StatisticsReads++;
                gate = _nextStatisticsGate;
                _nextStatisticsGate = null;
                failure = _statisticsFailure;
            }
            else
            {
                RunningReads++;
                gate = _nextRunningGate;
                _nextRunningGate = null;
                failure = _runningFailure;
            }
        }

        var read = new ReadLease(this);
        try
        {
            gate?.WaitForRelease();
            if (failure is not null)
            {
                throw new InvalidOperationException(failure);
            }

            return read;
        }
        catch
        {
            read.Dispose();
            throw;
        }
    }

    private sealed class ReadLease(TimingQueryControl owner) : IDisposable
    {
        public void Dispose()
        {
            lock (owner._sync)
            {
                owner._activeReads--;
            }
        }
    }
}

internal sealed class TimingReadGate : IDisposable
{
    private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly ManualResetEventSlim _release = new();

    public Task Entered => _entered.Task;

    public void Release() => _release.Set();

    public void WaitForRelease()
    {
        _entered.TrySetResult();
        if (!_release.Wait(TimeSpan.FromSeconds(10)))
        {
            throw new TimeoutException("The test did not release its timing query boundary.");
        }
    }

    public void Dispose() => _release.Dispose();
}
