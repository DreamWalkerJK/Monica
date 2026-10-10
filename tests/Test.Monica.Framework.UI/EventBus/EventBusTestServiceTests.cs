using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Monica.Core.JsonSerialization.Abstractions;
using Monica.Core.Modularity.Abstractions;
using Monica.Core.Results;
using Monica.EventBus.Abstractions;
using Monica.EventBus.Abstractions.Handlers;
using Monica.EventBus.Annotations;
using Monica.EventBus.Models;
using Monica.EventBus.Services;
using Monica.EventBus.Services.Support;
using Monica.Framework.UI.UIEventBus.State;
using Monica.Modules;
using Monica.Testing.Doubles;
using Monica.Testing.Hosting;
using Xunit;

namespace Test.Monica.Framework.UI.EventBus;

public sealed class EventBusTestServiceTests
{
    private const string TEST_BUS = "runtime-test-bus";
    private const string TEST_TOPIC = "runtime-test-topic";
    private const string EVENT_NAME = "test.eventbus-ui.durable.v1";
    private const string JSON_PAYLOAD = """{"value":"diagnostic payload"}""";

    [Theory]
    [InlineData(false, "1970-01-01", "00:00:00")]
    [InlineData(true, "19700101", "000000")]
    public async Task CreateRuntimeJsonSample_WhenNestedContractContainsDateAndTimeValues_ShouldValidateAndPublish(
        bool useCustomDateTimeConverters, string expectedDate, string expectedTime)
    {
        var factory = new EventBusUITestApplicationFactory(
            useCustomDateTimeConverters: useCustomDateTimeConverters);
        await using var app = await factory.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var subscription = await services.GetRequiredService<IEventSubscriptionRegistry>().SubscribeAsync(
            new EventSubscriptionDescriptor
            {
                EventType = typeof(RuntimeEntityChange<RuntimeFlightPlan>),
                TopicName = TEST_TOPIC,
                Scope = EventSubscriptionScope.Distributed,
                HandlerFactory = new IocEventHandlerFactory(
                    services.GetRequiredService<IServiceScopeFactory>(), typeof(RuntimeFlightPlanHandler))
            }, TestContext.Current.CancellationToken);
        var service = services.GetRequiredService<EventBusTestService>();

        var sample = service.CreateRuntimeJsonSample(subscription.Id);

        sample.Status.Should().Be(ResStatus.Ok);
        sample.Message.Should().BeNull();
        sample.Data.Should().NotBeNullOrWhiteSpace();
        using var document = JsonDocument.Parse(sample.Data!);
        var entity = document.RootElement.GetProperty("entity");
        entity.GetProperty("planDate").GetString().Should().Be(expectedDate);
        entity.GetProperty("requiredDate").GetString().Should().Be(expectedDate);
        entity.GetProperty("optionalTime").GetString().Should().Be(expectedTime);
        entity.GetProperty("requiredTime").GetString().Should().Be(expectedTime);

        var validation = service.ValidateRuntimePayload(subscription.Id, sample.Data!);

        validation.Status.Should().Be(ResStatus.Ok);
        validation.Message.Should().BeEmpty();

        var publication = await service.PublishRuntimePayloadAsync(
            subscription.Id, sample.Data!, TestContext.Current.CancellationToken);

        publication.Status.Should().Be(ResStatus.Ok);
        publication.Message.Should().BeEmpty();
        var message = factory.DefaultTransport.Messages.Should().ContainSingle().Which;
        var options = services.GetRequiredService<IJsonSerializerOptionsProvider>().SerializerOptions;
        JsonSerializer.Deserialize<RuntimeEntityChange<RuntimeFlightPlan>>(message.Body.Span, options)
            .Should().Be(new RuntimeEntityChange<RuntimeFlightPlan>(RuntimeChangeKind.Created,
                new RuntimeFlightPlan(new DateOnly(1970, 1, 1), new DateOnly(1970, 1, 1),
                    TimeOnly.MinValue, TimeOnly.MinValue), 0));
        factory.KeyedTransport.Messages.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(TEST_BUS)]
    public async Task PublishRuntimePayloadAsync_WhenDistributedOutboxEventHasNoTransaction_ShouldSendPreparedMessage(
        string? serviceKey)
    {
        var factory = new EventBusUITestApplicationFactory();
        await using var app = await factory.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var subscription = await SubscribeAsync(services, EventSubscriptionScope.Distributed, serviceKey);
        var gateway = serviceKey is null
            ? services.GetRequiredService<IDistributedEventBus>()
            : services.GetRequiredKeyedService<IDistributedEventBus>(serviceKey);

        // Ordinary application publication still requires the Outbox transaction boundary.
        await Assert.ThrowsAsync<InvalidOperationException>(() => gateway.PublishAsync(
            new RuntimePayload("application payload"), cancellationToken: TestContext.Current.CancellationToken));

        var result = await services.GetRequiredService<EventBusTestService>().PublishRuntimePayloadAsync(
            subscription.Id, JSON_PAYLOAD, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ResStatus.Ok);
        result.Message.Should().BeEmpty();
        var transport = serviceKey is null ? factory.DefaultTransport : factory.KeyedTransport;
        var otherTransport = serviceKey is null ? factory.KeyedTransport : factory.DefaultTransport;
        var message = transport.Messages.Should().ContainSingle().Which;
        otherTransport.Messages.Should().BeEmpty();
        factory.HandlerState.Deliveries.Should().BeEmpty();
        AssertMessage(message, EventSubscriptionScope.Distributed, serviceKey, services);
        transport.LastCancellationToken.Should().Be(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(TEST_BUS)]
    public async Task PublishRuntimePayloadAsync_WhenLocalOutboxEventHasNoTransaction_ShouldDispatchWithMetadata(
        string? serviceKey)
    {
        var factory = new EventBusUITestApplicationFactory();
        await using var app = await factory.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var subscription = await SubscribeAsync(services, EventSubscriptionScope.Local, serviceKey);
        var gateway = serviceKey is null
            ? services.GetRequiredService<ILocalEventBus>()
            : services.GetRequiredKeyedService<ILocalEventBus>(serviceKey);

        await Assert.ThrowsAsync<InvalidOperationException>(() => gateway.PublishAsync(
            new RuntimePayload("application payload"), cancellationToken: TestContext.Current.CancellationToken));

        var result = await services.GetRequiredService<EventBusTestService>().PublishRuntimePayloadAsync(
            subscription.Id, JSON_PAYLOAD, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ResStatus.Ok);
        result.Message.Should().BeEmpty();
        var delivery = factory.HandlerState.Deliveries.Should().ContainSingle().Which;
        delivery.Payload.Should().Be(new RuntimePayload("diagnostic payload"));
        delivery.Metadata.EventName.Should().Be(EVENT_NAME);
        delivery.Metadata.TopicName.Should().Be(TEST_TOPIC);
        delivery.Metadata.ServiceKey.Should().Be(serviceKey);
        delivery.Metadata.MessageId.Should().NotBeNullOrWhiteSpace();
        delivery.Metadata.Source.Should().NotBeNullOrWhiteSpace();
        factory.DefaultTransport.Messages.Should().BeEmpty();
        factory.KeyedTransport.Messages.Should().BeEmpty();
    }

    [Theory]
    [InlineData(EventSubscriptionScope.Distributed)]
    [InlineData(EventSubscriptionScope.Local)]
    public async Task PublishRuntimePayloadAsync_WhenDeliveryFails_ShouldReturnFailureWithDiagnostic(
        EventSubscriptionScope deliveryScope)
    {
        var failure = new InvalidOperationException("runtime delivery rejected");
        var factory = new EventBusUITestApplicationFactory();
        if (deliveryScope == EventSubscriptionScope.Distributed) factory.DefaultTransport.Failure = failure;
        else factory.HandlerState.Failure = failure;
        await using var app = await factory.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        await using var scope = app.Services.CreateAsyncScope();
        var subscription = await SubscribeAsync(scope.ServiceProvider, deliveryScope);

        var result = await scope.ServiceProvider.GetRequiredService<EventBusTestService>().PublishRuntimePayloadAsync(
            subscription.Id, JSON_PAYLOAD, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ResStatus.BadRequest);
        result.Message.Should().Contain(failure.Message);
        factory.HandlerState.Deliveries.Should().BeEmpty();
        factory.KeyedTransport.Messages.Should().BeEmpty();
    }

    [Theory]
    [InlineData(EventSubscriptionScope.Distributed)]
    [InlineData(EventSubscriptionScope.Local)]
    public async Task PublishRuntimePayloadAsync_WhenCancelled_ShouldReturnFailureWithoutDelivery(
        EventSubscriptionScope deliveryScope)
    {
        var factory = new EventBusUITestApplicationFactory();
        await using var app = await factory.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        await using var scope = app.Services.CreateAsyncScope();
        var subscription = await SubscribeAsync(scope.ServiceProvider, deliveryScope);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var result = await scope.ServiceProvider.GetRequiredService<EventBusTestService>().PublishRuntimePayloadAsync(
            subscription.Id, JSON_PAYLOAD, cancellation.Token);

        result.Status.Should().Be(ResStatus.BadRequest);
        result.Message.Should().NotBeNullOrWhiteSpace();
        factory.DefaultTransport.Messages.Should().BeEmpty();
        factory.KeyedTransport.Messages.Should().BeEmpty();
        factory.HandlerState.Deliveries.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-json")]
    [InlineData("null")]
    public async Task PublishRuntimePayloadAsync_WhenJsonIsInvalid_ShouldReturnFailureWithoutSending(string json)
    {
        var factory = new EventBusUITestApplicationFactory();
        await using var app = await factory.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        await using var scope = app.Services.CreateAsyncScope();
        var subscription = await SubscribeAsync(scope.ServiceProvider, EventSubscriptionScope.Distributed);

        var result = await scope.ServiceProvider.GetRequiredService<EventBusTestService>().PublishRuntimePayloadAsync(
            subscription.Id, json, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ResStatus.BadRequest);
        result.Message.Should().NotBeNullOrWhiteSpace();
        factory.DefaultTransport.Messages.Should().BeEmpty();
        factory.KeyedTransport.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task PublishRuntimePayloadAsync_WhenKeyedTransportIsMissing_ShouldNotFallBackToDefaultTransport()
    {
        var factory = new EventBusUITestApplicationFactory(registerKeyedTransport: false);
        await using var app = await factory.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        await using var scope = app.Services.CreateAsyncScope();
        var subscription = await SubscribeAsync(scope.ServiceProvider, EventSubscriptionScope.Distributed, TEST_BUS);

        var result = await scope.ServiceProvider.GetRequiredService<EventBusTestService>().PublishRuntimePayloadAsync(
            subscription.Id, JSON_PAYLOAD, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ResStatus.BadRequest);
        result.Message.Should().Contain(TEST_BUS);
        factory.DefaultTransport.Messages.Should().BeEmpty();
        factory.KeyedTransport.Messages.Should().BeEmpty();
    }

    private static Task<IEventSubscription> SubscribeAsync(IServiceProvider services,
        EventSubscriptionScope deliveryScope, string? serviceKey = null)
        => services.GetRequiredService<IEventSubscriptionRegistry>().SubscribeAsync(new EventSubscriptionDescriptor
        {
            EventType = typeof(RuntimePayload),
            TopicName = TEST_TOPIC,
            Scope = deliveryScope,
            ServiceKey = serviceKey,
            HandlerFactory = new IocEventHandlerFactory(
                services.GetRequiredService<IServiceScopeFactory>(), typeof(RuntimePayloadHandler))
        }, TestContext.Current.CancellationToken);

    private static void AssertMessage(EventMessage message, EventSubscriptionScope deliveryScope,
        string? serviceKey, IServiceProvider services)
    {
        message.Scope.Should().Be(deliveryScope);
        message.Metadata.EventName.Should().Be(EVENT_NAME);
        message.Metadata.TopicName.Should().Be(TEST_TOPIC);
        message.Metadata.ServiceKey.Should().Be(serviceKey);
        message.Metadata.TransportKey.Should().Be(typeof(RuntimePayload).FullName);
        message.Metadata.MessageId.Should().NotBeNullOrWhiteSpace();
        message.Metadata.Source.Should().NotBeNullOrWhiteSpace();
        var options = services.GetRequiredService<IJsonSerializerOptionsProvider>().SerializerOptions;
        JsonSerializer.Deserialize<RuntimePayload>(message.Body.Span, options)
            .Should().Be(new RuntimePayload("diagnostic payload"));
    }

    [Outbox]
    [EventName(EVENT_NAME)]
    public sealed record RuntimePayload(string Value);

    public enum RuntimeChangeKind
    {
        Created,
        Updated,
        Deleted
    }

    public sealed record RuntimeEntityChange<T>(RuntimeChangeKind Kind, T Entity, long Revision);

    public sealed record RuntimeFlightPlan(
        DateOnly? PlanDate, DateOnly RequiredDate, TimeOnly? OptionalTime, TimeOnly RequiredTime);

    private sealed class EventBusUITestApplicationFactory(
        bool registerKeyedTransport = true, bool useCustomDateTimeConverters = false)
        : MonicaTestApplicationFactory<EventBusTestService>
    {
        public TransportProbe DefaultTransport { get; } = new();
        public TransportProbe KeyedTransport { get; } = new();
        public HandlerProbe HandlerState { get; } = new();

        protected override void ConfigureHost(WebApplicationBuilder builder)
        {
            builder.Environment.ApplicationName = typeof(ModuleEventBusUI).Assembly.GetName().Name!;
        }

        protected override void ConfigureMonica(IMonicaBuilder builder)
        {
            builder.ConfigureModuleSystem(options =>
            {
                options.EnableMinimalApiByDefault = false;
                options.AutoAddMonicaHttpListener = false;
            });
            if (useCustomDateTimeConverters)
            {
                builder.AddJsonSerialization(options => options.ConfigureSerializer(serializerOptions =>
                {
                    serializerOptions.Converters.Add(new CompactDateOnlyConverter());
                    serializerOptions.Converters.Add(new CompactTimeOnlyConverter());
                }));
            }
            builder.AddEventBus(options => options.DisableAutoDiscovery = true)
                .UseNoOpDistributedEventBus()
                .AddKeyedLocalEventBus(TEST_BUS);
            builder.AddEventBusUI();
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            services.Replace(ServiceDescriptor.Singleton<IEventTransport>(DefaultTransport));
            if (registerKeyedTransport) services.AddKeyedSingleton<IEventTransport>(TEST_BUS, KeyedTransport);
            services.AddKeyedScoped<IDistributedEventBus>(TEST_BUS, (provider, _) =>
                new ScopedDistributedEventBusGateway(provider.GetRequiredService<RecordingEventBus>(),
                    provider.GetRequiredService<IEventMessageFactory>(), provider, TEST_BUS));
            services.AddSingleton(HandlerState);
            services.AddTransient<RuntimePayloadHandler>();
            services.AddTransient<RuntimeFlightPlanHandler>();
        }
    }

    private sealed class TransportProbe : IEventTransport
    {
        public List<EventMessage> Messages { get; } = [];
        public Exception? Failure { get; set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public Task SendAsync(EventMessage message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastCancellationToken = cancellationToken;
            if (Failure is { } error) throw error;
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    public sealed class HandlerProbe
    {
        public List<(RuntimePayload Payload, EventDeliveryMetadata Metadata)> Deliveries { get; } = [];
        public Exception? Failure { get; set; }
    }

    private sealed class CompactDateOnlyConverter : JsonConverter<DateOnly>
    {
        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => DateOnly.ParseExact(reader.GetString()!, "yyyyMMdd", CultureInfo.InvariantCulture);

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
    }

    private sealed class CompactTimeOnlyConverter : JsonConverter<TimeOnly>
    {
        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => TimeOnly.ParseExact(reader.GetString()!, "HHmmss", CultureInfo.InvariantCulture);

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString("HHmmss", CultureInfo.InvariantCulture));
    }

    public sealed class RuntimeFlightPlanHandler : IDistributedEventHandler<RuntimeEntityChange<RuntimeFlightPlan>>
    {
        public Task HandleEventAsync(RuntimeEntityChange<RuntimeFlightPlan> eventData, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    public sealed class RuntimePayloadHandler(HandlerProbe probe, IEventDeliveryContext delivery)
        : ILocalEventHandler<RuntimePayload>, IDistributedEventHandler<RuntimePayload>
    {
        public Task HandleEventAsync(RuntimePayload eventData, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (probe.Failure is { } error) throw error;
            probe.Deliveries.Add((eventData, delivery.Current
                ?? throw new InvalidOperationException("Test injection did not provide delivery metadata.")));
            return Task.CompletedTask;
        }
    }
}
