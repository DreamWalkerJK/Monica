using Monica.AI.Chat.Models;

namespace Monica.AI.Chat.Services;

/// <summary>Finds references retained by the transcript, active context, and request inspection history.</summary>
internal static class ChatSnapshotAttachments
{
    public static IEnumerable<ChatAttachmentReference> Enumerate(ChatSessionSnapshot snapshot)
    {
        var parts = snapshot.ContextMessages.SelectMany(message => message.Parts)
            .Concat(snapshot.Turns.SelectMany(turn => turn.ContextMessages).SelectMany(message => message.Parts))
            .Concat(snapshot.Turns.SelectMany(turn => turn.UserMessage.Parts))
            .Concat(snapshot.Turns.SelectMany(turn => turn.AssistantMessage?.Parts ?? []))
            .Concat(snapshot.Turns.SelectMany(turn => turn.ErrorMessages).SelectMany(message => message.Parts))
            .Concat(snapshot.ExecutionSteps.Where(step => step.Request is not null)
                .SelectMany(step => step.Request!.Messages.SelectMany(message => message.Parts)
                    .Concat(step.Request.Output)));
        return parts.Where(part => part.Attachment is not null).Select(part => part.Attachment!);
    }
}
