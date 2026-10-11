using System.IO.Ports;
using Monica.DataChannel.Abstractions.Communication;

namespace Monica.DataChannel.Providers.Serial;

/// <summary>Configures one host-owned serial port. Framing and protocol interpretation belong to the pipeline.</summary>
public class SerialPortOptions : CommunicationOptions<SerialPortEndpoint>
{
    /// <summary>Gets or sets the platform port name, such as COM1 or /dev/ttyS0.</summary>
    public required string PortName { get; set; }
    /// <summary>Gets or sets the baud rate. The default is 9600.</summary>
    public int BaudRate { get; set; } = 9600;
    /// <summary>Gets or sets the data bits per byte. The default is 8; valid values are 5 through 8.</summary>
    public int DataBits { get; set; } = 8;
    /// <summary>Gets or sets the parity check. The default is none.</summary>
    public Parity Parity { get; set; } = Parity.None;
    /// <summary>Gets or sets the stop bits. The default is one; None is unsupported.</summary>
    public StopBits StopBits { get; set; } = StopBits.One;
    /// <summary>Gets or sets the hardware or software flow control. The default is none.</summary>
    public Handshake Handshake { get; set; } = Handshake.None;
    /// <summary>Gets or sets whether to enable the Data Terminal Ready line. The default is false.</summary>
    public bool DtrEnable { get; set; }
    /// <summary>Gets or sets whether to enable the Request To Send line when hardware flow control is disabled.</summary>
    public bool RtsEnable { get; set; }

    /// <summary>Creates serial options, supporting input and output by default.</summary>
    public SerialPortOptions(ConnectionDirection direction = ConnectionDirection.InputAndOutput)
    {
        Type = CommunicationType.Serial;
        Direction = direction;
    }

    /// <inheritdoc />
    public override void EnrichOrValidate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(PortName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(BaudRate);
        if (DataBits is < 5 or > 8) throw new ArgumentOutOfRangeException(nameof(DataBits));
        if (!Enum.IsDefined(Parity)) throw new ArgumentOutOfRangeException(nameof(Parity));
        if (!Enum.IsDefined(StopBits) || StopBits == StopBits.None) throw new ArgumentOutOfRangeException(nameof(StopBits));
        if (!Enum.IsDefined(Handshake)) throw new ArgumentOutOfRangeException(nameof(Handshake));
        if (Direction is not (ConnectionDirection.Input or ConnectionDirection.Output or ConnectionDirection.InputAndOutput))
            throw new ArgumentOutOfRangeException(nameof(Direction));
    }
}
