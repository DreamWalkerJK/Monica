using Microsoft.Extensions.Logging;
using Monica.Tool.Extensions;

namespace Monica.Core.ObservableInstance.Models;

/// <summary>
/// Tracks state transitions, exceptions, and history for a single observable instance.
/// </summary>
public class ObservableInstanceTracker : IDisposable
{
    public delegate void StateChangedHandler(ObservableStateEntry stateChange);

    private readonly List<ObservableStateEntry> _stateHistory = [];
    private readonly object _gate = new();
    private bool _disposed;
    private readonly ILogger? _logger;
    private LogLevel? _defaultLogLevel;
    private long _historySequence;

    // Log level mappings storage (type-erased for multi-enum support)
    private readonly Dictionary<Type, Dictionary<object, LogLevel>> _logLevelMappings = new();

    /// <summary>Gets the immutable registration generation used for source-qualified maintenance.</summary>
    public string RegistrationId { get; } = Guid.NewGuid().ToString("N");

    // Identity
    /// <summary>
    /// Gets the unique instance identifier
    /// </summary>
    public string InstanceId { get; private set; }

    /// <summary>
    /// Gets the human-readable instance name
    /// </summary>
    public string InstanceName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the type of the instance
    /// </summary>
    public Type? InstanceType { get; init; }

    /// <summary>
    /// Gets the instance key for keyed service instances (optional)
    /// </summary>
    public string? InstanceKey { get; init; }

    /// <summary>
    /// Gets the group ID for grouping related instances (optional)
    /// </summary>
    public string? GroupId { get; init; }

    // Configuration
    /// <summary>
    /// Gets the maximum number of history entries to retain
    /// </summary>
    public int MaxHistorySize { get; init; } = 100;

    // Current State
    /// <summary>
    /// Gets the current state of the instance
    /// </summary>
    public object? CurrentState { get; private set; }

    /// <summary>
    /// Gets the timestamp of the last recorded entry, including message-only entries.
    /// </summary>
    public DateTime StateChangedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when this tracker was created and registered
    /// </summary>
    public DateTime RegisteredAt { get; init; }

    /// <summary>
    /// Gets the log level for the current state (null if not mapped)
    /// </summary>
    public LogLevel? CurrentLogLevel
    {
        get { lock (_gate) { ObjectDisposedException.ThrowIf(_disposed, this); return GetLogLevel(CurrentState); } }
    }

    // Statistics
    /// <summary>
    /// Gets the lifetime count of recorded entries, including messages and exceptions without state changes.
    /// </summary>
    public int TotalStateChanges { get; private set; }

    /// <summary>
    /// Gets the total number of exceptions that have occurred
    /// </summary>
    public int TotalExceptions { get; private set; }

