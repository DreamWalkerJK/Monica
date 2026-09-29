using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Monica.DataChannel.Providers.TCP;

internal sealed partial class TcpServerExtends
{
    private readonly object _taskLock = new();
    private readonly HashSet<Task> _clientTasks = [];
    private CancellationTokenSource? _source;
    private Task? _acceptTask;
    private string? _serverKey;

    internal void Init(TcpServerOptions metadata, ILogger logger)
    {
        metadata.EnrichOrValidate();
        if (!metadata.IsServer) return;
        var address = metadata.ServerAddress.Value.Address!;
        var listener = new TcpListener(IPAddress.Parse(address.Item1), address.Item2);
        listener.Start(); // Binding is part of initialization, so failures reach the pipeline diagnostics.
        Server = listener;
        _serverKey = metadata.ServerAddress.Key;
        _runtime.SetServer(_serverKey, this);
        _source = new CancellationTokenSource();
        _acceptTask = RunServerLoopAsync(listener, metadata, logger, _source.Token);
    }

    private async Task RunServerLoopAsync(TcpListener listener, TcpServerOptions metadata, ILogger logger, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await listener.AcceptTcpClientAsync(cancellationToken); }
            catch (Exception) when (cancellationToken.IsCancellationRequested) { break; }
            var remote = (IPEndPoint?)client.Client.RemoteEndPoint;
            if (remote is null) { client.Dispose(); continue; }
            var group = $"{metadata.ServerAddress.Key}:{GetNetworkGroup(remote.Address)}";
            var name = $"{metadata.ServerAddress.Key}|{remote.Address}:{remote.Port}";
            var connection = new TcpClientExtends(_runtime)
            {
                Client = client,
                Connected = true,
                ConnectionName = name
            };
            _runtime.RegisterServerClient(group, name, connection);
            var task = RunConnectionAsync(connection, metadata, logger, cancellationToken);
            lock (_taskLock) _clientTasks.Add(task);
            _ = task.ContinueWith(completed =>
            {
                lock (_taskLock) _clientTasks.Remove(completed);
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }

    private async Task RunConnectionAsync(TcpClientExtends connection, TcpServerOptions metadata, ILogger logger, CancellationToken cancellationToken)
    {
        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var heartbeat = RunHeartbeatAsync(connection, metadata.SendTime, logger, source.Token);
        try
        {
            await _runtime.ReceiveServerAsync(connection, logger, ReceivedMsgEvent, source.Token);
        }
        finally
        {
            source.Cancel();
            connection.Client?.Dispose();
            await heartbeat;
        }
    }

    private async Task RunHeartbeatAsync(TcpClientExtends connection, TimeSpan? interval, ILogger logger, CancellationToken cancellationToken)
    {
        try { await _runtime.SendServerHeartbeatAsync(connection, logger, interval, cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "TCP heartbeat failed for {ConnectionName}.", connection.ConnectionName);
            connection.Connected = false;
            connection.Client?.Dispose();
        }
    }

    private static string GetNetworkGroup(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return address.AddressFamily == AddressFamily.InterNetwork
            ? string.Join('.', bytes.Take(3))
            : Convert.ToHexString(bytes.AsSpan(0, Math.Min(8, bytes.Length)));
    }

    public async ValueTask DisposeAsync()
    {
        var source = _source;
        source?.Cancel();
        Server?.Stop();
        if (_serverKey is { } key) _runtime.UnregisterServer(key, this);
        try
        {
            if (_acceptTask is not null) await _acceptTask;
            Task[] clients;
            lock (_taskLock) clients = _clientTasks.ToArray();
            await Task.WhenAll(clients);
        }
        finally
        {
            source?.Dispose();
            _source = null;
            _acceptTask = null;
            _serverKey = null;
            Server = null;
        }
    }
}
