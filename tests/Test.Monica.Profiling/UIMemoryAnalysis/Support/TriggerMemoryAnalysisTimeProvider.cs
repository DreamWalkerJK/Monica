namespace Test.Monica.Profiling.UIMemoryAnalysis.Support;

/// <summary>
/// A page clock whose ticks are explicitly admitted by a test; it never changes the runtime collector sampling clock.
/// </summary>
internal sealed class TriggerMemoryAnalysisTimeProvider : TimeProvider
{
    private readonly object _sync = new();
    private readonly List<TriggerTimer> _timers = [];
    private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;

    public int MaximumActiveTimers { get; private set; }

    public int CreatedTimers
    {
        get
        {
            lock (_sync)
            {
                return _timers.Count;
            }
        }
    }

    public int ActiveTimers
    {
        get
        {
            lock (_sync)
            {
                return _timers.Count(static timer => timer.IsActive);
            }
        }
    }

    public override DateTimeOffset GetUtcNow()
    {
        lock (_sync)
        {
            return _utcNow;
        }
    }

    public void Advance(TimeSpan duration)
    {
        lock (_sync)
        {
            _utcNow += duration;
        }
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new TriggerTimer(callback, state, dueTime, period);
        lock (_sync)
        {
            _timers.Add(timer);
            MaximumActiveTimers = Math.Max(MaximumActiveTimers, _timers.Count(static item => item.IsActive));
        }

        return timer;
    }

    public int Tick()
    {
        TriggerTimer[] timers;
        lock (_sync)
        {
            timers = [.. _timers];
        }

        return timers.Count(static timer => timer.TryFire());
    }

    private sealed class TriggerTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) : ITimer
    {
        private readonly object _sync = new();
        private TimeSpan _period = period;
        private bool _active = dueTime != Timeout.InfiniteTimeSpan;
        private bool _disposed;

        public bool IsActive
        {
            get
            {
                lock (_sync)
                {
                    return _active && !_disposed;
                }
            }
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _period = period;
                _active = dueTime != Timeout.InfiniteTimeSpan;
                return true;
            }
        }

        public bool TryFire()
        {
            lock (_sync)
            {
                if (!_active || _disposed)
                {
                    return false;
                }

                if (_period == Timeout.InfiniteTimeSpan)
                {
                    _active = false;
                }
            }

            callback(state);
            return true;
        }

        public void Dispose()
        {
            lock (_sync)
            {
                _disposed = true;
                _active = false;
            }
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
