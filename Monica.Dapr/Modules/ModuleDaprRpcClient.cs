using Monica.Core.Modularity;
using Monica.Core.Modularity.Abstractions;
using Monica.Dapr.Services;
using Monica.Dapr.Providers;

// ReSharper disable once CheckNamespace
namespace Monica.Modules;

public static class ModuleDaprRpcClientBuilderExtensions
{
    /// <summary>Selects Dapr HTTP invocation and structured Dapr error classification for RPC clients.</summary>
    /// <remarks>Registers the Dapr client and sidecar health monitoring. Configure the complete call deadline on ModuleRpcClientOption.</remarks>
    public static ModuleRegistration<ModuleDaprRpcClient, ModuleDaprRpcClientOption> UseDaprProvider(
        this ModuleRegistration<ModuleRpcClient, ModuleRpcClientOption> module,
        Action<ModuleDaprRpcClientOption>? action = null)
    {
        return module.Include<ModuleDaprRpcClient, ModuleDaprRpcClientOption>(action);
    }
}

public class ModuleDaprRpcClient : MonicaModule<ModuleDaprRpcClientOption>
{

    public override void Describe(ModuleDescriptor module)
    {
        module.Require<ModuleDaprClient, ModuleDaprClientOption>();
        module.Require<ModuleRpcClient, ModuleRpcClientOption>(options =>
        {
            options.UseHttpClientProvider<DaprRpcClientProvider>();
            options.UseResponseClassifier<DaprRemoteResponseClassifier>("dapr");
        });
    }
}



public class ModuleDaprRpcClientOption : ModuleOptions<ModuleDaprRpcClient>
{
}
