using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Monica.DataChannel.Providers.TCP.Utils;

namespace Monica.DataChannel.Providers.TCP;

internal sealed partial class TcpClientExtends
{
    private CancellationTokenSource? _source;
    private Task? _runTask;

    internal void Init(TcpClientOptions metadata, ILogger logger)
    {
        metadata.EnrichOrValidate();
        ConnectionName = metadata.ClientAddress.Key;
        _source = new CancellationTokenSource();
        _runTask = RunClientLoopAsync(metadata, logger, _source.Token);
    }

    private async Task RunClientLoopAsync(TcpClientOptions metadata, ILogger logger, CancellationToken cancellationToken)
    {
        while (metadata.IsClient && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var address = metadata.ClientAddress.Value.Address!;
                var client = new TcpClient();
                Client = client;
                await client.ConnectAsync(address.Item1, address.Item2, cancellationToken);
                ConnectionEpoch = Guid.NewGuid();
                Connected = true;
                IsMainThread = metadata.ClientAddress.Value.IsMainConnected;
                _runtime.SetClient(ConnectionName!, this);
                logger.LogInformation("TCP client {ConnectionName} connected to {Host}:{Port}.", ConnectionName, address.Item1, address.Item2);
                await _runtime.ReceiveClientAsync(this, logger, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "TCP client {ConnectionName} disconnected; reconnecting after a delay.", ConnectionName);
            }
            finally
            {
                Connected = false;
                Client?.Dispose();
                Client = null;
            }
            try { await Task.Delay(TcpConnectionRuntime.ReconnectDelay, cancellationToken); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
        }
    }

    public async ValueTask DisposeAsync()
    {
        var source = _source;
        source?.Cancel();
        Connected = false;
        Client?.Dispose();
        try
        {
            if (_runTask is not null) await _runTask;
        }
        finally
        {
            source?.Dispose();
            _source = null;
            Client = null;
            if (!IsServerConnection) _runtime.UnregisterOutboundClient(this);
        }
    }
}
