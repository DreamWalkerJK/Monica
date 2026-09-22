namespace Monica.Repository.UnitOfWork.Abstractions;

/// <summary>
/// The active operation's persistence session. The execution boundary owns commit and rollback;
/// the dependency-injection scope owns its contexts. Never share a session between concurrent operations.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Gets the operation identity.</summary>
    Guid Id { get; }

    /// <summary>Flushes staged changes without committing. Throws after failure or completion.</summary>
    Task<int> FlushAsync(CancellationToken cancellationToken = default);

    /// <summary>Prevents commit, including when a nested failure was caught by its caller.</summary>
    void MarkRollbackOnly();
}
