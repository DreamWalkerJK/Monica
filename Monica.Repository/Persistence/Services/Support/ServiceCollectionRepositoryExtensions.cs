using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Monica.Repository.Persistence.Abstractions;
using Monica.Repository.Persistence.Extensions;

namespace Monica.Repository.Persistence.Services.Support;


public static class ServiceCollectionRepositoryExtensions
{
    public static IServiceCollection AddRepositoryServices(
        this IServiceCollection services,
        Type entityType,
        Type repositoryImplementationType,
        bool replaceExisting = false)
    {
        var repositoryInterface = typeof(IRepository<>).MakeGenericType(entityType);
        if (repositoryInterface.IsAssignableFrom(repositoryImplementationType))
        {
            RegisterService(services, repositoryInterface, repositoryImplementationType, replaceExisting);
        }

        var storeInterface = typeof(IEfEntityStore<>).MakeGenericType(entityType);
        if (storeInterface.IsAssignableFrom(repositoryImplementationType))
            RegisterService(services, storeInterface, repositoryImplementationType, replaceExisting);

        var primaryKeyType = EntityHelper.FindPrimaryKeyType(entityType);
        if (primaryKeyType != null)
        {
            var repositoryInterfaceWithPk = typeof(IRepository<,>).MakeGenericType(entityType, primaryKeyType);
            if (repositoryInterfaceWithPk.IsAssignableFrom(repositoryImplementationType))
            {
                RegisterService(services, repositoryInterfaceWithPk, repositoryImplementationType, replaceExisting);
            }
        }

        if (primaryKeyType != null)
        {
            var keyedStoreInterface = typeof(IEfEntityStore<,>).MakeGenericType(entityType, primaryKeyType);
            if (keyedStoreInterface.IsAssignableFrom(repositoryImplementationType))
                RegisterService(services, keyedStoreInterface, repositoryImplementationType, replaceExisting);
        }

        return services;
    }

    private static void RegisterService(
        IServiceCollection services,
        Type serviceType,
        Type implementationType,
        bool replaceExisting)
    {
        var descriptor = ServiceDescriptor.Transient(serviceType, implementationType);

        if (replaceExisting)
        {
            services.Replace(descriptor);
        }
        else
        {
            services.TryAdd(descriptor);
        }
    }
}
