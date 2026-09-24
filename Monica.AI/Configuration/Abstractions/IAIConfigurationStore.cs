using Monica.AI.Configuration.Models;

namespace Monica.AI.Configuration.Abstractions;

/// <summary>
/// Host-wide settings persistence. Replace this singleton for database-backed settings; conversation partitioning
/// does not apply. Implementations must return independent snapshots and atomically compare revisions on writes.
/// </summary>
public interface IAIConfigurationStore
{
    /// <summary>
    /// Reads the small startup configuration snapshot synchronously. An absent document returns revision zero.
    /// Implementations must never convert corruption or unavailable storage into an empty configuration.
    /// </summary>
    AIConfigurationDocument Read();

    /// <summary>
    /// Commits the supplied providers only when the stored revision matches <paramref name="expectedRevision"/>.
    /// Returns the committed snapshot at the next revision. Cancellation before commit leaves the old snapshot intact.
    /// </summary>
    /// <exception cref="AIConfigurationConflictException">Another write has advanced the revision.</exception>
    Task<AIConfigurationDocument> WriteAsync(
        AIConfigurationDocument document,
        long expectedRevision,
        CancellationToken cancellationToken = default);
}
