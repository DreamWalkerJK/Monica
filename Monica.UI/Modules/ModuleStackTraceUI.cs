using Microsoft.Extensions.DependencyInjection;
using Monica.Core;
using Monica.Core.Modularity;
using Monica.Core.Modularity.Abstractions;
using Monica.UI.Localization;
using Monica.UI.UIStackTrace.Support;


// ReSharper disable once CheckNamespace
namespace Monica.Modules;

/// <summary>Provides registration for shared diagnostic source viewers.</summary>
public static class ModuleStackTraceUIBuilderExtensions
{
    extension(IMonicaBuilder builder)
    {
        /// <summary>
        /// Registers shared diagnostic source viewers, their stateless parser, and localization resources.
        /// </summary>
        /// <param name="action">Optional module configuration.</param>
        /// <returns>The host-bound stack trace UI registration.</returns>
        /// <remarks>
        /// The host supplies MudBlazor services/providers, Monica static assets, and the shared
        /// <c>MoClipboard.copyText</c> script for copying. Registering this module does not add the shell.
        /// </remarks>
        public ModuleRegistration<ModuleStackTraceUI, ModuleStackTraceUIOption> AddStackTraceUI(
            Action<ModuleStackTraceUIOption>? action = null)
        {
            return builder.AddModule<ModuleStackTraceUI, ModuleStackTraceUIOption>(action);
        }
    }
}

/// <summary>
/// Provides shared source-preserving diagnostic viewers with host-owned localization and parser services.
/// </summary>
public class ModuleStackTraceUI : MonicaModule<ModuleStackTraceUIOption>, IUIModule
{
    /// <inheritdoc />
    public override void Describe(ModuleDescriptor module)
    {
        module.Require<ModuleLocalization, ModuleLocalizationOption>(static option => option.AddResource<SharedResource>());
    }

    /// <inheritdoc />
    public override void ConfigureServices(ModuleContext<ModuleStackTraceUIOption> context)
    {
        // Register the stateless stack-trace parser once per host.
        context.Services.AddSingleton<StackTraceParser>();
    }
}

/// <summary>
/// Configures the shared stack trace UI module, which currently has no additional options.
/// </summary>
public class ModuleStackTraceUIOption : ModuleOptions<ModuleStackTraceUI>
{
}
