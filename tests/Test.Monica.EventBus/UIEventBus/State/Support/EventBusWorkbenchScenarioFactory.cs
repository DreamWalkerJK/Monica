using Monica.Core.Modularity.Abstractions;
using Monica.Framework.UI.UIEventBus.State;
using Monica.Modules;
using Monica.Testing.Hosting;

namespace Test.Monica.EventBus.UIEventBus.State.Support;

internal sealed class EventBusWorkbenchScenarioFactory : MonicaTestApplicationFactory<EventBusWorkbenchState>
{
    protected override void ConfigureMonica(IMonicaBuilder builder)
    {
        builder.ConfigureModuleSystem(options =>
        {
            options.AutoAddMonicaHttpListener = false;
            options.EnableMinimalApiByDefault = false;
        });
        builder.AddEventBus(options => options.DisableAutoDiscovery = true)
            .UseNoOpDistributedEventBus()
            .AddKeyedLocalEventBus("default")
            .AddKeyedLocalEventBus("orders")
            .AddKeyedLocalEventBus("Orders");
        builder.AddEventBusUI();
    }
}
