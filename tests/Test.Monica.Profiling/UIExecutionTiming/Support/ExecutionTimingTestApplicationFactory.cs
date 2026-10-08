using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Monica.Core.Modularity.Abstractions;
using Monica.Modules;
using Monica.Profiling.ExecutionTiming.Abstractions;
using Monica.Profiling.ExecutionTiming.Abstractions.Internal;
using Monica.Profiling.ExecutionTiming.Models;
using Monica.Testing.Hosting;

namespace Test.Monica.Profiling.UIExecutionTiming.Support;

internal sealed class ExecutionTimingTestApplicationFactory(
    int refreshIntervalMs = 2000,
    ExecutionTimingAggregationMode aggregationMode = ExecutionTimingAggregationMode.Inline,
    TimingQueryControl? queryControl = null)
    : MonicaTestApplicationFactory<ExecutionTimingTestApplicationFactory>
{
    protected override void ConfigureHost(WebApplicationBuilder builder)
    {
        builder.Environment.ApplicationName = typeof(ModuleExecutionTimingUI).Assembly.GetName().Name!;
    }

    protected override void ConfigureMonica(IMonicaBuilder builder)
    {
        builder.ConfigureModuleSystem(options =>
        {
            options.EnableMinimalApiByDefault = false;
            options.AutoAddMonicaHttpListener = false;
        });
        var timing = builder.AddExecutionTiming(options => options.EnableMinimalApi = false);
        if (aggregationMode == ExecutionTimingAggregationMode.BackgroundBatch)
        {
            timing.UseBackgroundBatchAggregation(TimeSpan.FromHours(1));
        }
        else
        {
            timing.UseInlineAggregation();
        }

        builder.AddExecutionTimingUI(options => options.AutoRefreshIntervalMs = refreshIntervalMs);
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        if (queryControl is not null)
        {
            // The production service is singleton. Its boundary must retain that compatible lifetime.
            services.RemoveAll<IExecutionTimingQuery>();
            services.AddSingleton<IExecutionTimingQuery>(provider => new ControlledTimingQuery(
                provider.GetRequiredService<IExecutionTimingCoordinator>(), queryControl));
        }
    }
}
