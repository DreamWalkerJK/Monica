# Repository and UnitOfWork usage

`ModuleRepository` supplies EF Core repository contexts and `IRepository<TEntity>`; `ModuleUnitOfWork` owns one local write operation. Register a context inside `AddMonica`:

```csharp
builder.AddMonica(monica =>
{
    monica.AddRepository()
        .AddRepositoryDbContext<OrdersDbContext>((_, db) => db.UseSqlite(connectionString));
});
```

`OrdersDbContext` derives from `RepositoryDbContext<OrdersDbContext>`. `AddRepositoryDbContext` defaults to `DbContextProviderType.UnitOfWork` and requires `ModuleUnitOfWork` automatically. The registered context is scoped, while its `IDbContextFactory<T>` creates independently owned scopes for long-lived workers; dispose factory-created contexts. Use `DbContextProviderType.Default` for a store that owns its saves or is selected explicitly in a separate operation. The core `IRepository<TEntity>` stages `Add`/`Remove`; `IRepository<TEntity,TKey>` additionally loads a tracked aggregate by key. Domain-specific repositories add purposeful tracked queries. The repository interface deliberately has no `Save` method.

## Write outcome

`IUnitOfWorkManager.RunAsync` is scoped. It begins a relational transaction for the selected context, runs the delegate, drains same-scope domain events, flushes every participant, then commits. An exception, failed `Res`/`Res<T>`, or execution outcome marked for rollback rolls the transaction back. A nested call joins the active transaction; even a caught nested failure leaves the outer operation rollback-only. The manager becomes terminal after completion or failure, so retry with a new DI scope. Read-only operations should bypass the automatic write behavior.

For an explicit boundary, resolve the manager and repository from the **same scope**:

```csharp
await unitOfWork.RunAsync(() =>
{
    repository.Add(order);
    return Task.CompletedTask;
}, cancellationToken: cancellationToken);
```

The execution pipeline also applies `UnitOfWorkExecutionBehavior` to operations with `ExecutionTransactionMode.Automatic`; an application may use that boundary rather than calling `RunAsync` itself. If several UnitOfWork contexts are registered, select participants explicitly with `UnitOfWorkScopeOptions(DbContextTypes: [typeof(OrdersDbContext)])` or `[UnitOfWorkContext(...)]` on the execution entry. Multiple selected contexts must share the *same `DbConnection` instance* and relational provider. Independent databases cannot form this local transaction. `Current.FlushAsync()` and direct `SaveChangesAsync()` flush changes inside the active transaction; neither commits it. A failed save faults the context. `SaveChanges(false)` and concurrent/recursive saves are unsupported; retry in a fresh scope.

## Durable events

For `[Outbox]` events, enable `AddOutbox<OrdersDbContext>()` on the **primary transaction context**, generate its EF migration, and publish through the scoped EventBus gateway inside the write operation. Successful `PublishAsync` means the prepared message was staged. The worker sends it only after commit; sends may retry, so consumers still need idempotency. `AddOutbox` requires EventBus and HostedService. `RepositoryOutboxOptions` controls polling, lease, batch, retry delays, retention, and whether this host runs a worker. Entity event projections are optional: `AddEntityEventProjections<TContext>` stages projection events during save and also requires outbox on the primary context. Projection callbacks should be side-effect-free.

For an EventBus handler with durable deduplication, enable `AddInbox<OrdersDbContext>()`, migrate `MonicaInbox`, and mark the handler class or method `[Inbox("stable.consumer.name")]`. The receipt, business writes, and outgoing events share the primary context's transaction. Keep the consumer name stable across deployments. Missing message identity or missing active inbox transaction fails; duplicate delivery for the same consumer/source/message ID is suppressed. Concurrent contenders may surface a database conflict; let the transport redeliver into a fresh scope.

## Diagnose and verify

Check the context registration mode, participant selection, physical connection identity, whether the primary context has outbox/inbox enabled, migrations, and the returned result envelope before investigating transport delivery. Repository model and save policy live in `Monica.Repository/Persistence/Services/RepositoryDbContext.cs`; transaction semantics in `Monica.Repository/UnitOfWork/Services/UnitOfWorkManager.cs`; registrations in `Monica.Repository/Modules/ModuleRepository.cs` and `ModuleUnitOfWork.cs`. `tests/Test.Monica.Repository/UnitOfWork/UnitOfWorkRepositoryCommitTests.cs`, `InboxExecutionTests.cs`, `EntityEventPublishingTests.cs`, and `SharedTransactionTests.cs` exercise the important boundaries. To verify code changes, run the affected `Test.Monica.Repository` project, then the repository's required solution build and standard non-UI test gate.
