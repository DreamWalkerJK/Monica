# Repository and DbContext Template

Use $DomainNamespace$ for the domain project namespace and $RepositoryNamespace$ for its infrastructure side. Keep interfaces in domain Interfaces and implementations/mapping in domain Repository; no new shared project or Persistence folder is required.

## Rules

- Use narrow business aggregate contracts. The small IRepository<TEntity,TKey> staging/loading interface is optional; do not mirror every LINQ operation.
- Load tracked aggregates for mutation. The operation owns commit; no generic Update or per-repository save.
- Use native EF LINQ inside repository/query implementations. Return materialized projections from query interfaces.
- IEfEntityStore is infrastructure access for CRUD adapters, not a domain contract.
- Inject the scoped context directly into EfRepository. Context, repository and handler must share one operation scope.
- Use IPersistencePolicy for explicit save-time storage stamping; do not override SaveChanges or reintroduce tracking-time initialization.
- Queries marked [ReadOnlyOperation] avoid automatic transactions. Independent operations and retries get fresh scopes.

## Aggregate Repository

```csharp
public interface IRepositoryOrder : IRepository<Order, long>
{
    Task<Order?> FindForApprovalAsync(string number, CancellationToken cancellationToken);
}

public sealed class RepositoryOrder(OrderingDbContext context)
    : EfRepository<OrderingDbContext, Order, long>(context), IRepositoryOrder
{
    public Task<Order?> FindForApprovalAsync(string number, CancellationToken cancellationToken)
        => DbContext.Orders.AsTracking().Include(order => order.Lines)
            .SingleOrDefaultAsync(order => order.Number == number, cancellationToken);
}
```

The handler loads the aggregate and calls order.Approve(); it does not attach a detached copy or call Update. Add the normal ProjectUnitMetadata and requirement annotations used by the surrounding project.

## Read Projection

```csharp
public interface IOrderQueries
{
    Task<OrderSummary?> FindSummaryAsync(long id, CancellationToken cancellationToken);
}

public sealed class OrderQueries(OrderingDbContext context) : IOrderQueries
{
    public Task<OrderSummary?> FindSummaryAsync(long id, CancellationToken cancellationToken)
        => context.Orders.AsNoTracking().Where(order => order.Id == id)
            .Select(order => new OrderSummary(order.Id, order.Number))
            .SingleOrDefaultAsync(cancellationToken);
}
```

Register query implementations explicitly or through the project's established scoped dependency convention.

## DbContext and Composition

```csharp
public sealed class OrderingDbContext(
    DbContextOptions<OrderingDbContext> options,
    ICachedServiceProvider serviceProvider)
    : RepositoryDbContext<OrderingDbContext>(options, serviceProvider)
{
    public DbSet<Order> Orders => Set<Order>();
}

builder.AddMonica(monica =>
{
    monica.AddRepository()
        .AddRepositoryDbContext<OrderingDbContext>((_, options) => options.UseSqlite(connectionString))
        .AddOutbox<OrderingDbContext>(outbox =>
            outbox.Register<OrderApprovedV1>("orders.approved.v1"));
});
```

Default registration enables operation transaction participation. Independently managed infrastructure contexts use DbContextProviderType.Default. Multiple write contexts require explicit participant selection.

Outbox registration adds schema to the application context; create the application's migration before deployment. IOutboxWriter stages registered DTOs, and OutboxDispatcher delivers committed OutboxDelivery<T> envelopes on the contract-name topic. Delivery is at least once: consumers deduplicate MessageId and enforce application stream/version rules.

Use IDomainEventQueue and scoped IDomainEventHandler for business effects that must run before commit in the same scope. Auto row notifications, cache invalidation and external integration belong in the outbox. Native bulk SQL and physical purge deliberately bypass save policies and need explicit application semantics.
