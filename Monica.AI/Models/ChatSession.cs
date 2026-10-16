using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Monica.AI.Abstractions;
using Monica.AI.Chat.Models;
using Monica.AI.Models.Internal;
using Monica.AI.Services.Support;

namespace Monica.AI.Models;

/// <summary>One user request and its ordered execution, transcript, and original context messages.</summary>
public sealed class ChatTurn
{
    private readonly List<AIChatMessage> _errors = [];
    private readonly List<ChatExecutionStep> _steps = [];
    private readonly object _sync = new();

    internal ChatTurn(AIChatMessage userMessage)
    {
        UserMessage = userMessage;
        StartedAt = userMessage.CreatedAt;
    }

    /// <summary>Stable turn identity.</summary>
    public string Id { get; internal init; } = Guid.NewGuid().ToString("N");
    /// <summary>Original user submission, including attachment references.</summary>
    public AIChatMessage UserMessage { get; }
    /// <summary>Live or completed assistant projection.</summary>
    public AIChatMessage? AssistantMessage { get; internal set; }
    /// <summary>Errors encountered while executing this turn.</summary>
    public IReadOnlyList<AIChatMessage> ErrorMessages { get { lock (_sync) return _errors.ToArray(); } }
    /// <summary>Execution steps in their original start order.</summary>
    public IReadOnlyList<ChatExecutionStep> Steps { get { lock (_sync) return _steps.ToArray(); } }
    /// <summary>Current lifecycle status.</summary>
    public ChatExecutionStatus Status { get; internal set; } = ChatExecutionStatus.Running;
    /// <summary>Time when the user submitted the turn.</summary>
    public DateTimeOffset StartedAt { get; internal init; }
    /// <summary>Time when execution reached a terminal state.</summary>
    public DateTimeOffset? CompletedAt { get; internal set; }
    /// <summary>Original SDK-independent context for this turn, retained even after compaction.</summary>
    public IReadOnlyList<ChatContextMessage> ContextMessages { get; internal set; } = [];

    internal IEnumerable<AIChatMessage> Messages
        => new[] { UserMessage, AssistantMessage }.OfType<AIChatMessage>().Concat(ErrorMessages);

    internal void SetStep(ChatExecutionStep step)
    {
        lock (_sync)
        {
            var index = _steps.FindIndex(item => item.Id == step.Id);
            if (index < 0) { _steps.Add(step); _steps.Sort((left, right) => left.Sequence.CompareTo(right.Sequence)); }
            else _steps[index] = step;
        }
    }

    internal void AddError(string error)
    {
        lock (_sync)
        {
            if (_errors.LastOrDefault()?.Content == error) return;
            _errors.Add(new AIChatMessage { Role = AIChatRole.Assistant, Kind = AIChatMessageKind.Error, Content = error });
        }
    }

    internal void RestoreErrors(IEnumerable<AIChatMessage> errors) => _errors.AddRange(errors);
}

/// <summary>Owns one durable conversation and its temporary, provider-leased execution runtime.</summary>
/// <remarks>Only Monica-owned messages and execution records are persisted. A runtime is rebuilt from
/// those records for each new turn; no SDK session or provider conversation identifier is durable.</remarks>
public sealed class ChatSession : IAsyncDisposable
{
    private readonly List<ChatTurn> _turns = [];
    private readonly List<ChatExecutionStep> _steps = [];
    private readonly object _sync = new();
    private readonly CancellationTokenSource _lifetime = new();
    private TaskCompletionSource _operationFinished = CompletedOperation();
    private CancellationTokenSource? _operationCancellation;
    private readonly Dictionary<string, ToolApprovalRequestContent> _approvals = new(StringComparer.Ordinal);
    private AIChatAgentRuntime? _runtime;
    private AgentSession? _agentSession;
    private IAIProviderLease? _providerLease;
    private int _operationActive;
    private long _nextSequence;
    private bool _disposed;

    internal ChatSession(ChatSessionSettings settings, string title, AIChatRuntimeContext runtimeContext)
    {
        settings.Validate();
        SessionId = Guid.NewGuid().ToString("N");
        Settings = settings;
        Title = title;
        RuntimeContext = runtimeContext;
        CreatedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }

