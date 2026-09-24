# JobScheduler and Seeder usage

## JobScheduler

`AddJobScheduler()` discovers concrete `RecurringJob` and `TriggeredJob<TArgs>` types in the host's type-discovery scope. Composition requires both an `IJobSchedulerStore` and `UseSchedulerScope(scopeKey)`. The scope key names one logical scheduler installation and must be shared by its replicas; use different keys for unrelated environments. The host's project name identifies the owner of discovered jobs. Worker identity fences leases. Definitions, cursors, and gates are keyed by `(SchedulerScopeKey, OwnerKey, JobKey)`, and the current protocol has no publisher retirement handshake: changing one owner's job set or contracts requires a drain or blue-green rollout so old and new publishers overlap safely. Register one store:

```csharp
builder.AddMonica(monica =>
{
    monica.AddJobScheduler()
        .UseInMemoryStore()
        .UseSchedulerScope("orders-dev");
});
```

`UseInMemoryStore()` loses state on restart and cannot coordinate multiple hosts. For a local durable example, use `UseEfCoreStore((_, db) => db.UseSqlite(connectionString))` from `Monica.JobScheduler.EfCore`, then apply migrations before running the host. Production multi-replica deployments should use a shared PostgreSQL-compatible database with serializable transactions. The database owns the definition, policy, queue, lease, and history consistency boundary; all replicas for one scope must point to it. A custom `UseStore<TStore>()` implementation must satisfy the full `IJobSchedulerStore` contract. `examples/JobSchedulerMinimal/Program.cs` is a runnable in-memory composition example.

```csharp
[JobConfig(CronSchedule = "0 */5 * * * *", MaxConcurrency = 1)]
public sealed class RefreshOrdersJob(ILogger<RefreshOrdersJob> logger) : RecurringJob(logger)
{
    public override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Refreshing orders");
        return Task.CompletedTask;
    }
}
```

The six-field cron has second precision and uses `ModuleJobSchedulerOption.CronTimeZone` (local time zone by default); occurrences are persisted in UTC. Job instances are transient and resolved in a fresh DI scope per execution. Honor cancellation for timeout, operator cancellation, and shutdown. `[JobConfig]` can set name, cron, concurrency, retry count, and execution timeout. Triggered jobs derive from `TriggeredJob<TArgs>` with JSON-serializable args. Inject `ITriggeredJobManager` and call `EnqueueAsync(args, delay, cancellationToken)`; it returns an instance ID that `CancelExecutionAsync` accepts. A failed attempt is durably requeued up to `RetryCount`; a running cancellation is cooperative. At capacity, triggered work waits durably, while a scheduled recurring occurrence is recorded as skipped to avoid an unbounded backlog. If a job is missing, inspect type-discovery scope, the job's definition validation, store selection, scope key, and host owner. If it is queued but idle, inspect worker health, leases, and cancellation state.

`monica.AddJobSchedulerUI()` adds the optional operator UI. `JobSchedulerFacade` serves operational queries and commands; keep authorization around an exposed UI or API. Scheduler state and its health check are registered by the module.

## Seeder

`monica.AddSeeder()` discovers concrete `ISeeder` implementations, builds a dependency graph, registers seeders as transient, and starts a background scheduler **after** the Generic Host publishes `ApplicationStarted`. Derive from `SeederBase` for a host logger and override `SeedingAsync`:

```csharp
[SeederPolicy(MaxAttempts = 3)]
public sealed class DefaultOrdersSeeder(ILogger<DefaultOrdersSeeder> logger) : SeederBase(logger)
{
    public override Task SeedingAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding default orders");
        return Task.CompletedTask;
    }
}
```

Replace the logging body with an idempotent seed operation. Add `[SeederDependsOn<PrerequisiteSeeder>]` when this seeder needs another discovered seeder to succeed first. The graph validates missing dependencies and cycles before execution. A seeder can override inherited `ExecutionMode`, `Criticality`, `FailureBehavior`, and `MaxAttempts` through `[SeederPolicy]`. Defaults are concurrent execution, required criticality, continue-and-record after exhausted attempts, and one attempt. Retry only idempotent work. An exclusive seeder runs alone, regardless of maximum concurrency. The `monica.seeder` readiness check remains unhealthy until required seeders succeed. `SeederFacade` exposes status and attempts; `monica.AddSeederUI()` adds the optional operational view. For a stuck startup, distinguish host startup from readiness and inspect blocked dependencies and each seeder's final attempt.

## Source and checks

Scheduler composition and options: `Monica.JobScheduler/Modules/ModuleJobScheduler.cs`, EF store registration: `Monica.JobScheduler.EfCore/Modules/ModuleJobSchedulerEfCore.cs`, execution policies: `Monica.JobScheduler/Annotations/JobConfigAttribute.cs`. Seeder graph and defaults: `Monica.Framework/Modules/ModuleSeeder.cs`, `Monica.Framework/Seeder/Models/Internal/SeederGraph.cs`, and `Monica.Framework/Seeder/Annotations/SeederPolicyAttribute.cs`. Tests: `tests/Test.Monica.JobScheduler/Modules/ModuleJobSchedulerCompositionTests.cs`, `tests/Test.Monica.JobScheduler/Services/JobExecutorExecutionPipelineTests.cs`, `tests/Test.Monica.Framework/Seeder/ModuleSeederTests.cs`. Run the touched non-UI projects and the standard solution build/non-UI gate for implementation changes; do not run UI tests unless requested.
