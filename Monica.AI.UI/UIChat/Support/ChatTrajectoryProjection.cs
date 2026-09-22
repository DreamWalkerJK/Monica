using System.Text;
using Monica.AI.Chat.Models;
using Monica.AI.Models;

namespace Monica.AI.UI.UIChat.Support;

internal enum TrajectoryRole { System, User, Assistant, Tool, Context }

/// <summary>A display record retains its original message or execution step for inspection.</summary>
internal sealed record TrajectoryRecord(
    string Id, string? GroupId, TrajectoryRole Role, string Preview, string SearchText,
    DateTimeOffset? StartedAt, ChatExecutionStep? Step = null, ChatMessageSnapshot? Message = null,
    string? HeadingKey = null)
{
    internal string RoleKey => Role switch
    {
        TrajectoryRole.System => "Workbench:TrajectoryRoleSystem",
        TrajectoryRole.User => "Workbench:TrajectoryRoleUser",
        TrajectoryRole.Assistant => "Workbench:TrajectoryRoleAssistant",
        TrajectoryRole.Tool => "Workbench:TrajectoryRoleTool",
        _ => "Workbench:TrajectoryRoleContext"
    };
    internal string RoleClass => Role.ToString().ToLowerInvariant();
    internal bool IsFailure => Step?.Status == ChatExecutionStatus.Failed || Message?.Kind == AIChatMessageKind.Error;
    internal int Lane => Role == TrajectoryRole.Tool ? 2 : Role == TrajectoryRole.Assistant ? 1 : 0;
    internal string? ToolName => Step?.Tool?.Name;
    internal string? Arguments => Step?.Tool?.Arguments;
    internal string? Result => Step?.Tool?.Result ?? Step?.Error;
}

internal sealed record TrajectoryGroup(string Id, int? TurnNumber, IReadOnlyList<TrajectoryRecord> Records)
{
    internal int ModelCalls => Records.Count(record => record.Step?.Request is not null);
    internal int ToolCalls => Records.Count(record => record.Step?.Tool is not null);
}

internal sealed record TrajectoryLedgerRow(TrajectoryGroup? Group, TrajectoryRecord? Record);

internal sealed record TrajectoryTurnSource(string Id, ChatMessageSnapshot User, ChatMessageSnapshot? Assistant,
    IReadOnlyList<ChatMessageSnapshot> Errors);

/// <summary>Projects the original transcript and authoritative execution ledger into a conversation log.</summary>
internal sealed class ChatTrajectoryProjection
{
    internal IReadOnlyList<TrajectoryRecord> Records { get; private init; } = [];
    internal IReadOnlyList<TrajectoryGroup> Groups { get; private init; } = [];
    internal int TurnCount { get; private init; }
    internal int ModelCalls => Records.Count(record => record.Step?.Request is not null);
    internal int ToolCalls => Records.Count(record => record.Step?.Tool is not null);
    internal DateTimeOffset? Origin { get; private init; }
    internal double ExtentSeconds { get; private init; }
    internal TimeSpan? RecordedDuration { get; private init; }

    internal static ChatTrajectoryProjection Create(ChatSession session, DateTimeOffset now)
        => Create(session.Turns.Select(turn => new TrajectoryTurnSource(turn.Id, Capture(turn.UserMessage),
            turn.AssistantMessage is { } assistant ? Capture(assistant) : null, turn.ErrorMessages.Select(Capture).ToArray())).ToArray(),
            session.ExecutionSteps, session.SystemPrompt, now);

