using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Monica.AI.Abstractions;
using Monica.AI.Configuration.Abstractions;
using Monica.AI.Configuration.Models;
using Monica.AI.Providers;
using Monica.AI.Services;

namespace Monica.AI.Configuration.Services;

/// <summary>Owns settings precedence, credential protection, validation, and revision-checked mutations.</summary>
internal sealed class AIConfigurationService(
    IAIConfigurationStore store,
    IEnumerable<AIProviderDefinition> definitions,
    AIModelCatalog catalog,
    IDataProtectionProvider dataProtection,
    IEnumerable<IAIProvider>? customProviders = null)
{
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private readonly IReadOnlyDictionary<string, CodeProvider> _defaults = CreateDefaults(definitions, catalog);
    private readonly IReadOnlyList<IAIProvider> _customProviders = customProviders?.ToArray() ?? [];
    private AIConfigurationDocument _document = ValidateDocument(store.Read());

    public long CurrentRevision => Volatile.Read(ref _document).Revision;

    public string Redact(string message)
    {
        foreach (var key in Resolve().Providers.Select(static provider => provider.ApiKey).Where(static key => !string.IsNullOrWhiteSpace(key)))
        {
            message = message.Replace(key!, "[redacted]", StringComparison.Ordinal);
        }

        return message;
    }

    public AIConfigurationSnapshot GetSnapshot() => ProjectSnapshot(Volatile.Read(ref _document));

    public async Task<AIConfigurationSnapshot> RefreshAsync(CancellationToken ct)
    {
        await _writeGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var latest = ValidateDocument(store.Read());
            Volatile.Write(ref _document, latest);
            return ProjectSnapshot(latest);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public EffectiveAIConfiguration Resolve()
    {
        var document = Volatile.Read(ref _document);
        var overrides = document.Providers.ToDictionary(static entry => entry.Configuration.ProviderId, StringComparer.OrdinalIgnoreCase);
        var ids = _defaults.Keys.Concat(overrides.Keys).Distinct(StringComparer.OrdinalIgnoreCase);
        var providers = new List<ResolvedAIProvider>();
        foreach (var id in ids)
        {
            _defaults.TryGetValue(id, out var baseline);
            overrides.TryGetValue(id, out var persisted);
            var configuration = persisted?.Configuration ?? baseline!.Configuration;
            string? apiKey;
            string? credentialError = null;
            try
            {
                apiKey = persisted?.ProtectedApiKey is { } protectedKey
                    ? GetProtector(id).Unprotect(protectedKey)
                    : persisted is null || persisted.UseCodeApiKey ? baseline?.ApiKey : null;
            }
            catch (CryptographicException)
            {
                apiKey = null;
                credentialError = "The stored API key cannot be decrypted by this host. Restore the data-protection key ring or enter the API key again.";
            }

            providers.Add(new ResolvedAIProvider(configuration, apiKey, credentialError));
        }

        return new EffectiveAIConfiguration(document.Revision, providers);
    }

    internal ResolvedAIProvider ResolveDraft(AIProviderConfiguration draft, string? apiKey, bool useSavedApiKey)
    {
        ArgumentNullException.ThrowIfNull(draft);
        // Discovery needs connection settings only; unfinished model edits must not block listing models.
        var connection = Normalize(draft with { ProviderId = string.IsNullOrWhiteSpace(draft.ProviderId) ? "draft" : draft.ProviderId,
            Models = [], DefaultModel = null });
        ValidateConfiguration(connection);
        if (string.IsNullOrEmpty(apiKey) && useSavedApiKey)
        {
            var existing = Resolve().Providers.FirstOrDefault(provider => SameId(provider.Configuration.ProviderId, draft.ProviderId))
                ?? throw new KeyNotFoundException("The saved provider credential was not found.");
            if (existing.CredentialError is { } error) throw new InvalidOperationException(error);
            apiKey = existing.ApiKey;
        }
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("Enter an API key before fetching available models.");
        return new ResolvedAIProvider(connection, apiKey, null);
    }

    public Task<AIConfigurationSnapshot> UpsertAsync(
        AIProviderConfiguration configuration,
        string? apiKey,
        bool clearApiKey,
        long expectedRevision,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var normalized = Normalize(configuration);
        ValidateConfiguration(normalized);
        if (_customProviders.Any(provider => SameId(provider.ProviderId, normalized.ProviderId)))
        {
            throw new InvalidOperationException("This identifier belongs to a host-supplied custom provider and cannot be replaced by runtime settings.");
        }

        if (normalized.IsDefault && _customProviders.Any(static provider => provider.Info.IsValid && provider.Info.IsDefault))
        {
            throw new InvalidOperationException("The host has a custom default provider. Clear its code-defined default before choosing a runtime default.");
        }
        if (clearApiKey && !string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("An API key cannot be supplied and cleared in the same operation.");
        }

        return MutateAsync(expectedRevision, entries =>
        {
            var existing = entries.FirstOrDefault(entry => SameId(entry.Configuration.ProviderId, normalized.ProviderId));
            var replacement = new AIPersistedProvider
            {
                Configuration = normalized,
                ProtectedApiKey = clearApiKey ? null : !string.IsNullOrEmpty(apiKey)
                    ? GetProtector(normalized.ProviderId).Protect(apiKey)
                    : existing?.ProtectedApiKey,
                UseCodeApiKey = !clearApiKey && string.IsNullOrEmpty(apiKey)
                    && (existing?.UseCodeApiKey ?? _defaults.ContainsKey(normalized.ProviderId))
            };
            entries.RemoveAll(entry => SameId(entry.Configuration.ProviderId, normalized.ProviderId));
            if (normalized.IsDefault)
            {
                // Choosing a new default is one atomic settings mutation, including inherited code defaults.
                foreach (var current in ProjectSnapshot(new AIConfigurationDocument { Providers = entries }).Providers)
                {
                    if (!current.Configuration.IsDefault || SameId(current.Configuration.ProviderId, normalized.ProviderId))
                    {
                        continue;
                    }

                    var old = entries.FirstOrDefault(entry => SameId(entry.Configuration.ProviderId, current.Configuration.ProviderId));
                    entries.RemoveAll(entry => SameId(entry.Configuration.ProviderId, current.Configuration.ProviderId));
                    entries.Add((old ?? new AIPersistedProvider
                    {
                        Configuration = current.Configuration, UseCodeApiKey = current.IsCodeDefined
                    }) with { Configuration = current.Configuration with { IsDefault = false } });
                }
            }

            entries.Add(replacement);
        }, ct);
    }

    public Task<AIConfigurationSnapshot> RemoveAsync(string providerId, long expectedRevision, CancellationToken ct)
    {
        if (_defaults.ContainsKey(providerId))
        {
            throw new InvalidOperationException("Code-defined providers cannot be deleted. Disable the provider or reset its override.");
        }

        return MutateAsync(expectedRevision, entries =>
        {
            if (entries.RemoveAll(entry => SameId(entry.Configuration.ProviderId, providerId)) == 0)
            {
                throw new KeyNotFoundException($"Provider '{providerId}' does not exist.");
            }
        }, ct);
    }

    public Task<AIConfigurationSnapshot> ResetAsync(string providerId, long expectedRevision, CancellationToken ct)
    {
        if (!_defaults.ContainsKey(providerId))
        {
            throw new InvalidOperationException("Only code-defined providers have a baseline to restore.");
        }

        return MutateAsync(expectedRevision, entries =>
        {
            entries.RemoveAll(entry => SameId(entry.Configuration.ProviderId, providerId));
            EnsureOneDefault(entries);
        }, ct);
    }

    private async Task<AIConfigurationSnapshot> MutateAsync(
        long expectedRevision,
        Action<List<AIPersistedProvider>> mutation,
        CancellationToken ct)
    {
        await _writeGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var current = Volatile.Read(ref _document);
            if (current.Revision != expectedRevision)
            {
                throw new AIConfigurationConflictException(expectedRevision, current.Revision);
            }

            var entries = current.Providers.ToList();
            mutation(entries);
            var committed = await store.WriteAsync(new AIConfigurationDocument { Providers = entries.ToArray() }, expectedRevision, ct)
                .ConfigureAwait(false);
            Volatile.Write(ref _document, ValidateDocument(committed));
            return ProjectSnapshot(committed);
        }
        catch (AIConfigurationConflictException)
        {
            Volatile.Write(ref _document, ValidateDocument(store.Read()));
            throw;
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private AIConfigurationSnapshot ProjectSnapshot(AIConfigurationDocument document)
    {
        var providers = _defaults.ToDictionary(static entry => entry.Key, static entry => new AIProviderSettings
        {
            Configuration = Clone(entry.Value.Configuration), HasApiKey = !string.IsNullOrWhiteSpace(entry.Value.ApiKey), IsCodeDefined = true
        }, StringComparer.OrdinalIgnoreCase);
        foreach (var entry in document.Providers)
        {
            _defaults.TryGetValue(entry.Configuration.ProviderId, out var baseline);
            providers[entry.Configuration.ProviderId] = new AIProviderSettings
            {
                Configuration = Clone(entry.Configuration), IsCodeDefined = baseline is not null, HasOverride = true,
                HasApiKey = !string.IsNullOrWhiteSpace(entry.ProtectedApiKey)
                    || entry.UseCodeApiKey && !string.IsNullOrWhiteSpace(baseline?.ApiKey)
            };
        }

        return new AIConfigurationSnapshot
        {
            Revision = document.Revision,
            Providers = providers.Values.OrderBy(static entry => entry.Configuration.DisplayName ?? entry.Configuration.ProviderId, StringComparer.OrdinalIgnoreCase).ToArray()
        };
    }

    private void EnsureOneDefault(List<AIPersistedProvider> entries)
    {
        var defaults = ProjectSnapshot(new AIConfigurationDocument { Providers = entries }).Providers
            .Where(static entry => entry.Configuration.Enabled && entry.Configuration.IsDefault).ToArray();
        if (defaults.Length > 1)
        {
            throw new InvalidOperationException("Reset would restore more than one default provider. Clear the current default before resetting this provider.");
        }
    }

    private IDataProtector GetProtector(string providerId) => dataProtection.CreateProtector(
        "Monica.AI.ProviderCredentials.v1", providerId.ToUpperInvariant());

    private static bool SameId(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static AIProviderConfiguration Clone(AIProviderConfiguration configuration) => configuration with
    {
        Models = configuration.Models.Select(static model => model with { ReasoningLevels = model.ReasoningLevels.ToArray() }).ToArray()
    };

    private static AIProviderConfiguration Normalize(AIProviderConfiguration configuration) => Clone(configuration) with
    {
        ProviderId = configuration.ProviderId.Trim(),
        DisplayName = string.IsNullOrWhiteSpace(configuration.DisplayName) ? null : configuration.DisplayName.Trim(),
        BaseUrl = string.IsNullOrWhiteSpace(configuration.BaseUrl) ? null : configuration.BaseUrl.Trim(),
        DefaultModel = string.IsNullOrWhiteSpace(configuration.DefaultModel) ? null : configuration.DefaultModel.Trim(),
        Models = configuration.Models.Select(static model => model with
        {
            ModelName = model.ModelName.Trim(),
            ReasoningLevels = model.ReasoningLevels.Select(static level => level with { Id = level.Id.Trim() }).ToArray()
        }).ToArray()
    };

    private static AIConfigurationDocument ValidateDocument(AIConfigurationDocument document)
    {
        if (document.Revision < 0 || document.Providers.Select(static entry => entry.Configuration.ProviderId)
            .Distinct(StringComparer.OrdinalIgnoreCase).Count() != document.Providers.Count)
        {
            throw new InvalidDataException("The AI configuration document has an invalid revision or duplicate provider identifiers.");
        }

        foreach (var entry in document.Providers)
        {
            ValidateConfiguration(entry.Configuration);
        }

        return document with { Providers = document.Providers.Select(static entry => entry with { Configuration = Clone(entry.Configuration) }).ToArray() };
    }

    private static IReadOnlyDictionary<string, CodeProvider> CreateDefaults(IEnumerable<AIProviderDefinition> definitions, AIModelCatalog catalog)
    {
        var providers = new Dictionary<string, CodeProvider>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in definitions)
        {
            var configuration = Normalize(definition.ToConfiguration(catalog));
            ValidateConfiguration(configuration);
            if (!providers.TryAdd(configuration.ProviderId, new CodeProvider(configuration, definition.ApiKey)))
            {
                throw new InvalidOperationException($"Duplicate AI provider identifier '{configuration.ProviderId}'.");
            }
        }

        if (providers.Values.Count(static provider => provider.Configuration.Enabled && provider.Configuration.IsDefault) > 1)
        {
            throw new InvalidOperationException("Only one enabled AI provider may be configured as the default.");
        }

        return providers;
    }

    private static void ValidateConfiguration(AIProviderConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configuration.ProviderId);
        if (configuration.ProviderId.Length > 128 || configuration.ProviderId.Any(char.IsControl))
        {
            throw new ArgumentException("Provider identifiers must contain at most 128 characters and no control characters.");
        }

        if (configuration.ProviderType is not (EAIProviderType.OpenAI or EAIProviderType.Anthropic))
        {
            throw new ArgumentException("Runtime configuration supports OpenAI-compatible and Anthropic providers.");
        }

        if (configuration.TimeoutSeconds is < 1 or > 3600)
        {
            throw new ArgumentException("Request timeout must be between 1 and 3600 seconds.");
        }

        if (!Enum.IsDefined(configuration.OpenAIApiMode) || !Enum.IsDefined(configuration.OpenAIProtocolProfile)
            || !Enum.IsDefined(configuration.ResponsesHistoryMode)
            || configuration.PromptCacheRetention is { } retention && !Enum.IsDefined(retention))
        {
            throw new ArgumentException("The provider has an unsupported API mode, protocol profile, history mode, or cache-retention setting.");
        }

        if (configuration.BaseUrl is { } baseUrl
            && (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
                || uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo)
                || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)))
        {
            throw new ArgumentException("The provider endpoint must be an absolute HTTP(S) URL without embedded credentials, query, or fragment.");
        }

        if (configuration.Models.Select(static model => model.ModelName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != configuration.Models.Count)
        {
            throw new ArgumentException("Model identifiers must be unique within a provider.");
        }

        foreach (var model in configuration.Models)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(model.ModelName);
            if (!Enum.IsDefined(model.Kind))
            {
                throw new ArgumentException($"Model '{model.ModelName}' has an unsupported model kind.");
            }

            if (model.ContextWindow is <= 0 || model.MaxOutputTokens is <= 0 || model.EmbeddingDimensions is <= 0
                || model.ContextWindow is { } context && model.MaxOutputTokens is { } output && output >= context)
            {
                throw new ArgumentException($"Model '{model.ModelName}' needs positive capacities and an output budget smaller than its context window.");
            }

            if (model.ReasoningLevels.Select(static level => level.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != model.ReasoningLevels.Count
                || model.ReasoningLevels.Any(static level => string.IsNullOrWhiteSpace(level.Id) || level.BudgetTokens is <= 0))
            {
                throw new ArgumentException($"Model '{model.ModelName}' has invalid or duplicate reasoning levels.");
            }

            if (model.SupportsReasoning == false && model.ReasoningLevels.Count > 0)
            {
                throw new ArgumentException($"Model '{model.ModelName}' cannot declare reasoning levels while reasoning support is disabled.");
            }

            foreach (var level in model.ReasoningLevels.Where(static level => level.BudgetTokens is not null))
            {
                if (configuration.ProviderType == EAIProviderType.OpenAI)
                {
                    throw new ArgumentException("OpenAI-compatible model configuration supports reasoning effort but has no standard thinking-token budget field.");
                }

                if (level.BudgetTokens < 1024 || model.MaxOutputTokens is { } maximum && level.BudgetTokens >= maximum
                    || string.Equals(level.ProviderValue, "none", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"Model '{model.ModelName}' requires a thinking budget of at least 1024, below its maximum output budget, with reasoning enabled.");
                }
            }

            if (model.DefaultReasoningLevel is { } selected
                && !model.ReasoningLevels.Any(level => SameId(level.Id, selected)))
            {
                throw new ArgumentException($"Model '{model.ModelName}' has a default reasoning level that is not configured.");
            }
        }

        if (configuration.DefaultModel is { } defaultModel
            && !configuration.Models.Any(model => model.Kind == AIModelKind.Chat && SameId(model.ModelName, defaultModel)))
        {
            throw new ArgumentException("The default model must be a configured chat model belonging to this provider.");
        }
    }

    private sealed record CodeProvider(AIProviderConfiguration Configuration, string? ApiKey);
}

internal sealed record EffectiveAIConfiguration(long Revision, IReadOnlyList<ResolvedAIProvider> Providers);
internal sealed record ResolvedAIProvider(AIProviderConfiguration Configuration, string? ApiKey, string? CredentialError);
