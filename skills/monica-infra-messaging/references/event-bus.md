# Publish and receive events

For application EventBus contracts and consumers, including outbox or inbox refactors, first follow the ProjectUnit [event contract](../../monica-application-project-unit-development/references/12-entity-request-event-template.md) and [handler](../../monica-application-project-unit-development/references/14-event-handler-template.md) templates. This reference owns transport capabilities and setup; the interface example below demonstrates the general EventBus API.

`monica.AddEventBus()` registers the local bus, scoped publishing gateway, handler discovery, subscription registry, and receive dispatcher. Use `ILocalEventBus` when publisher and handlers run in one process; implement `ILocalEventHandler<TEvent>` in the host's discovery scope, or use `IEventBus.SubscribeAsync<TEvent,...>` for an explicit subscription. `DisableAutoDiscovery` disables the scan. A handler receives a cancellation token and should pass it to its own I/O.

Automatic subscriptions include only concrete handler types registered in the host container. When automatic handlers are discovered, the container must expose `IServiceProviderIsService`, as Microsoft DI does, so discovery can inspect registrations without constructing handlers. Disabled or empty discovery does not require this inspector; a discovered automatic handler without it causes a clear startup error. Each delivery activates its handler in a fresh asynchronously disposed DI scope; constructor failures remain delivery failures rather than removing the subscription during discovery.

EventBus execution defaults to an automatic UnitOfWork when that behavior is registered. Put any `ExecutionTransactionAttribute` override or context selection on the concrete `HandleEventAsync` method, following the persistence [transaction contract](../../monica-infra-persistence/references/transactions.md).

For example, a discovered handler for `OrderApproved` uses the exact event type named by the publisher:

```csharp
public sealed record OrderApproved(Guid OrderId);

public sealed class OrderApprovedHandler(ILogger<OrderApprovedHandler> logger)
    : ILocalEventHandler<OrderApproved>
{
    public Task HandleEventAsync(OrderApproved eventData, CancellationToken cancellationToken)
    {
        logger.LogInformation("Approved order {OrderId}", eventData.OrderId);
        return Task.CompletedTask;
    }
}
```

```csharp
builder.AddMonica(monica => monica.AddEventBus());

await localEventBus.PublishAsync(
    new OrderApproved(orderId), cancellationToken: cancellationToken);
```

`IEventBus.PublishAsync<TEvent>` and `BulkPublishAsync<TEvent>` match the **exact** event type and topic. A handler registered for a base event does not receive a derived type. The optional `topicName` overrides event-name metadata. If a local event has no receiver, check the discovered handler type, its implemented interface, the topic override, and startup subscription registration before changing the transport.

For cross-process delivery, inject `IDistributedEventBus` after selecting a provider. `UseDistributedEventBus<TProvider>()` requires a `DistributedEventBusBase` that also supplies `IEventTransport`; `UseNoOpDistributedEventBus()` satisfies composition but sends nothing externally. For native Kafka, register `monica.AddEventBusKafka().UseKafkaProvider(cluster)` with a configured `KafkaClusterConfig`; this selects the transport and starts its subscription worker. `AddConfiguredCluster(cluster)` only exposes a cluster to the Kafka console, and `UseDaprKafkaIntegration(...)` describes an existing Dapr-backed Kafka component rather than selecting an EventBus provider. `AddKeyedEventBus(key, useDistributed: true)` needs the default distributed provider, while `AddKeyedLocalEventBus(key)` creates an isolated keyed local bus.

Implement `IDistributedEventHandler<TEvent>` for distributed receives and make sure its assembly is discovered. If nothing arrives, inspect the publishing service's provider selection, the receiving service's worker and subscription startup, service key, exact topic/type, and handler discovery. A successful ordinary publish follows its selected provider's behavior; it is not an end-to-end acknowledgment from every handler.

For a database write that must publish only after commit, mark the event `[Outbox]`, enable outbox on the primary UnitOfWork context, and publish through the scoped gateway inside that operation. A successful outbox `PublishAsync` means the prepared payload was staged, not committed or transported. Worker delivery can retry, so consumers need idempotency; `[Inbox]` supplies durable handler deduplication when configured. Follow the persistence skill's [transactional-events reference](../../monica-infra-persistence/references/transactional-events.md) for migration and transaction ownership.

## Rejected deliveries and retries

