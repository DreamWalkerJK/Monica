using System.IO.Ports;
using Microsoft.Extensions.Logging;
using Monica.DataChannel.Abstractions.Communication;
using Monica.DataChannel.Pipeline;

namespace Monica.DataChannel.Providers.Serial;

/// <summary>Owns a serial port, preserves raw bytes, and awaits inbound pipeline work before reading another chunk.</summary>
public class SerialPortEndpoint(SerialPortOptions metadata, ILogger<SerialPortEndpoint> logger)
    : CommunicationEndpointBase<SerialPortOptions>(metadata)
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private SerialPort? _port;
    private CancellationTokenSource? _source;
    private Task? _receiveTask;

    /// <inheritdoc />
    public override async Task InitAsync(CancellationToken cancellationToken = default)
    {
        await DisposeAsync(cancellationToken);
        Metadata.EnrichOrValidate();
        cancellationToken.ThrowIfCancellationRequested();
        var port = new SerialPort(Metadata.PortName, Metadata.BaudRate, Metadata.Parity, Metadata.DataBits, Metadata.StopBits)
        {
            Handshake = Metadata.Handshake,
            DtrEnable = Metadata.DtrEnable,
            RtsEnable = Metadata.RtsEnable
        };
        try { port.Open(); }
        catch { port.Dispose(); throw; }
        _port = port;
        _source = new CancellationTokenSource();
        if (Metadata.Direction is ConnectionDirection.Input or ConnectionDirection.InputAndOutput)
            _receiveTask = ReceiveAsync(port.BaseStream, _source.Token);
    }

    /// <inheritdoc />
    public override async Task ReceiveDataAsync(ChannelDataContext data)
    {
        if (Metadata.Direction == ConnectionDirection.Input)
            throw new InvalidOperationException("The serial endpoint is input-only.");
        var bytes = TransportPayload.GetBytes(data.Data);
        await _writeLock.WaitAsync(data.CancellationToken);
        try
        {
            var port = _port;
            if (port?.IsOpen != true)
                throw new IOException("The serial port is not open.");
            await port.BaseStream.WriteAsync(bytes, data.CancellationToken);
            await port.BaseStream.FlushAsync(data.CancellationToken);
        }
        finally { _writeLock.Release(); }
    }

    private async Task ReceiveAsync(Stream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        while (!cancellationToken.IsCancellationRequested)
        {
            int count;
            try
            {
                count = await stream.ReadAsync(buffer, cancellationToken);
                if (count == 0) throw new IOException("The serial input stream closed.");
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                Pipe.IsNotAvailable = true;
                CollectException(exception, description: "Serial port receive failed.", logger: logger);
                break;
            }
            try
            {
                var data = CreateData(buffer[..count]);
                data.CancellationToken = cancellationToken;
                await SendDataAsync(data);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                CollectException(exception, description: "Serial pipeline delivery failed.", logger: logger);
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
        _port?.Dispose();
        _port = null;
        try
        {
            if (task is not null) await task.WaitAsync(cancellationToken);
        }
        finally { source?.Dispose(); }
    }

    /// <inheritdoc />
    public override ConnectionDirection SupportedConnectionDirection() => ConnectionDirection.InputAndOutput;
}