    internal static ChatTrajectoryProjection Create(IReadOnlyList<TrajectoryTurnSource> turns,
        IReadOnlyList<ChatExecutionStep> steps, string? configuredPrompt, DateTimeOffset now)
    {
        var records = new List<TrajectoryRecord>();
        var groups = new List<TrajectoryGroup>();
        var firstRequest = steps.FirstOrDefault(step => step.Request is { IsCompaction: false });
        var currentPrompt = firstRequest?.Request is { } initialRequest ? SystemPrompt(initialRequest) : null;
        if (firstRequest is not null)
        {
            if (!string.IsNullOrWhiteSpace(currentPrompt))
                records.Add(SystemRecord("system:initial", null, currentPrompt, firstRequest.StartedAt, "Workbench:InitialSystemPrompt"));
        }
        else if (!string.IsNullOrWhiteSpace(configuredPrompt))
        {
            // A draft preference is not evidence of an executed request and has no recorded timestamp.
            records.Add(SystemRecord("system:configured", null, configuredPrompt, null, "Workbench:ConfiguredSystemPrompt"));
        }

        var stepsByTurn = steps.ToLookup(step => step.TurnId);
        var sources = turns.Select((turn, index) => new GroupSource(turn.Id, index + 1, turn.User.CreatedAt, turn,
                stepsByTurn[turn.Id].ToArray()))
            .Concat(steps.Where(step => step.TurnId is null).Select(step => new GroupSource(step.Id, null, step.StartedAt, null, [step])))
            .OrderBy(group => group.StartedAt).ToArray();
        foreach (var source in sources)
        {
            var entries = new List<TrajectoryRecord>();
            if (source.Turn is { } turn) entries.Add(MessageRecord(turn.User, source.Id, TrajectoryRole.User));
            foreach (var step in source.Steps.OrderBy(step => step.Sequence))
            {
                if (step.Request is { IsCompaction: false } request && SystemPrompt(request) != currentPrompt)
                {
                    currentPrompt = SystemPrompt(request);
                    entries.Add(SystemRecord($"system:{step.Id}", source.Id, currentPrompt ?? string.Empty,
                        step.StartedAt, "Workbench:ChangedSystemPrompt"));
                }
                entries.Add(StepRecord(step, source.Id));
            }
            if (source.Turn is { Assistant: { Parts.Count: > 0 } assistant }
                && !source.Steps.Any(step => step.Request is { IsCompaction: false }))
                entries.Add(MessageRecord(assistant, source.Id, TrajectoryRole.Assistant));
            if (source.Turn is { } errors)
                entries.AddRange(errors.Errors.Select(error => MessageRecord(error, source.Id, TrajectoryRole.Assistant)));
            if (entries.Count == 0) continue;
            groups.Add(new TrajectoryGroup(source.Id, source.TurnNumber, entries));
            records.AddRange(entries);
        }

        var timestamps = records.Where(record => record.StartedAt.HasValue).Select(record => record.StartedAt!.Value).ToArray();
        var origin = timestamps.Length == 0 ? (DateTimeOffset?)null : timestamps.Min();
        var latest = records.Select(record => End(record, now) ?? record.StartedAt).OfType<DateTimeOffset>().DefaultIfEmpty(origin ?? now).Max();
        return new ChatTrajectoryProjection
        {
            Records = records, Groups = groups, TurnCount = turns.Count, Origin = origin,
            ExtentSeconds = origin is { } start ? Math.Max(1, (latest - start).TotalSeconds) : 1,
            RecordedDuration = MeasureDuration(steps, now)
        };
    }

    internal List<TrajectoryLedgerRow> Rows(string? query, IReadOnlySet<string> folded)
    {
        var hasQuery = !string.IsNullOrWhiteSpace(query);
        bool Matches(TrajectoryRecord record) => !hasQuery || record.SearchText.Contains(query!, StringComparison.OrdinalIgnoreCase);
        var rows = Records.Where(record => record.GroupId is null && Matches(record)).Select(record => new TrajectoryLedgerRow(null, record)).ToList();
        foreach (var group in Groups)
        {
            var matches = group.Records.Where(Matches).ToArray();
            if (matches.Length == 0) continue;
            rows.Add(new TrajectoryLedgerRow(group, null));
            if (hasQuery || !folded.Contains(group.Id)) rows.AddRange(matches.Select(record => new TrajectoryLedgerRow(null, record)));
        }
        return rows;
    }

    internal static DateTimeOffset? End(TrajectoryRecord record, DateTimeOffset now)
        => record.Step?.CompletedAt ?? (record.Step is { Status: ChatExecutionStatus.Running } ? now : null);

