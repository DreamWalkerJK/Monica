using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Monica.AI.UI.UIChat.Support;

namespace Test.Monica.AI.UI.UIChat.State;

public sealed class ChatInteropLifetimeTests
{
    [Fact]
    public async Task DisposeDuringImport_ShouldDisposeLateModuleWithoutCreatingController()
    {
        var import = new TaskCompletionSource<IJSObjectReference>(TaskCreationOptions.RunContinuationsAsynchronously);
        var module = new Reference();
        var session = new ChatMessageListInteropSession(new Runtime(import.Task));
        var initialize = session.InitializeAsync(default(ElementReference));
        var dispose = session.DisposeAsync().AsTask();

        import.SetResult(module);
        await Task.WhenAll(initialize, dispose);
        await session.DisposeAsync();

        module.DisposeCount.Should().Be(1);
        module.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task DisposeDuringScroll_ShouldDrainTheInvocationBeforeReleasingReferences()
    {
        var controller = new Reference();
        var module = new Reference { Child = controller };
        var session = new ChatMessageListInteropSession(new Runtime(Task.FromResult<IJSObjectReference>(module)));
        await session.InitializeAsync(default);
        controller.Block = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var scroll = session.ScrollToBottomAsync();
        var dispose = session.DisposeAsync().AsTask();
        controller.DisposeCount.Should().Be(0);
        controller.Block.SetResult();
        await Task.WhenAll(scroll, dispose);

        controller.Invocations.Should().Equal("scrollToBottom", "shutdown");
        controller.DisposeCount.Should().Be(1);
        module.DisposeCount.Should().Be(1);
    }

    [Fact]
    public async Task Trajectory_WhenControllerCreationFails_ShouldReleaseModuleAndSurfaceFailure()
    {
        var module = new Reference { FailInitialization = true };
        var session = new ChatTrajectoryInteropSession(new Runtime(Task.FromResult<IJSObjectReference>(module)));
        var initialize = () => session.InitializeAsync(default, default, default);

        await initialize.Should().ThrowAsync<JSException>();
        await session.DisposeAsync();
        await session.DisposeAsync();

        module.DisposeCount.Should().Be(1);
        module.Invocations.Should().Equal("initialize");
    }

    [Fact]
    public async Task Trajectory_DisposeDuringInterval_ShouldDrainAndRejectLaterOperations()
    {
        var controller = new Reference { Block = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously) };
        var module = new Reference { Child = controller };
        var session = new ChatTrajectoryInteropSession(new Runtime(Task.FromResult<IJSObjectReference>(module)));
        await session.InitializeAsync(default, default, default);
        var interval = session.IntervalAsync(10, 20);
        var dispose = session.DisposeAsync().AsTask();
        await session.FollowAsync();
        controller.DisposeCount.Should().Be(0);
        controller.Block.SetResult();
        await Task.WhenAll(interval, dispose);

        controller.Invocations.Should().Equal("interval", "shutdown");
        controller.DisposeCount.Should().Be(1);
        module.DisposeCount.Should().Be(1);
    }

    private sealed class Runtime(Task<IJSObjectReference> import) : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => ReadAsync<TValue>();
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => ReadAsync<TValue>();
        private async ValueTask<TValue> ReadAsync<TValue>() => (TValue)(object)await import;
    }

    private sealed class Reference : IJSObjectReference
    {
        public Reference? Child { get; init; }
        public bool FailInitialization { get; init; }
        public TaskCompletionSource? Block { get; set; }
        public int DisposeCount { get; private set; }
        public List<string> Invocations { get; } = [];
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeCoreAsync<TValue>(identifier);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => InvokeCoreAsync<TValue>(identifier);
        private async ValueTask<TValue> InvokeCoreAsync<TValue>(string identifier)
        {
            if (DisposeCount > 0) throw new InvalidOperationException("Invoked a disposed reference.");
            Invocations.Add(identifier);
            if (identifier == "initialize" && FailInitialization) throw new JSException("Controller initialization failed.");
            if (identifier is "scrollToBottom" or "interval" && Block is not null) await Block.Task;
            return Child is not null ? (TValue)(object)Child : default!;
        }
        public ValueTask DisposeAsync() { DisposeCount++; return ValueTask.CompletedTask; }
    }
}
