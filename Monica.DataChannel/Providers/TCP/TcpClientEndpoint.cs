using Microsoft.Extensions.Logging;
using Monica.DataChannel.Abstractions.Communication;
using Monica.DataChannel.Pipeline;
using Monica.DataChannel.Providers.TCP.Utils;
using Monica.Tool.Extensions;

namespace Monica.DataChannel.Providers.TCP;

/// <summary>
/// Connects a data-channel pipeline to an outbound TCP client owned by the current host.
/// </summary>
/// <param name="metadata">The TCP client configuration.</param>
/// <param name="logger">The host logger for connection events.</param>
/// <param name="runtime">The current host's TCP connection runtime.</param>
public class TcpClientEndpoint(
    TcpClientOptions metadata,
    ILogger<TcpClientEndpoint> logger,
    TcpConnectionRuntime runtime) : CommunicationEndpointBase<TcpClientOptions>(metadata)
{
    private TcpClientExtends? _client;

    /// <summary>Gets whether this endpoint currently has a connected peer. A later write can still fail.</summary>
    public bool IsConnected => _client?.Connected == true;

    /// <inheritdoc />
    public override Task ReceiveDataAsync(ChannelDataContext data)
    {
        if (Metadata.Direction == ConnectionDirection.Input)
            throw new InvalidOperationException("The TCP endpoint is input-only.");
        return GetClient().SendMsg(TransportPayload.GetBytes(data.Data), logger, data.CancellationToken);
    }

    /// <inheritdoc />
    public override async Task InitAsync(CancellationToken cancellationToken = default)
    {
        await DisposeAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        _client = new TcpClientExtends(runtime);
        _client.MsgReceivedEvent = async (eventArgs, token) =>
        {
            if (Metadata.Direction == ConnectionDirection.Output) return;
            var data = CreateData(eventArgs.Data);
            data.CancellationToken = token;
            data.Metadata.Set("ConnectionName", eventArgs.ConnectionName);
            data.Metadata.Set("ConnectionEpoch", eventArgs.ConnectionEpoch);
            await SendDataAsync(data);
        };
        _client.Init(metadata, logger);
    }

    /// <inheritdoc />
    public override ConnectionDirection SupportedConnectionDirection()
    {
        return ConnectionDirection.InputAndOutput;
    }

    /// <inheritdoc />
    public override async Task DisposeAsync(CancellationToken cancellationToken = default)
    {
        if (_client is { } client) await client.DisposeAsync();
        _client = null;
    }

    private TcpClientExtends GetClient()
    {
        return _client ?? throw new InvalidOperationException("TCP client is not initialized.");
    }
}
