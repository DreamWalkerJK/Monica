namespace Monica.Repository.Inbox.Annotations;

/// <summary>
/// Deduplicates this EventBus handler by its stable consumer name, producer source, and message ID.
/// Handling requires an active Repository transaction and Inbox-enabled primary context.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class InboxAttribute(string consumer) : Attribute
{
    /// <summary>Stable identity of this logical consumer; keep it unchanged across deployments.</summary>
    public string Consumer { get; } = !string.IsNullOrWhiteSpace(consumer)
        ? consumer : throw new ArgumentException("Inbox consumer name cannot be empty.", nameof(consumer));
}
