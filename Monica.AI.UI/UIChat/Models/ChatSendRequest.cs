namespace Monica.AI.UI.UIChat.Models;

/// <summary>Composer submission whose acceptance determines whether the draft may be cleared.</summary>
public sealed record ChatSendRequest(string Message)
{
    /// <summary>Whether the page accepted this input for a conversation turn.</summary>
    public bool Accepted { get; internal set; }
}
