namespace Monica.DevOps.K8S.Models;

/// <summary>
/// Configures startup publication of this Pod's Service NodePort addresses to a process environment variable.
/// </summary>
/// <remarks>
/// This capability uses the mounted ServiceAccount and requires permission to get the current Service.
/// It is independent of SSH runtime configuration and does not change the web server's listening endpoints.
/// </remarks>
public sealed class K8SNodePortAddressOptions
{
    /// <summary>
    /// Gets or sets whether to attempt publication once during host startup. Defaults to <see langword="false"/>.
    /// Failures are logged without failing application startup or replacing an existing published value.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the environment variable containing the current Service name. Defaults to <c>K8S_SERVICE_NAME</c>.
    /// </summary>
    public string ServiceNameEnvironmentVariable { get; set; } = "K8S_SERVICE_NAME";

    /// <summary>
    /// Gets or sets the environment variable containing the node address combined with each NodePort.
    /// Defaults to <c>K8S_HOST_IP</c>.
    /// </summary>
    public string BaseAddressEnvironmentVariable { get; set; } = "K8S_HOST_IP";

    /// <summary>
    /// Gets or sets the destination environment variable. Defaults to <c>K8S_LISTENING_ADDRESSES</c>.
    /// Addresses use HTTP and are separated by semicolons in Service port order. Service discovery consumers
    /// must separately include this key in their metadata collection and display options.
    /// </summary>
    public string ListeningAddressesEnvironmentVariable { get; set; } = "K8S_LISTENING_ADDRESSES";
}
