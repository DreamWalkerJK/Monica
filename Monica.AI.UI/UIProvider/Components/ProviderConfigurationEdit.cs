using Monica.AI.Configuration.Models;

namespace Monica.AI.UI.UIProvider.Components;

/// <summary>A single provider edit with a write-only replacement credential.</summary>
public sealed record ProviderConfigurationEdit(AIProviderConfiguration Configuration, string? ApiKey, bool ClearApiKey);
