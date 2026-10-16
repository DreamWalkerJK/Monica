namespace Monica.DataChannel.Providers.TCP
{
    public delegate void TcpReceiveEventHander(MsgReceivedEventArgs args);

    public class MsgReceivedEventArgs
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();

        public string? ConnectionName { get; set; }

        /// <summary>Gets or sets the unique socket session identity, changed on every reconnect.</summary>
        public Guid ConnectionEpoch { get; set; }
    }
}
