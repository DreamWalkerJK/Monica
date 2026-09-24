using Monica.AI.Models;

namespace Monica.AI.Providers;

/// <summary>
/// OpenAI API surface used by the provider for chat requests.
/// </summary>
public enum OpenAIProviderApiMode
{
    /// <summary>
    /// Use the OpenAI Responses API through the Microsoft.Extensions.AI adapter.
    /// </summary>
    Responses,

    /// <summary>
    /// Use the OpenAI Chat Completions API through the Microsoft.Extensions.AI adapter.
    /// </summary>
    Chat
}

/// <summary>
/// Conversation-history strategy used with the OpenAI Responses API.
/// </summary>
public enum OpenAIResponsesHistoryMode
{
    /// <summary>
    /// Keep history in the agent session and resend it with each request. This mode disables
    /// stored Responses output so response identifiers are not treated as server-managed history.
    /// Use this mode for OpenAI-compatible endpoints that do not support durable
    /// <c>previous_response_id</c> references.
    /// </summary>
    LocalHistory,

    /// <summary>
    /// Use the latest response identifier as <c>previous_response_id</c>. Configure this mode only
    /// when the provider stores responses and guarantees that returned identifiers remain available
    /// to subsequent requests made with the same credentials and project context.
    /// </summary>
    PreviousResponseId
}

/// <summary>
/// OpenAI prompt cache retention policy for eligible prompt prefixes.
/// </summary>
public enum OpenAIPromptCacheRetention
{
    /// <summary>
    /// Use OpenAI's in-memory prompt cache retention policy.
    /// </summary>
    InMemory,

    /// <summary>
    /// Request OpenAI's extended 24-hour prompt cache retention policy.
    /// Configure only for models that support extended prompt caching.
    /// </summary>
    TwentyFourHours
}

/// <summary>
/// Base class for AI provider configuration.
/// </summary>
public abstract class AIProviderOptions
{
    /// <summary>Whether this provider may accept new requests; defaults to enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Unique provider identifier, or provider name if not set.
    /// </summary>
    public string? ProviderId { get; set; }

    /// <summary>
    /// Provider display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// API key
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// Default system prompt for the provider.
    /// </summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// Model identifiers selected by code configuration. Explicit AddModel templates apply to these names on any
    /// endpoint; built-in templates apply only to official endpoints or the local fake provider. Unmatched names
    /// retain unknown capabilities. If empty and Models is not supplied, the provider is considered invalid.
    /// </summary>
    public IList<string>? SupportedModels { get; set; }

    /// <summary>
    /// Provider-scoped model definitions. When supplied, these replace catalog lookup through
    /// <see cref="SupportedModels"/> and permit two providers to configure the same model ID differently.
    /// </summary>
    public IList<AIModelInfo>? Models { get; set; }

    /// <summary>Default chat model ID. Null selects the first configured chat model.</summary>
    public string? DefaultModel { get; set; }

    /// <summary>
    /// Base API URL for custom endpoints, if applicable.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Indicates whether this provider should be set as the default.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;
}

/// <summary>
/// OpenAI provider configuration.
/// </summary>
public class OpenAIProviderOptions : AIProviderOptions
{
    /// <summary>
    /// OpenAI-compatible protocol variation. Auto recognizes the official DeepSeek endpoint;
    /// select DeepSeek explicitly for a custom proxy that requires its thinking and reasoning fields.
    /// </summary>
    public OpenAIProtocolProfile ProtocolProfile { get; set; } = OpenAIProtocolProfile.Auto;

    /// <summary>
    /// API surface used for chat requests. Defaults to <see cref="OpenAIProviderApiMode.Responses"/>.
    /// The Responses API is preferred because OpenAI prompt caching is automatic for eligible long
    /// prompts and Responses can improve cache utilization for supported workloads.
    /// </summary>
    public OpenAIProviderApiMode ApiMode { get; set; } = OpenAIProviderApiMode.Responses;

    /// <summary>
    /// Conversation-history strategy for Responses requests. Defaults to
    /// <see cref="OpenAIResponsesHistoryMode.LocalHistory"/> so custom OpenAI-compatible endpoints
    /// do not need to implement durable response storage. Set
    /// <see cref="OpenAIResponsesHistoryMode.PreviousResponseId"/> only for providers that fully
    /// support response-ID chaining. This setting has no effect in
    /// <see cref="OpenAIProviderApiMode.Chat"/> mode. Monica's chat workbench always replays its
    /// own retained context and does not chain remote response identifiers, independently of
    /// this provider-client setting.
    /// </summary>
    public OpenAIResponsesHistoryMode ResponsesHistoryMode { get; set; } = OpenAIResponsesHistoryMode.LocalHistory;

    /// <summary>
    /// Stable OpenAI prompt cache routing key for requests that share the same long static prompt prefix.
    /// This value is sent as <c>prompt_cache_key</c> for OpenAI chat requests. It can improve cache hit
    /// rates by routing similar prefixes together, but it does not bypass OpenAI's minimum prompt length
    /// or exact-prefix-match requirements. Avoid storing user-private data in this key.
    /// </summary>
    public string? PromptCacheKey { get; set; }

    /// <summary>
    /// Optional OpenAI prompt cache retention policy sent as <c>prompt_cache_retention</c>.
    /// Leave unset to use OpenAI's model default. Set <see cref="OpenAIPromptCacheRetention.TwentyFourHours"/>
    /// only for models that support extended prompt caching; unsupported models may reject the request.
    /// </summary>
    public OpenAIPromptCacheRetention? PromptCacheRetention { get; set; }

    /// <summary>
    /// Organization ID (optional)
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// Project ID (optional)
    /// </summary>
    public string? Project { get; set; }
}

/// <summary>
/// Anthropic provider configuration.
/// </summary>
public class AnthropicProviderOptions : AIProviderOptions
{
    /// <summary>
    /// Default maximum number of tokens.
    /// </summary>
    public int MaxTokens { get; set; } = 4096;
}
