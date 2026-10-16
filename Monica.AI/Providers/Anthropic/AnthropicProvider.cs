using Anthropic;
using Anthropic.Models.Models;
using System.Collections.Concurrent;
using Microsoft.Extensions.AI;
using Monica.AI.Abstractions;
using Monica.AI.Models;
using Monica.AI.Services;

namespace Monica.AI.Providers.Anthropic;

/// <summary>
/// Anthropic Provider implementation.
/// </summary>
internal sealed class AnthropicProvider : IAIProvider
{
    private const EAIProviderType PROVIDER_KIND = EAIProviderType.Anthropic;
    private readonly AnthropicProviderOptions _options;
    private readonly AnthropicClient _client;
    private readonly IReadOnlyList<AIModelInfo> _models;
    private readonly string? _defaultModel;
    private readonly bool _isValid;
    private readonly ConcurrentDictionary<string, Lazy<IChatClient>> _chatClients = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public AnthropicProvider(AnthropicProviderOptions options, AIModelCatalog modelCatalog)
    {
        _options = options;

        // Anthropic SDK v12 configures the client using an object initializer.
        _client = new AnthropicClient
        {
            ApiKey = options.ApiKey,
            BaseUrl = options.BaseUrl ?? "https://api.anthropic.com",
            Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
        };

        var resolution = AIProviderModelResolver.ResolveModels(modelCatalog, options);
        _models = resolution.Models;
        _defaultModel = resolution.DefaultModel;
        _isValid = resolution.IsValid;
    }

    /// <inheritdoc />
    public string ProviderId => _options.ProviderId ?? PROVIDER_KIND.ToString();

    /// <inheritdoc />
    public string ProviderType => PROVIDER_KIND.ToString();

    /// <inheritdoc />
    public string DisplayName => _options.DisplayName ?? AIProviderNaming.BuildDisplayName(ProviderType, ProviderId);

    /// <inheritdoc />
    public AIProviderInfo Info => new()
    {
        ProviderId = ProviderId,
        DisplayName = DisplayName,
        Description = $"{ProviderType} Claude models",
        ProviderType = ProviderType,
        DefaultModel = _defaultModel,
        SystemPrompt = _options.SystemPrompt,
        SupportedModels = _models,
        IsValid = _isValid,
        IsDefault = _options.IsDefault,
        Icon = "anthropic"
    };

    /// <inheritdoc />
    public IChatClient GetChatClient(string? modelName = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var resolvedModel = !string.IsNullOrWhiteSpace(modelName) ? modelName : _defaultModel;
        if (string.IsNullOrWhiteSpace(resolvedModel))
        {
            throw new InvalidOperationException("Anthropic model is not configured.");
        }

        if (!_models.OfType<LLMModelInfo>().Any(model => string.Equals(model.ModelName, resolvedModel, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Chat model '{resolvedModel}' is not configured on provider '{ProviderId}'.");
        }

        return _chatClients.GetOrAdd(resolvedModel, name => new Lazy<IChatClient>(() => new AnthropicRequestOptionsChatClient(
            _client.AsIChatClient(name, _options.MaxTokens), name, _options.MaxTokens))).Value;
    }

    /// <inheritdoc />
    public IEmbeddingGenerator<string, Embedding<float>> GetEmbeddingGenerator(
        string? modelName = null)
    {
        throw new NotSupportedException(
            "Anthropic does not provide embedding models. " +
            "Use an OpenAI provider for embedding generation.");
    }

    /// <inheritdoc />
    public async Task TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            var chatClient = GetChatClient();
            _ = await chatClient.GetResponseAsync(
                [new ChatMessage(ChatRole.User, "Hello")],
                new ChatOptions {MaxOutputTokens = 10},
                ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Anthropic connection test failed: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken ct = default)
    {
        IReadOnlyList<string> models = _models.Select(m => m.ModelName).ToList();
        return Task.FromResult(models);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AIRemoteModelInfo>> FetchRemoteModelsAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        try
        {
            var remoteModels = new List<AIRemoteModelInfo>();
            var page = await _client.Models.List(new ModelListParams { Limit = 1000 }, ct);

            void CollectModels(ModelListPage p)
            {
                foreach (var model in p.Items)
                {
                    remoteModels.Add(new AIRemoteModelInfo
                    {
                        ModelId = model.ID,
                        Configuration = AnthropicModelMetadata.Read(model),
                        Metadata = new Dictionary<string, string>
                        {
                            ["DisplayName"] = model.RawData.GetValueOrDefault("display_name").ToString(),
                            ["CreatedAt"] = model.RawData.GetValueOrDefault("created_at").ToString()
                        }
                    });
                }
            }

            CollectModels(page);

            while (page.HasNext())
            {
                page = await page.Next(ct);
                CollectModels(page);
            }

            remoteModels.Sort((a, b) => string.Compare(a.ModelId, b.ModelId, StringComparison.Ordinal));
            return remoteModels;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to fetch Anthropic models: " + ex.Message, ex);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var chatClient in _chatClients.Values)
        {
            if (chatClient.IsValueCreated) chatClient.Value.Dispose();
        }

        _chatClients.Clear();
        _client.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
