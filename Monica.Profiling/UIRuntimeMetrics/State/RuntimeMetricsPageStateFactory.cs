using Microsoft.Extensions.Options;
using Monica.Modules;
using Monica.Profiling.RuntimeMetrics.Facades;

namespace Monica.Profiling.UIRuntimeMetrics.State;

/// <summary>
/// Creates page-owned runtime metrics state so navigation can deterministically stop its refresh loop.
/// </summary>
/// <remarks>States own their polling and retained views; the scoped facade and host collector keep their host lifetimes.</remarks>
/// <param name="runtimeMetricsFacade">The scoped production metrics facade.</param>
/// <param name="options">Page polling configuration; non-positive intervals permit only manual reads.</param>
/// <param name="timeProvider">The optional page polling/capture clock; the system clock is the fallback.</param>
public sealed class RuntimeMetricsPageStateFactory(
    RuntimeMetricsFacade runtimeMetricsFacade,
    IOptions<ModuleRuntimeMetricsUIOption> options,
    TimeProvider? timeProvider = null)
{
    /// <summary>Creates a factory with the system clock, preserving the original constructor contract.</summary>
    /// <param name="runtimeMetricsFacade">The scoped production runtime metrics facade.</param>
    /// <param name="options">The page polling configuration.</param>
    public RuntimeMetricsPageStateFactory(RuntimeMetricsFacade runtimeMetricsFacade, IOptions<ModuleRuntimeMetricsUIOption> options)
        : this(runtimeMetricsFacade, options, null)
    {
    }

    /// <summary>
    /// Creates a fresh state instance for one rendered page.
    /// </summary>
    public RuntimeMetricsPageState Create() => new(runtimeMetricsFacade, options, timeProvider);
}
