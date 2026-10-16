using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Monica.DataChannel.Abstractions.Communication;
using Monica.DataChannel.Pipeline;
using Monica.Tool.Extensions;

namespace Monica.DataChannel.Providers.UDP;

/// <summary>Owns a UDP socket and awaits each received datagram's pipeline delivery.</summary>
public class UdpEndpoint(UdpOptions metadata, ILogger<UdpEndpoint> logger) : CommunicationEndpointBase<UdpOptions>(metadata)
{
    private UdpClient? _client;
    private IPEndPoint? _remoteEndpoint;
    private CancellationTokenSource? _source;
    private Task? _receiveTask;

    /// <summary>Gets the bound socket address after initialization, including an allocated ephemeral port.</summary>
    public IPEndPoint? LocalEndpoint => _client?.Client.LocalEndPoint as IPEndPoint;

    /// <inheritdoc />
    public override async Task ReceiveDataAsync(ChannelDataContext data)
    {
        if (Metadata.Direction == ConnectionDirection.Input)
            throw new InvalidOperationException("The UDP endpoint is input-only.");
        var client = _client ?? throw new InvalidOperationException("The UDP endpoint is not initialized.");
        var remote = _remoteEndpoint ?? throw new InvalidOperationException("The UDP destination is not configured.");
        await client.SendAsync(TransportPayload.GetBytes(data.Data), remote, data.CancellationToken);
    }

    /// <inheritdoc />
    public override async Task InitAsync(CancellationToken cancellationToken = default)
    {
        await DisposeAsync(cancellationToken);
        Metadata.EnrichOrValidate();
        cancellationToken.ThrowIfCancellationRequested();
        var local = IPAddress.Parse(Metadata.Address);
        if (Metadata.RemoteAddress is { } remoteAddress && Metadata.RemotePort is { } remotePort)
        {
            var addresses = await Dns.GetHostAddressesAsync(remoteAddress, cancellationToken);
            var address = addresses.FirstOrDefault(address => address.AddressFamily == local.AddressFamily)
                ?? throw new InvalidOperationException("The UDP destination and bind address use different address families.");
            _remoteEndpoint = new IPEndPoint(address, remotePort);
        }
        _client = new UdpClient(new IPEndPoint(local, Metadata.Port));
        _source = new CancellationTokenSource();
        if (Metadata.Direction is ConnectionDirection.Input or ConnectionDirection.InputAndOutput)
            _receiveTask = ReceiveAsync(_client, _source.Token);
    }

    private async Task ReceiveAsync(UdpClient client, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var received = await client.ReceiveAsync(cancellationToken);
                var context = CreateData(received.Buffer);
                context.CancellationToken = cancellationToken;
                context.Metadata.Set("RemoteEndpoint", received.RemoteEndPoint.ToString());
                await SendDataAsync(context);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested) { break; }
            catch (SocketException) when (cancellationToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                CollectException(exception, description: "UDP receive or pipeline delivery failed.", logger: logger);
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public override async Task DisposeAsync(CancellationToken cancellationToken = default)
    {
        var source = _source;
        var task = _receiveTask;
        _source = null;
        _receiveTask = null;
        source?.Cancel();
        _client?.Dispose();
        _client = null;
        _remoteEndpoint = null;
        try
        {
            if (task is not null)
                await task.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (source?.IsCancellationRequested == true && !cancellationToken.IsCancellationRequested) { }
        finally { source?.Dispose(); }
    }

    /// <inheritdoc />
    public override ConnectionDirection SupportedConnectionDirection() => ConnectionDirection.InputAndOutput;
}
