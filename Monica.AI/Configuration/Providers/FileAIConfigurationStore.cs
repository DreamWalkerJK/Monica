using Monica.AI.Configuration.Abstractions;
using Monica.AI.Configuration.Models;
using Monica.AI.Storage.Providers;

namespace Monica.AI.Configuration.Providers;

/// <summary>Stores host settings atomically under the shared AI storage root.</summary>
internal sealed class FileAIConfigurationStore(AIFileStore files) : IAIConfigurationStore
{
    private const string CONFIGURATION_PATH = "configuration/providers.json";

    public AIConfigurationDocument Read()
    {
        return files.Read<AIConfigurationDocument>(CONFIGURATION_PATH) ?? new AIConfigurationDocument();
    }

    public Task<AIConfigurationDocument> WriteAsync(
        AIConfigurationDocument document,
        long expectedRevision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        return files.WithLockAsync(CONFIGURATION_PATH, async ct =>
        {
            var current = await files.ReadAsync<AIConfigurationDocument>(CONFIGURATION_PATH, ct).ConfigureAwait(false)
                ?? new AIConfigurationDocument();
            if (current.Revision != expectedRevision)
            {
                throw new AIConfigurationConflictException(expectedRevision, current.Revision);
            }

            var committed = document with { Revision = checked(current.Revision + 1) };
            await files.WriteAsync(CONFIGURATION_PATH, committed, ct).ConfigureAwait(false);
            return committed;
        }, cancellationToken);
    }
}