    internal ChatSession(ChatSessionSnapshot snapshot, AIChatRuntimeContext runtimeContext)
    {
        SessionId = snapshot.SessionId;
        Title = snapshot.Title;
        CreatedAt = snapshot.CreatedAt;
        UpdatedAt = snapshot.UpdatedAt;
        Settings = snapshot.Settings;
        RuntimeContext = runtimeContext;
        PersistenceRevision = snapshot.Revision;
        ContextMessages = snapshot.ContextMessages;
        ContextUsage = snapshot.ContextUsage;
        _turns.AddRange(snapshot.Turns.Select(Chat.Services.ChatSessionSnapshotMapper.FromSnapshot));
        foreach (var step in snapshot.ExecutionSteps)
        {
            SetStep(step.Status == ChatExecutionStatus.Running
                ? step with { Status = ChatExecutionStatus.Interrupted, Error = "Execution was interrupted before completion." }
                : step);
        }
        _nextSequence = _steps.Count == 0 ? 0 : _steps.Max(step => step.Sequence);
        UpdatedAt = snapshot.UpdatedAt;
    }

    /// <summary>Unique conversation identifier.</summary>
    public string SessionId { get; }
    /// <summary>Current display title.</summary>
    public string Title { get; private set; }
    /// <summary>Conversation creation time.</summary>
    public DateTimeOffset CreatedAt { get; }
    /// <summary>Most recent durable state update.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }
    /// <summary>Revision assigned by the history provider.</summary>
    public long PersistenceRevision { get; private set; }
    /// <summary>Settings that will apply to the next new turn.</summary>
    public ChatSessionSettings Settings { get; private set; }
    /// <summary>Provider selected for the next new turn.</summary>
    public string ProviderId => Settings.ProviderId;
    /// <summary>Model selected for the next new turn.</summary>
    public string? ModelName => Settings.ModelName;
    /// <summary>Session instructions.</summary>
    public string? SystemPrompt => Settings.SystemPrompt;
    /// <summary>Reasoning level selected for the next new turn.</summary>
    public string? ReasoningLevel => Settings.ReasoningLevel;
    /// <summary>Runtime values supplied to tools, never persisted with the conversation.</summary>
    public AIChatRuntimeContext RuntimeContext { get; private set; }
    /// <summary>Server-resolved ownership partition, never accepted from a serialized snapshot.</summary>
    public ChatHistoryPartition? Partition { get; private set; }
    /// <summary>Chronological conversation turns.</summary>
    public IReadOnlyList<ChatTurn> Turns { get { lock (_sync) return _turns.ToArray(); } }
    /// <summary>Flattened transcript projection.</summary>
    public IReadOnlyList<AIChatMessage> Messages => Turns.SelectMany(turn => turn.Messages).ToArray();
    /// <summary>Single ordered ledger backing chat diagnostics and the Trajectory view.</summary>
    public IReadOnlyList<ChatExecutionStep> ExecutionSteps { get { lock (_sync) return _steps.ToArray(); } }
    /// <summary>Current request context, which may contain a summary instead of older turns.</summary>
    public IReadOnlyList<ChatContextMessage> ContextMessages { get; private set; } = [];
    /// <summary>Last measured and next estimated context usage.</summary>
    public ChatContextUsage ContextUsage { get; internal set; } = new();
    /// <summary>Current turn while executing or waiting for approval.</summary>
    public ChatTurn? ActiveTurn => Turns.LastOrDefault() is { Status: ChatExecutionStatus.Running or ChatExecutionStatus.AwaitingApproval } turn ? turn : null;
    /// <summary>Whether a model/tool operation or manual compaction currently owns this conversation.</summary>
    public bool IsBusy => Volatile.Read(ref _operationActive) != 0;
    /// <summary>Whether a temporary Agent Framework runtime is currently active.</summary>
    public bool IsRuntimeActive => _runtime is not null;

