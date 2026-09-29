using Microsoft.Extensions.Logging;
using Monica.DataChannel.Abstractions.Communication;
using Monica.DataChannel.Pipeline;
using Monica.DataChannel.Providers.TCP.Utils;
using Monica.Tool.Extensions;

namespace Monica.DataChannel.Providers.TCP;

/// <summary>Connects a pipeline to a host-owned TCP listener. Sends target only connections accepted by this listener.</summary>
/// <param name="metadata">The TCP listener configuration.</param>
/// <param name="logger">The host logger for connection events.</param>
/// <param name="runtime">The current host's TCP connection runtime.</param>
public class TcpServerEndpoint(TcpServerOptions metadata, ILogger<TcpServerEndpoint> logger, TcpConnectionRuntime runtime)
    : CommunicationEndpointBase<TcpServerOptions>(metadata)
{
    private TcpServerExtends? _server;

    /// <summary>Gets the bound listener address after initialization, including an allocated ephemeral port.</summary>
    public System.Net.IPEndPoint? LocalEndpoint => _server?.Server?.LocalEndpoint as System.Net.IPEndPoint;

    /// <inheritdoc />
    public override async Task ReceiveDataAsync(ChannelDataContext data)
    {
        if (Metadata.Direction == ConnectionDirection.Input)
            throw new InvalidOperationException("The TCP endpoint is input-only.");
        var key = data.Metadata.GetOrDefault("ConnectionName") as string;
        var bytes = TransportPayload.GetBytes(data.Data);
        var clients = runtime.GetServerClients(Metadata.ServerAddress.Key)
            .Where(client => client.Connected && (key is null || client.ConnectionName == key)).ToArray();
        if (clients.Length == 0)
            throw new IOException("The TCP listener has no matching connected client.");
        foreach (var client in clients)
            await client.SendMsg(bytes, logger, data.CancellationToken);
    }

    /// <inheritdoc />
    public override async Task InitAsync(CancellationToken cancellationToken = default)
    {
        await DisposeAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        _server = new TcpServerExtends(runtime);
        _server.ReceivedMsgEvent = async (eventArgs, token) =>
        {
            if (Metadata.Direction == ConnectionDirection.Output) return;
            var data = CreateData(eventArgs.Data);
            data.CancellationToken = token;
            data.Metadata.Set("ConnectionName", eventArgs.ConnectionName);
            data.Metadata.Set("ConnectionEpoch", eventArgs.ConnectionEpoch);
            await SendDataAsync(data);
        };
        _server.Init(metadata, logger);
    }

    /// <inheritdoc />
    public override ConnectionDirection SupportedConnectionDirection() => ConnectionDirection.InputAndOutput;

    /// <inheritdoc />
    public override async Task DisposeAsync(CancellationToken cancellationToken = default)
    {
        if (_server is { } server) await server.DisposeAsync();
        _server = null;
    }
}
