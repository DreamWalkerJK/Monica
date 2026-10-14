using Microsoft.Extensions.Options;
using Monica.Modules;
using Monica.Profiling.MemoryDiagnostics.Facades;
using Monica.Profiling.RuntimeMetrics.Facades;

namespace Monica.Profiling.UIMemoryAnalysis.State;

/// <summary>
/// Creates independent page-owned memory evidence and diagnostic action state.
/// </summary>
/// <param name="memoryDiagnosticsFacade">The scoped production memory diagnostics facade.</param>
/// <param name="runtimeMetricsFacade">The scoped production runtime metrics facade.</param>
/// <param name="options">The host's polling and diagnostic action policy.</param>
/// <param name="timeProvider">The page clock, or the system clock when none is registered.</param>
public sealed class MemoryAnalysisPageStateFactory(
    MemoryDiagnosticsFacade memoryDiagnosticsFacade,
    RuntimeMetricsFacade runtimeMetricsFacade,
    IOptions<ModuleMemoryAnalysisUIOption> options,
    TimeProvider? timeProvider = null)
{
    /// <summary>Preserves the original factory constructor using the system clock.</summary>
    /// <param name="memoryDiagnosticsFacade">The scoped production memory diagnostics facade.</param>
    /// <param name="runtimeMetricsFacade">The scoped production runtime metrics facade.</param>
    /// <param name="options">The host's polling and diagnostic action policy.</param>
    public MemoryAnalysisPageStateFactory(
        MemoryDiagnosticsFacade memoryDiagnosticsFacade,
        RuntimeMetricsFacade runtimeMetricsFacade,
        IOptions<ModuleMemoryAnalysisUIOption> options)
        : this(memoryDiagnosticsFacade, runtimeMetricsFacade, options, null)
    {
    }

    /// <summary>
    /// Creates a fresh state instance for one rendered page.
    /// </summary>
    public MemoryAnalysisPageState Create() => Create(true);

    /// <summary>
    /// Creates state with an explicit initial polling preference. Diagnostics can pass false
    /// to read on demand; non-positive intervals remain manual regardless of this preference.
    /// </summary>
    /// <param name="autoRefresh">Whether the page should initially use configured polling.</param>
    public MemoryAnalysisPageState Create(bool autoRefresh) =>
        new(memoryDiagnosticsFacade, runtimeMetricsFacade, options, timeProvider, autoRefresh);
}
