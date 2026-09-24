namespace Monica.AI.Providers;

/// <summary>Identifies a supported protocol variation of the OpenAI-compatible endpoint.</summary>
public enum OpenAIProtocolProfile
{
    /// <summary>
    /// Select DeepSeek for the official api.deepseek.com host and Standard for every other endpoint.
    /// Custom gateways serving DeepSeek must select DeepSeek explicitly.
    /// </summary>
    Auto,

    /// <summary>Use the standard OpenAI request fields and usage metadata.</summary>
    Standard,

    /// <summary>
    /// Use DeepSeek's Chat thinking toggle, output-token limit field, and cache counters.
    /// Observed reasoning replay is supported by every Chat profile; Responses keeps its own reasoning fields.
    /// </summary>
    DeepSeek
}
