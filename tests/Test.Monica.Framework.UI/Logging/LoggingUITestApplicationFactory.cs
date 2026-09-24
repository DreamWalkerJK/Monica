using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Monica.Core.Modularity.Abstractions;
using Monica.Modules;
using Monica.Testing.Hosting;

namespace Test.Monica.Framework.UI.Logging;

/// <summary>
/// Composes the production logging UI graph with a scenario-owned log directory and disabled output sinks.
/// Dispose the application before disposing this factory so every response stream has released its file.
/// </summary>
internal sealed class LoggingUITestApplicationFactory(
    bool enableMinimalApiByDefault = false,
    bool? enableMinimalApi = null,
    int? endpointPort = null,
    Action<HttpContext>? configureRequest = null)
    : MonicaTestApplicationFactory<ModuleLoggingUI>, IDisposable
{
    private readonly DirectoryInfo _logDirectory = Directory.CreateTempSubdirectory("monica-logging-ui-");

    public string LogDirectory => _logDirectory.FullName;

    protected override void ConfigureHost(WebApplicationBuilder builder)
    {
        // Use the UI project's real static-asset manifest when composing its shell in TestServer.
        builder.Environment.ApplicationName = typeof(ModuleLoggingUI).Assembly.GetName().Name!;
    }

    protected override void ConfigureMonica(IMonicaBuilder builder)
    {
        builder.ConfigureModuleSystem(options =>
        {
            options.EnableMinimalApiByDefault = enableMinimalApiByDefault;
            options.MonicaEndpointPort = endpointPort;
            options.AutoAddMonicaHttpListener = false;
        });
        builder.AddLogging(options =>
        {
            options.EnableConsoleSink = false;
            options.EnableFileSink = false;
            options.LogDirectory = LogDirectory;
        });
        builder.AddLoggingUI(options =>
        {
            options.EnableMinimalApi = enableMinimalApi;
            options.LogDirectory = LogDirectory;
        }).ConfigureApplicationBuilder(context =>
        {
            if (configureRequest is null)
            {
                return;
            }

            context.ApplicationBuilder.Use((httpContext, next) =>
            {
                configureRequest(httpContext);
                return next(httpContext);
            });
        });
    }

    public void Dispose() => _logDirectory.Delete(recursive: true);
}
