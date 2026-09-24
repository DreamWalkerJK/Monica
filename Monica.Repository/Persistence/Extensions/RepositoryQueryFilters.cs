using Microsoft.EntityFrameworkCore;

namespace Monica.Repository.Persistence.Extensions;

/// <summary>Named repository filters, independently removable without disabling tenant or security filters.</summary>
public static class RepositoryQueryFilters
{
    /// <summary>The soft-delete model filter name.</summary>
    public const string SoftDelete = "Monica.SoftDelete";

    /// <summary>Includes soft-deleted rows while retaining all unrelated query filters.</summary>
    public static IQueryable<TEntity> IncludeSoftDeleted<TEntity>(this IQueryable<TEntity> query) where TEntity : class
        => query.IgnoreQueryFilters([SoftDelete]);
}
