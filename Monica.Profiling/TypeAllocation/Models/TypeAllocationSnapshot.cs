namespace Monica.Profiling.TypeAllocation.Models;

/// <summary>
/// Type allocation snapshot - type allocation data at a certain point in time
/// </summary>
public class TypeAllocationSnapshot
{
    /// <summary>
    /// UTC evidence time. Active ETW views use their read time; stopped ETW evidence retains its stop
    /// time. Heap snapshots use scan completion time, not an exact native clone creation timestamp.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Elapsed current/last ETW session window (or since reset), retained after stop. ETW totals may
    /// include earlier sessions until reset. Heap snapshots instead report total scan operation time.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Retained type rows, sorted by TotalBytes in descending order. Totals-only ETW collection keeps
    /// this list empty while cumulative source counters continue to update.
    /// </summary>
    public IReadOnlyList<TypeAllocationInfo> Types { get; init; } = [];

    /// <summary>
    /// Observed allocation-tick count for ETW, or enumerated non-free object count for a heap scan.
    /// ETW ticks are not allocated-object counts; ETW counters are cumulative until reset.
    /// </summary>
    public long TotalAllocationCount { get; init; }

    /// <summary>
    /// ETW tick allocation volume attributed to the threshold-crossing types, or enumerated heap
    /// object bytes. Excluded types are omitted; evicted ranking rows remain in ETW cumulative totals.
    /// </summary>
    public long TotalBytes { get; init; }

    /// <summary>
    /// Number of retained type rows in this snapshot, rather than all types ever observed.
    /// Zero does not imply zero allocation volume when totals-only collection is configured.
    /// </summary>
    public int UniqueTypeCount => Types.Count;

    /// <summary>
    /// Whether data is being collected
    /// </summary>
    public bool IsCollecting { get; init; }

    /// <summary>
    /// Requested legacy mode for current/last ETW evidence, retained after stop. Both enabled modes
    /// currently use the same CLR allocation-tick event strategy; this does not imply a sample rate.
    /// </summary>
    public AllocationSamplingMode SamplingMode { get; init; }

    /// <summary>
    /// Current-process ticks skipped for unknown type names or invalid byte payloads. This does not
    /// measure ETW session transport loss, excluded types, or ranking eviction.
    /// </summary>
    public long DroppedEventCount { get; init; }

    /// <summary>
    /// Data source type
    /// </summary>
    public TypeAllocationDataSource DataSource { get; init; }

    /// <summary>
    /// Error message (if any)
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Assign sampling mode
/// </summary>
public enum AllocationSamplingMode
{
    /// <summary>
    /// Collection is disabled
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Legacy enabled-mode alias for CLR GCAllocationTick collection. It uses the same event stream
    /// as High; no fixed event frequency, allocation percentage, or reduced-overhead guarantee exists.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Legacy enabled-mode alias for CLR GCAllocationTick collection. It uses the same event stream
    /// as Low; it does not enable the separate CLR GCSampledObjectAllocationHigh strategy.
    /// </summary>
    High = 2
}

/// <summary>
/// Type assignment data source
/// </summary>
public enum TypeAllocationDataSource
{
    /// <summary>
    /// No data
    /// </summary>
    None = 0,

    /// <summary>
    /// Current-process CLR allocation-tick tracking through Windows ETW. No EventPipe provider is
    /// implemented by this data source.
    /// </summary>
    AllocationTracking = 1,

    /// <summary>
    /// Windows ClrMD process-clone heap scan. Enumerated objects need not be root-reachable.
    /// </summary>
    HeapSnapshot = 2
}
