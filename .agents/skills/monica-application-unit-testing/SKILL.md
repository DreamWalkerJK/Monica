---
name: monica-application-unit-testing
description: Create, migrate, or review sociable tests for Monica-based business services. Use for Test.{ProductionProjectName} architecture, MonicaTestApplicationFactory host-owned scenarios, handler/domain-service/repository/module tests, request-shaped write execution with MonicaTestScope.InvokeAsync, seeding and write-path persistence semantics, pre-build seam replacement, database isolation, raw ProjectUnit fast paths, parallel test isolation, or migration from mock-heavy legacy fixtures.
---

# Monica Application Unit Testing

Test business behavior through a complete Monica host when composition matters. Each scenario owns its host; registrations are finalized before build, and scopes only provide normal scoped lifetimes.

## Repository Test Infrastructure First

Before writing any factory, search the repository's shared test layer (for example a `Platform.UnitTests` project or a base factory such as `PlatformTestApplicationFactory`). When one covers the project under test, inherit and extend it: its seams already encode repo-wide decisions such as the current user, ID generators, and test databases. Apply this skill's shapes only when no repo-level factory exists.

## Workflow

1. Inspect the production startup path, module registrations, discovery assemblies, DbContexts, external adapters, and the repository's shared test layer.
2. Create one runnable project named `Test.{ProductionProjectName}` for the exact production project stem.
3. Add a project-level factory derived from `MonicaTestApplicationFactory<TDiscoveryAnchor>`:
   - Override `ConfigureMonica(IMonicaBuilder)` with the production module graph required by the service.
   - Override `TypeDiscoveryAssemblies` when production ProjectUnits span more than the anchor assembly.
   - Override `ConfigureHost(WebApplicationBuilder)` only for test host configuration or environment inputs.
   - Override `ConfigureServices(IServiceCollection)` for stable test providers and boundary seams used by every scenario; call the base implementation first to retain Monica's standard seams.
4. In every sociable test, call `CreateAsync(...)` to build a complete host for that scenario.
5. Supply scenario-specific registrations through the optional `Action<ISeamReplacementBuilder>` callback to `CreateAsync`. The callback runs before host build.
6. Call `application.CreateScope(...)`, resolve the unit from the concrete `MonicaTestScope`, and pass its cancellation token to async operations.
7. Assert public behavior and observable side effects, then dispose the scope and application.
8. Run the target test project.

## Write Path and Persistence

Repository write methods (`InsertAsync`, `UpdateAsync`, `DeleteAsync`) only stage changes. Persistence concepts — soft-delete rewriting, audit stamping, concurrency stamps, entity events — are applied by the DbContext save pipeline on every save path, with or without a unit of work.

- Write scenarios: wrap the action in `await scope.InvokeAsync(async s => { ... })` (a generic overload returns a result). This mirrors the request pipeline: an ambient unit of work wraps the action, the scope's DbContexts are saved when the action succeeds, and the unit of work commits; nothing persists when the action throws. No manual context initialization and no manual `SaveChangesAsync`.
- Read-only scenarios: resolve the service from the scope and query directly; no wrapper is needed.
- A directly resolved service never runs the auto-controller execution pipeline. Any write that must commit as one business operation goes through `scope.InvokeAsync`.
- `scope.InvokeAsync` requires the UnitOfWork module in the composition (`monica.AddUnitOfWork()` or a `DbContextProviderType.UnitOfWork` registration); it fails fast when the module is missing.

## Seeding

- `await scope.SeedAsync(entity1, entity2, ...)` seeds one entity graph in a single save: mixed types and entities reachable through navigation properties belong together in one call, because the tracker clears after each call and a shared parent seeded again in a later call would be inserted as a duplicate row. A collection element (array or list) is flattened automatically, so `SeedAsync(users)` seeds the items, never the collection as one element.
- `await scope.SeedRangeAsync(entities)` is the typed path for one entity type from an `IEnumerable<T>`.
- Seeds save through the repository save pipeline: creation audit applies (subject to the registered `IAuditPropertySetter` seam) and the change tracker is cleared afterwards, so later no-tracking reads followed by `UpdateAsync` never collide with leftover tracked instances. Do not call `ChangeTracker.Clear()` manually after seeding.

