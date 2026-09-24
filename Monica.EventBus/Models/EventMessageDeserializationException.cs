namespace Monica.EventBus.Models;

/// <summary>Indicates that a received event body cannot be decoded as its known contract type.</summary>
public sealed class EventMessageDeserializationException : Exception
{
    /// <summary>Creates an exception for a malformed or unsupported event body.</summary>
    public EventMessageDeserializationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
