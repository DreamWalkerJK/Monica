using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Monica.Repository.Persistence.Services.Support;

internal interface IRepositoryModelFeatures
{
    bool HasOutbox { get; }
    bool HasInbox { get; }
    object ModelCacheKey { get; }
}

internal sealed class RepositoryModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
        => context is IRepositoryModelFeatures features
            ? (features.ModelCacheKey, features.HasOutbox, features.HasInbox, designTime)
            : (context.GetType(), false, designTime);
}
