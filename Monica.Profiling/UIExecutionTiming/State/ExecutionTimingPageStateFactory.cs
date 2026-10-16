using Microsoft.Extensions.Options;
using Monica.Modules;
using Monica.Profiling.ExecutionTiming.Facades;

namespace Monica.Profiling.UIExecutionTiming.State;

/// <summary>
/// Creates page-owned execution timing state so periodic work ends with the rendered page.
/// </summary>
/// <remarks>
/// Each state owns only its polling and retained evidence; it never disposes the scoped facade or host collector.
/// An optional registered clock controls page polling and capture times, with the system clock as the fallback.
/// </remarks>
/// <param name="executionTimingFacade">The host-owned diagnostics facade shared within this scope.</param>
/// <param name="options">The page polling interval; non-positive values permit only manual reads.</param>
/// <param name="timeProvider">The page clock, or the system clock when no clock is registered.</param>
public sealed class ExecutionTimingPageStateFactory(
    ExecutionTimingFacade executionTimingFacade,
    IOptions<ModuleExecutionTimingUIOption> options,
    TimeProvider? timeProvider = null)
{
    /// <summary>Creates a page-state factory using the system clock.</summary>
    /// <param name="facade">The host-owned diagnostics facade shared within this scope.</param>
    /// <param name="configuration">The page polling configuration.</param>
    public ExecutionTimingPageStateFactory(ExecutionTimingFacade facade, IOptions<ModuleExecutionTimingUIOption> configuration)
        : this(facade, configuration, null)
    {
    }

    /// <summary>
    /// Creates a fresh state instance for one rendered page.
    /// </summary>
    public ExecutionTimingPageState Create() => new(executionTimingFacade, options, timeProvider);
}
