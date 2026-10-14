using AwesomeAssertions;
using Microsoft.JSInterop;
using Monica.AI.UI.UIChat.Support;

namespace Test.Monica.AI.UI.UIChat.Support;

public sealed class ChatLayoutInteropSessionTests
{
    [Fact]
    public async Task Initialize_WhenDisposedDuringImport_ShouldRetireLateModuleExactlyOnce()
    {
        var import = new TaskCompletionSource<IJSObjectReference>(TaskCreationOptions.RunContinuationsAsynchronously);
        var module = new Reference();
        var session = new ChatLayoutInteropSession(new Runtime(import.Task), _ => Task.CompletedTask);
        var initialization = session.InitializeAsync(default, default);
        var disposal = session.DisposeAsync().AsTask();

        import.SetResult(module);
        await Task.WhenAll(initialization, disposal);
        await session.DisposeAsync();

        module.Invocations.Should().BeEmpty();
        module.DisposeCount.Should().Be(1);
    }

    [Fact]
    public async Task Initialize_WhenDisposedDuringControllerCreation_ShouldStopAndRetireLateController()
    {
        var creation = new TaskCompletionSource<IJSObjectReference>(TaskCreationOptions.RunContinuationsAsynchronously);
        var controller = new Reference();
        var module = new Reference { Creation = creation.Task };
        var session = new ChatLayoutInteropSession(new Runtime(Task.FromResult<IJSObjectReference>(module)), _ => Task.CompletedTask);
        var initialization = session.InitializeAsync(default, default);
        var disposal = session.DisposeAsync().AsTask();

        creation.SetResult(controller);
        await Task.WhenAll(initialization, disposal);
        await session.DisposeAsync();

        controller.Invocations.Should().Equal("shutdown");
        controller.DisposeCount.Should().Be(1);
        module.DisposeCount.Should().Be(1);
        Action callbackAccess = () => _ = module.Callback!.Value;
        callbackAccess.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task Dispose_WhenShutdownIsPending_ShouldKeepCallbackAliveUntilShutdownCompletes()
    {
        var shutdown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var controller = new Reference { Shutdown = shutdown.Task };
        var module = new Reference { Creation = Task.FromResult<IJSObjectReference>(controller) };
        var reports = 0;
        var session = new ChatLayoutInteropSession(new Runtime(Task.FromResult<IJSObjectReference>(module)), _ => { reports++; return Task.CompletedTask; });
        await session.InitializeAsync(default, default);

        var disposal = session.DisposeAsync().AsTask();
        module.Callback!.Value.Should().BeSameAs(session);
        controller.DisposeCount.Should().Be(0);
        await session.ReportLayoutAsync(1000, 800, 700);
        reports.Should().Be(0);
        shutdown.SetResult();
        await disposal;

        controller.DisposeCount.Should().Be(1);
        module.DisposeCount.Should().Be(1);
        Action callbackAccess = () => _ = module.Callback.Value;
        callbackAccess.Should().Throw<ObjectDisposedException>();
    }

    private sealed class Runtime(Task<IJSObjectReference> import) : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => ImportAsync<TValue>();
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => ImportAsync<TValue>();
        private async ValueTask<TValue> ImportAsync<TValue>() => (TValue)(object)await import;
    }

    private sealed class Reference : IJSObjectReference
    {
        internal Task<IJSObjectReference>? Creation { get; init; }
        internal Task? Shutdown { get; init; }
        internal DotNetObjectReference<ChatLayoutInteropSession>? Callback { get; private set; }
        internal int DisposeCount { get; private set; }
        internal List<string> Invocations { get; } = [];
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeCoreAsync<TValue>(identifier, args);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => InvokeCoreAsync<TValue>(identifier, args);
        private async ValueTask<TValue> InvokeCoreAsync<TValue>(string identifier, object?[]? args)
        {
            Invocations.Add(identifier);
            if (identifier == "createLayoutObserver")
            {
                Callback = (DotNetObjectReference<ChatLayoutInteropSession>)args![2]!;
                return (TValue)(object)await Creation!;
            }
            if (identifier == "shutdown" && Shutdown is not null) await Shutdown;
            return default!;
        }
        public ValueTask DisposeAsync() { DisposeCount++; return ValueTask.CompletedTask; }
    }
}
