using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Monica.DependencyInjection.Abstractions;
using Monica.Modules;
using Monica.Repository.Entity.Abstractions;
using Monica.Repository.Entity.Extensions;
using Monica.Repository.Persistence.Abstractions;
using Monica.Repository.Persistence.Extensions;
using Monica.Repository.Persistence.Services.Support;
using Monica.Repository.UnitOfWork.Abstractions;
using Monica.Repository.UnitOfWork.Models;
using Monica.Tool.Extensions;

namespace Monica.Repository.Persistence.Services;

/// <summary>
/// Base DbContext for Monica repositories. Persistence concepts — audit stamping, soft-delete rewriting,
/// concurrency stamps, and entity change events — are intrinsic to <see cref="SaveChangesAsync(bool, CancellationToken)"/>
/// and apply on every save path, with or without an ambient unit of work.
/// </summary>
public abstract class RepositoryDbContext<TDbContext>(DbContextOptions<TDbContext> options, ICachedServiceProvider serviceProvider)
    : DbContext(options), IUnitOfWorkAwareDbContext
    where TDbContext : DbContext
{
    private IServiceScope? _factoryScope;

    public ICachedServiceProvider CachedServiceProvider { get; } = serviceProvider;

    protected IAuditPropertySetter AuditPropertySetter => CachedServiceProvider.GetRequiredService<IAuditPropertySetter>();

    protected ILogger<RepositoryDbContext<TDbContext>> Logger => CachedServiceProvider.GetService<ILogger<RepositoryDbContext<TDbContext>>>() ?? NullLogger<RepositoryDbContext<TDbContext>>.Instance;

    protected ModuleRepositoryOption Options => CachedServiceProvider.GetRequiredService<IOptions<ModuleRepositoryOption>>().Value;

    /// <summary>
    /// Gets whether <see cref="Initialize"/> has already been applied to this context by a unit of work.
    /// </summary>
    public bool HasInit { get; protected set; }

    /// <summary>
    /// Transfers ownership of the factory-created dependency injection scope to this context.
    /// </summary>
    /// <remarks>
    /// Contexts resolved normally from an application scope never use this path. Factory-created contexts dispose
    /// the attached scope together with the context so scoped dependencies cannot escape their operation boundary.
    /// </remarks>
    internal void OwnFactoryScope(IServiceScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        if (Interlocked.CompareExchange(ref _factoryScope, scope, null) is not null)
        {
            throw new InvalidOperationException("A repository DbContext can own only one factory scope.");
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        var scope = Interlocked.Exchange(ref _factoryScope, null);
        try
        {
            base.Dispose();
        }
        finally
        {
            scope?.Dispose();
        }
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        var scope = Interlocked.Exchange(ref _factoryScope, null);
        try
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            if (scope is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                scope?.Dispose();
            }
        }
    }


    protected readonly DbContextOptions DbContextOptions = options;
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var enableSensitiveDataLogging = Options.EnableSensitiveDataLogging
            ?? CachedServiceProvider.GetService<IHostEnvironment>()?.IsDevelopment()
            ?? false;

        if (enableSensitiveDataLogging)
        {
            // EF Core must receive this setting during OnConfiguring for parameter values to be included in diagnostics.
            optionsBuilder.EnableSensitiveDataLogging();
        }
    }

    #region 待优化

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HavePrecision(0);
        configurationBuilder.Properties<DateTime>().HaveColumnType("timestamp");
        configurationBuilder.Properties<TimeOnly>().HavePrecision(0);
        configurationBuilder.Properties<TimeSpan>().HavePrecision(0);
        base.ConfigureConventions(configurationBuilder);
    }

    /// <summary>
    /// Extend DbContext default field settings
    /// </summary>
    /// <param name="builder"></param>
    protected virtual void OnModelCreatingExtend(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        //set all string property default value to ""
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                //Set up automatic generation of SnowflakeIdGenerator ID
                if (property.Name.Equals("Id") && property.ValueGenerated != ValueGenerated.Never &&
                    property.ClrType == typeof(long))
                {
                    property.SetValueGeneratorFactory((_, _) => new SnowflakeLongValueGenerator());
                    builder.Entity(entityType.ClrType).Property(property.Name).ValueGeneratedNever();
                }

                if (property.ClrType == typeof(string) && !IsKeyProperty(entityType, property))
                {
                    property.SetDefaultValue("");
                }

                //pgsql will automatically fill in spaces for char type
                if (property.GetColumnType() == "char")
                {
                    property.SetValueConverter(new CharTrimEndValueConverter());
                }

                //Enum conversion database storage as string
                if (property.ClrType.BaseType == typeof(Enum))
                {
                    var columnType = property.GetColumnType();
                    if (columnType == "varchar")
                    {
                        var type = typeof(EnumToStringConverter<>).MakeGenericType(property.ClrType);
                        var converter = Activator.CreateInstance(type, new ConverterMappingHints()) as ValueConverter;
                        property.SetValueConverter(converter);
                    }
                }
            }
        }
    }

    private static bool IsKeyProperty(IMutableEntityType entityType, IMutableProperty property)
    {
        return entityType.GetKeys().Any(key => key.Properties.Contains(property));
    }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            _configureBasePropertiesMethodInfo
                .MakeGenericMethod(entityType.ClrType)
                .Invoke(this, [builder, entityType]);

            _configureValueConverterMethodInfo
                .MakeGenericMethod(entityType.ClrType)
                .Invoke(this, [builder, entityType]);
        }

        builder.ApplyEntitySelfConfigurations(Options, Logger);

        builder.ApplyEntitySeparateConfigurations(Options, Logger);

        OnModelCreatingExtend(builder);
    }


    #endregion


    /// <summary>
    /// Saves all tracked changes after applying the repository persistence concepts
    /// (audit stamping, soft-delete rewriting, concurrency stamps, entity events) to changed entries.
    /// </summary>
    /// <remarks>
    /// Concepts are intrinsic to the save pipeline: every save path applies them, whether it runs inside a
    /// unit of work, in a background worker, or in a test that resolves the context directly. Entity events
    /// staged during the save defer until the unit of work commits when one is active, and publish right
    /// after the save commits when none is. Use <see cref="SaveChangesOnDbContextAsync"/> to bypass the
    /// concepts deliberately.
    /// </remarks>
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        // Outside a unit of work the successful save itself is the commit boundary, so its entity events
        // publish immediately after it instead of waiting for a unit-of-work completion.
        var publisher = Publisher;
        var detachedEvents = publisher?.TryBeginDetachedEventBuffer();
        try
        {
            ApplyConceptsBeforeSave();

            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            if (detachedEvents != null)
            {
                await publisher!.PublishDetachedEventsAsync(detachedEvents);
            }

            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw WrapConcurrencyException(ex);
        }
        finally
        {
            detachedEvents?.Dispose();
            ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }

    /// <summary>
    /// Saves all tracked changes synchronously after applying the same persistence concepts as
    /// <see cref="SaveChangesAsync(bool, CancellationToken)"/>, except entity events: without an ambient
    /// unit of work a synchronous save has no async-safe point to publish them from, and such a save on an
    /// event-enabled entity fails instead of dropping the events silently.
    /// </summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        try
        {
            ApplyConceptsBeforeSave();

            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw WrapConcurrencyException(ex);
        }
        finally
        {
            ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }

    private Exception WrapConcurrencyException(DbUpdateConcurrencyException ex)
    {
        if (ex.Entries.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine(ex.Entries.Count > 1
                ? "There are some entries which are not saved due to concurrency exception:"
                : "There is an entry which is not saved due to concurrency exception:");
            foreach (var entry in ex.Entries)
            {
                sb.AppendLine(entry.ToString());
            }

            Logger.LogWarning(sb.ToString());
        }

        return new Exception(ex.Message, ex);
    }

    /// <summary>
    /// Calls the EF Core save pipeline directly without applying the repository persistence concepts.
    /// Use this only when raw EF semantics are explicitly wanted.
    /// </summary>
    public virtual Task<int> SaveChangesOnDbContextAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Applies unit-of-work operational settings (currently the command timeout) to this DbContext.
    /// </summary>
    /// <remarks>
    /// Persistence concepts are intrinsic to <see cref="SaveChangesAsync(bool, CancellationToken)"/> and do not
    /// depend on this call; it only carries unit-of-work scope settings for contexts that participate in one.
    /// </remarks>
    public virtual void Initialize(UnitOfWorkScopeOptions options)
    {
        if (HasInit) throw new InvalidOperationException("The same repository DbContext was initialized for unit-of-work participation twice; the calling structure is invalid.");
        HasInit = true;

        if (options.Timeout.HasValue &&
            Database.IsRelational() &&
            !Database.GetCommandTimeout().HasValue)
        {
            Database.SetCommandTimeout(TimeSpan.FromMilliseconds(options.Timeout.Value));
        }
    }

    /// <summary>
    /// Gets the entity change event publisher resolved from the application service provider, if registered.
    /// </summary>
    protected IAsyncLocalEventPublisher? Publisher => CachedServiceProvider.GetService<IAsyncLocalEventPublisher>();

    #region 保存时应用 concepts：审计等自动属性、软删重写、实体事件

    /// <summary>
    /// Applies the repository persistence concepts to every changed entry right before the save is handed to EF Core.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Runs inside every <see cref="SaveChangesAsync(bool, CancellationToken)"/> and <see cref="SaveChanges(bool)"/>
    /// call, so write semantics (soft delete, audit stamping, concurrency stamps, entity events) hold on every
    /// runtime shape: request pipelines with an ambient unit of work, background workers, and direct context usage in tests.
    /// </para>
    /// <para>
    /// Cascaded dependents must already sit in the change tracker with their final state when this pass runs, which is
    /// why the EF Core default cascade timing (<see cref="CascadeTiming.Immediate"/>) must be kept: with
    /// <see cref="CascadeTiming.OnSaveChanges"/> dependents would only transition during the EF save and this pass would miss them.
    /// </para>
    /// <para>
    /// Compared to applying concepts from <c>ChangeTracker</c> events, an entry that was tracked as Added and then
    /// explicitly transitioned to Modified before saving is handled once, by its current state (Modified), instead of twice.
    /// </para>
    /// </remarks>
    protected virtual void ApplyConceptsBeforeSave()
    {
        foreach (var entry in ChangeTracker.Entries().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                case EntityState.Modified:
                case EntityState.Deleted:
                    PublishEventsForTrackedEntity(entry);
                    break;
            }
        }
    }

    /// <summary>
    /// Applies the concepts and buffers the entity change events for one changed entry.
    /// </summary>
    /// <param name="entry">The entry whose state is Added, Modified, or Deleted.</param>
    protected virtual void PublishEventsForTrackedEntity(EntityEntry entry)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                ApplyConceptsForAddedEntity(entry);
                Publisher?.AddEntityCreatedEvent(entry.Entity);
                break;

            case EntityState.Modified:
                ApplyConceptsForModifiedEntity(entry);

                //Big Pitfall: In ABP 8.0.2, OnAdd is not considered for new addition judgment, resulting in no triggering of related events.
                if (entry.Properties.Any(x => x is { IsModified: true, Metadata.ValueGenerated: ValueGenerated.Never or ValueGenerated.OnAdd }))
                {
                    if (entry.Entity is IHasSoftDelete && entry.Entity.As<IHasSoftDelete>().IsDeleted)
                    {
                        Publisher?.AddEntityDeletedEvent(entry.Entity);

                    }
                    else
                    {
                        Publisher?.AddEntityUpdatedEvent(entry.Entity);
                    }
                }

                UpdateConcurrencyStamp(entry);
                break;

            case EntityState.Deleted:
                ApplyConceptsForDeletedEntity(entry);
                Publisher?.AddEntityDeletedEvent(entry.Entity);
                UpdateConcurrencyStamp(entry);
                break;
        }
    }

    protected virtual void UpdateConcurrencyStamp(EntityEntry entry)
    {
        if (entry.Entity is not IHasConcurrencyStamp entity)
        {
            return;
        }

        Entry(entity).Property(x => x.ConcurrencyStamp).OriginalValue = entity.ConcurrencyStamp;
        entity.ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }

    protected virtual void SetConcurrencyStampIfNull(EntityEntry entry)
    {
        if (entry.Entity is not IHasConcurrencyStamp entity)
        {
            return;
        }

        if (entity.ConcurrencyStamp.IsNotNullOrEmpty())
        {
            return;
        }

        entity.ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }

    protected virtual void ApplyConceptsForAddedEntity(EntityEntry entry)
    {
        SetConcurrencyStampIfNull(entry);
        SetCreationAuditProperties(entry);
    }

    protected virtual void ApplyConceptsForModifiedEntity(EntityEntry entry)
    {
        if (entry.State == EntityState.Modified && entry.Properties.Any(x => x is { IsModified: true, Metadata.ValueGenerated: ValueGenerated.Never or ValueGenerated.OnAdd }))
        {
            IncrementEntityVersionProperty(entry);
            SetModificationAuditProperties(entry);

            if (entry.Entity is IHasSoftDelete && entry.Entity.As<IHasSoftDelete>().IsDeleted)
            {
                SetDeletionAuditProperties(entry);
            }
        }
    }

    protected virtual void ApplyConceptsForDeletedEntity(EntityEntry entry)
    {
        if (entry.Entity is not IHasSoftDelete entity)
        {
            return;
        }

        entry.State = EntityState.Unchanged;
        entity.IsDeleted = true;

        SetDeletionAuditProperties(entry);
    }

    protected virtual void SetCreationAuditProperties(EntityEntry entry)
    {
        AuditPropertySetter?.SetCreationProperties(entry.Entity);
    }

    protected virtual void SetModificationAuditProperties(EntityEntry entry)
    {
        AuditPropertySetter?.SetModificationProperties(entry.Entity);
    }

    protected virtual void SetDeletionAuditProperties(EntityEntry entry)
    {
        AuditPropertySetter?.SetDeletionProperties(entry.Entity);
    }

    protected virtual void IncrementEntityVersionProperty(EntityEntry entry)
    {
        AuditPropertySetter?.IncrementEntityVersionProperty(entry.Entity);
    }
    #endregion

    #region 实体额外配置
    private static readonly MethodInfo _configureBasePropertiesMethodInfo
        = typeof(RepositoryDbContext<TDbContext>)
            .GetMethod(
                nameof(ConfigureBaseProperties),
                BindingFlags.Instance | BindingFlags.NonPublic
            )!;

    private static readonly MethodInfo _configureValueConverterMethodInfo
        = typeof(RepositoryDbContext<TDbContext>)
            .GetMethod(
                nameof(ConfigureValueConverter),
                BindingFlags.Instance | BindingFlags.NonPublic
            )!;

    protected virtual void ConfigureBaseProperties<TEntity>(ModelBuilder modelBuilder, IMutableEntityType mutableEntityType)
       where TEntity : class
    {
        if (mutableEntityType.IsOwned())
        {
            return;
        }

        if (!typeof(IEntity).IsAssignableFrom(typeof(TEntity)))
        {
            return;
        }

        modelBuilder.Entity<TEntity>().ConfigureByConvention();

        ConfigureGlobalFilters<TEntity>(modelBuilder, mutableEntityType);
    }

    /// <summary>
    /// Configures global filters for given entity.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="modelBuilder"></param>
    /// <param name="mutableEntityType"></param>
    protected virtual void ConfigureGlobalFilters<TEntity>(ModelBuilder modelBuilder, IMutableEntityType mutableEntityType)
        where TEntity : class
    {
        if (mutableEntityType.BaseType == null && ShouldFilterEntity<TEntity>(mutableEntityType))
        {
            var filterExpression = CreateFilterExpression<TEntity>(modelBuilder);
            if (filterExpression != null)
            {
                modelBuilder.Entity<TEntity>().HasMoQueryFilter(filterExpression);
            }
        }
    }

    protected virtual void ConfigureValueConverter<TEntity>(ModelBuilder modelBuilder, IMutableEntityType mutableEntityType)
        where TEntity : class
    {
        //TODO Automatic conversion between UTC DateTime type and local time
    }

    /// <summary>
    /// Checks if given entity should be filtered.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entityType"></param>
    /// <returns></returns>
    protected virtual bool ShouldFilterEntity<TEntity>(IMutableEntityType entityType) where TEntity : class
    {
        if (typeof(IHasSoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a filter expression for given entity.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="modelBuilder"></param>
    /// <returns></returns>
    protected virtual Expression<Func<TEntity, bool>>? CreateFilterExpression<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class
    {
        Expression<Func<TEntity, bool>>? expression = null;

        if (typeof(IHasSoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            var softDeleteColumnName = modelBuilder.Entity<TEntity>().Metadata.FindProperty(nameof(IHasSoftDelete.IsDeleted))?.GetColumnName() ?? nameof(IHasSoftDelete.IsDeleted);

            if (Options.UseDbFunction)
            {
                expression = e => EfCoreDataFilterDbFunctions.SoftDeleteFilter(((IHasSoftDelete)e).IsDeleted, true);
                modelBuilder.ConfigureSoftDeleteDbFunction(EfCoreDataFilterDbFunctions.SoftDeleteFilterMethodInfo, true);
            }
            else
            {
                expression = e => !EF.Property<bool>(e, softDeleteColumnName);
            }
        }

        return expression;
    }


    #endregion


}