## Factory Contract

`MonicaTestApplicationFactory<TDiscoveryAnchor>` is a reusable composition recipe, not a shared host.

- `ConfigureHost(WebApplicationBuilder)` configures the future host.
- `TypeDiscoveryAssemblies` selects the production assemblies scanned for ProjectUnits; it contains the anchor assembly by default.
- `ConfigureMonica(IMonicaBuilder)` defines the real Monica composition and is required.
- `ConfigureServices(IServiceCollection)` applies stable test registrations before build.
- `CreateAsync(Action<ISeamReplacementBuilder>? configureScenario = null, CancellationToken cancellationToken = default)` creates and starts a new full host.

The resulting `MonicaTestApplication` exposes `Services`, `Application`, `ModuleSnapshots`, and `CreateScope(CancellationToken)`. `CreateScope` never changes service registrations. Use another `CreateAsync` call when a test needs a different registration graph.

## Scope API

`MonicaTestScope` mirrors the runtime shapes a production host offers:

- `Resolve<T>()` resolves a scoped service.
- `InvokeAsync(action, options?)` runs a write-scenario action with request-shaped unit-of-work semantics.
- `SeedAsync(params object[])` seeds one entity graph (mixed types, collections flattened); `SeedRangeAsync<T>(IEnumerable<T>)` seeds a typed sequence.
- `GetDbContextAsync<TDbContext>()` resolves the scope's repository DbContext for direct assertions (query with `AsNoTracking()`; use `IgnoreQueryFilters()` to see soft-deleted rows).

## Boundary Choice

Use a full scenario host for application services, domain services, repositories, module registration, options, mapping, interceptors, events, jobs, and behavior spanning scopes.

Use raw `ProjectUnitFixture<TUnit>` only for a narrow collaboration test where every dependency is explicit and Monica composition is irrelevant. It does not validate discovery, conventional registration, dynamic proxies, module options, hosted lifecycle, or host ownership. Do not use or recreate `ApplicationServiceFixture<THandler>`.

Entity invariant tests and deterministic value-object tests may construct objects directly.

## Required Conventions

- Project, folder, assembly, and root namespace: `Test.{ProductionProjectName}`
- Project factory: `{Service}TestApplicationFactory`
- Test class: `{TypeUnderTest}Tests`
- Test method: `Method_WhenCondition_ShouldExpectation`
- Host-backed test field: `_factory`
- Source-aligned folders: `HandlersCommand`, `HandlersQuery`, `DomainServices`, `Repositories`, `Entities`, and `Modules`
- Test-only support folders: `Factories`, `Builders`, `TestDoubles`, and `TestData`

Do not put all service tests in one xUnit collection. Independent scenario hosts run in parallel by default. Use a named collection only when tests intentionally share a real external resource that cannot be isolated, and document that resource.

## Hard Rules

- Replace boundaries, not domain logic.
- Resolve production handlers, domain services, repositories, mappers, and options from the scenario host.
- Apply registration overrides before host build; never copy descriptors from a built provider or replace services while creating a scope.
- Never share a `MonicaApplication` between root providers.
- Keep stateful doubles owned by one scenario host or its scopes.
- Use unique names or explicit serialization for external databases, ports, files, topics, and queues.
- Avoid real network, uncontrolled external databases, sleeps, random/manual output, and hidden machine dependencies.
- Keep assertions in tests rather than setup helpers.
- Never manually initialize a repository DbContext for unit-of-work participation or call `ChangeTracker.Clear()` to work around seeding; both needs are covered by the scope API.

## References

- Read `references/standards.md` for project, ownership, and migration rules.
- Read `references/templates.md` for exact factory, scenario, and write-path shapes.
- Read `references/database-isolation.md` before selecting a database strategy.

## Validation

- Run the target test project with `dotnet test`, following the repository's established path conventions.
- Use one `dotnet build` or `dotnet test` process at a time.
- Treat warnings introduced by the touched test project as failures.
