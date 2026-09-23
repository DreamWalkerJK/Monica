using Microsoft.EntityFrameworkCore;
using Monica.Repository.Entity.Abstractions;

namespace Monica.Repository.Persistence.Abstractions;

/// <summary>
/// Infrastructure access for CRUD adapters and query implementations. Do not expose EF queries across domain
/// boundaries or enumerate them outside the owning scope. Set-based EF operations bypass aggregate policies,
/// audit stamps, concurrency checks and notifications; technical callers must implement those semantics explicitly.
/// </summary>
public interface IEfEntityStore<TEntity> : IRepository<TEntity> where TEntity : class, IEntity
{
    /// <summary>The same scoped context used by the operation transaction and direct injection.</summary>
    DbContext Context { get; }
    /// <summary>Native no-tracking query for projections. Use AsTracking explicitly for infrastructure mutation queries.</summary>
    IQueryable<TEntity> Query { get; }
    /// <summary>Whether projected queries require provider-specific sharding ordering behavior.</summary>
    bool IsShardingTable => false;
}

/// <summary>Infrastructure CRUD access with the narrow tracked aggregate load contract.</summary>
public interface IEfEntityStore<TEntity, in TKey> : IEfEntityStore<TEntity>, IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>;
