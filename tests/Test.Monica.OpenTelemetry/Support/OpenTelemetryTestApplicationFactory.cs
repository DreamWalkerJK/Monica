using Microsoft.AspNetCore.Builder;
using Monica.Core.Modularity.Abstractions;
using Monica.Modules;
using Monica.Testing.Hosting;

namespace Test.Monica.OpenTelemetry.Support;

/// <summary>Composes one real metrics/UI host with a scenario-owned meter namespace.</summary>
/// <remarks>Use one factory per application so concurrent listeners cannot consume another scenario's instruments.</remarks>
internal sealed class OpenTelemetryTestApplicationFactory(
    int samplesPerSeries = 4,
    int maxTagSetsPerInstrument = 8,
    TimeSpan? observableInterval = null,
    bool enableMinimalApi = false)
    : MonicaTestApplicationFactory<OpenTelemetryTestApplicationFactory>
{
    public string MeterName { get; } = $"Monica.UiScenarios.UnitTests.{Guid.NewGuid():N}";

    public string OtherMeterName => MeterName + ".Other";

    protected override void ConfigureHost(WebApplicationBuilder builder)
    {
        builder.Environment.ApplicationName = typeof(ModuleOpenTelemetryUI).Assembly.GetName().Name!;
    }

    protected override void ConfigureMonica(IMonicaBuilder builder)
    {
        builder.ConfigureModuleSystem(options =>
        {
            options.AutoAddMonicaHttpListener = false;
            options.EnableMinimalApiByDefault = false;
        });
        builder.ConfigureTypeDiscovery(discovery => discovery.ExcludeDefault());
        builder.AddOpenTelemetry(options =>
        {
            options.ResourceServiceName = "OpenTelemetry behavior test host";
            options.EnableMinimalApi = enableMinimalApi;
            options.MeterPatterns = [MeterName + "*"];
            options.IncludeInstrumentationMetersInProcessCollector = false;
            options.IncludeAspNetCoreInstrumentation = false;
            options.IncludeHttpClientInstrumentation = false;
            options.IncludeRuntimeInstrumentation = false;
            options.SamplesPerSeries = samplesPerSeries;
            options.MaxTagSetsPerInstrument = maxTagSetsPerInstrument;
            options.ObservableInstrumentInterval = observableInterval ?? TimeSpan.FromDays(1);
        }).UseInProcessCollector();
        builder.AddOpenTelemetryUI();
    }
}
