using Microsoft.Extensions.Logging;
using Monica.Profiling.TypeAllocation.Facades;

namespace Monica.Profiling.UIMemoryAnalysis.State;

/// <summary>
/// Creates independent allocation workspaces that retain source evidence and retire their own subscriptions.
/// </summary>
/// <param name="typeAllocationFacade">Borrowed scoped entry point to the host-owned allocation providers.</param>
/// <param name="timeProvider">Optional adoption/action clock; omitted clocks use the system UTC clock.</param>
/// <param name="logger">Optional state retirement logger; provider failures retain their facade envelopes.</param>
public sealed class TypeAllocationPanelStateFactory(TypeAllocationFacade typeAllocationFacade, TimeProvider? timeProvider = null,
    ILogger<TypeAllocationPanelState>? logger = null)
{
    /// <summary>Preserves the original factory constructor with the system clock.</summary>
    public TypeAllocationPanelStateFactory(TypeAllocationFacade typeAllocationFacade) : this(typeAllocationFacade, null) { }

    /// <summary>
    /// Creates a fresh state instance for one rendered allocation panel.
    /// </summary>
    public TypeAllocationPanelState Create() => new(typeAllocationFacade, timeProvider, logger);
}
