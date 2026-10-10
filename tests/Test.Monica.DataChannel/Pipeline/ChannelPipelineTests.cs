using AwesomeAssertions;
using Monica.Core.Results;
using Monica.DataChannel.Abstractions;
using Monica.DataChannel.Facades;
using Monica.DataChannel.Middlewares;
using Monica.DataChannel.Models;
using Test.Monica.DataChannel.Support;
using Xunit;

namespace Test.Monica.DataChannel.Pipeline;

public sealed class ChannelPipelineTests
{
    [Fact]
    public async Task Reinitialize_WhenAnEndpointFailsThenRecoversAndFailsAgain_ShouldRetireEachActualAttemptAccurately()
    {
        var scenario = new DataChannelScenario();
        scenario.Primary.Outer.Initialization = _ => throw new InvalidOperationException("Initial endpoint failure.");
        await using var application = await new DataChannelTestApplicationFactory(scenario).CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);
        var facade = scope.Resolve<DataChannelFacade>();
        var failed = ResultAssertions.Success(await facade.GetChannelsStatusAsync(scope.CancellationToken)).Single();
        failed.IsInitialized.Should().BeFalse();
        failed.IsInitializing.Should().BeFalse();
        failed.IsNotAvailable.Should().BeTrue();
        failed.TotalExceptionCount.Should().Be(1);

        scenario.Primary.Outer.Initialization = _ => Task.CompletedTask;
        var recovered = await facade.ReInitializeChannelAsync(scenario.Primary.Id, scope.CancellationToken);
        recovered.Status.Should().Be(ResStatus.Ok);
        recovered.Message.Should().Contain("reinitialized successfully");
        var ready = ResultAssertions.Success(await facade.GetChannelsStatusAsync(scope.CancellationToken)).Single();
        ready.IsInitialized.Should().BeTrue();
        ready.IsInitializing.Should().BeFalse();
        ready.IsNotAvailable.Should().BeFalse();
        ready.TotalExceptionCount.Should().Be(1, "recovering does not clear retained or lifetime exceptions");

