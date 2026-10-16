using Monica.AI.Chat.Models;
using Monica.AI.Models;

namespace Monica.AI.Chat.Services;

internal static class ChatSessionSnapshotMapper
{
    internal static ChatTurnSnapshot ToSnapshot(ChatTurn turn) => new()
    {
        Id = turn.Id, Status = turn.Status, StartedAt = turn.StartedAt, CompletedAt = turn.CompletedAt,
        ContextMessages = turn.ContextMessages, UserMessage = Capture(turn.UserMessage),
        AssistantMessage = turn.AssistantMessage is null ? null : Capture(turn.AssistantMessage),
        ErrorMessages = turn.ErrorMessages.Select(Capture).ToArray()
    };

    internal static ChatTurn FromSnapshot(ChatTurnSnapshot snapshot)
    {
        var turn = new ChatTurn(Restore(snapshot.UserMessage))
        {
            Id = snapshot.Id, StartedAt = snapshot.StartedAt, CompletedAt = snapshot.CompletedAt,
            Status = snapshot.Status is ChatExecutionStatus.Running or ChatExecutionStatus.AwaitingApproval
                ? ChatExecutionStatus.Interrupted : snapshot.Status,
            ContextMessages = snapshot.ContextMessages,
            AssistantMessage = snapshot.AssistantMessage is null ? null : Restore(snapshot.AssistantMessage)
        };
        turn.RestoreErrors(snapshot.ErrorMessages.Select(Restore));
        return turn;
    }

    private static ChatMessageSnapshot Capture(AIChatMessage message) => new()
    {
        Id = message.Id, Role = message.Role, Kind = message.Kind, Parts = message.Parts,
        CreatedAt = message.CreatedAt, ModelName = message.ModelName, ProviderId = message.ProviderId
    };

    private static AIChatMessage Restore(ChatMessageSnapshot message) => new()
    {
        Id = message.Id, Role = message.Role, Kind = message.Kind, Parts = message.Parts,
        CreatedAt = message.CreatedAt, ModelName = message.ModelName, ProviderId = message.ProviderId
    };
}
