# Repository and DbContext Template

Use $DomainNamespace$ for the domain project namespace and $RepositoryNamespace$ for its infrastructure side. Keep interfaces in domain Interfaces and implementations/mapping in domain Repository; no new shared project or Persistence folder is required.

## Rules

- Use narrow business aggregate contracts. The small IRepository<TEntity,TKey> staging/loading interface is optional; do not mirror every LINQ operation.
- Load tracked aggregates for mutation. The operation owns commit; no generic Update or per-repository save.
- Use native EF LINQ inside repository/query implementations. Return materialized projections from query interfaces.
- IEfEntityStore is infrastructure access for CRUD adapters, not a domain contract.
- Inject the scoped context directly into EfRepository. Context, repository and handler must share one operation scope.
- Use the current `$monica-infra-persistence` contract for save policies, operation scopes, transactions, and read-only requests.

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
        .AddOutbox<OrderingDbContext>();
});
```

The example shows application-owned repository and context placement. For `AddRepositoryDbContext`, multi-context participant selection, save policies, outbox, and commit behavior, read `$monica-infra-persistence`. For domain events versus distributed delivery, read `$monica-infra-messaging`. Keep migrations with the application that owns the data.