    internal AIAgent Agent => _runtime?.Agent ?? throw new InvalidOperationException("The chat runtime is not active.");
    internal AgentSession AgentSession => _agentSession ?? throw new InvalidOperationException("The chat runtime is not active.");
    internal IList<ChatMessage>? ChatHistory => _runtime?.Agent.GetService<InMemoryChatHistoryProvider>()?.GetMessages(_agentSession);
    internal ChatRunContext? RunContext { get; set; }
    internal CancellationToken LifetimeCancellation => _lifetime.Token;
    internal CancellationToken OperationCancellation => _operationCancellation?.Token ?? CancellationToken.None;

    internal void BindPartition(ChatHistoryPartition partition)
    {
        ArgumentNullException.ThrowIfNull(partition);
        if (Partition is not null && Partition != partition)
            throw new UnauthorizedAccessException("The conversation belongs to a different user or workspace.");
        Partition = partition;
    }

    internal IDisposable BeginOperation()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_operationActive != 0) throw new InvalidOperationException("This conversation already has an active operation.");
            _operationActive = 1;
            _operationFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _operationCancellation = new CancellationTokenSource();
            return new OperationScope(this);
        }
    }

    internal void ApplySettings(ChatSessionSettings settings)
    {
        settings.Validate();
        Settings = settings;
        Touch();
    }

    internal void Rename(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title.Trim();
        Touch();
    }

    internal void SetRuntimeContext(AIChatRuntimeContext context) => RuntimeContext = context;

    internal ChatTurn BeginTurn(ChatUserInput input)
    {
        var turn = new ChatTurn(new AIChatMessage
        {
            Role = AIChatRole.User, Parts = input.Parts
        });
        turn.AssistantMessage = new AIChatMessage
        {
            Role = AIChatRole.Assistant, Content = string.Empty, ProviderId = ProviderId,
            ModelName = ModelName, IsStreaming = true
        };
        lock (_sync) _turns.Add(turn);
        Touch();
        return turn;
    }

    internal ChatTurn GetOpenTurn() => ActiveTurn is { Status: ChatExecutionStatus.AwaitingApproval } turn
        ? turn : throw new InvalidOperationException("No turn is waiting for approval.");

    internal void CompleteTurn(ChatTurn turn, ChatExecutionStatus status)
    {
        turn.Status = status;
        turn.CompletedAt = status == ChatExecutionStatus.AwaitingApproval ? null : DateTimeOffset.UtcNow;
        if (turn.AssistantMessage is { } assistant) assistant.IsStreaming = false;
        Touch();
    }

    internal void RecordError(ChatTurn turn, string error) { turn.AddError(error); Touch(); }
    internal void RecordError(string error) => RecordError(_turns.LastOrDefault()
        ?? throw new InvalidOperationException("There is no turn for the error entry."), error);

    internal ChatExecutionStep StartStep(ChatExecutionStepKind kind, string? turnId,
        ChatModelRequest? request = null, ChatToolExecution? tool = null, ChatCompactionResult? compaction = null)
    {
        lock (_sync)
        {
            var step = new ChatExecutionStep
            {
                Id = Guid.NewGuid().ToString("N"), Sequence = ++_nextSequence, TurnId = turnId,
                Kind = kind, StartedAt = DateTimeOffset.UtcNow, Status = ChatExecutionStatus.Running,
                Request = request, Tool = tool, Compaction = compaction
            };
            SetStep(step);
            return step;
        }
    }

    internal void SetStep(ChatExecutionStep step)
    {
        lock (_sync)
        {
            var index = _steps.FindIndex(item => item.Id == step.Id);
            if (index < 0) { _steps.Add(step); _steps.Sort((left, right) => left.Sequence.CompareTo(right.Sequence)); }
            else _steps[index] = step;
            _turns.FirstOrDefault(turn => turn.Id == step.TurnId)?.SetStep(step);
        }
        Touch();
    }

    internal void ReplaceContext(IReadOnlyList<ChatContextMessage> messages, ChatTurn? turn = null)
    {
        ContextMessages = messages.ToArray();
        if (turn is not null) turn.ContextMessages = messages.Where(message => message.TurnId == turn.Id).ToArray();
        Touch();
    }

    internal ChatUserInput RewindForEdit(string messageId, string content)
    {
        lock (_sync)
        {
            var index = _turns.FindIndex(turn => turn.UserMessage.Id == messageId);
            if (index < 0) throw new KeyNotFoundException($"User message '{messageId}' was not found.");
            var parts = _turns[index].UserMessage.Parts.Where(part => part.Kind == ChatContentKind.Attachment).ToList();
            parts.Insert(0, ChatContentPart.FromText(content));
            Rewind(index);
            return new ChatUserInput { Parts = parts };
        }
    }

    internal ChatUserInput RewindForRetry(string messageId)
    {
        lock (_sync)
        {
            var index = _turns.FindIndex(turn => turn.AssistantMessage?.Id == messageId || turn.ErrorMessages.Any(error => error.Id == messageId));
            if (index < 0) throw new KeyNotFoundException($"Assistant message '{messageId}' was not found.");
            var input = new ChatUserInput { Parts = _turns[index].UserMessage.Parts };
            Rewind(index);
            return input;
        }
    }

    private void Rewind(int index)
    {
        if (IsBusy || ActiveTurn is not null)
            throw new InvalidOperationException("Stop generation or cancel the pending approval before editing the conversation.");
        var removed = _turns.Skip(index).Select(turn => turn.Id).ToHashSet(StringComparer.Ordinal);
        var cutoff = _turns[index].StartedAt;
        _turns.RemoveRange(index, _turns.Count - index);
        _steps.RemoveAll(step => step.TurnId is { } id && removed.Contains(id) || step.StartedAt >= cutoff);
        ContextMessages = _turns.SelectMany(turn => turn.ContextMessages).ToArray();
        _approvals.Clear();
        Touch();
    }

    internal async Task SetRuntimeAsync(AIChatAgentRuntime runtime, AgentSession session, IAIProviderLease lease)
    {
        await ReleaseRuntimeAsync();
        _runtime = runtime;
        _agentSession = session;
        _providerLease = lease;
    }

    internal async Task ReleaseRuntimeAsync()
    {
        var runtime = _runtime;
        var lease = _providerLease;
        _runtime = null; _agentSession = null; _providerLease = null; RunContext = null;
        try { if (runtime is not null) await runtime.DisposeAsync(); }
        finally { lease?.Dispose(); }
    }

    internal string StorePendingApproval(ToolApprovalRequestContent request)
    {
        var existing = _approvals.FirstOrDefault(item => item.Value.RequestId == request.RequestId);
        if (existing.Key is not null) return existing.Key;
        var id = Guid.NewGuid().ToString("N");
        _approvals.Add(id, request);
        return id;
    }

    internal ToolApprovalRequestContent TakePendingApproval(string id)
        => _approvals.Remove(id, out var request) ? request : throw new KeyNotFoundException("The approval is no longer active.");

    internal void ClearPendingApprovals() => _approvals.Clear();

    internal async Task CancelOperationAsync()
    {
        Task pending;
        lock (_sync)
        {
            _operationCancellation?.Cancel();
            pending = _operationFinished.Task;
        }
        await pending;
    }

    internal void ClearHistory()
    {
        if (IsBusy) throw new InvalidOperationException("Stop generation before clearing the conversation.");
        _turns.Clear(); _steps.Clear(); _approvals.Clear(); ContextMessages = []; ContextUsage = new(); Touch();
    }

    internal void MarkPersisted(long revision) => PersistenceRevision = revision;
    internal void MarkUpdated() => Touch();
    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Task pending;
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            pending = _operationFinished.Task;
        }
        _lifetime.Cancel();
        await pending;
        if (ActiveTurn is { } turn) CompleteTurn(turn, ChatExecutionStatus.Cancelled);
        _approvals.Clear();
        await ReleaseRuntimeAsync();
        _lifetime.Dispose();
    }

    private static TaskCompletionSource CompletedOperation()
    {
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        source.SetResult();
        return source;
    }

    private sealed class OperationScope(ChatSession session) : IDisposable
    {
        public void Dispose()
        {
            lock (session._sync)
            {
                session._operationActive = 0;
                session._operationCancellation?.Dispose();
                session._operationCancellation = null;
                session._operationFinished.TrySetResult();
            }
        }
    }
}
