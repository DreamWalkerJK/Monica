using Microsoft.EntityFrameworkCore;
using Monica.Repository.Entity.Abstractions;
using Monica.Repository.Persistence.Abstractions;

namespace Monica.Repository.Persistence.Extensions;

/// <summary>Infrastructure conveniences that retain tracked deletion policies.</summary>
public static class RepositoryKeyExtensions
{
    /// <summary>Loads and stages matching rows for policy-aware removal; does not execute a physical bulk delete.</summary>
    public static async Task RemoveByIdsAsync<TEntity, TKey>(
        this IEfEntityStore<TEntity, TKey> store, IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
        where TEntity : class, IEntity<TKey>
    {
        var keys = ids.ToArray();
        if (keys.Length == 0) return;
        var entities = await store.Query.AsTracking().Where(x => keys.Contains(x.Id)).ToListAsync(cancellationToken);
        foreach (var entity in entities) store.Remove(entity);
    }
}
