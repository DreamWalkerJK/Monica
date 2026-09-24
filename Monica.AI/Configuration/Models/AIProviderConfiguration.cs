using Monica.AI.Providers;

namespace Monica.AI.Configuration.Models;

/// <summary>
/// Host-wide, non-secret provider settings. API keys are supplied separately on writes and never returned here.
/// Saving replaces this provider's override as one coherent configuration; resetting restores its code defaults.
/// </summary>
public sealed record AIProviderConfiguration
{
    /// <summary>Stable provider identifier. Model identifiers are scoped to this value.</summary>
    public required string ProviderId { get; init; }
    /// <summary>Wire protocol. Runtime editing supports OpenAI-compatible and Anthropic providers.</summary>
    public EAIProviderType ProviderType { get; init; } = EAIProviderType.OpenAI;
    /// <summary>Display name, or null to use the provider identifier.</summary>
    public string? DisplayName { get; init; }
    /// <summary>Optional absolute HTTP(S) endpoint. Omit to use the protocol's official endpoint.</summary>
    public string? BaseUrl { get; init; }
    /// <summary>Whether new requests can use this provider. Running requests retain their captured settings.</summary>
    public bool Enabled { get; init; } = true;
    /// <summary>Whether this provider is preferred when a request does not specify one.</summary>
    public bool IsDefault { get; init; }
    /// <summary>Request timeout in seconds; defaults to 120 seconds.</summary>
    public int TimeoutSeconds { get; init; } = 120;
    /// <summary>Optional provider-wide system instruction default.</summary>
    public string? SystemPrompt { get; init; }
    /// <summary>Default chat model identifier. Null selects the first configured chat model.</summary>
    public string? DefaultModel { get; init; }
    /// <summary>OpenAI wire surface; Chat defaults to broad compatibility with custom endpoints.</summary>
    public OpenAIProviderApiMode OpenAIApiMode { get; init; } = OpenAIProviderApiMode.Chat;
    /// <summary>Protocol variation. Auto recognizes the official DeepSeek host; custom proxies need explicit selection.</summary>
    public OpenAIProtocolProfile OpenAIProtocolProfile { get; init; } = OpenAIProtocolProfile.Auto;
    /// <summary>
    /// History strategy for direct OpenAI Responses client consumers. Defaults to local history.
    /// The chat workbench always owns and replays its retained context without remote response-ID chaining.
    /// </summary>
    public OpenAIResponsesHistoryMode ResponsesHistoryMode { get; init; } = OpenAIResponsesHistoryMode.LocalHistory;
    /// <summary>Optional OpenAI prompt-cache routing key, without private user data.</summary>
    public string? PromptCacheKey { get; init; }
    /// <summary>Optional OpenAI prompt-cache retention request.</summary>
    public OpenAIPromptCacheRetention? PromptCacheRetention { get; init; }
    /// <summary>Optional OpenAI organization identifier.</summary>
    public string? Organization { get; init; }
    /// <summary>Optional OpenAI project identifier.</summary>
    public string? Project { get; init; }
    /// <summary>Provider-scoped model definitions. An empty list permits discovery before models are selected.</summary>
    public IReadOnlyList<AIModelConfiguration> Models { get; init; } = [];
}
