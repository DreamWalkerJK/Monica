using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Monica.DataChannel.Providers.TCP.Utils;

/// <summary>
/// Owns TCP connections, listeners, and listener-local standby selection for one application host.
/// </summary>
/// <remarks>
/// The DataChannel module registers this type as a singleton in the current host. Its state is never
/// shared with another host in the same process. Application code should consume DataChannel abstractions
/// instead of mutating this provider runtime directly.
/// </remarks>
public sealed class TcpConnectionRuntime
{
    private static readonly TimeSpan HEARTBEAT_POLL_INTERVAL = TimeSpan.FromSeconds(1);

    private readonly object _serverGroupLock = new();
    private readonly ConcurrentDictionary<string, TcpClientExtends> _clients = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, TcpServerExtends> _servers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>> _serverConnectionGroups = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _serverGroupByConnection = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the delay used between outbound TCP reconnection attempts.
    /// </summary>
    internal static TimeSpan ReconnectDelay { get; } = TimeSpan.FromSeconds(2);

    internal bool TryGetClient(string key, out TcpClientExtends? client)
    {
        return _clients.TryGetValue(key, out client);
    }

    internal TcpClientExtends[] GetServerClients(string serverKey)
    {
        return _clients.Values.Where(client => client.IsServerConnection && client.ConnectionName?.StartsWith(serverKey + "|", StringComparison.Ordinal) == true).ToArray();
    }

    internal void SetClient(string key, TcpClientExtends client)
    {
        _clients[key] = client;
    }

    internal bool TryGetServer(string key, out TcpServerExtends? server)
    {
        return _servers.TryGetValue(key, out server);
    }

    internal void SetServer(string key, TcpServerExtends server)
    {
        _servers[key] = server;
    }

    internal void UnregisterOutboundClient(TcpClientExtends connection)
    {
        var connectionName = connection.ConnectionName;
        if (connectionName is null)
        {
            return;
        }

        if (_clients.TryGetValue(connectionName, out var registeredConnection) &&
            ReferenceEquals(registeredConnection, connection))
        {
            _clients.TryRemove(connectionName, out _);
        }
    }

    internal void UnregisterServer(string serverKey, TcpServerExtends server)
    {
        if (_servers.TryGetValue(serverKey, out var registeredServer) &&
            ReferenceEquals(registeredServer, server))
        {
            _servers.TryRemove(serverKey, out _);
        }

        lock (_serverGroupLock)
        {
            var groupPrefix = $"{serverKey}:";
            var groupKeys = _serverConnectionGroups.Keys
                .Where(key => key.StartsWith(groupPrefix, StringComparison.Ordinal))
                .ToArray();

            foreach (var groupKey in groupKeys)
            {
                var connectionNames = _serverConnectionGroups[groupKey];
                _serverConnectionGroups.Remove(groupKey);

                foreach (var connectionName in connectionNames)
                {
                    _serverGroupByConnection.Remove(connectionName);
                    if (_clients.TryRemove(connectionName, out var connection))
                    {
                        connection.Connected = false;
                        connection.Client?.Dispose();
                    }
                }
            }
        }
    }

    internal void RegisterServerClient(string groupKey, string connectionName, TcpClientExtends connection)
    {
        lock (_serverGroupLock)
        {
            if (!_serverConnectionGroups.TryGetValue(groupKey, out var connections))
            {
                connections = [];
                _serverConnectionGroups.Add(groupKey, connections);
            }

            var isPrimary = connections.Count == 0;
            connections.Add(connectionName);
            _serverGroupByConnection.Add(connectionName, groupKey);
            connection.IsServerConnection = true;
            connection.IsMainThread = isPrimary;
            _clients[connectionName] = connection;
        }
    }

    internal Task ReceiveClientAsync(TcpClientExtends connection, ILogger logger, CancellationToken cancellationToken) =>
        ReceiveAsync(connection, logger, connection.MsgReceivedEvent, cancellationToken);

    internal Task ReceiveServerAsync(
        TcpClientExtends connection,
        ILogger logger,
        Func<MsgReceivedEventArgs, CancellationToken, Task>? handler,
        CancellationToken cancellationToken) => ReceiveAsync(connection, logger, handler, cancellationToken);

    internal async Task SendServerHeartbeatAsync(
        TcpClientExtends connection,
        ILogger logger,
        TimeSpan? sendInterval,
        CancellationToken cancellationToken)
    {
        if (sendInterval is null)
        {
            return;
        }

        connection.LastSendMsgTime ??= DateTime.UtcNow;
        while (connection.Connected && !cancellationToken.IsCancellationRequested)
        {
            var elapsed = DateTime.UtcNow - connection.LastSendMsgTime.GetValueOrDefault();
            if (elapsed >= sendInterval.Value)
            {
                await connection.SendMsg(Encoding.UTF8.GetBytes(CreateHeartbeatPayload()), logger, cancellationToken);
            }

            await Task.Delay(HEARTBEAT_POLL_INTERVAL, cancellationToken);
        }
    }

    internal void HandleServerDisconnect(TcpClientExtends connection)
    {
        var connectionName = connection.ConnectionName;
        if (connectionName is null)
        {
            return;
        }

        lock (_serverGroupLock)
        {
            _clients.TryRemove(connectionName, out _);
            if (!_serverGroupByConnection.Remove(connectionName, out var groupKey) ||
                !_serverConnectionGroups.TryGetValue(groupKey, out var connections))
            {
                return;
            }

            connections.Remove(connectionName);
            if (connections.Count == 0)
            {
                _serverConnectionGroups.Remove(groupKey);
                return;
            }

            if (!connection.IsMainThread)
            {
                return;
            }

            var nextPrimary = connections
                .Select(name => _clients.GetValueOrDefault(name))
                .FirstOrDefault(candidate => candidate?.Connected == true);
            if (nextPrimary is not null)
            {
                nextPrimary.IsMainThread = true;
            }
        }
    }

    private async Task ReceiveAsync(
        TcpClientExtends connection,
        ILogger logger,
        Func<MsgReceivedEventArgs, CancellationToken, Task>? handler,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        try
        {
            var stream = connection.Client?.GetStream()
                ?? throw new IOException("TCP connection has no stream.");
            while (connection.Connected && !cancellationToken.IsCancellationRequested)
            {

                var count = await stream.ReadAsync(buffer, cancellationToken);
                if (count == 0) break;
                if (!connection.IsMainThread || handler is null) continue;
                try
                {
                    await handler(new MsgReceivedEventArgs
                    {
                        Data = buffer[..count],
                        ConnectionName = connection.ConnectionName,
                        ConnectionEpoch = connection.ConnectionEpoch
                    }, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
                catch (Exception exception)
                {
                    // Pipeline dispatch already records the exception. Keep the receiver alive for subsequent input.
                    logger.LogError(exception, "TCP pipeline delivery failed for {ConnectionName}.", connection.ConnectionName);
                }
            }
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception) when (exception is SocketException or IOException or ObjectDisposedException)
        {
            logger.LogWarning(exception, "TCP connection {ConnectionName} was interrupted.", connection.ConnectionName);
        }
        finally
        {
            connection.Connected = false;
            connection.Client?.Dispose();
            if (connection.IsServerConnection) HandleServerDisconnect(connection);

        }
    }

    private static string CreateHeartbeatPayload()
    {
        return "ZCZC\r\n" +
               "-TITLE SHBT\r\n" +
               "-BEGIN REFDATA\r\n" +
               "-SENDER -FAC ZTMA\r\n" +
               "-RECVR -FAC ZUUU\r\n" +
               "-END REFDATA\r\n" +
               "NNNN";
    }
}
