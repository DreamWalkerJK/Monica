using Dapr.Client;
using System.Buffers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monica.Modules;
using Monica.EventBus.Abstractions;
using Monica.EventBus.Services.Support;
using Monica.EventBus.Models;

namespace Monica.Dapr.Services;

/// <summary>
/// Dapr-based distributed event bus implementation.
/// Publishes events using Dapr PubSub component.
/// </summary>
/// <param name="serviceScopeFactory">Creates scopes for event handlers.</param>
/// <param name="eventHandlerInvoker">Invokes resolved event handlers.</param>
/// <param name="subscriptionManager">Owns this host's subscription catalog.</param>
/// <param name="daprClient">Publishes events through the Dapr sidecar.</param>
/// <param name="messageFactory">Captures stable metadata and the JSON payload.</param>
/// <param name="daprOptions">Provides Dapr event bus options.</param>
/// <param name="loggerFactory">Creates the event bus logger.</param>
/// <param name="serviceKey">An optional keyed-provider identifier.</param>
public class DaprEventBusProvider(
    IServiceScopeFactory serviceScopeFactory,
    IEventHandlerInvoker eventHandlerInvoker,
    IEventSubscriptionRegistry subscriptionManager,
    DaprClient daprClient,
    IEventMessageFactory messageFactory,
    IOptions<ModuleDaprEventBusOption> daprOptions,
    ILoggerFactory loggerFactory,
    string? serviceKey = null)
    : DistributedEventBusBase(serviceScopeFactory, eventHandlerInvoker, subscriptionManager, loggerFactory, serviceKey), IEventTransport
{
    private readonly DaprClient _daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
    private readonly ModuleDaprEventBusOption _daprOptions = daprOptions.Value ?? throw new ArgumentNullException(nameof(daprOptions));

    /// <inheritdoc />
    public async Task SendAsync(EventMessage message, CancellationToken cancellationToken)
    {
        if (message.Scope != EventSubscriptionScope.Distributed || message.Metadata.ServiceKey != ServiceKey)
        {
            throw new InvalidOperationException("The prepared event does not target this Dapr transport.");
        }

        var metadata = message.Metadata;
        if (string.IsNullOrWhiteSpace(metadata.MessageId) || string.IsNullOrWhiteSpace(metadata.Source))
        {
            throw new InvalidOperationException("A durable Dapr event requires a stable ID and source.");
        }

        // Dapr preserves an explicitly supplied CloudEvent envelope when sent with its
        // CloudEvents content type. The data is the original captured JSON value.
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("specversion", "1.0");
            writer.WriteString("id", metadata.MessageId);
            writer.WriteString("source", metadata.Source);
            writer.WriteString("type", metadata.EventName);
            writer.WriteString("datacontenttype", "application/json");
            if (metadata.TraceParent is not null) writer.WriteString("traceparent", metadata.TraceParent);
            if (metadata.TraceState is not null) writer.WriteString("tracestate", metadata.TraceState);
            writer.WritePropertyName("data");
            writer.WriteRawValue(message.Body.Span);
            writer.WriteEndObject();
        }

        await _daprClient.PublishByteEventAsync(
            _daprOptions.PubSubName, metadata.TopicName, buffer.WrittenMemory,
            "application/cloudevents+json", cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Publishes an event to Dapr PubSub.
    /// </summary>
    public override async Task PublishAsync(Type eventType, object eventData, string? topicName = null, CancellationToken cancellationToken = default)
    {
        await SendAsync(messageFactory.Prepare(eventType, eventData,
            EventSubscriptionScope.Distributed, topicName, ServiceKey), cancellationToken);
    }

    /// <summary>
    /// Publishes multiple events in bulk to Dapr PubSub.
    /// </summary>
    public override async Task BulkPublishAsync(Type eventType, IEnumerable<object> eventDataList, string? topicName = null, CancellationToken cancellationToken = default)
    {
        var eventsList = eventDataList.ToList();
        var messages = eventsList.Select(eventData => messageFactory.Prepare(
            eventType, eventData, EventSubscriptionScope.Distributed, topicName, ServiceKey)).ToArray();
        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await SendAsync(message, cancellationToken);
        }
    }
}
