namespace Monica.AI.Providers;

/// <summary>Limits built-in model templates to the endpoints whose model identities they describe.</summary>
internal static class AIProviderMetadataPolicy
{
    internal static bool AllowsBuiltInTemplates(AIProviderOptions options) => options switch
    {
        OpenAIProviderOptions => AllowsBuiltInTemplates(EAIProviderType.OpenAI, options.BaseUrl),
        AnthropicProviderOptions => AllowsBuiltInTemplates(EAIProviderType.Anthropic, options.BaseUrl),
        // The synthetic provider deliberately reuses catalog shapes without contacting an endpoint.
        FakeProviderOptions => true,
        _ => false
    };

    internal static bool AllowsBuiltInTemplates(EAIProviderType providerType, string? baseUrl)
    {
        var expectedHost = providerType switch
        {
            EAIProviderType.OpenAI => "api.openai.com",
            EAIProviderType.Anthropic => "api.anthropic.com",
            _ => null
        };
        if (expectedHost is null) return false;
        if (string.IsNullOrWhiteSpace(baseUrl)) return true;
        return Uri.TryCreate(baseUrl, UriKind.Absolute, out var endpoint)
            && endpoint.Scheme == Uri.UriSchemeHttps && endpoint.IsDefaultPort
            && string.IsNullOrEmpty(endpoint.UserInfo)
            && string.Equals(endpoint.Host, expectedHost, StringComparison.OrdinalIgnoreCase);
    }
}
