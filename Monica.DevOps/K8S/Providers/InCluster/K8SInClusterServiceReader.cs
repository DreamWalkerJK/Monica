using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Monica.DevOps.K8S.Providers.InCluster;

internal interface IK8SInClusterServiceReader
{
    Task<string?> ReadServiceAsync(string serviceName, CancellationToken cancellationToken);
}

/// <summary>
/// Reads one Service using the Pod's mounted ServiceAccount without requiring kubectl or SSH access.
/// </summary>
internal sealed class K8SInClusterServiceReader(ILogger<K8SInClusterServiceReader> logger)
    : IK8SInClusterServiceReader
{
    private const string SERVICE_ACCOUNT_DIRECTORY = "/var/run/secrets/kubernetes.io/serviceaccount";

    public async Task<string?> ReadServiceAsync(string serviceName, CancellationToken cancellationToken)
    {
        var namespacePath = Path.Combine(SERVICE_ACCOUNT_DIRECTORY, "namespace");
        var namespaceName = File.Exists(namespacePath)
            ? (await File.ReadAllTextAsync(namespacePath, cancellationToken)).Trim()
            : "default";

        var host = Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST");
        var port = Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_PORT_HTTPS");
        var serviceUrl = $"https://{host}:{port}/api/v1/namespaces/{namespaceName}/services/{serviceName}";

        var token = (await File.ReadAllTextAsync(
            Path.Combine(SERVICE_ACCOUNT_DIRECTORY, "token"), cancellationToken)).Trim();
        using var caCertificate = X509CertificateLoader.LoadCertificateFromFile(
            Path.Combine(SERVICE_ACCOUNT_DIRECTORY, "ca.crt"));
        using var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (_, certificate, chain, _) =>
        {
            if (certificate is null || chain is null)
            {
                return false;
            }

            // Preserve the existing ServiceAccount CA trust policy during the extraction.
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(caCertificate);
            return chain.Build(certificate);
        };

        using var client = new HttpClient(handler);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.GetAsync(serviceUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to get Kubernetes Service {ServiceName}. Status code: {StatusCode}.",
                serviceName, response.StatusCode);
            return null;
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
