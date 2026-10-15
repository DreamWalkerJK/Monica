using System.Text.RegularExpressions;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Extensions.Logging;
using Monica.Profiling.TypeAllocation.Models;

namespace Monica.Profiling.TypeAllocation.Providers.TraceEvent;

/// <summary>
/// Owns current-process CLR allocation-tick collection and bounded snapshot history.
/// </summary>
/// <remarks>
/// ETW requires Windows and an identity permitted to create the session and enable the CLR provider.
/// Ticks represent sampled allocation volume attributed to the type crossing the allocation threshold;
/// they do not count allocated objects. Low and High are legacy aliases for the same event stream.
/// Totals and history survive stop/start until ResetData; duration describes the current or last session
/// window, or the window since its last reset. Synchronous notification callbacks must not initiate
/// lifecycle operations: such re-entry is rejected rather than waiting on the callback's own worker.
/// </remarks>
internal sealed class TraceEventTypeAllocationProvider : IDisposable, IAsyncDisposable
{
    private const long MAX_TIMER_MILLISECONDS = uint.MaxValue - 1;

    [ThreadStatic]
    private static TraceEventTypeAllocationProvider? _notifyingProvider;

    private readonly ILogger<TraceEventTypeAllocationProvider> _logger;
    private readonly object _stateGate = new();
    private readonly object _notificationGate = new();
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private readonly Dictionary<string, TypeAllocationInfo> _typeAllocations = new(StringComparer.Ordinal);
    private readonly Queue<TypeAllocationSnapshot> _history = new();
    private readonly List<Task> _monitorTasks = [];
    private readonly List<Regex> _excludePatterns;
    private readonly int _maxTrackedTypes;
    private readonly TimeSpan _snapshotInterval;
    private readonly int _maxHistorySnapshots;
    private readonly int _etwBufferSizeMB;
    private readonly TimeSpan? _autoStopAfter;

    private CollectionSession? _activeSession;
    private bool _disposed;
    private bool _hasEvidence;
    private long _evidenceGeneration;
    private AllocationSamplingMode _recordedMode = AllocationSamplingMode.Disabled;
    private DateTime _windowStartedUtc;
    private DateTime _stoppedAtUtc;
    private TimeSpan _stoppedDuration;
    private long _totalAllocationCount;
    private long _totalBytes;
    private long _droppedEventCount;
    private string? _errorMessage;
    private Task? _disposeTask;

    /// <summary>
    /// Receives copied evidence on a provider worker. Subscribers must marshal their own UI work,
    /// remove their subscription when retired, and avoid synchronous lifecycle re-entry.
    /// Each subscriber receives detached type rows. Subscriber failures are logged independently
    /// and do not end collection.
    /// </summary>
    public event Action<TypeAllocationSnapshot>? OnSnapshotUpdated;

    public bool IsCollecting
    {
        get { lock (_stateGate) return _activeSession != null; }
    }

    public AllocationSamplingMode CurrentMode
    {
        get { lock (_stateGate) return _activeSession?.Mode ?? AllocationSamplingMode.Disabled; }
    }

    public TraceEventTypeAllocationProvider(
        ILogger<TraceEventTypeAllocationProvider> logger,
        int maxTrackedTypes = 500,
        TimeSpan? snapshotInterval = null,
        int maxHistorySnapshots = 60,
        int etwBufferSizeMB = 64,
        TimeSpan? autoStopAfter = null,
        IEnumerable<string>? excludeTypePatterns = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxTrackedTypes);
        ArgumentOutOfRangeException.ThrowIfNegative(maxHistorySnapshots);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(etwBufferSizeMB);
        var interval = snapshotInterval ?? TimeSpan.FromSeconds(2);
        if (interval.TotalMilliseconds is < 1 or > MAX_TIMER_MILLISECONDS)
            throw new ArgumentOutOfRangeException(nameof(snapshotInterval), "Snapshot interval must fit the timer range and be at least one millisecond.");
        if (autoStopAfter is { } duration && duration.TotalMilliseconds is < 1 or > MAX_TIMER_MILLISECONDS)
            throw new ArgumentOutOfRangeException(nameof(autoStopAfter), "Auto-stop duration must fit the timer range and be at least one millisecond, or null for manual stop.");

