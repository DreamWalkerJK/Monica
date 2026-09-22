using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.AI;
using Monica.AI.Chat.Models;
using Monica.AI.Chat.Services;
using Monica.AI.Models;

namespace Monica.AI.Services.Support;

/// <summary>Measures actual provider calls below the agent tool loop without taking ownership of the leased client.</summary>
internal sealed class RecordingChatClient(IChatClient inner, ChatRunContext context) : IChatClient
{
    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var requestOptions = WithoutRemoteHistory(options);
        var input = await PrepareRequestAsync(messages.ToArray(), requestOptions, cancellationToken);
        var step = await BeginAsync(input, requestOptions);
        try
        {
            var response = await inner.GetResponseAsync(input, requestOptions, cancellationToken);
            response.ConversationId = null;
            var request = step.Request! with
            {
                ResponseId = response.ResponseId,
                ModelName = response.ModelId ?? step.Request!.ModelName,
                FinishReason = response.FinishReason?.ToString(),
                Usage = response.Usage is { } usage ? TokenUsage.FromProvider(usage) : null,
                Output = response.Messages.SelectMany(message => message.Contents)
                    .Select(content => ChatContentMapper.Capture(content, redact: true)).OfType<ChatContentPart>().Select(context.Redact).ToArray()
            };
            await CompleteAsync(step, request, ChatExecutionStatus.Completed);
            return response;
        }
        catch (Exception ex)
        {
            await CompleteAsync(step, step.Request!, ex is OperationCanceledException ? ChatExecutionStatus.Cancelled : ChatExecutionStatus.Failed, ex);
            throw;
        }
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestOptions = WithoutRemoteHistory(options);
        var input = await PrepareRequestAsync(messages.ToArray(), requestOptions, cancellationToken);
        var step = await BeginAsync(input, requestOptions);
        var clock = Stopwatch.StartNew();
        var parts = new List<ChatContentPart>();
        var usage = new UsageDetails();
        var hasUsage = false;
        TimeSpan? first = null;
        TimeSpan? last = null;
        string? responseId = null;
        string? modelId = null;
        string? finishReason = null;
        Exception? failure = null;
        var completed = false;
        var lastPublication = TimeSpan.MinValue;
        await using var enumerator = inner.GetStreamingResponseAsync(input, requestOptions, cancellationToken).GetAsyncEnumerator(cancellationToken);
        try
        {
            while (true)
            {
                bool hasNext;
                try { hasNext = await enumerator.MoveNextAsync(); }
                catch (Exception ex) { failure = ex; break; }
                if (!hasNext) { completed = true; break; }

                var observedAt = clock.Elapsed;
                var update = enumerator.Current;
                responseId ??= update.ResponseId;
                modelId ??= update.ModelId;
                finishReason = update.FinishReason?.ToString() ?? finishReason;
                foreach (var content in update.Contents)
                {
                    if (content is UsageContent { Details: { } details })
                    {
                        usage.Add(details);
                        hasUsage = true;
                    }
                    else if (ChatContentMapper.Capture(content, redact: true) is { } part)
                    {
                        if (part.Kind == ChatContentKind.ToolCall
                            || part.Kind is ChatContentKind.Text or ChatContentKind.Reasoning && !string.IsNullOrEmpty(part.Text))
                        {
                            // One received update is one timing observation, even when it contains several
                            // parts. Empty reasoning markers and metadata cannot establish a generation span.
                            first ??= observedAt;
                            last = observedAt;
                        }
                        Append(parts, context.Redact(part));
                    }
                }

                // Publish the first visible chunk immediately, then bound full projection copies for fast token streams.
                if (parts.Count > 0 && (lastPublication == TimeSpan.MinValue
                    || clock.Elapsed - lastPublication >= TimeSpan.FromMilliseconds(50)
                    || update.Contents.Any(content => content is FunctionCallContent)))
                {
                    await context.RecordAsync(step with
                    {
                        Request = step.Request! with { Output = parts.ToArray(), TimeToFirstToken = first, ResponseId = responseId }
                    });
                    lastPublication = clock.Elapsed;
                }

                // The durable local transcript is authoritative for subsequent requests and provider switches.
                update.ConversationId = null;
                yield return update;
            }
        }
        finally
        {
            var request = step.Request! with
            {
                ResponseId = responseId,
                ModelName = modelId ?? step.Request!.ModelName,
                FinishReason = finishReason,
                Usage = hasUsage ? TokenUsage.FromProvider(usage) : null,
                TimeToFirstToken = first,
                GenerationDuration = first.HasValue && last.HasValue ? last.Value - first.Value : null,
                Output = parts.ToArray()
            };
            var status = completed ? ChatExecutionStatus.Completed
                : failure is null or OperationCanceledException ? ChatExecutionStatus.Cancelled : ChatExecutionStatus.Failed;
            await CompleteAsync(step, request, status, failure);
        }

        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private async Task<ChatExecutionStep> BeginAsync(IReadOnlyList<ChatMessage> messages, ChatOptions options)
    {
        var request = new ChatModelRequest
        {
            ProviderId = context.Settings.ProviderId,
            ModelName = options.ModelId ?? context.Settings.ModelName,
            ConfigurationRevision = context.ConfigurationRevision,
            IsCompaction = context.IsCompaction,
            Settings = context.Settings with
            {
                ModelName = options.ModelId ?? context.Settings.ModelName,
                SystemPrompt = context.Redact(options.Instructions),
                MaxOutputTokens = options.MaxOutputTokens,
                Temperature = options.Temperature,
                ReasoningLevel = context.IsCompaction ? null : context.Settings.ReasoningLevel
            },
            Instructions = context.Redact(options.Instructions),
            Messages = messages.Select(message => ChatContentMapper.Capture(message, context.Turn?.Id, redact: true))
                .Select(message => message with { Parts = message.Parts.Select(context.Redact).ToArray() }).ToArray(),
            Tools = options.Tools?.OfType<AIFunctionDeclaration>().Select(tool => new ChatToolSchema(
                tool.Name, context.Redact(tool.Description), ChatInspectionRedactor.Redact(tool.JsonSchema, context.RedactDiagnostic))).ToArray() ?? []
        };
        var step = context.Session.StartStep(ChatExecutionStepKind.ModelRequest, context.Turn?.Id, request);
        await context.PublishAsync(new ChatStepChangedEvent(step));
        if (!context.IsCompaction)
        {
            context.Session.ContextUsage = context.Session.ContextUsage with
            {
                HasUnestimatedAttachments = request.Messages.SelectMany(message => message.Parts)
                    .Any(part => part.Kind == ChatContentKind.Attachment && string.IsNullOrEmpty(part.Text))
            };
            await context.PublishAsync(new ChatContextChangedEvent(context.Session.ContextUsage));
        }
        return step;
    }

