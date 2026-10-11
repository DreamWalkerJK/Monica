using AwesomeAssertions;
using Monica.DataChannel.Abstractions;
using Monica.DataChannel.Middlewares;
using Monica.DataChannel.Pipeline;
using Test.Monica.DataChannel.Support;
using Xunit;

namespace Test.Monica.DataChannel.Middlewares;

public sealed class MessageDebuggerMiddlewareTests
{
    [Fact]
    public async Task Capture_WhenNestedPayloadMetadataAndLegacyViewsChange_ShouldPreserveCaptureTimeEvidenceAfterTrimAndClear()
    {
        var debugger = new MessageDebuggerMiddleware { IsActive = true, MaxQueueSize = 2 };
        var scenario = new DataChannelScenario();
        scenario.Primary.Middlewares.Add(debugger);
        await using var application = await new DataChannelTestApplicationFactory(scenario).CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);
        var channel = scope.Resolve<IDataChannelManager>().Fetch(scenario.Primary.Id)!;
        var payload = new MutablePayload();
        var nested = new MutablePayload();
        var context = new ChannelDataContext(ChannelSide.Inner, payload) { CancellationToken = TestContext.Current.CancellationToken };
        var metadata = (IDictionary<string, object?>)context.Metadata;
        metadata["nested"] = nested;
        metadata["null"] = null;
        metadata["unreadable"] = new FailingPayload();
        await channel.Pipe.SendDataAsync(context);
        var selected = debugger.GetDebugMessageSnapshots().Single();
        selected.Sequence.Should().Be(1);
        selected.Timestamp.Kind.Should().Be(DateTimeKind.Utc);
        selected.Metadata["null"].Json.Should().Be("null");
        selected.Metadata["unreadable"].Json.Should().BeNull();
        selected.Metadata["unreadable"].CaptureError.Should().NotBeNullOrEmpty();
        payload.Content = "Mutated payload";
        nested.Content = "Mutated metadata";
        nested.Children.Clear();
        debugger.GetDebugMessages().Single().FormattedContent = "Mutated legacy view";
        await channel.SendDataFromOuterAsync("outer message", TestContext.Current.CancellationToken);
        await channel.SendDataFromInnerAsync("third message", TestContext.Current.CancellationToken);
        debugger.GetDebugMessageSnapshots().Select(message => message.Sequence).Should().Equal(2, 3);
        debugger.MaxQueueSize = 1;
        debugger.GetDebugMessageSnapshots().Single().Sequence.Should().Be(3);
        debugger.ClearDebugMessages();
        debugger.GetDebugMessageSnapshots().Should().BeEmpty();
        debugger.IsActive.Should().BeTrue();
        selected.Payload.Json.Should().Contain("Original evidence 完整");
        selected.Payload.Json.Should().NotContain("Mutated");
        selected.FormattedContent.Should().NotContain("Mutated legacy view");
        selected.Metadata["nested"].Json.Should().Contain("nested");
        selected.Metadata["nested"].Json.Should().NotContain("Mutated");
        await channel.SendDataFromOuterAsync("after clear", TestContext.Current.CancellationToken);
        debugger.GetDebugMessageSnapshots().Single().Sequence.Should().Be(4);
        debugger.GetDebugMessageSnapshots().Single().Source.Should().Be(ChannelSide.Outer);
    }

    [Fact]
    public async Task Capture_WhenFilteringPausedOrEndpointDispatchFails_ShouldObserveOnlyAdmittedPayloadsBeforeReceipt()
    {
        var debugger = new MessageDebuggerMiddleware { IsActive = true, FilterKeyword = "match" };
        var scenario = new DataChannelScenario();
        scenario.Primary.Middlewares.Add(debugger);
        await using var application = await new DataChannelTestApplicationFactory(scenario).CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);
        var channel = scope.Resolve<IDataChannelManager>().Fetch(scenario.Primary.Id)!;
        await channel.SendDataFromInnerAsync("MATCH inner", TestContext.Current.CancellationToken);
        await channel.SendDataFromOuterAsync("match outer", TestContext.Current.CancellationToken);
        await channel.SendDataFromInnerAsync("excluded", TestContext.Current.CancellationToken);
        debugger.IsActive = false;
        await channel.SendDataFromOuterAsync("match while stopped", TestContext.Current.CancellationToken);
        debugger.GetDebugMessageSnapshots().Select(message => message.Source).Should().Equal(ChannelSide.Inner, ChannelSide.Outer);
        scenario.Primary.Outer.Receipts.Should().HaveCount(2);
        scenario.Primary.Inner.Receipts.Should().HaveCount(2);

        debugger.IsActive = true;
        scenario.Primary.Outer.FailReceive = true;
        var failingSend = () => channel.SendDataFromInnerAsync("match but no receipt", TestContext.Current.CancellationToken);
        await failingSend.Should().ThrowAsync<InvalidOperationException>();
        debugger.GetDebugMessageSnapshots().Should().HaveCount(3, "debugger capture precedes endpoint dispatch");
        scenario.Primary.Outer.Receipts.Should().HaveCount(2);
        channel.Pipe.ObservableTracker.TotalExceptions.Should().Be(1);
    }
}
