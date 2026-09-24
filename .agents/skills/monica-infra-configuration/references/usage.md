# Managed configuration usage

## One input plan for startup and runtime

`MonicaConfigurationInputPlan.Create` declares exactly one store composition, an optional section-path convention, and ordered managed JSON files. Creating the plan performs no store I/O. `AddConfiguration(plan)` applies its runtime half; the plan's path convention cannot be overridden later. The file store is a simple local choice:

```csharp
var plan = MonicaConfigurationInputPlan.Create(inputs => inputs
    .UseFileConfigurationStore(options => options.RootDirectory = "configuration-store")
    .AddManagedJsonFile("appsettings.Managed.json", optional: true));

builder.AddMonica(monica => monica.AddConfiguration(plan));
```

Build bootstrap configuration from the same plan before composing topology driven by managed settings: `using var bootstrap = plan.BuildBootstrapConfiguration(builder);` then call `plan.LoadEffectiveOptionsSnapshot(bootstrap, [typeof(OrdersOptions)])` (or the async variant) and obtain the typed value from the snapshot. This read uses the host's current configuration, then the plan's managed JSON files in declaration order; later files have higher precedence. It overlays effective values from the selected store without writing definitions or values. Treat it as a point-in-time read: runtime activation or another writer may later change the store. Keep topology-driving options static after startup or require restart. `examples/Monica.ReferenceApplication/src/AppHost/Monica.Reference.Api/Program.cs` shows basic plan composition; `tests/Test.Monica.Configuration/Bootstrap/MonicaConfigurationInputPlanTests.cs` exercises snapshots.

For a relational store, reference `Monica.Configuration.EfCore` and select `UseDbConfigurationStore(db => db.UseSqlite(connectionString))` in the plan. The callback can run separately for bootstrap and runtime: keep it deterministic, side-effect-free, and stable across both. The host owns `ConfigurationDbContext` migrations; neither input plan nor runtime module creates/upgrades the schema. The file store uses identity-addressed files under `RootDirectory`; legacy key-named files require migration or recreation. A custom store composition must provide the same logical store in startup and runtime phases.

## Definition and binding

Annotate concrete options with `[Configuration("Orders")]`; the explicit path is the highest-priority binding path. Without it, the default convention is the short CLR type name. Set `DefinitionKey` when persisted identity must survive a CLR rename. `[OptionSetting]` adds node metadata such as stable `NodeKey`, sensitivity, editor hint, and reload behavior; ordinary .NET validation attributes still define value constraints. The module discovers annotated types in the host's configured type-discovery scope and registers Microsoft options binding. Consumers use `IOptions<T>` or `IOptionsMonitor<T>` according to their lifetime and reload needs.

At runtime, `ModuleConfiguration` appends its effective-value provider after the host's bootstrap providers, activates that provider, validates effective values, and exposes source, history, mutation, rollback, and reload operations through `ConfigurationFacade`. `ModuleConfigurationOption.RuntimeValidationBehavior` defaults to `DiagnosticOnly`: invalid effective values produce a warning and report but do not prevent startup or managed options resolution. `FailFast` rejects invalid values at startup and subsequent options resolution. Store access, provider activation, registration, and binding errors remain fatal in both modes. Duplicate resolved section paths fail by default; change to `Warning` only when overlap is intentional. `IConfigurationRuntimeValidationService` and the facade expose diagnostic reports.

## Mutation, reload, and UI

Prefer `ConfigurationFacade` or the operator UI at user-facing boundaries for source inspection, candidate validation, mutation groups, history, rollback preview, and reload status. Apply changes against current source revisions; a stale revision is a conflict to review and retry, not a reason to force an overwrite. Stored value mutation and in-process projection reload are separate concerns: a service may require restart for a changed setting. The optional `monica.AddConfigurationUI()` contributes state, storage, history, and version pages; `EnableAffectedServiceConfirmation` adds a point-in-time affected-service prompt for shared stores. It does not prove that those services are live or reloaded.

For distributed invalidation, call `.UseEventBus()` on the Configuration registration and provide a default `IDistributedEventBus`, or pass a keyed distributed service key. The bridge publishes only invalidation metadata, never configuration values. All participants use the same topic (default `monica.configuration.reload`). Its subscription signals each replica to reload its local projection; ensure the EventBus provider and subscriptions are functioning before diagnosing reload behavior. Unified versions are disabled by default; enable `UseUnifiedVersionControl()` and add an explicit inclusion filter for definitions or categories that should be captured and restorable.

## Source and checks

Input plans: `Monica.Configuration/Bootstrap/MonicaConfigurationInputPlan.cs`, `MonicaConfigurationInputPlanBuilder.cs`, and `Monica.Configuration.EfCore/Bootstrap/MonicaConfigurationInputPlanEfCoreBuilderExtensions.cs`. Runtime options and activation: `Monica.Configuration/Modules/ModuleConfiguration.cs` and `Monica.Configuration/Services/Support/MonicaConfigurationProviderActivationCoordinator.cs`. UI/bridge: `Monica.Configuration.UI/Modules/ModuleConfigurationUI.cs` and `Monica.Configuration.EventBus/Modules/ModuleConfigurationEventBus.cs`. Tests: `tests/Test.Monica.Configuration/Bootstrap/MonicaConfigurationInputPlanTests.cs`, `tests/Test.Monica.Configuration.EfCore/DatabaseConfigurationInputPlanTests.cs`, `tests/Test.Monica.Configuration/Projection/MonicaConfigurationProviderPartialReloadTests.cs`, and `tests/Test.Monica.Configuration.EventBus/ConfigurationEventBusBridgeTests.cs`. Run affected non-UI projects, then the standard solution build and non-UI test gate for implementation changes. UI tests run only on explicit request.
