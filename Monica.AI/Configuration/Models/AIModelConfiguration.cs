using Monica.AI.Models;

namespace Monica.AI.Configuration.Models;

/// <summary>The protocol capability for which a provider-scoped model is configured.</summary>
public enum AIModelKind
{
    /// <summary>A conversational language model.</summary>
    Chat,
    /// <summary>A text embedding model.</summary>
    Embedding
}

/// <summary>
/// A selectable reasoning level and its provider mapping. Unknown mappings are never inferred from a model name.
/// </summary>
public sealed record AIReasoningLevel
{
    /// <summary>Stable identifier selected by conversations, such as <c>high</c>.</summary>
    public required string Id { get; init; }

    /// <summary>Optional human-readable label; consumers fall back to <see cref="Id"/>.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Reasoning-effort value understood by the provider; null omits the effort parameter.</summary>
    public string? ProviderValue { get; init; }

    /// <summary>Optional provider-supported thinking-token budget. Null uses the provider default.</summary>
    public int? BudgetTokens { get; init; }
}

/// <summary>
/// Editable metadata for one model within a provider. Nullable capabilities distinguish unknown from unsupported.
/// </summary>
public sealed record AIModelConfiguration
{
    /// <summary>Exact model identifier sent to this provider.</summary>
    public required string ModelName { get; init; }
    /// <summary>Optional presentation name; defaults to the model identifier.</summary>
    public string? DisplayName { get; init; }
    /// <summary>Model purpose. Defaults to conversational chat.</summary>
    public AIModelKind Kind { get; init; }
    /// <summary>Optional operator-facing description.</summary>
    public string? Description { get; init; }
    /// <summary>Total context capacity in tokens, or null when unverified.</summary>
    public int? ContextWindow { get; init; }
    /// <summary>Maximum output tokens, or null when unverified.</summary>
    public int? MaxOutputTokens { get; init; }
    /// <summary>Whether native image input is supported; null means unknown.</summary>
    public bool? SupportsImage { get; init; }
    /// <summary>Whether native document input is supported; extracted text is independent of this capability.</summary>
    public bool? SupportsDocuments { get; init; }
    /// <summary>Whether structured function tools are supported; null means unknown.</summary>
    public bool? SupportsTools { get; init; }
    /// <summary>Whether configurable reasoning is supported; null means unknown.</summary>
    public bool? SupportsReasoning { get; init; }
    /// <summary>Known selectable reasoning levels and their explicit protocol mappings.</summary>
    public IReadOnlyList<AIReasoningLevel> ReasoningLevels { get; init; } = [];
    /// <summary>Default reasoning-level identifier, or null to leave reasoning at the provider default.</summary>
    public string? DefaultReasoningLevel { get; init; }
    /// <summary>Embedding vector dimensions, or null until configured or deliberately probed.</summary>
    public int? EmbeddingDimensions { get; init; }

    /// <summary>Fills unknown metadata from endpoint evidence while preserving every explicit configured value.</summary>
    public AIModelConfiguration WithDiscoveredMetadata(AIModelConfiguration evidence) => this with
    {
        DisplayName = DisplayName ?? evidence.DisplayName,
        Description = Description ?? evidence.Description,
        ContextWindow = ContextWindow ?? evidence.ContextWindow,
        MaxOutputTokens = MaxOutputTokens ?? evidence.MaxOutputTokens,
        SupportsImage = SupportsImage ?? evidence.SupportsImage,
        SupportsDocuments = SupportsDocuments ?? evidence.SupportsDocuments,
        SupportsTools = SupportsTools ?? evidence.SupportsTools,
        SupportsReasoning = SupportsReasoning ?? evidence.SupportsReasoning,
        ReasoningLevels = SupportsReasoning == false || ReasoningLevels.Count > 0 ? ReasoningLevels : evidence.ReasoningLevels,
        EmbeddingDimensions = EmbeddingDimensions ?? evidence.EmbeddingDimensions
    };

    internal AIModelInfo ToModelInfo() => Kind switch
    {
        AIModelKind.Embedding => new EmbeddingModelInfo
        {
            ModelName = ModelName, DisplayName = DisplayName, Description = Description,
            Dimensions = EmbeddingDimensions, MaxInputTokens = ContextWindow
        },
        _ => new LLMModelInfo
        {
            ModelName = ModelName, DisplayName = DisplayName, Description = Description,
            ContextWindow = ContextWindow, MaxOutputTokens = MaxOutputTokens,
            SupportsImage = SupportsImage, SupportsDocuments = SupportsDocuments,
            SupportsTools = SupportsTools, SupportsReasoning = SupportsReasoning,
            ReasoningLevels = ReasoningLevels.ToArray(), DefaultReasoningLevel = DefaultReasoningLevel
        }
    };

    internal static AIModelConfiguration FromModelInfo(AIModelInfo model) => model switch
    {
        EmbeddingModelInfo embedding => new AIModelConfiguration
        {
            ModelName = embedding.ModelName, DisplayName = embedding.DisplayName, Description = embedding.Description,
            Kind = AIModelKind.Embedding, EmbeddingDimensions = embedding.Dimensions, ContextWindow = embedding.MaxInputTokens
        },
        LLMModelInfo chat => new AIModelConfiguration
        {
            ModelName = chat.ModelName, DisplayName = chat.DisplayName, Description = chat.Description,
            ContextWindow = chat.ContextWindow, MaxOutputTokens = chat.MaxOutputTokens,
            SupportsImage = chat.SupportsImage, SupportsDocuments = chat.SupportsDocuments,
            SupportsTools = chat.SupportsTools, SupportsReasoning = chat.SupportsReasoning,
            ReasoningLevels = chat.ReasoningLevels.ToArray(), DefaultReasoningLevel = chat.DefaultReasoningLevel
        },
        _ => new AIModelConfiguration { ModelName = model.ModelName, DisplayName = model.DisplayName, Description = model.Description }
    };
}
