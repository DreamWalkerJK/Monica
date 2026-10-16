using System.Text;

namespace Monica.DataChannel.Providers;

internal static class TransportPayload
{
    internal static ReadOnlyMemory<byte> GetBytes(object? payload) => payload switch
    {
        byte[] bytes => bytes,
        ReadOnlyMemory<byte> bytes => bytes,
        Memory<byte> bytes => bytes,
        string text => Encoding.UTF8.GetBytes(text),
        _ => throw new ArgumentException("Transport payloads must be bytes or a UTF-8 string.", nameof(payload))
    };
}
