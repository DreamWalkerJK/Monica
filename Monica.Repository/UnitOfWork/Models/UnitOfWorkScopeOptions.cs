using System.Data;

namespace Monica.Repository.UnitOfWork.Models;

/// <summary>Settings for one local transaction. Read-only work should bypass the write behavior.</summary>
/// <param name="DbContextTypes">
/// Explicit participant types. Null selects the single UnitOfWork-registered context (or no context).
/// Multiple contexts require this explicit selection, the same DbConnection instance and relational provider.
/// Independent databases and shards cannot participate in one local transaction.
/// </param>
/// <param name="IsolationLevel">Null uses the database provider's default isolation.</param>
/// <param name="CommandTimeout">Optional command timeout applied to every participant.</param>
public sealed record UnitOfWorkScopeOptions(
    IReadOnlyList<Type>? DbContextTypes = null,
    IsolationLevel? IsolationLevel = null,
    TimeSpan? CommandTimeout = null);
