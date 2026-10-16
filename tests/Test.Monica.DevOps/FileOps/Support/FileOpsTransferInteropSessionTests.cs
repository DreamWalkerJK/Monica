using Microsoft.JSInterop;
using Monica.DevOps.FileOps.Support;
using Xunit;

namespace Test.Monica.DevOps.FileOps.Support;

public sealed class FileOpsTransferInteropSessionTests
{
    private static readonly TimeSpan ObservationTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task DisposeAsync_WhenImportIsPending_ShouldDrainLateModuleAndSkipInvocationWithoutOwningDescriptorStream()
    {
        var import = Signal<IJSObjectReference>();
        var module = new ControlledFileOpsJsModule();
        var runtime = new ControlledFileOpsJsRuntime { Import = () => import.Task };
        await using var stream = new TrackingDownloadStream();
        var session = new FileOpsTransferInteropSession(runtime);
        var download = session.DownloadAsync(ControlledFileOpsServices.Descriptor("late.txt", stream));
        var retirement = session.DisposeAsync().AsTask();
        try
        {
            Assert.False(download.IsCompleted);
            Assert.False(retirement.IsCompleted);
            Assert.Same(retirement, session.DisposeAsync().AsTask());
            Assert.False(await session.DownloadAsync(ControlledFileOpsServices.Descriptor("rejected.txt", stream)));
            Assert.Equal(1, runtime.Imports);
            import.SetResult(module);
            await ObserveAsync(retirement);
            Assert.False(await ObserveAsync(download));
            Assert.Equal(0, module.Invocations);
            Assert.Equal(1, module.Releases);
            Assert.Equal(0, stream.DisposeCount);
            Assert.True(stream.CanRead);
        }
        finally
        {
            import.TrySetResult(module);
            await ObserveAsync(session.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenInvocationIsPending_ShouldJoinActualInvocationBeforeReleasingModule()
    {
        var invocation = Signal<bool>();
        var module = new ControlledFileOpsJsModule { Invoke = () => invocation.Task };
        var runtime = new ControlledFileOpsJsRuntime { Import = () => Task.FromResult<IJSObjectReference>(module) };
        await using var stream = new TrackingDownloadStream();
        var session = new FileOpsTransferInteropSession(runtime);
        var download = session.DownloadAsync(ControlledFileOpsServices.Descriptor("pending.txt", stream));
        var retirement = session.DisposeAsync().AsTask();
        try
        {
            Assert.Equal(1, module.Invocations);
            Assert.False(retirement.IsCompleted);
            Assert.Equal(0, module.Releases);
            Assert.Same(stream, Assert.IsType<DotNetStreamReference>(module.StreamReference).Stream);
            invocation.SetResult(true);
            await ObserveAsync(retirement);
            Assert.False(await ObserveAsync(download));
            Assert.Equal(1, module.Releases);
            Assert.True(stream.CanRead);
            Assert.Equal(0, stream.DisposeCount);
        }
        finally
        {
            invocation.TrySetResult(true);
            await ObserveAsync(session.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DownloadAsync_WhenJavascriptFails_ShouldPropagateFailureAndStillReleaseModuleAtRetirement()
    {
        var failure = new JSException("Browser transfer failed");
        var module = new ControlledFileOpsJsModule { Invoke = () => Task.FromException<bool>(failure) };
        var runtime = new ControlledFileOpsJsRuntime { Import = () => Task.FromResult<IJSObjectReference>(module) };
        await using var stream = new TrackingDownloadStream();
        var session = new FileOpsTransferInteropSession(runtime);

        var actual = await Assert.ThrowsAsync<JSException>(() => session.DownloadAsync(ControlledFileOpsServices.Descriptor("failure.txt", stream)));
        Assert.Same(failure, actual);
        Assert.True(stream.CanRead);
        await ObserveAsync(session.DisposeAsync().AsTask());
        await ObserveAsync(session.DisposeAsync().AsTask());
        Assert.Equal(1, module.Releases);
        Assert.Equal(0, stream.DisposeCount);
    }

    [Fact]
    public async Task DownloadAsync_WhenCircuitDisconnects_ShouldReturnFalseAndRetainCallerOwnedStream()
    {
        var module = new ControlledFileOpsJsModule
        {
            Invoke = () => Task.FromException<bool>(new JSDisconnectedException("Circuit disconnected"))
        };
        var runtime = new ControlledFileOpsJsRuntime { Import = () => Task.FromResult<IJSObjectReference>(module) };
        await using var stream = new TrackingDownloadStream();
        await using var session = new FileOpsTransferInteropSession(runtime);

        Assert.False(await session.DownloadAsync(ControlledFileOpsServices.Descriptor("disconnected.txt", stream)));
        Assert.True(stream.CanRead);
    }

    [Fact]
    public async Task DownloadAsync_WhenLiveImportIsCanceled_ShouldPropagateCancellation()
    {
        var runtime = new ControlledFileOpsJsRuntime
        {
            Import = () => Task.FromException<IJSObjectReference>(new OperationCanceledException("Live import timeout"))
        };
        await using var stream = new TrackingDownloadStream();
        await using var session = new FileOpsTransferInteropSession(runtime);

        await Assert.ThrowsAsync<OperationCanceledException>(() => session.DownloadAsync(ControlledFileOpsServices.Descriptor("timeout.txt", stream)));
        Assert.True(stream.CanRead);
    }

    [Fact]
    public async Task DisposeAsync_WhenLateImportIsCanceledWithoutOwnerCancellation_ShouldObserveFailureAndShareFaultedRetirement()
    {
        var import = Signal<IJSObjectReference>();
        var runtime = new ControlledFileOpsJsRuntime { Import = () => import.Task };
        await using var stream = new TrackingDownloadStream();
        var session = new FileOpsTransferInteropSession(runtime);
        var download = session.DownloadAsync(ControlledFileOpsServices.Descriptor("late-canceled.txt", stream));
        var retirement = session.DisposeAsync().AsTask();
        Assert.False(retirement.IsCompleted);
        Assert.Same(retirement, session.DisposeAsync().AsTask());
        import.SetException(new OperationCanceledException("The runtime canceled its import independently"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ObserveAsync(download));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ObserveAsync(retirement));
        Assert.Same(retirement, session.DisposeAsync().AsTask());
        Assert.False(await session.DownloadAsync(ControlledFileOpsServices.Descriptor("rejected.txt", stream)));
        Assert.Equal(1, runtime.Imports);
        Assert.True(stream.CanRead);
        Assert.Equal(0, stream.DisposeCount);
    }

    [Fact]
    public async Task DisposeAsync_WhenModuleReleaseIsPending_ShouldReserveOneRetirementUntilActualReleaseCompletes()
    {
        var release = Signal<bool>();
        var module = new ControlledFileOpsJsModule { Release = () => release.Task };
        var runtime = new ControlledFileOpsJsRuntime { Import = () => Task.FromResult<IJSObjectReference>(module) };
        await using var stream = new TrackingDownloadStream();
        var session = new FileOpsTransferInteropSession(runtime);
        Assert.True(await session.DownloadAsync(ControlledFileOpsServices.Descriptor("complete.txt", stream)));
        var retirement = session.DisposeAsync().AsTask();
        try
        {
            Assert.Equal(1, module.Releases);
            Assert.False(retirement.IsCompleted);
            Assert.Same(retirement, session.DisposeAsync().AsTask());
            release.SetResult(true);
            await ObserveAsync(retirement);
            Assert.Equal(1, module.Releases);
            Assert.True(stream.CanRead);
        }
        finally
        {
            release.TrySetResult(true);
            await ObserveAsync(session.DisposeAsync().AsTask());
        }
    }

    private static TaskCompletionSource<T> Signal<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static Task ObserveAsync(Task task) => task.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);
    private static Task<T> ObserveAsync<T>(Task<T> task) => task.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);
}