    private async ValueTask CompleteAsync(ChatExecutionStep step, ChatModelRequest request, ChatExecutionStatus status, Exception? error = null)
    {
        await context.RecordAsync(step with
        {
            Request = request, Status = status, CompletedAt = DateTimeOffset.UtcNow,
            Error = context.Redact(error?.Message)
        });
        if (!context.IsCompaction && request.Usage?.InputTokens is { } input)
        {
            context.Session.ContextUsage = context.Session.ContextUsage with { LastRequestInputTokens = input };
            await context.PublishAsync(new ChatContextChangedEvent(context.Session.ContextUsage));
        }
    }

    internal static void Append(List<ChatContentPart> parts, ChatContentPart part)
    {
        if (parts.LastOrDefault() is { } previous && previous.Kind == part.Kind
            && previous.ReasoningFormat == part.ReasoningFormat
            && part.Kind is ChatContentKind.Text or ChatContentKind.Reasoning)
            parts[^1] = previous with { Text = previous.Text + part.Text };
        else parts.Add(part);
    }

    private async Task<ChatMessage[]> PrepareRequestAsync(ChatMessage[] input, ChatOptions options, CancellationToken ct)
    {
        if (context.IsCompaction) return input;
        var captured = input.Select(message => ChatContentMapper.Capture(message, context.Turn?.Id)).ToArray();
        var tools = options.Tools?.OfType<AIFunctionDeclaration>().Select(tool => new ChatToolSchema(tool.Name, tool.Description, tool.JsonSchema)).ToArray();
        var estimated = ChatContextEstimator.Estimate(captured, options.Instructions, tools);
        var previous = context.Session.ExecutionSteps.LastOrDefault(step => step.Request is { IsCompaction: false, Usage.InputTokens: not null })?.Request;
        if (previous is not null && previous.ProviderId == context.Settings.ProviderId && previous.ModelName == context.Settings.ModelName)
        {
            // Anchor the text heuristic to the latest observed request to account for provider tokenization and image overhead.
            var previousEstimate = ChatContextEstimator.Estimate(previous.Messages, previous.Instructions, previous.Tools);
            estimated = (int)Math.Clamp((long)previous.Usage!.InputTokens!.Value + estimated - previousEstimate, 0, int.MaxValue);
        }
        context.Session.ContextUsage = context.Session.ContextUsage with { EstimatedNextInputTokens = estimated };
        if (!context.Settings.AutomaticCompaction || context.AutomaticCompactionAttempted
            || context.Session.ContextUsage.OccupancyRatio is not { } occupancy || occupancy < context.Settings.CompactionThreshold)
            return input;

        context.AutomaticCompactionAttempted = true;
        var original = context.Session.ContextMessages;
        var result = await ChatContextCompactor.CompactAsync(context, inner, automatic: true, ct);
        if (!result.Applied) return input;
        var retainedIds = context.Session.ContextMessages.Select(message => message.TurnId).OfType<string>().ToHashSet(StringComparer.Ordinal);
        var removedIds = original.Select(message => message.TurnId).OfType<string>().Where(id => !retainedIds.Contains(id)).ToHashSet(StringComparer.Ordinal);
        var summary = context.Session.ContextMessages.Single(message => message.IsSummary);
        var summaryMessage = new ChatMessage(ChatRole.User, string.Concat(summary.Parts.Select(part => part.Text)))
        {
            AdditionalProperties = new AdditionalPropertiesDictionary { [ChatContentMapper.SUMMARY_KEY] = true }
        };

        var transformed = ReplaceCompactedMessages(input, removedIds, summaryMessage);
        var transformedEstimate = ChatContextEstimator.Estimate(transformed.Select(message => ChatContentMapper.Capture(message, context.Turn?.Id)), options.Instructions, tools);
        context.Session.ContextUsage = context.Session.ContextUsage with
        {
            EstimatedNextInputTokens = Math.Max(0, estimated + transformedEstimate - ChatContextEstimator.Estimate(captured, options.Instructions, tools))
        };
        if (context.Session.ChatHistory is { } history)
        {
            var replacement = ReplaceCompactedMessages(history, removedIds, summaryMessage);
            history.Clear();
            foreach (var message in replacement) history.Add(message);
        }
        return transformed;
    }

    private static ChatMessage[] ReplaceCompactedMessages(IEnumerable<ChatMessage> source, HashSet<string> removedIds, ChatMessage summary)
    {
        var output = new List<ChatMessage>();
        var inserted = false;
        foreach (var message in source)
        {
            var properties = message.AdditionalProperties;
            var removed = properties?.TryGetValue(ChatContentMapper.TURN_ID_KEY, out var turn) == true && turn is string id && removedIds.Contains(id)
                || properties?.TryGetValue(ChatContentMapper.SUMMARY_KEY, out var isSummary) == true && isSummary is true;
            if (!removed) output.Add(message);
            else if (!inserted) { output.Add(summary); inserted = true; }
        }
        return output.ToArray();
    }

    private static ChatOptions WithoutRemoteHistory(ChatOptions? options)
    {
        var copy = options?.Clone() ?? new ChatOptions();
        copy.ConversationId = null;
        return copy;
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceKey is null && serviceType.IsInstanceOfType(this) ? this : inner.GetService(serviceType, serviceKey);

    // The provider lease, not this per-run instrumentation adapter, owns the underlying client.
    public void Dispose() { }
}
