using Microsoft.EntityFrameworkCore;
using Monica.Repository.Entity.Abstractions;
using Monica.Repository.Persistence.Abstractions;
using Monica.Repository.Persistence.Exceptions;

namespace Monica.Repository.Persistence.Services;

/// <summary>EF aggregate staging and infrastructure access backed by one directly injected scoped context.</summary>
public class EfRepository<TDbContext, TEntity>(TDbContext context) : IEfEntityStore<TEntity>
    where TDbContext : RepositoryDbContext<TDbContext>
    where TEntity : class, IEntity
{
    /// <summary>The strongly typed context owned by the operation scope.</summary>
    protected TDbContext DbContext { get; } = context;
    /// <inheritdoc />
    public DbContext Context => DbContext;
    /// <inheritdoc />
    public virtual IQueryable<TEntity> Query => DbContext.Set<TEntity>().AsNoTracking();
    /// <inheritdoc />
    public virtual bool IsShardingTable => false;
    /// <inheritdoc />
    public virtual void Add(TEntity entity) => DbContext.Set<TEntity>().Add(entity);
    /// <inheritdoc />
    public virtual void Remove(TEntity entity) => DbContext.Set<TEntity>().Remove(entity);
}

/// <summary>Tracked single-key aggregate loading without detached update/attach semantics.</summary>
public class EfRepository<TDbContext, TEntity, TKey>(TDbContext context)
    : EfRepository<TDbContext, TEntity>(context), IEfEntityStore<TEntity, TKey>
    where TDbContext : RepositoryDbContext<TDbContext>
    where TEntity : class, IEntity<TKey>
{
    /// <inheritdoc />
    public virtual Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default)
        => DbContext.Set<TEntity>().AsTracking().FirstOrDefaultAsync(entity => entity.Id!.Equals(id), cancellationToken);

    /// <inheritdoc />
    public virtual async Task<TEntity> GetAsync(TKey id, CancellationToken cancellationToken = default)
        => await FindAsync(id, cancellationToken) ?? throw new EntityNotFoundException(typeof(TEntity), id);
}
