using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monica.Modules;

namespace Monica.DevOps.K8S.Services;

/// <summary>
/// Publishes external addresses before hosted services begin registering instance metadata.
/// </summary>
internal sealed class K8SNodePortAddressInitializer(
    K8SNodePortAddressResolver resolver,
    IOptions<ModuleK8SOption> options,
    ILogger<K8SNodePortAddressInitializer> logger) : IHostedLifecycleService
{
    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        try
        {
            var addresses = await resolver.ResolveAsync(cancellationToken);
            if (addresses is not null)
            {
                Environment.SetEnvironmentVariable(
                    options.Value.NodePortAddresses.ListeningAddressesEnvironmentVariable,
                    string.Join(";", addresses));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to publish Kubernetes Service NodePort addresses.");
        }
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
