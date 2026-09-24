using Monica.EventBus.Abstractions.Handlers;
using Monica.EventBus.Models.Internal;
using Xunit;

namespace Test.Monica.EventBus.Services;

public sealed class EventHandlerRegistrationTests
{
    [Fact]
    public void Discovery_ShouldIgnoreOpenHandlerTemplatesButAcceptClosedHandlers()
    {
        Assert.Empty(EventHandlerRegistration.CreateFromHandlerType(typeof(EnvelopeHandler<>)));
        var registration = Assert.Single(EventHandlerRegistration.CreateFromHandlerType(typeof(EnvelopeHandler<string>)));
        Assert.Equal(typeof(Envelope<string>), registration.EventType);
        Assert.True(registration.IsDistributed);
    }

    [Fact]
    public void Discovery_ShouldStillRejectAmbiguousClosedHandlers()
        => Assert.Throws<InvalidOperationException>(() =>
            EventHandlerRegistration.CreateFromHandlerType(typeof(AmbiguousHandler<string>)));

    private sealed record Envelope<T>(T Value);
    private sealed class EnvelopeHandler<T> : IDistributedEventHandler<Envelope<T>>
    {
        public Task HandleEventAsync(Envelope<T> eventData, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class AmbiguousHandler<T> : ILocalEventHandler<Envelope<T>>, IDistributedEventHandler<Envelope<T>>
    {
        public Task HandleEventAsync(Envelope<T> eventData, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
