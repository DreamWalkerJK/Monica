using Monica.AI.Chat.Models;
using Monica.AI.Models;

namespace Monica.AI.Chat.Services;

internal static class ChatSessionSnapshotValidator
{
    private static readonly DateTimeOffset MINIMUM_TIMESTAMP = DateTimeOffset.UnixEpoch;
    private static readonly TimeSpan MAXIMUM_FUTURE_SKEW = TimeSpan.FromDays(1);

    public static void Validate(ChatSessionSnapshot snapshot, string? expectedSessionId = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.Version != ChatSessionSnapshot.CurrentVersion)
        {
            throw new NotSupportedException(
                $"Chat session snapshot version '{snapshot.Version}' is not supported.");
        }

        ValidateIdentifier(snapshot.SessionId, nameof(snapshot.SessionId));
        if (expectedSessionId is not null
            && !string.Equals(snapshot.SessionId, expectedSessionId, StringComparison.Ordinal))
        {
            throw Invalid(
                nameof(snapshot.SessionId),
                $"expected '{expectedSessionId}' but found '{snapshot.SessionId}'.");
        }

        if (snapshot.Revision < 0)
        {
            throw Invalid(nameof(snapshot.Revision), "cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(snapshot.Title))
        {
            throw Invalid(nameof(snapshot.Title), "cannot be empty.");
        }

        if (snapshot.Settings is null || string.IsNullOrWhiteSpace(snapshot.Settings.ProviderId))
        {
            throw Invalid(nameof(snapshot.Settings), "must identify a provider.");
        }

        ValidateTimestamp(snapshot.CreatedAt, nameof(snapshot.CreatedAt));
        ValidateTimestamp(snapshot.UpdatedAt, nameof(snapshot.UpdatedAt));
        if (snapshot.UpdatedAt < snapshot.CreatedAt)
        {
            throw Invalid(nameof(snapshot.UpdatedAt), "cannot precede the session creation time.");
        }

        ValidateTurns(snapshot);
        snapshot.Settings.Validate();
        var stepIds = new HashSet<string>(StringComparer.Ordinal);
        var turnIds = snapshot.Turns.Select(turn => turn.Id).ToHashSet(StringComparer.Ordinal);
        long previousSequence = 0;
        for (var index = 0; index < snapshot.ExecutionSteps.Count; index++)
        {
            var step = snapshot.ExecutionSteps[index];
            var path = $"{nameof(snapshot.ExecutionSteps)}[{index}]";
            ValidateIdentifier(step.Id, $"{path}.{nameof(step.Id)}");
            if (!stepIds.Add(step.Id) || step.Sequence <= previousSequence)
                throw Invalid(nameof(snapshot.ExecutionSteps), "step identities and sequence must be unique and ordered.");
            if (step.TurnId is { } turnId && !turnIds.Contains(turnId))
                throw Invalid(nameof(snapshot.ExecutionSteps), "a step refers to an unknown turn.");
            if (!Enum.IsDefined(step.Kind) || !Enum.IsDefined(step.Status))
                throw Invalid(path, "the operation kind or lifecycle status is not supported.");
            ValidateTimestamp(step.StartedAt, $"{path}.{nameof(step.StartedAt)}");
            if (step.CompletedAt is { } completedAt)
            {
                ValidateTimestamp(completedAt, $"{path}.{nameof(step.CompletedAt)}");
                if (completedAt < step.StartedAt)
                    throw Invalid($"{path}.{nameof(step.CompletedAt)}", "cannot precede the operation start time.");
            }
            if (step.Request is { } request)
                ValidateRequest(request, $"{path}.{nameof(step.Request)}");
            previousSequence = step.Sequence;
        }
    }

    private static void ValidateTurns(ChatSessionSnapshot snapshot)
    {
        var messageIds = new HashSet<string>(StringComparer.Ordinal);
        var turnIds = new HashSet<string>(StringComparer.Ordinal);
        var previousMessageTimestamp = snapshot.CreatedAt;

        for (var turnIndex = 0; turnIndex < snapshot.Turns.Count; turnIndex++)
        {
            var turn = snapshot.Turns[turnIndex]
                       ?? throw Invalid($"Turns[{turnIndex}]", "cannot be null.");
            var turnPath = $"Turns[{turnIndex}]";

            if (!turnIds.Add(turn.Id)) throw Invalid(turnPath, "turn identities must be unique.");
            previousMessageTimestamp = ValidateMessage(
                turn.UserMessage,
                AIChatRole.User,
                AIChatMessageKind.Message,
                $"{turnPath}.{nameof(turn.UserMessage)}",
                snapshot,
                previousMessageTimestamp,
                messageIds);

            if (turn.AssistantMessage is not null)
            {
                previousMessageTimestamp = ValidateMessage(
                    turn.AssistantMessage,
                    AIChatRole.Assistant,
                    AIChatMessageKind.Message,
                    $"{turnPath}.{nameof(turn.AssistantMessage)}",
                    snapshot,
                    previousMessageTimestamp,
                    messageIds);
            }

            for (var errorIndex = 0; errorIndex < turn.ErrorMessages.Count; errorIndex++)
            {
                previousMessageTimestamp = ValidateMessage(
                    turn.ErrorMessages[errorIndex],
                    AIChatRole.Assistant,
                    AIChatMessageKind.Error,
                    $"{turnPath}.{nameof(turn.ErrorMessages)}[{errorIndex}]",
                    snapshot,
                    previousMessageTimestamp,
                    messageIds);
            }
        }
    }

    private static DateTimeOffset ValidateMessage(
        ChatMessageSnapshot message,
        AIChatRole expectedRole,
        AIChatMessageKind expectedKind,
        string path,
        ChatSessionSnapshot session,
        DateTimeOffset previousTimestamp,
        HashSet<string> messageIds)
    {
        if (message is null)
        {
            throw Invalid(path, "cannot be null.");
        }

        ValidateIdentifier(message.Id, $"{path}.{nameof(message.Id)}");
        if (!messageIds.Add(message.Id))
        {
            throw Invalid($"{path}.{nameof(message.Id)}", "must be unique within the session.");
        }

        if (message.Role != expectedRole || message.Kind != expectedKind)
        {
            throw Invalid(
                path,
                $"must use role '{expectedRole}' and kind '{expectedKind}'.");
        }

        ValidateTimestamp(message.CreatedAt, $"{path}.{nameof(message.CreatedAt)}");
        if (message.CreatedAt < session.CreatedAt
            || message.CreatedAt > session.UpdatedAt)
        {
            throw Invalid(
                $"{path}.{nameof(message.CreatedAt)}",
                "must be within the session lifetime.");
        }

        if (message.CreatedAt < previousTimestamp)
        {
            throw Invalid(
                $"{path}.{nameof(message.CreatedAt)}",
                "must be chronological within the transcript.");
        }

        return message.CreatedAt;
    }

    private static void ValidateRequest(ChatModelRequest request, string path)
    {
        ValidateIdentifier(request.ProviderId, $"{path}.{nameof(request.ProviderId)}");
        if (request.TimeToFirstToken < TimeSpan.Zero || request.GenerationDuration < TimeSpan.Zero)
            throw Invalid(path, "measured durations cannot be negative.");
        if (request.Usage is { } usage
            && (usage.InputTokens < 0 || usage.OutputTokens < 0 || usage.ReasoningTokens < 0
                || usage.CachedInputTokens < 0 || usage.TotalTokens < 0))
            throw Invalid($"{path}.{nameof(request.Usage)}", "token counts cannot be negative.");
    }

    private static void ValidateIdentifier(string? value, string path)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Invalid(path, "cannot be empty.");
        }
    }

    private static void ValidateTimestamp(DateTimeOffset value, string path)
    {
        if (value < MINIMUM_TIMESTAMP || value > DateTimeOffset.UtcNow + MAXIMUM_FUTURE_SKEW)
        {
            throw Invalid(path, "is outside the supported timestamp range.");
        }
    }

    private static InvalidDataException Invalid(string path, string message)
        => new($"Invalid chat session snapshot at '{path}': {message}");
}
