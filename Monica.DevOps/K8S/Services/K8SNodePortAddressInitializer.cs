using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monica.Modules;

namespace Monica.DevOps.K8S.Services;

/// <summary>
/// Publishes external addresses once in the background without delaying host startup.
/// </summary>
internal sealed class K8SNodePortAddressInitializer(
    K8SNodePortAddressResolver resolver,
    IOptions<ModuleK8SOption> options,
    ILogger<K8SNodePortAddressInitializer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var addresses = await resolver.ResolveAsync(stoppingToken);
            stoppingToken.ThrowIfCancellationRequested();
            if (addresses is not null)
            {
                Environment.SetEnvironmentVariable(
                    options.Value.NodePortAddresses.ListeningAddressesEnvironmentVariable,
                    string.Join(";", addresses));
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to publish Kubernetes Service NodePort addresses.");
        }
    }
}
