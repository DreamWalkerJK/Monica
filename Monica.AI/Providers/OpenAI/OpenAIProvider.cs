using System.ClientModel;
using System.Collections.Concurrent;
using Microsoft.Extensions.AI;
using Monica.AI.Abstractions;
using Monica.AI.Models;
using Monica.AI.Services;
using OpenAI;
using AIChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Monica.AI.Providers.OpenAI;

/// <summary>
/// OpenAI provider implementation.
/// </summary>
internal sealed class OpenAIProvider : IAIProvider
{
    private const EAIProviderType PROVIDER_KIND = EAIProviderType.OpenAI;
    private readonly OpenAIProviderOptions _options;
    private readonly OpenAIClient _client;
    private readonly IReadOnlyList<AIModelInfo> _models;
    private readonly string? _defaultModel;
    private readonly bool _isValid;
    private readonly ConcurrentDictionary<string, Lazy<IChatClient>> _chatClients = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Lazy<IEmbeddingGenerator<string, Embedding<float>>>>
        _embeddingGenerators = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public OpenAIProvider(OpenAIProviderOptions options, AIModelCatalog modelCatalog)
    {
        _options = options;

        var clientOptions = new OpenAIClientOptions
        {
            NetworkTimeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
            OrganizationId = options.Organization,
            ProjectId = options.Project
        };
        if (!string.IsNullOrEmpty(options.BaseUrl))
        {
            clientOptions.Endpoint = new Uri(options.BaseUrl);
        }

        _client = new OpenAIClient(new ApiKeyCredential(options.ApiKey), clientOptions);

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
        Description = $"{ProviderType} GPT models",
        ProviderType = ProviderType,
        DefaultModel = _defaultModel,
        SystemPrompt = _options.SystemPrompt,
        SupportedModels = _models,
        IsValid = _isValid,
        Metadata = BuildMetadata(_options),
        IsDefault = _options.IsDefault,
        Icon = "openai"
    };

    /// <inheritdoc />
    public IChatClient GetChatClient(string? modelName = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var resolvedModel = !string.IsNullOrWhiteSpace(modelName) ? modelName : _defaultModel;
        if (string.IsNullOrWhiteSpace(resolvedModel))
        {
            throw new InvalidOperationException("OpenAI model is not configured.");
        }

        if (!_models.OfType<LLMModelInfo>().Any(model => string.Equals(model.ModelName, resolvedModel, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Chat model '{resolvedModel}' is not configured on provider '{ProviderId}'.");
        }

        return _chatClients.GetOrAdd(resolvedModel, name => new Lazy<IChatClient>(() => CreateChatClient(name))).Value;
    }

    /// <inheritdoc />
    public IEmbeddingGenerator<string, Embedding<float>> GetEmbeddingGenerator(
        string? modelName = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var resolvedModel = !string.IsNullOrWhiteSpace(modelName)
            ? modelName
            : _models.OfType<EmbeddingModelInfo>().FirstOrDefault()?.ModelName
              ?? throw new NotSupportedException(
                  "No embedding model configured for this OpenAI provider. " +
                  "Add an embedding model to SupportedModels (e.g., 'text-embedding-3-small').");

        if (!_models.OfType<EmbeddingModelInfo>().Any(model => string.Equals(model.ModelName, resolvedModel, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Embedding model '{resolvedModel}' is not configured on provider '{ProviderId}'.");
        }

        return _embeddingGenerators.GetOrAdd(resolvedModel, name =>
            new Lazy<IEmbeddingGenerator<string, Embedding<float>>>(() => _client.GetEmbeddingClient(name).AsIEmbeddingGenerator())).Value;
    }

    /// <inheritdoc />
    public async Task TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            var chatClient = GetChatClient();
            _ = await chatClient.GetResponseAsync(
                [new AIChatMessage(ChatRole.User, "Hello")],
                new ChatOptions { MaxOutputTokens = 10 },
                ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"OpenAI connection test failed: {ex.Message}", ex);
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
            var modelClient = _client.GetOpenAIModelClient();
            var result = await modelClient.GetModelsAsync(ct);
            var metadata = OpenAIModelMetadata.Read(result.GetRawResponse().Content);
            IReadOnlyList<AIRemoteModelInfo> remoteModels = result.Value
                .Select(m => new AIRemoteModelInfo
                {
                    ModelId = m.Id,
                    Configuration = metadata.GetValueOrDefault(m.Id),
                    Metadata = new Dictionary<string, string>
                    {
                        ["OwnedBy"] = m.OwnedBy ?? "",
                        ["CreatedAt"] = m.CreatedAt.ToString("O")
                    }
                })
                .OrderBy(m => m.ModelId)
                .ToList();
            return remoteModels;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to fetch OpenAI models: " + ex.Message, ex);
        }
    }

    /// <summary>
    /// Builds OpenAI-specific provider metadata.
    /// </summary>
    internal static IReadOnlyDictionary<string, string> BuildMetadata(OpenAIProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new Dictionary<string, string>
        {
            [OpenAIProviderMetadataKeys.ApiMode] = options.ApiMode.ToString(),
            [OpenAIProviderMetadataKeys.ProtocolProfile] = ResolveProtocolProfile(options).ToString(),
            [OpenAIProviderMetadataKeys.ResponsesHistoryMode] = options.ResponsesHistoryMode.ToString()
        };
    }

    private IChatClient CreateChatClient(string resolvedModel)
    {
        var chatClient = _options.ApiMode switch
        {
#pragma warning disable OPENAI001
            OpenAIProviderApiMode.Responses => _client.GetResponsesClient().AsIChatClient(resolvedModel),
#pragma warning restore OPENAI001
            OpenAIProviderApiMode.Chat => _client.GetChatClient(resolvedModel).AsIChatClient(),
            _ => throw new InvalidOperationException($"Unsupported OpenAI API mode '{_options.ApiMode}'.")
        };

        IChatClient configuredClient = new OpenAIRequestOptionsChatClient(
            chatClient,
            _options.ApiMode,
            _options.ResponsesHistoryMode,
            _options.PromptCacheKey,
            _options.PromptCacheRetention,
            ResolveProtocolProfile(_options));
        // Compatible endpoints may put a leading <think> block in ordinary content. Interpret it
        // only for explicitly reasoning-capable Chat models; Responses and unknown models stay literal.
        return _options.ApiMode == OpenAIProviderApiMode.Chat
               && _models.OfType<LLMModelInfo>().Any(model => model.ModelName.Equals(resolvedModel, StringComparison.OrdinalIgnoreCase)
                   && model.SupportsReasoning == true)
            ? new TaggedReasoningChatClient(configuredClient)
            : configuredClient;
    }

    internal static OpenAIProtocolProfile ResolveProtocolProfile(OpenAIProviderOptions options) =>
        options.ProtocolProfile != OpenAIProtocolProfile.Auto ? options.ProtocolProfile
        : Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var endpoint)
          && string.Equals(endpoint.Host, "api.deepseek.com", StringComparison.OrdinalIgnoreCase)
            ? OpenAIProtocolProfile.DeepSeek
            : OpenAIProtocolProfile.Standard;

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
        foreach (var generator in _embeddingGenerators.Values)
        {
            if (generator.IsValueCreated) generator.Value.Dispose();
        }

        _embeddingGenerators.Clear();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
