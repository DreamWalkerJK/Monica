using System.Text.Json;
using System.Text.Json.Serialization;
using Monica.AI.Models;

namespace Monica.AI.Chat.Models;

/// <summary>The work measured by an execution step.</summary>
public enum ChatExecutionStepKind
{
    /// <summary>One actual request to a model provider.</summary>
    ModelRequest,
    /// <summary>One actual local or remote tool invocation.</summary>
    Tool,
    /// <summary>One attempted context summarization.</summary>
    Compaction
}

/// <summary>Execution status; unfinished persisted work restores as interrupted.</summary>
public enum ChatExecutionStatus
{
    /// <summary>The operation is executing.</summary>
    Running,
    /// <summary>The operation completed successfully.</summary>
    Completed,
    /// <summary>The operation failed.</summary>
    Failed,
    /// <summary>The caller cancelled the operation.</summary>
    Cancelled,
    /// <summary>The process ended before a terminal status was recorded.</summary>
    Interrupted,
    /// <summary>The turn is waiting for a user approval decision.</summary>
    AwaitingApproval
}

/// <summary>One immutable revision of an ordered execution step shared by Chat and Trajectory.</summary>
public sealed record ChatExecutionStep
{
    /// <summary>Stable step identity across live revisions and restoration.</summary>
    public required string Id { get; init; }
    /// <summary>One-based conversation-wide order of operation starts.</summary>
    public long Sequence { get; init; }
    /// <summary>Owning turn, or null for manual compaction between turns.</summary>
    public string? TurnId { get; init; }
    /// <summary>Operation discriminator.</summary>
    public ChatExecutionStepKind Kind { get; init; }
    /// <summary>Actual local operation start time.</summary>
    public DateTimeOffset StartedAt { get; init; }
    /// <summary>Actual local completion time, when terminal.</summary>
    public DateTimeOffset? CompletedAt { get; init; }
    /// <summary>Current lifecycle state.</summary>
    public ChatExecutionStatus Status { get; init; }
    /// <summary>Model request details for model steps.</summary>
    public ChatModelRequest? Request { get; init; }
    /// <summary>Tool invocation details for tool steps.</summary>
    public ChatToolExecution? Tool { get; init; }
    /// <summary>Context change details for compaction steps.</summary>
    public ChatCompactionResult? Compaction { get; init; }
    /// <summary>Sanitized failure description.</summary>
    public string? Error { get; init; }
    /// <summary>Measured wall-clock duration, unavailable before completion.</summary>
    [JsonIgnore]
    public TimeSpan? Duration => CompletedAt - StartedAt;
}

/// <summary>Safe request inspector data captured immediately before the underlying provider request.</summary>
public sealed record ChatModelRequest
{
    private IReadOnlyList<ChatContextMessage> _messages = [];
    private IReadOnlyList<ChatToolSchema> _tools = [];
    private IReadOnlyList<ChatContentPart> _output = [];
    /// <summary>Provider identifier.</summary>
    public required string ProviderId { get; init; }
    /// <summary>Effective requested model.</summary>
    public string? ModelName { get; init; }
    /// <summary>Provider configuration revision held by the run's lease.</summary>
    public long ConfigurationRevision { get; init; }
    /// <summary>Whether this provider request produced a compaction summary instead of a normal reply.</summary>
    public bool IsCompaction { get; init; }
    /// <summary>Effective next-request settings, captured before execution.</summary>
    public required ChatSessionSettings Settings { get; init; }
    /// <summary>Effective instructions, with credential-shaped data redacted.</summary>
    public string? Instructions { get; init; }
    /// <summary>Actual request messages in order, with credential-shaped data redacted.</summary>
    public IReadOnlyList<ChatContextMessage> Messages { get => _messages; init => _messages = ChatSnapshotOwnership.Own(value); }
    /// <summary>Actual exposed function schemas.</summary>
    public IReadOnlyList<ChatToolSchema> Tools { get => _tools; init => _tools = ChatSnapshotOwnership.Own(value); }
    /// <summary>Provider response identifier, if exposed.</summary>
    public string? ResponseId { get; init; }
    /// <summary>Provider-reported finish reason.</summary>
    public string? FinishReason { get; init; }
    /// <summary>Provider-reported token counts; individual unknown values remain null.</summary>
    public TokenUsage? Usage { get; init; }
    /// <summary>Time from request start until the first nonempty text/reasoning delta or function call.</summary>
    public TimeSpan? TimeToFirstToken { get; init; }
    /// <summary>
    /// Observed interval from first to final generated-content update; excludes tool execution.
    /// Zero means all generated content arrived in one update and throughput cannot be measured.
    /// Null means no streaming generation interval was observed, including non-streaming responses.
    /// </summary>
    public TimeSpan? GenerationDuration { get; init; }
    /// <summary>Ordered model content observed during this request.</summary>
    public IReadOnlyList<ChatContentPart> Output { get => _output; init => _output = ChatSnapshotOwnership.Own(value); }
    /// <summary>Output token speed when both provider usage and a positive measured interval are available.</summary>
    [JsonIgnore]
    public double? OutputTokensPerSecond => Usage?.OutputTokens is { } count
        && GenerationDuration is { TotalSeconds: > 0 } duration ? count / duration.TotalSeconds : null;
}

/// <summary>A function schema included in one provider request.</summary>
/// <param name="Name">Registered function name.</param>
/// <param name="Description">Function description.</param>
/// <param name="Parameters">JSON parameter schema, when exposed by the SDK.</param>
public sealed record ChatToolSchema(string Name, string? Description, JsonElement? Parameters);

/// <summary>Invocation details captured at the actual tool boundary.</summary>
public sealed record ChatToolExecution
{
    /// <summary>Model-assigned invocation identity.</summary>
    public required string CallId { get; init; }
    /// <summary>Registered tool name.</summary>
    public required string Name { get; init; }
    /// <summary>Redacted serialized invocation arguments.</summary>
    public string? Arguments { get; init; }
    /// <summary>Redacted serialized result.</summary>
    public string? Result { get; init; }
}
