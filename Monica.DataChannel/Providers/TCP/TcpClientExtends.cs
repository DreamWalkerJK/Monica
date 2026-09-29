using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Monica.DataChannel.Providers.TCP.Utils;

namespace Monica.DataChannel.Providers.TCP;

internal sealed partial class TcpClientExtends : IAsyncDisposable
{
    private readonly TcpConnectionRuntime _runtime;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    internal TcpClientExtends(TcpConnectionRuntime runtime) => _runtime = runtime;

    internal bool Connected { get; set; }
    internal DateTime? LastSendMsgTime { get; set; }
    internal TcpClient? Client { get; set; }
    internal Func<MsgReceivedEventArgs, CancellationToken, Task>? MsgReceivedEvent { get; set; }
    internal bool IsMainThread { get; set; }
    internal bool IsServerConnection { get; set; }
    internal string? ConnectionName { get; set; }
    internal Guid ConnectionEpoch { get; set; } = Guid.NewGuid();

    internal async Task SendMsg(ReadOnlyMemory<byte> bytes, ILogger logger, CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            if (!Connected || Client is not { } client)
                throw new IOException($"TCP connection '{ConnectionName}' is not connected.");
            try
            {
                await client.GetStream().WriteAsync(bytes, cancellationToken);
                LastSendMsgTime = DateTime.UtcNow;
                logger.LogDebug("Sent {Length} byte(s) to TCP connection {ConnectionName}.", bytes.Length, ConnectionName);
            }
            catch (Exception exception) when (exception is SocketException or IOException)
            {
                Connected = false;
                client.Dispose();
                if (IsServerConnection) _runtime.HandleServerDisconnect(this);
                logger.LogError(exception, "Failed to write to TCP connection {ConnectionName}.", ConnectionName);
                // The application owns retries: a failed write may already have sent part of the payload.
                throw;
            }
        }
        finally { _writeLock.Release(); }
    }
}