    /// <summary>
    /// Gets the current number of history entries
    /// </summary>
    public int Count
    {
        get
        {
            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return _stateHistory.Count;
            }
        }
    }

    /// <summary>
    /// Gets the count of entries with exceptions
    /// </summary>
    public int ExceptionCount
    {
        get
        {
            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return _stateHistory.Count(h => h.Exception != null);
            }
        }
    }

    /// <summary>
    /// Gets whether there are any exceptions in history
    /// </summary>
    public bool HasExceptions
    {
        get
        {
            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return _stateHistory.Any(h => h.Exception != null);
            }
        }
    }

    /// <summary>
    /// Raised when a state transition is recorded.
    /// </summary>
    public event StateChangedHandler? StateChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableInstanceTracker"/> class.
    /// </summary>
    /// <param name="instanceId">Unique instance identifier</param>
    /// <param name="maxHistorySize">Maximum number of history entries to retain</param>
    /// <param name="option">Configuration options for logger and log level mappings</param>
    internal ObservableInstanceTracker(string instanceId, int maxHistorySize, ObservableInstanceRegistration option)
    {
        if (string.IsNullOrEmpty(instanceId))
            throw new ArgumentException("Instance ID cannot be empty", nameof(instanceId));
        if (maxHistorySize <= 0)
            throw new ArgumentException("Max history size must be greater than 0", nameof(maxHistorySize));

        InstanceId = instanceId;
        MaxHistorySize = maxHistorySize;
        _logger = option?.Logger;
        _defaultLogLevel = option?.DefaultLogLevel;
        RegisteredAt = DateTime.UtcNow;
        StateChangedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a state change with optional message and exception.
    /// This is the unified method that handles both state transitions and exception tracking.
    /// Automatically logs to ILogger if configured with log level mappings.
    /// </summary>
    /// <param name="message">Descriptive message about the state change</param>
    /// <param name="newState">The new state to transition to (null if state not changing)</param>
    /// <param name="exception">Optional exception associated with this state change</param>
    /// <param name="logLevel">Use specific log level instead of default.</param>
    public void RecordState(string message, object? newState = null, Exception? exception = null,
        LogLevel? logLevel = null)
    {
        ObservableStateEntry? stateEntry = null;
        var shouldNotifyStateChanged = false;
        var shouldLog = false;

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var previousState = CurrentState;
            logLevel ??= GetLogLevel(newState);
            stateEntry = new ObservableStateEntry
            {
                Sequence = ++_historySequence,
                PreviousState = previousState,
                CurrentState = newState ?? previousState,
                Message = message,
                Exception = exception,
                Timestamp = DateTime.UtcNow,
                LogLevel = logLevel
            };

            _stateHistory.Add(stateEntry);
            if (_stateHistory.Count > MaxHistorySize)
            {
                _stateHistory.RemoveAt(0);
            }

            if(newState != null)
            {
                CurrentState = newState;
            }
            StateChangedAt = DateTime.UtcNow;
            TotalStateChanges++;
            if (exception != null)
            {
                TotalExceptions++;
            }

            shouldLog = _logger != null && newState != null && logLevel.HasValue;
            shouldNotifyStateChanged = newState != null && !Equals(previousState, newState);
        }

        if (shouldLog && logLevel.HasValue && newState != null)
        {
            LogStateChange(logLevel.Value, message, newState, exception);
        }

        if (shouldNotifyStateChanged && stateEntry != null)
        {
            StateChanged?.Invoke(stateEntry);
        }
    }

    /// <summary>
    /// Gets all state history entries
    /// </summary>
    /// <returns>Read-only list of all history entries ordered by timestamp</returns>
    public IReadOnlyList<ObservableStateEntry> GetHistory()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _stateHistory.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets recent history entries
    /// </summary>
    /// <param name="count">Number of recent entries to retrieve</param>
    /// <returns>Read-only list of recent history entries</returns>
    public IReadOnlyList<ObservableStateEntry> GetRecentHistory(int count)
    {
        if (count <= 0) return new List<ObservableStateEntry>().AsReadOnly();

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _stateHistory
                .OrderByDescending(h => h.Timestamp)
                .ThenByDescending(h => h.Sequence)
                .Take(count)
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Gets all exceptions from history (convenience method for backward compatibility)
    /// </summary>
    /// <returns>Read-only list of history entries that have exceptions</returns>
    public IReadOnlyList<ObservableStateEntry> GetExceptions()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _stateHistory
                .Where(h => h.Exception != null)
                .OrderByDescending(h => h.Timestamp)
                .ThenByDescending(h => h.Sequence)
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Gets recent exceptions (convenience method for backward compatibility)
    /// </summary>
    /// <param name="count">Number of recent exceptions to retrieve</param>
    /// <returns>Read-only list of recent history entries with exceptions</returns>
    public IReadOnlyList<ObservableStateEntry> GetRecentExceptions(int count)
    {
        if (count <= 0) return new List<ObservableStateEntry>().AsReadOnly();

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _stateHistory
                .Where(h => h.Exception != null)
                .OrderByDescending(h => h.Timestamp)
                .ThenByDescending(h => h.Sequence)
                .Take(count)
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Clears all history
    /// </summary>
    public void ClearHistory()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _stateHistory.Clear();
        }
    }

    /// <summary>
    /// Captures tracker-owned metadata and retained entries under one lock, then converts values
    /// into detached evidence outside that lock. Caller-owned mutable values must provide their
    /// own synchronization if their serialization must be atomic. No value is truncated.
    /// </summary>
    /// <param name="cancellationToken">Cancels before capture and between retained entries.</param>
    /// <returns>A frozen capture containing the complete retained history.</returns>
    public ObservableInstanceSnapshot CaptureSnapshot(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        object? state;
        ObservableStateEntry[] history;
        ObservableInstanceSnapshot snapshot;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            state = CurrentState;
            history = _stateHistory.ToArray();
            snapshot = new ObservableInstanceSnapshot
            {
                Identity = new(InstanceId, RegistrationId), InstanceName = InstanceName,
                InstanceType = InstanceType, InstanceKey = InstanceKey, GroupId = GroupId,
                InstanceTypeName = InstanceType?.GetCleanFullName(), InstanceTypeShortName = InstanceType?.GetCleanName(),
                CurrentState = new(null, null, null), CurrentLogLevel = GetLogLevel(state),
                RegisteredAt = RegisteredAt, LastRecordedAt = StateChangedAt, CapturedAt = DateTime.UtcNow,
                TotalRecords = TotalStateChanges, TotalExceptions = TotalExceptions,
                MaxHistorySize = MaxHistorySize, History = []
            };
        }

        var entries = new List<ObservableStateEntrySnapshot>(history.Length);
        foreach (var entry in history)
        {
            cancellationToken.ThrowIfCancellationRequested();
            entries.Add(new(entry.Sequence, entry.Timestamp, ObservableValueSnapshot.Capture(entry.PreviousState),
                ObservableValueSnapshot.Capture(entry.CurrentState), entry.Message,
                ObservableExceptionSnapshot.Capture(entry.Exception), entry.LogLevel));
        }
        cancellationToken.ThrowIfCancellationRequested();
        return snapshot with { CurrentState = ObservableValueSnapshot.Capture(state), History = entries.AsReadOnly() };
    }

    #region Status log Mapping

    /// <summary>
    /// Checks if the current state is mapped to Debug log level
    /// </summary>
    public bool IsDebug() => CheckLogLevel(LogLevel.Debug);

    /// <summary>
    /// Checks if the current state is mapped to Information log level
    /// </summary>
    public bool IsInformation() => CheckLogLevel(LogLevel.Information);

    /// <summary>
    /// Checks if the current state is mapped to Warning log level
    /// </summary>
    public bool IsWarning() => CheckLogLevel(LogLevel.Warning);

    /// <summary>
    /// Checks if the current state is mapped to Error log level
    /// </summary>
    public bool IsError() => CheckLogLevel(LogLevel.Error);

    /// <summary>
    /// Checks if the current state is mapped to Critical log level
    /// </summary>
    public bool IsCritical() => CheckLogLevel(LogLevel.Critical);

    /// <summary>
    /// Checks if the current state is unhealthy (Warning, Error, or Critical)
    /// </summary>
    public bool IsUnhealthy() => IsWarning() || IsError() || IsCritical();

    /// <summary>
    /// Maps specific state enum values to a log level
    /// </summary>
    /// <typeparam name="TState">The enum type representing states</typeparam>
    /// <param name="logLevel">The Microsoft.Extensions.Logging.LogLevel to use</param>
    /// <param name="states">One or more state values to map to this log level</param>
    /// <returns>This agent instance for fluent chaining</returns>
    public ObservableInstanceTracker SetLogLevel<TState>(LogLevel logLevel, params TState[] states)
        where TState : struct, Enum
    {
        if (states == null || states.Length == 0)
            throw new ArgumentException("At least one state must be provided", nameof(states));

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var stateType = typeof(TState);
            if (!_logLevelMappings.ContainsKey(stateType))
            {
                _logLevelMappings[stateType] = new Dictionary<object, LogLevel>();
            }

            foreach (var state in states)
            {
                _logLevelMappings[stateType][state] = logLevel;
            }
        }

        return this;
    }

    /// <summary>
    /// Maps states to Debug log level
    /// </summary>
    public ObservableInstanceTracker SetDebugStates<TState>(params TState[] states)
        where TState : struct, Enum
        => SetLogLevel(LogLevel.Debug, states);

    /// <summary>
    /// Maps states to Information log level
    /// </summary>
    public ObservableInstanceTracker SetInformationStates<TState>(params TState[] states)
        where TState : struct, Enum
        => SetLogLevel(LogLevel.Information, states);

    /// <summary>
    /// Maps states to Warning log level
    /// </summary>
    public ObservableInstanceTracker SetWarningStates<TState>(params TState[] states)
        where TState : struct, Enum
        => SetLogLevel(LogLevel.Warning, states);

    /// <summary>
    /// Maps states to Error log level
    /// </summary>
    public ObservableInstanceTracker SetErrorStates<TState>(params TState[] states)
        where TState : struct, Enum
        => SetLogLevel(LogLevel.Error, states);

    /// <summary>
    /// Maps states to Critical log level
    /// </summary>
    public ObservableInstanceTracker SetCriticalStates<TState>(params TState[] states)
        where TState : struct, Enum
        => SetLogLevel(LogLevel.Critical, states);

    /// <summary>
    /// Sets the default log level for unmapped states
    /// </summary>
    public ObservableInstanceTracker SetDefaultLogLevel(LogLevel? logLevel)
    {
        lock (_gate) { ObjectDisposedException.ThrowIf(_disposed, this); _defaultLogLevel = logLevel; }
        return this;
    }

    private bool CheckLogLevel(LogLevel targetLevel)
    {
        return CurrentLogLevel == targetLevel;
    }

    /// <summary>
    /// Gets the log level for a given state
    /// </summary>
    private LogLevel? GetLogLevel(object? state)
    {
        if (state == null) return _defaultLogLevel;

        var stateType = state.GetType();
        if (_logLevelMappings.TryGetValue(stateType, out var mappings))
        {
            if (mappings.TryGetValue(state, out var logLevel))
            {
                return logLevel;
            }
        }

        return _defaultLogLevel;
    }

    private void LogStateChange(LogLevel logLevel, string message, object state, Exception? exception)
    {
        // Use structured logging with state information
        var logMessage = "[{InstanceName}] {Message} (State: {State})";

        if (exception != null)
        {
            _logger!.Log(logLevel, exception, logMessage, InstanceName, message, state);
        }
        else
        {
            _logger!.Log(logLevel, logMessage, InstanceName, message, state);
        }
    }

    #endregion


    /// <summary>
    /// Disposes resources used by this tracker.
    /// </summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _stateHistory.Clear();
            CurrentState = null;
            StateChanged = null;
        }
        GC.SuppressFinalize(this);
    }
}
