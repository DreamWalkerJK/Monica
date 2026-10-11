using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Monica.Modules;
using Monica.Testing.Localization;
using Monica.Utilities.Connectivity.Facades;
using Monica.Utilities.Connectivity.Services;
using Monica.Utilities.Localization;
using Monica.Utilities.Text.Facades;
using Monica.Utilities.Text.Services;
using Monica.Utilities.UIUtilities.State;

namespace Test.Monica.Utilities.Support;

internal static class UtilitiesTestServices
{
    public static TextTransformState CreateTextState() => new(new TextTransformFacade(
        new TextTransformService(),
        new EchoStringLocalizer<UtilitiesResource>(),
        NullLogger<TextTransformFacade>.Instance));

    public static ConnectivityProbeFacade CreateConnectivityFacade(ModuleUtilitiesOption options) => new(
        new ConnectivityProbeService(Options.Create(options)),
        new EchoStringLocalizer<UtilitiesResource>(),
        NullLogger<ConnectivityProbeFacade>.Instance);

    public static ConnectivityProbeState CreateConnectivityState(ModuleUtilitiesOption? options = null)
    {
        options ??= new ModuleUtilitiesOption
        {
            DefaultProbeTimeoutMilliseconds = 15000,
            MaximumProbeTimeoutMilliseconds = 20000
        };
        return new ConnectivityProbeState(CreateConnectivityFacade(options), Options.Create(options));
    }
}
