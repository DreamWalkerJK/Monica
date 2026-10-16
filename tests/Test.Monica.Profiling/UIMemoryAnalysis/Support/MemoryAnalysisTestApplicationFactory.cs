using Microsoft.AspNetCore.Builder;
using Monica.Core.Modularity.Abstractions;
using Monica.Modules;
using Monica.Testing.Hosting;

namespace Test.Monica.Profiling.UIMemoryAnalysis.Support;

internal sealed class MemoryAnalysisTestApplicationFactory(
    int refreshIntervalMs = 2000,
    bool allowManualGc = false,
    bool allowGcDump = false,
    int maxHistoryPoints = 8,
    int sampleIntervalMs = 50,
    bool enableAllocation = false)
    : MonicaTestApplicationFactory<MemoryAnalysisTestApplicationFactory>
{
    protected override void ConfigureHost(WebApplicationBuilder builder)
    {
        builder.Environment.ApplicationName = typeof(ModuleMemoryAnalysisUI).Assembly.GetName().Name!;
    }

    protected override void ConfigureMonica(IMonicaBuilder builder)
    {
        builder.ConfigureModuleSystem(options =>
        {
            options.EnableMinimalApiByDefault = false;
            options.AutoAddMonicaHttpListener = false;
        });
        builder.AddRuntimeMetrics(options =>
        {
            options.EnableMinimalApi = false;
            options.MaxHistoryPoints = maxHistoryPoints;
            options.SampleIntervalMs = sampleIntervalMs;
        });
        var registration = builder.AddMemoryAnalysisUI(options =>
        {
            options.AutoRefreshIntervalMs = refreshIntervalMs;
            options.AllowManualGC = allowManualGc;
            options.AllowGcDump = allowGcDump;
        });
        if (enableAllocation) registration.EnableTypeAllocationTab();
    }
}
