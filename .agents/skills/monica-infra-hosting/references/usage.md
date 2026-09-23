# Host composition and runtime services

## Compose once

```csharp
using Monica.Core.Modularity.Extensions;
using Monica.Modules;

var builder = WebApplication.CreateBuilder(args);
builder.AddMonica(monica =>
{
    monica.ConfigureApplication(options => options.AppName = "My application");
    monica.AddModuleSystem();
    monica.AddDependencyInjection();
    monica.AddHostedService();
});

var app = builder.Build();
app.UseMonica();
app.MapMonica();
app.Run();
```

`AddMonica(...)` composes and validates the graph before `Build()`. It is single-use per builder; registrations are sealed when its callback returns. On a Web host, call `UseMonica()` before `MapMonica()`, once each, on the same app before starting it. `UseMonica()` installs Monica middleware around routing; `MapMonica()` maps module endpoints. A generic host uses `Host.CreateApplicationBuilder`, calls `AddMonica(...)`, then builds and starts without the web calls. Web-capable modules retain their ordinary services there; modules intrinsically requiring Web hosting fail composition. If a module has an optional web feature, select it only on a Web host.

`AddModuleSystem()` provides `ModuleDiagnosticsFacade` for the composed graph and bounded option diagnostics. `ConfigureModuleOptionDiagnostics<TModule,TOptions>` adds host-owned sensitivity rules where automatic classification is insufficient. The graph is identified by module `Type`; a rendered module key is diagnostic output, not a registration key.

## Conventional DI and decoration

`AddDependencyInjection()` discovers concrete classes implementing `ITransientDependency`, `IScopedDependency`, or `ISingletonDependency`, or marked with `DependencyAttribute`, from the configured type-discovery scope. `EnableAutoRegistrationDiagnostics()` captures a startup snapshot and pulls in the hosted-service module; `EnableAutoRegistrationLogging()` controls per-type startup logs independently. A missing conventional service is often a missing discovery assembly or marker, so check `ConfigureTypeDiscovery(...)` and the diagnostics snapshot before adding a duplicate registration.

There is no current production `ModuleDynamicProxy` or `AddDynamicProxy()` registration in Monica DI/Core. The remaining public interface-proxy decorator extension, `IServiceCollection.DecorateInterfaceProxy<TInterface,TDecorator>()`, is in `Monica.Experimental`, which is included in the solution but is not a Monica module. It decorates matching registered interfaces; the non-`Try` form throws `DecorationException` when none is registered, while `TryDecorateInterfaceProxy` returns `false`. Treat this as an explicit experimental DI choice, not an automatic effect of `AddDependencyInjection()`. For execution-wide behavior use the typed execution pipeline; for a local wrapper, choose decoration after checking the service lifetime and interface contract.

## Hosted service lifetime

`AddHostedService()` supplies a registry, runtime observers, checkpoints, and metrics for Monica hosted services. `MoHostedService` and `MoBackgroundService` report transitions through `RecordState(...)`; they do not replace normal `IHostedService` registration. Register the concrete service through DI and expose that same instance through `services.AddHostedService(sp => sp.GetRequiredService<MyWorker>())` when a consumer also needs to resolve it. `DefaultMaxHistorySize` is 100, the default background heartbeat interval is one minute, and `FailFastOnStartupError` defaults to `false`. Turning that flag on rethrows start failures and fails the host; leaving it off records/logs them while startup can continue. For implementation of a new subclass, read `$monica-development`'s hosted-service reference.

## Service discovery and state-store selection

```csharp
builder.AddMonica(monica =>
{
    monica.AddServiceDiscovery()
        .AsStandalone()
        .UseMemoryStorage();
});
```

The role defaults to `Worker`; `AsRegistry()` gives control-plane ownership, and `AsStandalone()` combines registry and worker on one host. A storage mode is mandatory even for the default worker. `UseMemoryStorage()` is process-local. `UseDistributedStorage()` requires a distributed provider selected on `ModuleStateStore`. `UseExternalKeyedStorage(key)` requires an existing keyed `IStateStore` registration under that exact key; there is no unkeyed fallback. Composition validates the storage choice and its service contract. Service discovery can add registry endpoints on Web hosts, while its worker services also run on generic hosts.

## State-store providers and consumers

`AddStateStore()` registers `IMemoryStateStore` and makes it the default unkeyed `IStateStore`. It is process-local. Consumers inject `IStateStore` and use `GetStateAsync<T>(key)`, `SaveStateAsync(key, value, cancellationToken, ttl)`, `DeleteStateAsync(key)`, and ETag-based methods when concurrent updates matter. `GetStateAsync<T>` returns `default(T)` for a missing key; use `ExistAsync(key)` when the distinction matters for value types. A zero TTL means permanent storage; provider capabilities such as query and key scanning vary, so check the chosen provider before depending on them.

For a shared default, choose a distributed provider on the `AddStateStore()` registration. `SetCommonDistributedStateStoreProvider<TProvider>()` accepts a custom `IDistributedStateStore`; the Redis and Dapr extensions include their provider modules and setup:

```csharp
builder.AddMonica(monica =>
{
    monica.AddStateStore()
        .UseRedisStateStoreProvider(options =>
        {
            options.UseNormalConnection("redis", 6379);
            options.KeyPrefix = "my-app:";
        });
});
```

`UseDaprStateStoreProvider(options => options.StateStoreName = "app-state")` instead uses the Dapr sidecar and requires a state-store component with the same name. Dapr's client module brings in sidecar readiness monitoring. Redis also supports Sentinel and cluster connection methods. Configure credentials and endpoints from host configuration. Typed distributed values follow the host's canonical JSON contract; changing that contract can make persisted values unreadable.

Use `AddKeyedCommonStateStore(key, useDistributed: true)` to expose the selected common provider under a key, or `AddKeyedStateStore<TProvider>(key)`, `AddKeyedRedisStateStore(key, configureOptions)`, or `AddKeyedDaprStateStore(key, configureOptions)` for independently configured instances. Resolve with `GetRequiredKeyedService<IStateStore>(key)`. A keyed Redis/Dapr store does not automatically replace the unkeyed default. Selecting a distributed common store declares and satisfies the `distributed-provider` feature; requesting distributed storage without a provider fails composition rather than silently falling back to memory.

## Source checks

When working inside the repository, verify version-sensitive calls in `Monica.Core/Modularity/Extensions/MonicaHostBuilderExtensions.cs`, `Monica.Core/Modularity/Extensions/MonicaApplicationBuilderExtensions.cs`, `Monica.DependencyInjection/Modules/ModuleDependencyInjection.cs`, `Monica.Core/Modules/ModuleHostedService.cs`, `Monica.ServiceDiscovery/Modules/ModuleServiceDiscovery.cs`, `Monica.StateStore/Modules/ModuleStateStore.cs`, `Monica.StateStore.StackExchange/Modules/ModuleRedisStateStore.cs`, and `Monica.Dapr/Modules/ModuleDaprStateStore.cs`. `examples/Monica.ReferenceApplication/src/AppHost/Monica.Reference.Api/Program.cs` is a working host composition example.
