namespace Monica.AI.Providers;

/// <summary>Supported built-in AI wire protocols and the deterministic development provider.</summary>
public enum EAIProviderType
{
    /// <summary>OpenAI Responses or Chat Completions, including compatible endpoints.</summary>
    OpenAI,
    /// <summary>Anthropic Messages API.</summary>
    Anthropic,
    /// <summary>Local synthetic embedding generation for development.</summary>
    Fake
}