        _logger = logger;
        _maxTrackedTypes = maxTrackedTypes;
        _snapshotInterval = interval;
        _maxHistorySnapshots = maxHistorySnapshots;
        _etwBufferSizeMB = etwBufferSizeMB;
        _autoStopAfter = autoStopAfter;
        _excludePatterns = (excludeTypePatterns ?? ["^System\\.Runtime\\."])
            .Select(pattern => new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase,
                TimeSpan.FromMilliseconds(250)))
            .ToList();
    }

    /// <summary>
    /// Starts one current-process ETW session. Cumulative counters/history are retained until reset.
    /// Null auto-stop is preserved; the module option owns the ten-minute configuration default.
    /// </summary>
    public void StartCollection(AllocationSamplingMode mode)
    {
        RejectNotificationReentry();
        if (mode is not AllocationSamplingMode.Low and not AllocationSamplingMode.High)
            throw new ArgumentOutOfRangeException(nameof(mode), "Select an enabled allocation-tick mode.");
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("CLR allocation-tick collection requires the Windows ETW provider.");

        _lifecycleGate.Wait();
        CollectionSession? collection = null;
        TraceEventSession? session = null;
        (TypeAllocationSnapshot Snapshot, long Generation)? initialEvidence = null;
        try
        {
            lock (_stateGate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (_activeSession != null)
                    throw new InvalidOperationException("Type allocation collection is already running.");
            }

            var startedUtc = DateTime.UtcNow;
            var processId = Environment.ProcessId;
            session = new TraceEventSession($"Monica_TypeAllocation_{Guid.NewGuid():N}", TraceEventSessionOptions.Create)
            {
                BufferSizeMB = _etwBufferSizeMB
            };
            session.EnableProvider(ClrTraceEventParser.ProviderGuid, TraceEventLevel.Verbose,
                (ulong)ClrTraceEventParser.Keywords.GC,
                new TraceEventProviderOptions { ProcessIDFilter = [processId] });

            var ownedCollection = new CollectionSession(session, session.Source, mode, processId);
            collection = ownedCollection;
            ownedCollection.AllocationHandler = data => OnAllocationTick(ownedCollection, data);
            ownedCollection.Source.Clr.GCAllocationTick += ownedCollection.AllocationHandler;

            lock (_stateGate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _activeSession = ownedCollection;
                _hasEvidence = true;
                _recordedMode = mode;
                _windowStartedUtc = startedUtc;
                _stoppedDuration = TimeSpan.Zero;
                _errorMessage = null;
                _evidenceGeneration++;
                _monitorTasks.RemoveAll(task => task.IsCompleted);
                initialEvidence = (BuildSnapshot(startedUtc), _evidenceGeneration);
            }

            // The worker captures its session/source/token; retirement never nulls a worker's inputs.
            ownedCollection.ProcessingTask = Task.Run(() => ProcessEvents(ownedCollection));
            ownedCollection.SnapshotTask = CaptureSnapshotsAsync(ownedCollection);
            ownedCollection.MonitorTask = MonitorSessionAsync(ownedCollection);
            lock (_stateGate) _monitorTasks.Add(ownedCollection.MonitorTask);
            _logger.LogInformation("Current-process CLR allocation-tick collection started with legacy mode {Mode}.", mode);
        }
        catch
        {
            try
            {
                if (collection != null)
                    RetireSessionAsync(collection, "Allocation collection failed during startup.").GetAwaiter().GetResult();
                else
                    session?.Dispose();
            }
            catch (Exception cleanupError)
            {
                _logger.LogError(cleanupError, "Failed to clean up partial CLR allocation startup.");
            }
            throw;
        }
        finally
        {
            _lifecycleGate.Release();
        }

        if (initialEvidence is { } evidence)
            Notify(evidence.Snapshot, evidence.Generation);
    }

    /// <summary>
    /// Stops mutation, closes the real-time source, and drains event/snapshot workers and expiry ownership.
    /// Final copied evidence retains its recorded mode, stop timestamp, and elapsed window duration.
    /// </summary>
    public void StopCollection()
    {
        RejectNotificationReentry();
        StopCollectionAsync().GetAwaiter().GetResult();
    }

    private async Task StopCollectionAsync()
    {
        (TypeAllocationSnapshot Snapshot, long Generation)? evidence = null;
        Task[] monitors;
        await _lifecycleGate.WaitAsync().ConfigureAwait(false);
        try
        {
            CollectionSession? collection;
            lock (_stateGate) collection = _activeSession;
            if (collection != null)
                evidence = await RetireSessionAsync(collection, null).ConfigureAwait(false);
            lock (_stateGate) monitors = _monitorTasks.ToArray();
        }
        finally { _lifecycleGate.Release(); }

        // A monitor may have been waiting for the same lifecycle gate. Drain only after releasing it.
        await Task.WhenAll(monitors).ConfigureAwait(false);
        if (evidence is { } finalEvidence)
            Notify(finalEvidence.Snapshot, finalEvidence.Generation);
    }

    private void ProcessEvents(CollectionSession collection)
    {
        try { collection.Source.Process(); }
        catch (Exception) when (collection.Token.IsCancellationRequested) { }
    }

    private async Task CaptureSnapshotsAsync(CollectionSession collection)
    {
        using var timer = new PeriodicTimer(_snapshotInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(collection.Token).ConfigureAwait(false))
            {
                TypeAllocationSnapshot snapshot;
                long generation;
                lock (_stateGate)
                {
                    if (!ReferenceEquals(_activeSession, collection)) return;
                    snapshot = BuildSnapshot(DateTime.UtcNow);
                    generation = _evidenceGeneration;
                    AddHistory(snapshot);
                }
                Notify(snapshot, generation);
            }
        }
        catch (OperationCanceledException) when (collection.Token.IsCancellationRequested) { }
    }

    private async Task MonitorSessionAsync(CollectionSession collection)
    {
        var expiry = Task.Delay(_autoStopAfter ?? Timeout.InfiniteTimeSpan, collection.Token);
        try
        {
            var completed = await Task.WhenAny(collection.ProcessingTask, collection.SnapshotTask, expiry)
                .ConfigureAwait(false);
            if (collection.Token.IsCancellationRequested) return;

            string? error = null;
            if (!ReferenceEquals(completed, expiry))
            {
                error = completed.Exception?.GetBaseException().Message
                    ?? "The CLR allocation event stream ended unexpectedly.";
                _logger.LogError("CLR allocation collection ended: {Error}", error);
            }

            (TypeAllocationSnapshot Snapshot, long Generation)? evidence = null;
            await _lifecycleGate.WaitAsync().ConfigureAwait(false);
            try
            {
                lock (_stateGate)
                {
                    if (!ReferenceEquals(_activeSession, collection)) return;
                }
                evidence = await RetireSessionAsync(collection, error).ConfigureAwait(false);
            }
            finally { _lifecycleGate.Release(); }

            if (evidence is { } finalEvidence)
                Notify(finalEvidence.Snapshot, finalEvidence.Generation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retire CLR allocation collection.");
        }
        finally
        {
            // Retirement also cancels the delay; observing it owns both expiry and manual-stop paths.
            try { await expiry.ConfigureAwait(false); }
            catch (OperationCanceledException) when (collection.Token.IsCancellationRequested) { }
        }
    }

    // Called while lifecycle admission is held; worker callbacks never acquire that semaphore.
    private async Task<(TypeAllocationSnapshot Snapshot, long Generation)> RetireSessionAsync(
        CollectionSession collection, string? error)
    {
        bool ownedActiveEvidence;
        lock (_stateGate)
        {
            ownedActiveEvidence = ReferenceEquals(_activeSession, collection);
            if (ownedActiveEvidence)
            {
                _activeSession = null;
                _stoppedAtUtc = DateTime.UtcNow;
                _stoppedDuration = _stoppedAtUtc - _windowStartedUtc;
                _errorMessage = error;
                _evidenceGeneration++;
            }
        }

        collection.Cancellation.Cancel();
        if (collection.AllocationHandler != null)
            collection.Source.Clr.GCAllocationTick -= collection.AllocationHandler;
        try { collection.Session.Dispose(); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dispose the CLR ETW session; closing its source.");
            try { collection.Source.Dispose(); }
            catch (Exception sourceError) { _logger.LogError(sourceError, "Failed to close the CLR ETW source."); }
        }

        await ObserveWorkerAsync(collection.ProcessingTask).ConfigureAwait(false);
        await ObserveWorkerAsync(collection.SnapshotTask).ConfigureAwait(false);
        // Token is a captured value; the monitor can still safely observe its canceled delay.
        collection.Cancellation.Dispose();

        lock (_stateGate)
        {
            var snapshot = BuildSnapshot(_hasEvidence ? _stoppedAtUtc : DateTime.UtcNow);
            if (ownedActiveEvidence) AddHistory(snapshot);
            return (snapshot, _evidenceGeneration);
        }
    }

    private async Task ObserveWorkerAsync(Task worker)
    {
        try { await worker.ConfigureAwait(false); }
        catch (Exception ex) { _logger.LogDebug(ex, "Retired CLR allocation worker finished with an error."); }
    }

    private void OnAllocationTick(CollectionSession collection, GCAllocationTickTraceData data)
    {
        // Scope filtering is omitted by TraceEvent on unsupported Windows versions; keep this guard.
        if (data.ProcessID != collection.ProcessId) return;
        var typeName = data.TypeName;
        var seenUtc = data.TimeStamp.ToUniversalTime();
        var hasBytes = TryReadAllocationBytes(data.Version, data.AllocationAmount64, data.AllocationAmount, out var bytes);
        lock (_stateGate)
        {
            if (!ReferenceEquals(_activeSession, collection) || seenUtc < _windowStartedUtc) return;
            if (string.IsNullOrWhiteSpace(typeName) || !hasBytes)
            {
                _droppedEventCount++;
                return;
            }
            if (_excludePatterns.Any(pattern => pattern.IsMatch(typeName))) return;

            if (!_typeAllocations.TryGetValue(typeName, out var info))
            {
                info = new TypeAllocationInfo { TypeName = typeName };
            }
            long typeCount, typeBytes, totalCount, totalBytes;
            checked
            {
                typeCount = info.AllocationCount + 1;
                typeBytes = info.TotalBytes + bytes;
                totalCount = _totalAllocationCount + 1;
                totalBytes = _totalBytes + bytes;
            }
            info.AllocationCount = typeCount;
            info.TotalBytes = typeBytes;
            _totalAllocationCount = totalCount;
            _totalBytes = totalBytes;
            info.LastSeenUtc = seenUtc;
            _typeAllocations[typeName] = info;
            while (_typeAllocations.Count > _maxTrackedTypes)
            {
                var smallest = _typeAllocations.MinBy(pair => pair.Value.TotalBytes);
                _typeAllocations.Remove(smallest.Key);
            }
        }
    }

    /// <summary>
    /// Reads unaltered payload bytes. Version 2+ owns a 64-bit value; legacy payloads own an unsigned
    /// 32-bit value despite TraceEvent exposing an int. Invalid modern values are not clamped or guessed.
    /// </summary>
    internal static bool TryReadAllocationBytes(int version, long amount64, int amount32, out long bytes)
    {
        bytes = version >= 2 ? amount64 : (uint)amount32;
        return bytes > 0;
    }

    private TypeAllocationSnapshot BuildSnapshot(DateTime capturedAtUtc)
    {
        var types = _typeAllocations.Values.Select(type => new TypeAllocationInfo
        {
            TypeName = type.TypeName,
            AllocationCount = type.AllocationCount,
            TotalBytes = type.TotalBytes,
            AllocationPercentage = _totalAllocationCount > 0 ? 100d * type.AllocationCount / _totalAllocationCount : 0,
            BytesPercentage = _totalBytes > 0 ? 100d * type.TotalBytes / _totalBytes : 0,
            LastSeenUtc = type.LastSeenUtc
        }).OrderByDescending(type => type.TotalBytes).ThenBy(type => type.TypeName, StringComparer.Ordinal).ToArray();

        return new TypeAllocationSnapshot
        {
            Timestamp = capturedAtUtc,
            Duration = _activeSession != null ? capturedAtUtc - _windowStartedUtc : _stoppedDuration,
            Types = types,
            TotalAllocationCount = _totalAllocationCount,
            TotalBytes = _totalBytes,
            IsCollecting = _activeSession != null,
            SamplingMode = _recordedMode,
            DroppedEventCount = _droppedEventCount,
            DataSource = _hasEvidence ? TypeAllocationDataSource.AllocationTracking : TypeAllocationDataSource.None,
            ErrorMessage = _errorMessage
        };
    }

    // These ownership boundaries can be verified independently of native session activation.
    internal void AddHistory(TypeAllocationSnapshot snapshot)
    {
        lock (_stateGate)
        {
            if (_maxHistorySnapshots == 0) return;
            _history.Enqueue(Detach(snapshot));
            while (_history.Count > _maxHistorySnapshots) _history.Dequeue();
        }
    }

    internal void Notify(TypeAllocationSnapshot snapshot, long generation)
    {
        lock (_notificationGate)
        {
            var callbacks = OnSnapshotUpdated?.GetInvocationList() ?? [];
            foreach (var callback in callbacks)
            {
                lock (_stateGate)
                {
                    if (_disposed || _evidenceGeneration != generation) return;
                }
                var previous = _notifyingProvider;
                _notifyingProvider = this;
                try { ((Action<TypeAllocationSnapshot>)callback)(Detach(snapshot)); }
                catch (Exception ex) { _logger.LogWarning(ex, "CLR allocation snapshot subscriber failed."); }
                finally { _notifyingProvider = previous; }
            }
        }
    }

    public TypeAllocationSnapshot GetCurrentSnapshot()
    {
        lock (_stateGate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return BuildSnapshot(_activeSession != null || !_hasEvidence ? DateTime.UtcNow : _stoppedAtUtc);
        }
    }

    public IReadOnlyList<TypeAllocationSnapshot> GetHistory()
    {
        lock (_stateGate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _history.Select(Detach).ToArray();
        }
    }

    private static TypeAllocationSnapshot Detach(TypeAllocationSnapshot snapshot) => new()
    {
        Timestamp = snapshot.Timestamp,
        Duration = snapshot.Duration,
        Types = snapshot.Types.Select(type => new TypeAllocationInfo
        {
            TypeName = type.TypeName,
            AllocationCount = type.AllocationCount,
            TotalBytes = type.TotalBytes,
            AllocationPercentage = type.AllocationPercentage,
            BytesPercentage = type.BytesPercentage,
            LastSeenUtc = type.LastSeenUtc
        }).ToArray(),
        TotalAllocationCount = snapshot.TotalAllocationCount,
        TotalBytes = snapshot.TotalBytes,
        IsCollecting = snapshot.IsCollecting,
        SamplingMode = snapshot.SamplingMode,
        DroppedEventCount = snapshot.DroppedEventCount,
        DataSource = snapshot.DataSource,
        ErrorMessage = snapshot.ErrorMessage
    };

    public void ResetData()
    {
        RejectNotificationReentry();
        _lifecycleGate.Wait();
        TypeAllocationSnapshot snapshot;
        long generation;
        try
        {
            lock (_stateGate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _typeAllocations.Clear();
                _history.Clear();
                _totalAllocationCount = _totalBytes = _droppedEventCount = 0;
                _windowStartedUtc = _stoppedAtUtc = DateTime.UtcNow;
                _stoppedDuration = TimeSpan.Zero;
                _hasEvidence = _activeSession != null;
                _recordedMode = _activeSession?.Mode ?? AllocationSamplingMode.Disabled;
                _errorMessage = null;
                generation = ++_evidenceGeneration;
                snapshot = BuildSnapshot(_windowStartedUtc);
            }
        }
        finally { _lifecycleGate.Release(); }
        Notify(snapshot, generation);
    }

    private void RejectNotificationReentry()
    {
        if (ReferenceEquals(_notifyingProvider, this))
            throw new InvalidOperationException("Allocation lifecycle operations cannot run synchronously inside a snapshot notification. Schedule the operation after the callback returns.");
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    public ValueTask DisposeAsync()
    {
        RejectNotificationReentry();
        lock (_stateGate)
        {
            _disposed = true;
            // One stored retirement task makes concurrent disposal callers await the same drain.
            return new ValueTask(_disposeTask ??= Task.Run(DisposeCoreAsync));
        }
    }

    private async Task DisposeCoreAsync()
    {
        Task[] monitors;
        await _lifecycleGate.WaitAsync().ConfigureAwait(false);
        try
        {
            CollectionSession? collection;
            lock (_stateGate)
            {
                collection = _activeSession;
            }
            if (collection != null)
                await RetireSessionAsync(collection, null).ConfigureAwait(false);
            lock (_stateGate) monitors = _monitorTasks.ToArray();
        }
        finally { _lifecycleGate.Release(); }
        await Task.WhenAll(monitors).ConfigureAwait(false);
        // Serialize with a callback already admitted before the disposed flag was set.
        lock (_notificationGate) { }
        GC.SuppressFinalize(this);
    }

    private sealed class CollectionSession
    {
        public CollectionSession(TraceEventSession session, ETWTraceEventSource source,
            AllocationSamplingMode mode, int processId)
        {
            Session = session;
            Source = source;
            Mode = mode;
            ProcessId = processId;
            Cancellation = new CancellationTokenSource();
            Token = Cancellation.Token;
        }

        public TraceEventSession Session { get; }
        public ETWTraceEventSource Source { get; }
        public AllocationSamplingMode Mode { get; }
        public int ProcessId { get; }
        public CancellationTokenSource Cancellation { get; }
        public CancellationToken Token { get; }
        public Action<GCAllocationTickTraceData>? AllocationHandler { get; set; }
        public Task ProcessingTask { get; set; } = Task.CompletedTask;
        public Task SnapshotTask { get; set; } = Task.CompletedTask;
        public Task MonitorTask { get; set; } = Task.CompletedTask;
    }
}