Throw `Monica.Core.ExceptionHandling.Exceptions.BusinessException` only when retrying the same delivery cannot resolve a business-rule rejection. Built-in distributed providers treat it as permanent after the handler operation and its scope have unwound. They also treat `EventMessageDeserializationException` as permanent. A malformed or null event body must not keep a Kafka partition pinned indefinitely.

| Receive outcome | Dapr streaming provider | Native Kafka provider |
| --- | --- | --- |
| Handler completes | `Success` | Commit offset |
| `BusinessException` or malformed event body | Log and return `Drop`; Dapr forwards to the configured dead-letter topic, or discards when none is configured | Log and commit offset, allowing the next record to proceed |
| Other exception, timeout, or cancellation | Request `Retry` | Rewind without acknowledging; retry after backoff, or stop during shutdown |

Native Kafka currently has no dead-letter destination option. Its permanent rejections are discarded after their offset commit succeeds. An offset-commit failure remains retryable even after a business rejection; if rewind fails, the consumer stops rather than risking acknowledgment of later records. Dapr and Kafka retain the exception in topic diagnostics and log message identity and topic information; rejected deliveries are not counted as successfully processed messages. Broker delivery is still at least once: an acknowledgment failure can cause a rejected message to reappear.

For Dapr, configure the existing option when rejected message bodies must be retained:

```csharp
builder.AddMonica(monica =>
    monica.AddEventBus().UseDaprProvider(options =>
        options.DeadLetterTopicSuffix = ".dead-letter"));
```

The default suffix is `null`. A configured suffix must be non-empty and contain no whitespace. Each source topic uses `<topic><suffix>`; a topic already ending in the suffix has no further dead-letter target. Provision broker topics and their retention separately. `Drop` delegates terminal forwarding to Dapr; Monica does not republish the payload itself. Configure Dapr resiliency and component retry limits separately for transient failures; setting a suffix does not define a retry budget. See the [Dapr streaming response contract](https://v1-16.docs.dapr.io/developing-applications/sdks/dotnet/dotnet-messaging/dotnet-messaging-pubsub-howto/).

Do not wrap every failed `Res`, `InvalidOperationException`, database error, or remote-call error in `BusinessException`. The framework cannot infer permanence from an exception message or arbitrary inner exception. Unknown failures remain retryable, so classify a known business rejection at the application boundary:

```csharp
if (notice.RequiredReference is null)
{
    throw new BusinessException("The notice has no required reference.");
}
```

This rejects the entire transport delivery: subsequent handlers for that topic are not invoked, and any earlier handler's separately committed work is not undone. The failing handler's transaction, including its Inbox receipt, rolls back. If an invalid business document must be saved or an alarm raised, model that as a handled outcome, persist it and return normally so its transaction and Inbox receipt commit. Do not throw after staging those effects. Local EventBus calls continue to propagate exceptions to their caller; publishing and Outbox send retries are unchanged.

Source and checks: `Monica.EventBus/Abstractions/IEventBus.cs`, `Abstractions/Handlers/ILocalEventHandler.cs` and `IDistributedEventHandler.cs`, `Monica.EventBus/Modules/ModuleEventBus.cs`, `Monica.Dapr/Services/DaprEventBusSubscriptionHostedService.cs`, `Monica.EventBus.Kafka/Providers/ConfluentKafka/KafkaEventBusSubscriptionHostedService.cs`, `tests/Test.Monica.Dapr/Services/DaprEventBusSubscriptionHostedServiceTests.cs`, `tests/Test.Monica.EventBus.Kafka/Providers/ConfluentKafka/KafkaEventBusSubscriptionHostedServiceTests.cs`, `tests/Test.Monica.Repository/UnitOfWork/InboxExecutionTests.cs`, `tests/Test.Monica.EventBus/Services/EventBusAutoDiscoveryLifecycleTests.cs`, `tests/Test.Monica.EventBus/Modules/ModuleEventBusCompositionTests.cs`, and `OutboxGatewayTests.cs`.

For registered-handler inspection, independent transport health, live change-feed ownership, and diagnostic publishing, use [EventBus diagnostics](event-bus-diagnostics.md).

For Kafka broker configuration, routed topic and consumer inspection, sampled evidence, and persisted-snapshot upgrades, use [Kafka console diagnostics](kafka-console.md).
