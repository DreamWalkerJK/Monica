using Monica.DataChannel.Abstractions.Communication;

namespace Monica.DataChannel.Providers.UDP;

public class UdpOptions : CommunicationOptions<UdpEndpoint>
{
    /// <summary>Gets or sets the local IP address to bind. Use 0.0.0.0 for all IPv4 interfaces.</summary>
    public required string Address { get; set; }
    /// <summary>Gets or sets the local port. Zero allocates an ephemeral port.</summary>
    public required int Port { get; set; }

    /// <summary>Gets or sets the remote host for outbound datagrams. Required for output directions.</summary>
    public string? RemoteAddress { get; set; }

    /// <summary>Gets or sets the remote port for outbound datagrams. Required for output directions.</summary>
    public int? RemotePort { get; set; }

    public string? SubscriptionName { get; set; } = nameof(UdpOptions);

    public UdpOptions(ConnectionDirection direction = ConnectionDirection.Input)
    {
        Type = CommunicationType.UDP;
        Direction = direction;
    }

    /// <inheritdoc />
    public override void EnrichOrValidate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Address);
        if (!System.Net.IPAddress.TryParse(Address, out _))
            throw new ArgumentException("UDP bind address must be an IP address.", nameof(Address));
        if (Port is < 0 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(Port));
        if (Direction is not (ConnectionDirection.Input or ConnectionDirection.Output or ConnectionDirection.InputAndOutput))
            throw new ArgumentOutOfRangeException(nameof(Direction));
        if (Direction is ConnectionDirection.Output or ConnectionDirection.InputAndOutput)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(RemoteAddress);
            if (RemotePort is null or < 1 or > 65535)
                throw new ArgumentOutOfRangeException(nameof(RemotePort));
        }
    }
}
