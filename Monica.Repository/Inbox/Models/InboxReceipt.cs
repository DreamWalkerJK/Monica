namespace Monica.Repository.Inbox.Models;

/// <summary>A durable claim for one consumer's successful handling of a transport message.</summary>
public sealed class InboxReceipt
{
    /// <summary>Stable logical consumer name.</summary>
    public string Consumer { get; set; } = string.Empty;
    /// <summary>Stable producer identity.</summary>
    public string Source { get; set; } = string.Empty;
    /// <summary>Stable message identity supplied by the producer.</summary>
    public string MessageId { get; set; } = string.Empty;
    /// <summary>UTC time the handler first accepted this message.</summary>
    public DateTime ReceivedAtUtc { get; set; }
}
