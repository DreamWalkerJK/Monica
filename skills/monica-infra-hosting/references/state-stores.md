# State stores

Inside the host's `builder.AddMonica(monica => { ... })` callback, `monica.AddStateStore()` registers `IMemoryStateStore` and makes it the default unkeyed `IStateStore`. It is process-local. Consumers inject `IStateStore` and use `GetStateAsync<T>(key)`, `SaveStateAsync(key, value, cancellationToken, ttl)`, `DeleteStateAsync(key)`, and ETag-based methods when concurrent updates matter. `GetStateAsync<T>` returns `default(T)` for a missing key; use `ExistAsync(key)` when the distinction matters for value types. Provider capabilities such as query and key scanning vary, so check the chosen provider before depending on them.

For the concrete Memory provider, omit the TTL or pass `null` for a permanent entry. Any supplied duration is added to the current time; `TimeSpan.Zero` therefore expires immediately. Do not assume that a zero-duration convention applies uniformly across providers. Memory's inherited `ExistAsync` tests a non-null object read: an actual null payload reports false, and its typed value/ETag read returns null with an empty ETag, although its key can remain in the scan registry. A JSON-null element is a distinct stored value. Use the actual existence, value, scan and TTL contracts for the chosen provider rather than treating those observations as interchangeable.

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

Single-key Dapr reads treat the Redis `nil` response for the requested key and store as missing state, including when the sidecar reports it as an `Internal` gRPC failure. Typed reads return `default(T)`, raw reads return `null`, and ETag reads return an empty ETag. Conditional creation can then proceed through its normal missing-key path. Other failures, including connection, authentication, wrong-type, and cancellation errors, remain failures; write failures are never converted into missing state.

Use `AddKeyedCommonStateStore(key, useDistributed: true)` to expose the selected common provider under a key, or `AddKeyedStateStore<TProvider>(key)`, `AddKeyedRedisStateStore(key, configureOptions)`, or `AddKeyedDaprStateStore(key, configureOptions)` for independently configured instances. Resolve with `GetRequiredKeyedService<IStateStore>(key)`. A keyed Redis/Dapr store does not automatically replace the unkeyed default. Selecting a distributed common store declares and satisfies the `distributed-provider` feature; requesting distributed storage without a provider fails composition rather than silently falling back to memory.

`AddStateStoreUI()` adds an optional shell page at `/state-store-dashboard` for inspecting state. It requires the StateStore module, localization, and the shell. Its browser adapters cover memory, Redis, and Dapr; the selected provider still determines which operations, such as key scanning or query, can work. `MaxKeysPerPage` defaults to 50 displayed results. A scan result's matched count and `HasMore` describe a truncated snapshot; the adapter does not supply a cursor or a next page, and its scan can enumerate more keys than the display limit. Exact lookup distinguishes an existing record from a successful missing result. TTL and ETag are observed metadata, not an atomic snapshot or a live countdown.

Create, edit and delete default to enabled and have independent UI option gates. Creation uses create-if-absent; an ETag, when supplied, protects a conditional update or single deletion. Bulk deletion names explicit selected keys without per-key ETags; it does not mean all matches or all provider keys. The dialog's zero-second TTL input is converted to a null duration before persistence. These UI controls do not configure host authorization. For service-discovery storage, read [service-discovery.md](service-discovery.md): its choice binds directly to the selected memory/distributed/keyed service, independently of the default unkeyed `IStateStore`.

Check `Monica.StateStore/Modules/ModuleStateStore.cs`, `Monica.StateStore.StackExchange/Modules/ModuleRedisStateStore.cs`, and `Monica.Dapr/Modules/ModuleDaprStateStore.cs` for provider registration, and `Monica.StateStore.UI/Modules/ModuleStateStoreUI.cs` for the optional browser.
