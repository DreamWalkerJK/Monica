using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Monica.AI.Chat.Models;
using Monica.AI.Models;
using Monica.AI.Services.Support;

namespace Monica.AI.Chat.Services;

/// <summary>Updates the live transcript from the same ordered stream that supplies the execution ledger.</summary>
internal sealed class ChatTranscriptProjector(ChatSession session, ChatTurn turn)
{
    private readonly List<ChatContentPart> _parts = [.. turn.AssistantMessage?.Parts ?? []];

    internal IEnumerable<ChatStreamEvent> Apply(AgentResponseUpdate update)
    {
        foreach (var content in update.Contents)
        {
            if (content is ChatRuntimeEventContent runtimeEvent)
            {
                yield return runtimeEvent.Event;
                continue;
            }

            if (content is ToolApprovalRequestContent approval && approval.ToolCall is FunctionCallContent call)
            {
                var id = session.StorePendingApproval(approval);
                yield return new ChatApprovalRequestEvent(id, GetArgument(call, "skillName", "skill_name") ?? "Skill",
                    GetArgument(call, "scriptName", "script_name") ?? call.Name, "External script",
                    session.RunContext?.Redact(ToolCallContentSerializer.SerializeArguments(call.Arguments))
                        ?? ChatInspectionRedactor.Redact(ToolCallContentSerializer.SerializeArguments(call.Arguments)) ?? "{}");
                continue;
            }

            var part = ChatContentMapper.Capture(content, redact: content is FunctionCallContent or FunctionResultContent or TextReasoningContent);
            if (part is null) continue;
            if (part.Kind != ChatContentKind.Text && session.RunContext is { } run) part = run.Redact(part);
            if (part.Kind is ChatContentKind.ToolCall or ChatContentKind.ToolResult)
            {
                var existing = _parts.FindIndex(value => value.Kind == part.Kind && value.CallId == part.CallId);
                if (existing >= 0) _parts[existing] = part;
                else _parts.Add(part);
            }
            else RecordingChatClient.Append(_parts, part);
            RefreshMessage();

            switch (content)
            {
                case TextContent text when !string.IsNullOrEmpty(text.Text):
                    yield return new ChatTextDeltaEvent(text.Text);
                    break;
                case TextReasoningContent reasoning when !string.IsNullOrEmpty(reasoning.Text):
                    yield return new ChatReasoningDeltaEvent(reasoning.Text);
                    break;
                case FunctionCallContent functionCall:
                    yield return new ChatToolEvent(functionCall.Name, functionCall.CallId, ChatToolEventStatus.Started,
                        part.Data?.GetRawText(), null, null);
                    break;
                case FunctionResultContent result:
                    var tool = turn.Steps.LastOrDefault(step => step.Tool?.CallId == result.CallId);
                    yield return new ChatToolEvent(tool?.Tool?.Name ?? "Tool", result.CallId,
                        tool?.Status == ChatExecutionStatus.Failed ? ChatToolEventStatus.Failed : ChatToolEventStatus.Completed,
                        tool?.Tool?.Arguments, part.Text, tool?.Error ?? part.Error);
                    break;
            }
        }
        session.MarkUpdated();
    }

    private void RefreshMessage()
    {
        var message = turn.AssistantMessage!;
        message.Parts = _parts.ToArray();
    }

    private static string? GetArgument(FunctionCallContent call, params string[] names)
    {
        foreach (var name in names)
            if (call.Arguments?.TryGetValue(name, out var value) == true) return value?.ToString();
        return null;
    }
}