    internal static string Condense(string? value, int maximumLength = 220)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var result = new StringBuilder(Math.Min(value.Length, maximumLength));
        var spaced = false;
        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character)) { spaced = result.Length > 0; continue; }
            if (spaced) result.Append(' ');
            spaced = false;
            result.Append(character);
            if (result.Length >= maximumLength) return result.ToString() + "…";
        }
        return result.ToString();
    }

    private static TrajectoryRecord SystemRecord(string id, string? group, string prompt, DateTimeOffset? time, string heading)
        => new(id, group, TrajectoryRole.System, Condense(prompt), $"System {prompt}", time,
            Message: new ChatMessageSnapshot { Id = id, Role = AIChatRole.System, Parts = [ChatContentPart.FromText(prompt)] }, HeadingKey: heading);

    private static TrajectoryRecord MessageRecord(ChatMessageSnapshot message, string group, TrajectoryRole role)
    {
        var text = PartsText(message.Parts);
        return new TrajectoryRecord($"message:{message.Id}", group, role, Condense(text), $"{role} {text}", message.CreatedAt,
            Message: message, HeadingKey: message.Kind == AIChatMessageKind.Error ? "Chat:Error:Title" : null);
    }

    private static TrajectoryRecord StepRecord(ChatExecutionStep step, string group)
    {
        var role = step.Kind switch { ChatExecutionStepKind.Tool => TrajectoryRole.Tool,
            ChatExecutionStepKind.Compaction => TrajectoryRole.Context, _ => TrajectoryRole.Assistant };
        var content = step.Request is { } request ? PartsText(request.Output)
            : step.Tool is { } tool ? $"{tool.Name} {tool.Arguments} → {tool.Result ?? step.Error}"
            : step.Compaction?.Summary ?? step.Compaction?.Reason;
        var text = string.IsNullOrWhiteSpace(content) ? step.Error : content;
        return new TrajectoryRecord($"step:{step.Id}", group, role, Condense(text),
            $"{role} {ChatExecutionPresentation.SearchText(step)} {text}", step.StartedAt, Step: step,
            HeadingKey: step.Request?.IsCompaction == true ? "Workbench:SummaryRequest" : step.Kind == ChatExecutionStepKind.Compaction ? "Workbench:Compaction" : null);
    }

    private static string PartsText(IEnumerable<ChatContentPart> parts) => string.Join(' ', parts.Select(part =>
        part.Attachment?.FileName ?? part.Text ?? (part.Kind == ChatContentKind.ToolCall ? $"{part.ToolName} {part.Data}" : part.Data?.ToString())));

    private static string? SystemPrompt(ChatModelRequest request)
    {
        // Contributors can supply instructions through options or actual system-role messages.
        // Both are request evidence; current session preferences are never substituted for a past request.
        var parts = new[] { request.Instructions }.Concat(request.Messages
            .Where(message => message.Role == AIChatRole.System).Select(message => PartsText(message.Parts)))
            .Where(text => !string.IsNullOrWhiteSpace(text)).Distinct(StringComparer.Ordinal);
        var prompt = string.Join("\n\n", parts);
        return prompt.Length == 0 ? null : prompt;
    }

    private static ChatMessageSnapshot Capture(AIChatMessage message) => new()
    {
        Id = message.Id, Role = message.Role, Kind = message.Kind, Parts = message.Parts,
        CreatedAt = message.CreatedAt, ProviderId = message.ProviderId, ModelName = message.ModelName
    };

    private static TimeSpan? MeasureDuration(IReadOnlyList<ChatExecutionStep> steps, DateTimeOffset now)
    {
        if (steps.Count == 0 || steps.Any(step => step.CompletedAt is null && step.Status != ChatExecutionStatus.Running)) return null;
        var ranges = steps.Select(step => (Start: step.StartedAt, End: step.CompletedAt ?? now)).OrderBy(range => range.Start).ToArray();
        var start = ranges[0].Start;
        var end = ranges[0].End;
        var ticks = 0L;
        foreach (var range in ranges.Skip(1))
        {
            if (range.Start > end) { ticks += Math.Max(0, (end - start).Ticks); start = range.Start; end = range.End; }
            else if (range.End > end) end = range.End;
        }
        return TimeSpan.FromTicks(ticks + Math.Max(0, (end - start).Ticks));
    }

    private sealed record GroupSource(string Id, int? TurnNumber, DateTimeOffset StartedAt, TrajectoryTurnSource? Turn,
        IReadOnlyList<ChatExecutionStep> Steps);
}
