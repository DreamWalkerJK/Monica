using Monica.Core.Modularity.Abstractions;
using Monica.Modules;
using Monica.Repository.UI.UIRepository.State;
using Monica.Testing.Hosting;

namespace Test.Monica.Repository.Hosting;

internal sealed class RepositoryDashboardScenarioFactory : MonicaTestApplicationFactory<RepositoryDashboardState>
{
    protected override void ConfigureMonica(IMonicaBuilder builder)
    {
        builder.ConfigureModuleSystem(options =>
        {
            options.AutoAddMonicaHttpListener = false;
            options.EnableMinimalApiByDefault = false;
        });
        builder.AddRepositoryUI(options => options.MigrationCommandTimeout = TimeSpan.FromSeconds(17));
    }
}
