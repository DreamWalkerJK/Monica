using Microsoft.Extensions.AI;
using Monica.AI.Abstractions;

namespace Monica.AI.RAG.Services;

/// <summary>Owns a provider lease without disposing the shared underlying generator.</summary>
internal sealed class LeasedEmbeddingGenerator(
    IEmbeddingGenerator<string, Embedding<float>> inner,
    IAIProviderLease lease) : IEmbeddingGenerator<string, Embedding<float>>
{
    private IAIProviderLease? _lease = lease;

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_lease is null, this);
        return inner.GenerateAsync(values, options, cancellationToken);
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ObjectDisposedException.ThrowIf(_lease is null, this);
        return inner.GetService(serviceType, serviceKey);
    }

    public void Dispose() => Interlocked.Exchange(ref _lease, null)?.Dispose();
}
