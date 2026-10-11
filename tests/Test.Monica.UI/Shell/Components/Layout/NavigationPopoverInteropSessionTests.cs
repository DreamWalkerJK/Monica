using AwesomeAssertions;
using Microsoft.JSInterop;
using Monica.UI.Shell.Components.Layout;
using Test.Monica.UI.UIModuleSystem;
using Xunit;

namespace Test.Monica.UI.Shell.Components.Layout;

public sealed class NavigationPopoverInteropSessionTests
{
    [Fact]
    public async Task DisposeAsync_WhenBrowserCancelsReferenceCleanup_ShouldReleaseEveryOwner()
    {
        DotNetObjectReference<NavigationPopover>? callback = null;
        var observer = new ControlledJsReference(
            (identifier, _) => identifier == "dispose"
                ? ValueTask.FromException<object?>(new TaskCanceledException())
                : ValueTask.FromResult<object?>(null),
            () => ValueTask.FromException(new TaskCanceledException()));
        var module = new ControlledJsReference(
            (_, arguments) =>
            {
                callback = (DotNetObjectReference<NavigationPopover>)arguments![^1]!;
                return ValueTask.FromResult<object?>(observer);
            },
            () => ValueTask.FromException(new TaskCanceledException()));
        var runtime = new ControlledJsRuntime(() => ValueTask.FromResult<IJSObjectReference>(module));
        var session = new NavigationPopoverInteropSession(runtime, new NavigationPopover());
        await SynchronizeAsync(session);

        await session.DisposeAsync();
        await session.DisposeAsync();
        await SynchronizeAsync(session);

        observer.InvocationIdentifiers.Should().Equal("synchronize", "dispose");
        observer.DisposeCalls.Should().Be(1);
        module.DisposeCalls.Should().Be(1);
        runtime.ImportCalls.Should().Be(1);
        AssertCallbackDisposed(callback!);
    }

    [Fact]
    public async Task SynchronizeAsync_WhenLiveBrowserCallIsCanceled_ShouldPreserveCancellation()
    {
        var observer = new ControlledJsReference((identifier, _) => identifier == "synchronize"
            ? ValueTask.FromException<object?>(new TaskCanceledException())
            : ValueTask.FromResult<object?>(null));
        var module = new ControlledJsReference((_, _) => ValueTask.FromResult<object?>(observer));
        var runtime = new ControlledJsRuntime(() => ValueTask.FromResult<IJSObjectReference>(module));
        var session = new NavigationPopoverInteropSession(runtime, new NavigationPopover());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SynchronizeAsync(session));
        observer.DisposeCalls.Should().Be(0);
        await session.DisposeAsync();
        observer.DisposeCalls.Should().Be(1);
        module.DisposeCalls.Should().Be(1);
    }

    [Fact]
    public async Task DisposeAsync_WhenObserverArrivesLate_ShouldDrainCallbacksAndShareCompletion()
    {
        var factoryStarted = NewCompletion<bool>();
        var factoryCompletion = NewCompletion<object?>();
        var drainStarted = NewCompletion<bool>();
        var drainCompletion = NewCompletion<object?>();
        DotNetObjectReference<NavigationPopover>? callback = null;
        var observer = new ControlledJsReference((identifier, _) =>
        {
            identifier.Should().Be("dispose");
            drainStarted.SetResult(true);
            return new ValueTask<object?>(drainCompletion.Task);
        });
        var module = new ControlledJsReference((_, arguments) =>
        {
            callback = (DotNetObjectReference<NavigationPopover>)arguments![^1]!;
            factoryStarted.SetResult(true);
            return new ValueTask<object?>(factoryCompletion.Task);
        });
        var runtime = new ControlledJsRuntime(() => ValueTask.FromResult<IJSObjectReference>(module));
        var session = new NavigationPopoverInteropSession(runtime, new NavigationPopover());

        var synchronization = SynchronizeAsync(session);
        await factoryStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var disposal = session.DisposeAsync().AsTask();
        var repeatedDisposal = session.DisposeAsync().AsTask();
        factoryCompletion.SetResult(observer);
        await drainStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        disposal.IsCompleted.Should().BeFalse();
        repeatedDisposal.IsCompleted.Should().BeFalse();
        callback!.Value.Should().NotBeNull();
        observer.DisposeCalls.Should().Be(0);
        drainCompletion.SetResult(null);

        await Task.WhenAll(synchronization, disposal, repeatedDisposal)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        observer.InvocationIdentifiers.Should().Equal("dispose");
        observer.DisposeCalls.Should().Be(1);
        module.DisposeCalls.Should().Be(1);
        AssertCallbackDisposed(callback);
    }

    [Fact]
    public async Task DisposeAsync_WhenObserverCleanupFails_ShouldContinueCleanupAndPreserveFailure()
    {
        var failure = new InvalidOperationException("Observer callback drain failed.");
        DotNetObjectReference<NavigationPopover>? callback = null;
        var observer = new ControlledJsReference((identifier, _) => identifier == "dispose"
            ? ValueTask.FromException<object?>(failure)
            : ValueTask.FromResult<object?>(null));
        var module = new ControlledJsReference((_, arguments) =>
        {
            callback = (DotNetObjectReference<NavigationPopover>)arguments![^1]!;
            return ValueTask.FromResult<object?>(observer);
        });
        var runtime = new ControlledJsRuntime(() => ValueTask.FromResult<IJSObjectReference>(module));
        var session = new NavigationPopoverInteropSession(runtime, new NavigationPopover());
        await SynchronizeAsync(session);

        var observed = await Assert.ThrowsAsync<InvalidOperationException>(() => session.DisposeAsync().AsTask());
        var repeated = await Assert.ThrowsAsync<InvalidOperationException>(() => session.DisposeAsync().AsTask());

        observed.Should().BeSameAs(failure);
        repeated.Should().BeSameAs(failure);
        observer.DisposeCalls.Should().Be(1);
        module.DisposeCalls.Should().Be(1);
        AssertCallbackDisposed(callback!);
    }

    [Fact]
    public async Task SynchronizeAsync_WhenFactoryAndReferenceCleanupFail_ShouldPreserveBothAndReleaseAdmission()
    {
        var factoryFailure = new InvalidOperationException("Observer factory failed.");
        var cleanupFailure = new InvalidOperationException("Module release failed.");
        DotNetObjectReference<NavigationPopover>? callback = null;
        var module = new ControlledJsReference((_, arguments) =>
        {
            callback = (DotNetObjectReference<NavigationPopover>)arguments![^1]!;
            return ValueTask.FromException<object?>(factoryFailure);
        }, () => ValueTask.FromException(cleanupFailure));
        var runtime = new ControlledJsRuntime(() => ValueTask.FromResult<IJSObjectReference>(module));
        var session = new NavigationPopoverInteropSession(runtime, new NavigationPopover());

        var observed = await Assert.ThrowsAsync<AggregateException>(() => SynchronizeAsync(session));
        observed.InnerExceptions.Should().Contain(factoryFailure).And.Contain(cleanupFailure);
        await session.DisposeAsync().AsTask()
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        module.DisposeCalls.Should().Be(1);
        AssertCallbackDisposed(callback!);
    }

    private static Task SynchronizeAsync(NavigationPopoverInteropSession session) =>
        session.SynchronizeAsync(default, "test-navigation", true, false, false, false, false, 0);

    private static TaskCompletionSource<T> NewCompletion<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void AssertCallbackDisposed(DotNetObjectReference<NavigationPopover> callback)
    {
        Action read = () => _ = callback.Value;
        read.Should().Throw<ObjectDisposedException>();
    }
}
