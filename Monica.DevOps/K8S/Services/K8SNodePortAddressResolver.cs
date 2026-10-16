using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monica.DevOps.K8S.Providers.InCluster;
using Monica.Modules;

namespace Monica.DevOps.K8S.Services;

internal sealed class K8SNodePortAddressResolver(
    IOptions<ModuleK8SOption> options,
    IK8SInClusterServiceReader serviceReader,
    ILogger<K8SNodePortAddressResolver> logger)
{
    /// <summary>
    /// Resolves a complete address set, or returns null when the published value should remain unchanged.
    /// An empty ports array resolves to an empty set, preserving the previous publication behavior.
    /// </summary>
    public async Task<IReadOnlyList<string>?> ResolveAsync(CancellationToken cancellationToken)
    {
        var addressOptions = options.Value.NodePortAddresses;
        var baseAddress = Environment.GetEnvironmentVariable(addressOptions.BaseAddressEnvironmentVariable);
        if (string.IsNullOrEmpty(baseAddress))
        {
            logger.LogError("Environment variable {EnvironmentVariable} is not set.",
                addressOptions.BaseAddressEnvironmentVariable);
            return null;
        }

        var serviceName = Environment.GetEnvironmentVariable(addressOptions.ServiceNameEnvironmentVariable);
        if (string.IsNullOrEmpty(serviceName))
        {
            logger.LogError("Environment variable {EnvironmentVariable} is not set.",
                addressOptions.ServiceNameEnvironmentVariable);
            return null;
        }

        var json = await serviceReader.ReadServiceAsync(serviceName, cancellationToken);
        if (json is null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("spec", out var spec)
            || !spec.TryGetProperty("type", out var type))
        {
            return null;
        }

        if (type.GetString() != "NodePort")
        {
            logger.LogError("Kubernetes Service {ServiceName} is not a NodePort Service.", serviceName);
            return null;
        }

        return spec.TryGetProperty("ports", out var ports)
            ? ports.EnumerateArray()
                .Select(port => $"http://{baseAddress}:{port.GetProperty("nodePort")}")
                .ToArray()
            : null;
    }
}
