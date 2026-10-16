using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Monica.AI.Chat.Models;
using Monica.AI.Models;

namespace Monica.AI.Services.Support;

/// <summary>Run-owned request settings and event publication shared by provider and tool instrumentation.</summary>
internal sealed class ChatRunContext(ChatSession session, ChatTurn? turn, ChatSessionSettings settings, long configurationRevision)
{
    internal ChatSession Session { get; } = session;
    internal ChatTurn? Turn { get; } = turn;
    internal ChatSessionSettings Settings { get; } = settings;
    internal long ConfigurationRevision { get; } = configurationRevision;
    internal AgentResponseUpdateChannel? Channel { get; set; }
    internal bool IsCompaction { get; init; }
    internal LLMModelInfo? Model { get; init; }
    internal bool AutomaticCompactionAttempted { get; set; }
    internal Func<string, string>? RedactDiagnostic { get; init; }

    internal string? Redact(string? value) => value is null ? null
        : Chat.Services.ChatInspectionRedactor.Redact(RedactDiagnostic?.Invoke(value) ?? value);

    internal ChatContentPart Redact(ChatContentPart part) => part with
    {
        Text = Redact(part.Text), Error = Redact(part.Error),
        Data = part.Data is { } data ? Chat.Services.ChatInspectionRedactor.Redact(data, RedactDiagnostic) : null
    };

    internal ValueTask PublishAsync(ChatStreamEvent update)
        => Channel?.PublishAsync(new AgentResponseUpdate(ChatRole.Assistant, [new ChatRuntimeEventContent(update)])) ?? ValueTask.CompletedTask;

    internal ValueTask RecordAsync(ChatExecutionStep step)
    {
        Session.SetStep(step);
        return PublishAsync(new ChatStepChangedEvent(step));
    }
}

/// <summary>Internal transport adapter; SDK objects never cross the public chat stream boundary.</summary>
internal sealed class ChatRuntimeEventContent(ChatStreamEvent update) : AIContent
{
    internal ChatStreamEvent Event { get; } = update;
}
