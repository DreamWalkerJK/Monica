using Monica.Repository.UnitOfWork.Models;

namespace Monica.Repository.Persistence.Abstractions;

/// <summary>
/// Marks a repository DbContext that receives unit-of-work operational settings when it participates in a unit of work.
/// </summary>
/// <remarks>
/// Persistence concepts (soft delete, audit stamping, entity events) are intrinsic to the DbContext save pipeline
/// and do not depend on this interface; <see cref="Initialize"/> only applies unit-of-work scope settings
/// such as the command timeout.
/// </remarks>
public interface IUnitOfWorkAwareDbContext
{
    /// <summary>
    /// Applies the active unit-of-work scope options to the DbContext.
    /// </summary>
    /// <param name="options">The options used by the active unit-of-work scope.</param>
    void Initialize(UnitOfWorkScopeOptions options);
}
