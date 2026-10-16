namespace Monica.AI.Chat.Models;

/// <summary>The evidence or choice used to determine an effective chat context capacity.</summary>
public enum ChatContextCapacitySource
{
    /// <summary>No capacity was configured or discovered; Monica uses its 256K planning default.</summary>
    Default,
    /// <summary>The selected model's configured or discovered metadata supplies the capacity.</summary>
    Model,
    /// <summary>An explicit conversation setting overrides the model capacity.</summary>
    Conversation
}

/// <summary>Effective context capacity for planning, kept separate from nullable provider metadata.</summary>
/// <param name="Tokens">Resolved context capacity in tokens.</param>
/// <param name="Source">Origin of the capacity, including an explicit fallback marker.</param>
public readonly record struct ChatContextCapacity(int Tokens, ChatContextCapacitySource Source)
{
    /// <summary>Fallback capacity when neither conversation settings nor model metadata specify a limit.</summary>
    public const int DefaultTokens = 256 * 1024;

    /// <summary>Resolves conversation override, then model evidence, then the 256K default without mutating either input.</summary>
    public static ChatContextCapacity Resolve(int? conversationOverride, int? modelContextWindow) =>
        conversationOverride is { } selected ? new(selected, ChatContextCapacitySource.Conversation)
        : modelContextWindow is { } configured ? new(configured, ChatContextCapacitySource.Model)
        : new(DefaultTokens, ChatContextCapacitySource.Default);
}
