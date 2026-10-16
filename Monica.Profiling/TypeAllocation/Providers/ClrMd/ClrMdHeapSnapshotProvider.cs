using Microsoft.Diagnostics.Runtime;
using Microsoft.Extensions.Logging;
using Monica.Profiling.TypeAllocation.Models;

namespace Monica.Profiling.TypeAllocation.Providers.ClrMd;

/// <summary>
/// Serializes Windows PSS clone scans of the current process and drains admitted scans on disposal.
/// </summary>
/// <remarks>
/// Requires Windows 8.1/Server 2012 R2 or later and a matching readable DAC. It does not force GC,
/// create a dump, or prove root reachability. The synchronous native capture/walk cannot be forcibly
/// canceled; callers own result publication while the provider owns admitted scan completion.
/// </remarks>
internal sealed class ClrMdHeapSnapshotProvider(ILogger<ClrMdHeapSnapshotProvider> logger)
    : IDisposable, IAsyncDisposable
{
    private readonly object _gate = new();
    private Task _captureTail = Task.CompletedTask;
    private bool _disposed;

    public Task<TypeAllocationSnapshot> TakeHeapSnapshotAsync()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!OperatingSystem.IsWindowsVersionAtLeast(6, 3))
                throw new PlatformNotSupportedException("CLR heap snapshots require Windows 8.1/Server 2012 R2 or later and the Windows process snapshot provider.");
            var capture = CaptureAfterAsync(_captureTail);
            _captureTail = capture;
            return capture;
        }
    }

    private async Task<TypeAllocationSnapshot> CaptureAfterAsync(Task previous)
    {
        // An earlier caller owns its failure; a failed scan must not poison later admitted scans.
        try { await previous.ConfigureAwait(false); }
        catch (Exception ex) { logger.LogDebug(ex, "The preceding CLR heap scan failed."); }
        return await Task.Run(() =>
        {
            logger.LogInformation("Capturing CLR heap snapshot.");
            var startTime = DateTime.UtcNow;

            // This version uses PSS_CAPTURE_VA_CLONE on Windows. Its Linux branch writes a full dump,
            // so platform admission above is part of the clone-only operation's contract.
            using var dataTarget = DataTarget.CreateSnapshotAndAttach(Environment.ProcessId);

            using var runtime = dataTarget.ClrVersions.FirstOrDefault()?.CreateRuntime();
            if (runtime == null)
            {
                throw new InvalidOperationException("Unable to create the CLR runtime. The current process may not be managed.");
            }

            var heap = runtime.Heap;
            if (!heap.CanWalkHeap)
            {
                throw new InvalidOperationException("Unable to walk the CLR heap. A GC may already be running.");
            }

            var typeStats = new Dictionary<string, (long Count, long Size)>();

            foreach (var obj in heap.EnumerateObjects())
            {
                if (!obj.IsValid || obj.IsFree)
                {
                    continue;
                }

                var typeName = obj.Type?.Name ?? "Unknown";
                var size = checked((long)obj.Size);

                if (typeStats.TryGetValue(typeName, out var stats))
                {
                    typeStats[typeName] = (checked(stats.Count + 1), checked(stats.Size + size));
                }
                else
                {
                    typeStats[typeName] = (1, size);
                }
            }

            var totalCount = typeStats.Values.Sum(v => v.Count);
            var totalBytes = typeStats.Values.Sum(v => v.Size);
            var snapshotTimestamp = DateTime.UtcNow;

            var types = typeStats
                .Select(kvp => new TypeAllocationInfo
                {
                    TypeName = kvp.Key,
                    AllocationCount = kvp.Value.Count,
                    TotalBytes = kvp.Value.Size,
                    AllocationPercentage = totalCount > 0 ? (double)kvp.Value.Count / totalCount * 100 : 0,
                    BytesPercentage = totalBytes > 0 ? (double)kvp.Value.Size / totalBytes * 100 : 0,
                    LastSeenUtc = snapshotTimestamp
                })
                .OrderByDescending(t => t.TotalBytes)
                .ToList();

            var duration = DateTime.UtcNow - startTime;
            logger.LogInformation(
                "CLR heap snapshot captured. TypeCount={TypeCount}, DurationSeconds={DurationSeconds:F2}",
                types.Count,
                duration.TotalSeconds);

            return new TypeAllocationSnapshot
            {
                Timestamp = snapshotTimestamp,
                Duration = duration,
                Types = types,
                TotalAllocationCount = totalCount,
                TotalBytes = totalBytes,
                IsCollecting = false,
                SamplingMode = AllocationSamplingMode.Disabled,
                DataSource = TypeAllocationDataSource.HeapSnapshot
            };
        }).ConfigureAwait(false);
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync()
    {
        Task captures;
        lock (_gate)
        {
            _disposed = true;
            captures = _captureTail;
        }
        try { await captures.ConfigureAwait(false); }
        catch (Exception ex) { logger.LogDebug(ex, "Admitted CLR heap scan completed with failure during disposal."); }
        GC.SuppressFinalize(this);
    }
}
