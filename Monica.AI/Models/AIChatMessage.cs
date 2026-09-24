using Microsoft.Extensions.AI;
using Monica.AI.Chat.Models;

namespace Monica.AI.Models;

/// <summary>Live transcript message backed by one ordered content collection.</summary>
public sealed class AIChatMessage
{
    private IReadOnlyList<ChatContentPart> _parts = [];

    /// <summary>Stable message identity.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    /// <summary>Message role.</summary>
    public required AIChatRole Role { get; init; }
    /// <summary>Normal message or execution failure.</summary>
    public AIChatMessageKind Kind { get; init; }
    /// <summary>Plain-text projection; initializing it creates a single text part.</summary>
    public string Content
    {
        get => string.Concat(_parts.Where(part => part.Kind == ChatContentKind.Text).Select(part => part.Text));
        init => _parts = [ChatContentPart.FromText(value)];
    }
    /// <summary>Original ordered text, reasoning, tool, and attachment content.</summary>
    public IReadOnlyList<ChatContentPart> Parts { get => _parts; internal set => _parts = value.ToArray(); }
    /// <summary>Message creation time.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    /// <summary>Model that produced the message, when known.</summary>
    public string? ModelName { get; init; }
    /// <summary>Provider that produced the message, when known.</summary>
    public string? ProviderId { get; init; }
    /// <summary>Whether the current process is still generating this message.</summary>
    public bool IsStreaming { get; internal set; }
}

/// <summary>Transcript entry presentation.</summary>
public enum AIChatMessageKind
{
    /// <summary>An ordinary message.</summary>
    Message,
    /// <summary>An execution failure kept after partial output.</summary>
    Error
}

/// <summary>Provider-independent conversation role.</summary>
public enum AIChatRole
{
    /// <summary>System instructions.</summary>
    System,
    /// <summary>User submission.</summary>
    User,
    /// <summary>Assistant output.</summary>
    Assistant,
    /// <summary>Tool execution result.</summary>
    Tool
}

/// <summary>Conversions used only at the provider SDK boundary.</summary>
public static class AIChatRoleExtensions
{
    /// <summary>Converts a Monica role to the corresponding SDK role.</summary>
    public static ChatRole ToChatRole(this AIChatRole role) => role switch
    {
        AIChatRole.System => ChatRole.System,
        AIChatRole.User => ChatRole.User,
        AIChatRole.Assistant => ChatRole.Assistant,
        AIChatRole.Tool => ChatRole.Tool,
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };

    /// <summary>Converts an SDK role to a supported Monica role.</summary>
    public static AIChatRole FromChatRole(ChatRole role)
    {
        if (role == ChatRole.System) return AIChatRole.System;
        if (role == ChatRole.User) return AIChatRole.User;
        if (role == ChatRole.Assistant) return AIChatRole.Assistant;
        if (role == ChatRole.Tool) return AIChatRole.Tool;
        throw new NotSupportedException($"Unsupported chat role '{role}'.");
    }
}
