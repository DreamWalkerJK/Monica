using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Monica.AI.Abstractions;
using Monica.AI.AgentCapabilities.Abstractions;
using Monica.AI.Chat.Abstractions;
using Monica.AI.Chat.Models;
using Monica.AI.Chat.Services;
using Monica.AI.Models;
using Monica.AI.Services.Support;
using Monica.Core.Extensions;
using Monica.Modules;

namespace Monica.AI.Services;

/// <summary>Executes durable conversations through a leased provider and the shared Monica capability pipeline.</summary>
internal sealed class AIChatService(
    IAIProviderFactory providerFactory,
    IOptions<ModuleAIOption> options,
    IAIChatAgentFactory agentFactory,
    IAgentCapabilityStateStore capabilityStateStore,
    AgentStreamingCoordinator streamingCoordinator,
    IChatAttachmentStore attachmentStore)
{
    private readonly ChatContentMapper _contentMapper = new(attachmentStore);

    internal Task<ChatSession> CreateSessionAsync(string? providerId = null, string? modelName = null,
        string? systemPrompt = null, AIChatRuntimeContext? runtimeContext = null, string? reasoningLevel = null,
        string? title = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var provider = providerId is null ? providerFactory.GetDefaultProviderInfo() : providerFactory.GetProviderInfo(providerId);
        if (provider is null) throw new InvalidOperationException("No configured chat provider is available.");
        var session = new ChatSession(new ChatSessionSettings(provider.ProviderId, modelName, systemPrompt, reasoningLevel),
            title ?? "New Chat", runtimeContext ?? AIChatRuntimeContext.Empty);
        RefreshContextUsage(session);
        return Task.FromResult(session);
    }

    internal void UpdateSettings(ChatSession session, ChatSessionSettings settings)
    {
        var provider = providerFactory.GetProviderInfo(settings.ProviderId);
        var effective = provider is null ? settings with { ContextWindow = ChatContextCapacity.Resolve(settings.ContextWindow, null).Tokens }
            : ResolveSettings(settings, provider);
        effective.Validate(provider is null ? null : FindModel(provider, effective.ModelName));
        session.ApplySettings(settings);
        if (!session.IsBusy) RefreshContextUsage(session, effective);
    }

    internal ChatSession RestoreSession(ChatSessionSnapshot snapshot, AIChatRuntimeContext? runtimeContext = null,
        string? expectedSessionId = null)
    {
        ChatSessionSnapshotValidator.Validate(snapshot, expectedSessionId);
        var session = new ChatSession(snapshot, runtimeContext ?? AIChatRuntimeContext.Empty);
        RefreshContextUsage(session);
        return session;
    }

    internal Task<ChatSessionSnapshot> CreateSnapshotAsync(ChatSession session, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(new ChatSessionSnapshot
        {
            SessionId = session.SessionId, Title = session.Title, CreatedAt = session.CreatedAt, UpdatedAt = session.UpdatedAt,
            Settings = session.Settings, Turns = session.Turns.Select(ChatSessionSnapshotMapper.ToSnapshot).ToArray(),
            ContextMessages = session.ContextMessages, ContextUsage = session.ContextUsage,
            ExecutionSteps = session.ExecutionSteps, Revision = session.PersistenceRevision
        });
    }

    internal IAsyncEnumerable<ChatStreamEvent> SendMessageStreamingAsync(ChatSession session, string message, CancellationToken ct = default)
        => SendMessageStreamingAsync(session, ChatUserInput.FromText(message), ct);

    internal IAsyncEnumerable<ChatStreamEvent> SendMessageStreamingAsync(ChatSession session, ChatUserInput input, CancellationToken ct = default)
        => RunAsync(session, input, null, ct);

    internal IAsyncEnumerable<ChatStreamEvent> ContinueApprovalStreamingAsync(ChatSession session,
        ToolApprovalResponseContent response, CancellationToken ct = default) => RunAsync(session, null, response, ct);

    private async IAsyncEnumerable<ChatStreamEvent> RunAsync(ChatSession session, ChatUserInput? input,
        ToolApprovalResponseContent? approval, [EnumeratorCancellation] CancellationToken ct)
    {
        using var operation = session.BeginOperation();
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(ct, session.LifetimeCancellation, session.OperationCancellation);
        if (input is not null) ValidateInput(input);
        if (input is not null && session.ActiveTurn is not null)
            throw new InvalidOperationException("Resolve the pending tool approval before starting another turn.");
        var turn = input is null ? session.GetOpenTurn() : session.BeginTurn(input);
        turn.Status = ChatExecutionStatus.Running;
        turn.AssistantMessage!.IsStreaming = true;
        var projector = new ChatTranscriptProjector(session, turn);
        var awaitingApproval = false;
        var completed = false;
        Exception? failure = null;
        ChatMessage? sdkInput = null;
        AgentResponseUpdateChannel? channel = null;
        try
        {
            try
            {
                if (input is not null)
                {
                    (sdkInput, channel) = await PrepareTurnAsync(session, turn, input, lifetime.Token);
                }
                else
                {
                    var run = session.RunContext ?? throw new InvalidOperationException("The approval runtime is no longer available; retry the turn.");
                    channel = new AgentResponseUpdateChannel { RunContext = run };
                    run.Channel = channel;
                    sdkInput = new ChatMessage(ChatRole.User, [approval!]);
                }
            }
            catch (Exception ex) { failure = ex; }

            if (failure is not null) ThrowDiagnosticFailure(session.RunContext, failure);
            ChatClientAgentRunOptions? runOptions = null;
            try { runOptions = CreateRunOptions(session.RunContext!.Settings, session.RunContext.Model, channel!); }
            catch (Exception ex) { failure = ex; }
            if (failure is not null) ThrowDiagnosticFailure(session.RunContext, failure);
            await using var enumerator = streamingCoordinator.RunAsync(session.Agent, session.AgentSession, sdkInput!,
                session.RuntimeContext, runOptions!, channel!, lifetime.Token).GetAsyncEnumerator(lifetime.Token);
            while (true)
            {
                bool hasNext;
                try { hasNext = await enumerator.MoveNextAsync(); }
                catch (Exception ex) { failure = ex; break; }
                if (!hasNext) { completed = true; break; }
                foreach (var update in projector.Apply(enumerator.Current))
                {
                    awaitingApproval |= update is ChatApprovalRequestEvent;
                    yield return update;
                }
            }
            if (failure is not null && failure is not OperationCanceledException) ThrowDiagnosticFailure(session.RunContext, failure);
        }
        finally
        {
            lifetime.Cancel();
            CaptureContext(session, turn);
            if (failure is not null && failure is not OperationCanceledException)
                session.RecordError(turn, session.RunContext?.Redact(failure.GetMessageRecursively())
                    ?? ChatInspectionRedactor.Redact(failure.GetMessageRecursively())!);
            session.CompleteTurn(turn, failure is not null && failure is not OperationCanceledException ? ChatExecutionStatus.Failed
                : !completed ? ChatExecutionStatus.Cancelled
                : awaitingApproval ? ChatExecutionStatus.AwaitingApproval : ChatExecutionStatus.Completed);
            RefreshContextUsage(session);
            if (!awaitingApproval || !completed) await session.ReleaseRuntimeAsync();
        }
        yield return new ChatContextChangedEvent(session.ContextUsage);
        yield return new ChatCompletedEvent(!completed, awaitingApproval);
    }

    private async Task<(ChatMessage Input, AgentResponseUpdateChannel Channel)> PrepareTurnAsync(
        ChatSession session, ChatTurn turn, ChatUserInput input, CancellationToken ct)
    {
        var lease = AcquireProvider(session.ProviderId);
        var transferred = false;
        try
        {
            var provider = lease.Provider;
            var settings = ResolveSettings(session.Settings, provider.Info);
            var model = FindModel(provider.Info, settings.ModelName);
            settings.Validate(model);
            var run = new ChatRunContext(session, turn, settings, lease.ConfigurationRevision)
            {
                Model = model, RedactDiagnostic = lease.RedactDiagnostic
            };
            var channel = new AgentResponseUpdateChannel { RunContext = run };
            run.Channel = channel;
            session.ContextUsage = session.ContextUsage with
            {
                ContextWindow = settings.ContextWindow,
                ContextWindowSource = ChatContextCapacity.Resolve(session.Settings.ContextWindow, model?.ContextWindow).Source,
                ReservedOutputTokens = settings.MaxOutputTokens
            };
            var sdkInput = await _contentMapper.MaterializeAsync(session, new ChatContextMessage
            {
                TurnId = turn.Id, Role = AIChatRole.User, Parts = input.Parts
            }, model, ct, settings);
            var nextEstimate = ChatContextEstimator.Estimate(session.ContextMessages.Append(ChatContentMapper.Capture(sdkInput)), settings.SystemPrompt);
            session.ContextUsage = session.ContextUsage with { EstimatedNextInputTokens = nextEstimate };
            var client = provider.GetChatClient(settings.ModelName);
            var capabilityState = await capabilityStateStore.LoadAsync(ct);
            var runtime = await agentFactory.CreateAsync(new RecordingChatClient(client, run), new AIChatAgentCreateContext
            {
                Instructions = settings.SystemPrompt, CapabilityState = capabilityState
            }, ct);
            try
            {
                var agentSession = await runtime.Agent.CreateSessionAsync(ct);
                var history = new List<ChatMessage>();
                foreach (var message in session.ContextMessages)
                    history.Add(await _contentMapper.MaterializeAsync(session, message, model, ct, settings));
                var historyProvider = runtime.Agent.GetService<InMemoryChatHistoryProvider>()
                    ?? throw new InvalidOperationException("Chat agents must expose the Monica-owned in-memory request history provider.");
                historyProvider.SetMessages(agentSession, history);
                await session.SetRuntimeAsync(runtime, agentSession, lease);
                session.RunContext = run;
                transferred = true;
                return (sdkInput, channel);
            }
            catch
            {
                await runtime.DisposeAsync();
                throw;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var safe = ChatInspectionRedactor.Redact(lease.RedactDiagnostic(ex.GetMessageRecursively()))!;
            if (safe == ex.GetMessageRecursively()) throw;
            throw new InvalidOperationException(safe);
        }
        finally { if (!transferred) lease.Dispose(); }
    }

    internal async Task<ChatCompactionResult> CompactAsync(ChatSession session, CancellationToken ct = default)
    {
        using var operation = session.BeginOperation();
        if (session.ActiveTurn is not null) throw new InvalidOperationException("Resolve the pending approval before compacting context.");
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(ct, session.LifetimeCancellation, session.OperationCancellation);
        using var lease = AcquireProvider(session.ProviderId);
        var settings = ResolveSettings(session.Settings, lease.Provider.Info);
        settings.Validate(FindModel(lease.Provider.Info, settings.ModelName));
        var run = new ChatRunContext(session, null, settings, lease.ConfigurationRevision) { RedactDiagnostic = lease.RedactDiagnostic };
        try
        {
            var result = await ChatContextCompactor.CompactAsync(run, lease.Provider.GetChatClient(settings.ModelName), automatic: false, lifetime.Token);
            RefreshContextUsage(session);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ThrowDiagnosticFailure(run, ex);
            throw;
        }
    }

    internal async Task CancelAsync(ChatSession session)
    {
        await session.CancelOperationAsync();
        using var operation = session.BeginOperation();
        if (session.ActiveTurn is { } turn) session.CompleteTurn(turn, ChatExecutionStatus.Cancelled);
        session.ClearPendingApprovals();
        await session.ReleaseRuntimeAsync();
    }

    private static void ThrowDiagnosticFailure(ChatRunContext? run, Exception failure)
    {
        if (failure is OperationCanceledException) ExceptionDispatchInfo.Capture(failure).Throw();
        var message = failure.GetMessageRecursively();
        var safe = run?.Redact(message) ?? ChatInspectionRedactor.Redact(message)!;
        if (safe == message) ExceptionDispatchInfo.Capture(failure).Throw();
        throw new InvalidOperationException(safe);
    }

    internal async Task<string> SendMessageAsync(ChatSession session, string message, CancellationToken ct = default)
    {
        await foreach (var _ in SendMessageStreamingAsync(session, message, ct)) { }
        return session.Turns.Last().AssistantMessage?.Content ?? string.Empty;
    }

    private IAIProviderLease AcquireProvider(string providerId)
    {
        var lease = providerFactory.AcquireProvider(providerId)
            ?? throw new InvalidOperationException($"Provider '{providerId}' is not configured.");
        if (lease.Provider.Info.IsValid) return lease;
        var error = AIProviderAvailabilityMessages.BuildProviderUnavailableMessage(lease.Provider.Info);
        lease.Dispose();
        throw new InvalidOperationException(error);
    }

    private ChatSessionSettings ResolveSettings(ChatSessionSettings settings, AIProviderInfo provider)
    {
        var modelName = settings.ModelName ?? provider.DefaultModel;
        var model = FindModel(provider, modelName);
        var effective = settings with
        {
            ModelName = modelName,
            SystemPrompt = settings.SystemPrompt ?? provider.SystemPrompt ?? options.Value.DefaultSystemPrompt,
            ReasoningLevel = settings.ReasoningLevel ?? model?.DefaultReasoningLevel,
            ContextWindow = ChatContextCapacity.Resolve(settings.ContextWindow, model?.ContextWindow).Tokens,
            MaxOutputTokens = settings.MaxOutputTokens
        };
        return effective;
    }

    private static LLMModelInfo? FindModel(AIProviderInfo provider, string? name)
        => provider.SupportedModels?.OfType<LLMModelInfo>().FirstOrDefault(model => string.Equals(model.ModelName, name, StringComparison.OrdinalIgnoreCase));

    private static ChatClientAgentRunOptions CreateRunOptions(ChatSessionSettings settings, LLMModelInfo? model, AgentResponseUpdateChannel channel)
    {
        var chatOptions = new ChatOptions
        {
            ModelId = settings.ModelName, MaxOutputTokens = settings.MaxOutputTokens, Temperature = settings.Temperature
        };
        if (settings.ReasoningLevel is { } id)
        {
            var level = model?.ReasoningLevels.FirstOrDefault(level => level.Id == id)
                ?? throw new InvalidOperationException($"Reasoning level '{id}' is not configured for model '{settings.ModelName}'.");
            if (model?.SupportsReasoning == false) throw new NotSupportedException("The selected model does not support reasoning.");
            var providerValue = level.ProviderValue;
            var effort = providerValue?.ToLowerInvariant() switch
            {
                "none" => ReasoningEffort.None, "low" => ReasoningEffort.Low,
                "medium" => ReasoningEffort.Medium, "high" => ReasoningEffort.High,
                "xhigh" or "extra_high" or "extrahigh" => ReasoningEffort.ExtraHigh,
                _ => (ReasoningEffort?)null
            };
            if (effort is not null) chatOptions.Reasoning = new ReasoningOptions { Effort = effort };
            chatOptions.AdditionalProperties = new AdditionalPropertiesDictionary();
            if (providerValue is not null) chatOptions.AdditionalProperties["monica.reasoning.effort"] = providerValue;
            if (level.BudgetTokens is { } budget) chatOptions.AdditionalProperties["monica.reasoning.budget_tokens"] = budget;
        }
        var runOptions = new ChatClientAgentRunOptions { ChatOptions = chatOptions, AdditionalProperties = new AdditionalPropertiesDictionary() };
        runOptions.AdditionalProperties.Add(channel);
        return runOptions;
    }

    private static void ValidateInput(ChatUserInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Parts.Count == 0 || input.Parts.All(part => part.Kind == ChatContentKind.Text && string.IsNullOrWhiteSpace(part.Text)))
            throw new ArgumentException("Enter a message or attach a file.", nameof(input));
        if (input.Parts.Any(part => part.Kind is not (ChatContentKind.Text or ChatContentKind.Attachment)
            || part.Kind == ChatContentKind.Attachment && part.Attachment is null))
            throw new ArgumentException("User input may contain only text and uploaded attachment references.", nameof(input));
    }

    private static void CaptureContext(ChatSession session, ChatTurn turn)
    {
        var settings = session.RunContext?.Settings ?? session.Settings;
        var history = session.ChatHistory?.Select(message => ChatContentMapper.Capture(message, turn.Id))
            .Select(message => message with
            {
                Parts = message.Parts.Select(part => part.Kind == ChatContentKind.Reasoning && part.OriginProviderId is null
                    ? part with { OriginProviderId = settings.ProviderId, OriginModelName = settings.ModelName } : part).ToArray()
            }).ToArray() ?? [];
        if (!history.Any(message => message.TurnId == turn.Id))
        {
            history = [.. session.ContextMessages, new ChatContextMessage
            {
                TurnId = turn.Id, Role = AIChatRole.User, Parts = turn.UserMessage.Parts
            }, new ChatContextMessage { TurnId = turn.Id, Role = AIChatRole.Assistant, Parts = turn.AssistantMessage?.Parts ?? [] }];
        }
        session.ReplaceContext(ChatContentMapper.KeepCompleteToolPairs(history), turn);
    }

    private void RefreshContextUsage(ChatSession session, ChatSessionSettings? effectiveSettings = null)
    {
        var provider = providerFactory.GetProviderInfo(session.ProviderId);
        var settings = effectiveSettings ?? (provider is null ? session.Settings : ResolveSettings(session.Settings, provider));
        var capacity = ChatContextCapacity.Resolve(session.Settings.ContextWindow,
            provider is null ? null : FindModel(provider, settings.ModelName)?.ContextWindow);
        var lastRequest = session.ExecutionSteps.LastOrDefault(step => step.Request is { IsCompaction: false })?.Request;
        var estimate = ChatContextEstimator.Estimate(session.ContextMessages, settings.SystemPrompt, lastRequest?.Tools);
        var sameModel = lastRequest is not null && lastRequest.ModelName == settings.ModelName && lastRequest.ProviderId == settings.ProviderId;
        if (sameModel && lastRequest?.Usage?.InputTokens is { } actual)
            estimate = (int)Math.Clamp((long)actual + estimate - ChatContextEstimator.Estimate(lastRequest.Messages, lastRequest.Instructions, lastRequest.Tools), 0, int.MaxValue);
        session.ContextUsage = session.ContextUsage with
        {
            EstimatedNextInputTokens = estimate,
            LastRequestInputTokens = sameModel ? lastRequest?.Usage?.InputTokens : null,
            ContextWindow = capacity.Tokens,
            ContextWindowSource = capacity.Source,
            ReservedOutputTokens = settings.MaxOutputTokens,
            HasUnestimatedAttachments = session.ContextMessages.SelectMany(message => message.Parts)
                .Any(part => part.Kind == ChatContentKind.Attachment && string.IsNullOrEmpty(part.Text))
        };
    }
}
