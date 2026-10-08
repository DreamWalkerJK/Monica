using Microsoft.AspNetCore.Builder;
using Monica.Core.Modularity.Abstractions;
using Monica.Modules;
using Monica.Testing.Hosting;

namespace Test.Monica.Profiling.UIRuntimeMetrics.Support;

internal sealed class RuntimeMetricsTestApplicationFactory(
    int refreshIntervalMs = 2000,
    int maxHistoryPoints = 8,
    int sampleIntervalMs = 50)
    : MonicaTestApplicationFactory<RuntimeMetricsTestApplicationFactory>
{
    protected override void ConfigureHost(WebApplicationBuilder builder)
    {
        builder.Environment.ApplicationName = typeof(ModuleRuntimeMetricsUI).Assembly.GetName().Name!;
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
        builder.AddRuntimeMetricsUI(options => options.AutoRefreshIntervalMs = refreshIntervalMs);
    }
}