        scenario.Primary.Outer.Initialization = _ => throw new InvalidOperationException("Later endpoint failure.");
        var failedRetry = await facade.ReInitializeChannelAsync(scenario.Primary.Id, scope.CancellationToken);
        failedRetry.Status.Should().Be(ResStatus.BadRequest);
        failedRetry.Message.Should().Contain("Later endpoint failure");
        var after = ResultAssertions.Success(await facade.GetChannelsStatusAsync(scope.CancellationToken)).Single();
        after.IsInitialized.Should().BeFalse();
        after.IsInitializing.Should().BeFalse();
        after.IsNotAvailable.Should().BeTrue();
        after.TotalExceptionCount.Should().Be(2);
        scenario.Primary.Outer.InitializationCalls.Should().Be(3);
    }

    [Fact]
    public async Task Reinitialize_WhenAnotherAttemptIsPending_ShouldRejectConcurrentCallAndPropagateAdmittedCancellation()
    {
        var scenario = new DataChannelScenario();
        await using var application = await new DataChannelTestApplicationFactory(scenario).CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);
        var facade = scope.Resolve<DataChannelFacade>();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        scenario.Primary.Outer.Initialization = async token =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(token);
        };
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var pending = facade.ReInitializeChannelAsync(scenario.Primary.Id, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
            var initializing = ResultAssertions.Success(await facade.GetChannelsStatusAsync(scope.CancellationToken)).Single();
            initializing.IsInitializing.Should().BeTrue();
            initializing.IsInitialized.Should().BeFalse();
            var rejected = await facade.ReInitializeChannelAsync(scenario.Primary.Id, scope.CancellationToken);
            rejected.Status.Should().Be(ResStatus.BadRequest);
            rejected.Message.Should().Contain("already initializing");
            pending.IsCompleted.Should().BeFalse();
            scenario.Primary.Outer.InitializationCalls.Should().Be(2, "a rejected attempt never calls an endpoint");

            cancellation.Cancel();
            var canceled = () => pending;
            await canceled.Should().ThrowAsync<OperationCanceledException>();
            var retired = ResultAssertions.Success(await facade.GetChannelsStatusAsync(scope.CancellationToken)).Single();
            retired.IsInitializing.Should().BeFalse();
            retired.IsInitialized.Should().BeFalse();
            retired.IsNotAvailable.Should().BeTrue();
            scenario.Primary.Outer.Initialization = _ => Task.CompletedTask;
            (await facade.ReInitializeChannelAsync(scenario.Primary.Id, scope.CancellationToken)).Status.Should().Be(ResStatus.Ok);
        }
        finally
        {
            release.TrySetResult();
            cancellation.Cancel();
            try { await pending; }
            catch (OperationCanceledException) { }
        }
    }

    [Fact]
    public async Task Reinitialize_WhenATransientEndpointFailsStartup_ShouldRetryItsActualInitializationAndExposeDeclaredIdentity()
    {
        var scenario = new DataChannelScenario();
        scenario.Channels.Clear();
        var channel = new ScenarioChannel { TransientOuter = true, TransientDebugger = true };
        channel.Outer.Initialization = _ => throw new InvalidOperationException("Transient endpoint failed.");
        scenario.Channels.Add(channel);
        await using var application = await new DataChannelTestApplicationFactory(scenario).CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);
        var facade = scope.Resolve<DataChannelFacade>();
        var capture = ResultAssertions.Success(await facade.GetChannelsStatusAsync(scope.CancellationToken)).Single();
        capture.OuterEndpoint.IsTransient.Should().BeTrue();
        capture.OuterEndpoint.TypeName.Should().Contain(nameof(TransientScenarioOuterEndpoint));
        capture.OuterEndpoint.AssemblyQualifiedName.Should().Be(typeof(TransientScenarioOuterEndpoint).AssemblyQualifiedName);
        capture.OuterEndpoint.Metadata.CaptureError.Should().BeNull();
        capture.OuterEndpoint.Metadata.Json.Should().Contain(channel.Id);
        capture.OuterEndpoint.Metadata.Json.Should().Contain("Outer");
        var debugger = capture.Middlewares.Single();
        debugger.IsTransient.Should().BeTrue();
        debugger.CanDebugMessages.Should().BeFalse();
        debugger.Type.Should().Be(global::Monica.DataChannel.UIDataChannel.Models.PipelineComponentKind.InfoDisplayMiddleware);
        var transientAction = await facade.GetMiddlewareByKeyAsync<MessageDebuggerMiddleware>(channel.Id, debugger.ComponentKey);
        transientAction.Status.Should().Be(ResStatus.BadRequest);
        transientAction.Message.Should().Contain("unavailable");
        transientAction.Data.Should().BeNull();

        channel.Outer.Initialization = _ => Task.CompletedTask;
        var retry = await facade.ReInitializeChannelAsync(channel.Id, scope.CancellationToken);
        retry.Status.Should().Be(ResStatus.Ok);
        retry.Message.Should().Contain("reinitialized successfully");
        channel.Outer.InitializationCalls.Should().Be(2, "a failed transient initialization must not latch success");
        var after = ResultAssertions.Success(await facade.GetChannelsStatusAsync(scope.CancellationToken)).Single();
        after.IsInitialized.Should().BeTrue();
        after.IsNotAvailable.Should().BeFalse();
        after.TotalExceptionCount.Should().Be(1);
    }

    [Fact]
    public async Task Send_WhenTransformAndConfigurationStagesCoexist_ShouldKeepActualTransformOrderInBothDirections()
    {
        var scenario = new DataChannelScenario();
        scenario.Primary.Middlewares.AddRange([new AppendMiddleware("|first"), new ConfigurationMiddleware(), new AppendMiddleware("|second")]);
        await using var application = await new DataChannelTestApplicationFactory(scenario).CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);
        var channel = scope.Resolve<IDataChannelManager>().Fetch(scenario.Primary.Id)!;
        await channel.SendDataFromInnerAsync("inner");
        await channel.SendDataFromOuterAsync("outer");
        scenario.Primary.Outer.Receipts.Should().Equal("inner|first|second");
        scenario.Primary.Inner.Receipts.Should().Equal("outer|first|second");

        var components = ResultAssertions.Success(await scope.Resolve<DataChannelFacade>().GetChannelsStatusAsync(scope.CancellationToken)).Single().Middlewares;
        components.Select(component => component.Stage).Should().Equal(PipelineComponentStage.EndpointConfiguration,
            PipelineComponentStage.Transform, PipelineComponentStage.Transform);
        components.Select(component => component.Ordinal).Should().Equal(0, 0, 1);
        components[0].TypeName.Should().Contain(nameof(ConfigurationMiddleware));
    }
}
