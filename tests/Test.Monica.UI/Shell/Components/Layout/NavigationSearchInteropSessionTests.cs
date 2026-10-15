using AwesomeAssertions;
using Microsoft.JSInterop;
using Monica.UI.Shell.Components.Layout;
using Test.Monica.UI.UIModuleSystem;
using Xunit;

namespace Test.Monica.UI.Shell.Components.Layout;

public sealed class NavigationSearchInteropSessionTests
{
    [Fact]
    public async Task DisposeAsync_WhenPendingImportIsCanceled_ShouldCompleteRetirement()
    {
        var importStarted = NewCompletion<bool>();
        var importCompletion = NewCompletion<IJSObjectReference>();
        var runtime = new ControlledJsRuntime(() =>
        {
            importStarted.SetResult(true);
            return new ValueTask<IJSObjectReference>(importCompletion.Task);
        });
        var session = new NavigationSearchInteropSession<object>(runtime, new object());

        var initialization = session.InitializeAsync("observeShortcut");
        await importStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        var disposal = session.DisposeAsync().AsTask();
        importCompletion.SetCanceled(TestContext.Current.CancellationToken);

        await Task.WhenAll(initialization, disposal).WaitAsync(TestContext.Current.CancellationToken);
        await session.DisposeAsync();
        await session.InitializeAsync("observeShortcut");
        runtime.ImportCalls.Should().Be(1);
    }

    [Fact]
    public async Task InitializeAsync_WhenBrowserCallIsCanceledWhileLive_ShouldPropagateCancellation()
    {
        var target = new object();
        DotNetObjectReference<object>? callback = null;
        var factoryCompletion = NewCompletion<object?>();
        var module = new ControlledJsReference((_, arguments) =>
        {
            callback = (DotNetObjectReference<object>)arguments![^1]!;
            return new ValueTask<object?>(factoryCompletion.Task);
        });
        var runtime = new ControlledJsRuntime(() => ValueTask.FromResult<IJSObjectReference>(module));
        var session = new NavigationSearchInteropSession<object>(runtime, target);

        var initialization = session.InitializeAsync("observeShortcut");
        factoryCompletion.SetCanceled(TestContext.Current.CancellationToken);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => initialization);
        callback.Should().NotBeNull();
        callback!.Value.Should().BeSameAs(target);
        await session.DisposeAsync();
        module.DisposeCalls.Should().Be(1);
        AssertCallbackDisposed(callback);
    }

    [Fact]
    public async Task DisposeAsync_WhenObserverArrivesLate_ShouldDrainCallbacksBeforeReleasingReferences()
    {
        var target = new object();
        DotNetObjectReference<object>? callback = null;
        var factoryStarted = NewCompletion<bool>();
        var factoryCompletion = NewCompletion<object?>();
        var drainStarted = NewCompletion<bool>();
        var drainCompletion = NewCompletion<object?>();
        var observer = new ControlledJsReference((identifier, _) =>
        {
            identifier.Should().Be("dispose");
            drainStarted.SetResult(true);
            return new ValueTask<object?>(drainCompletion.Task);
        });
        var module = new ControlledJsReference((_, arguments) =>
        {
            callback = (DotNetObjectReference<object>)arguments![^1]!;
            factoryStarted.SetResult(true);
            return new ValueTask<object?>(factoryCompletion.Task);
        });
        var runtime = new ControlledJsRuntime(() => ValueTask.FromResult<IJSObjectReference>(module));
        var session = new NavigationSearchInteropSession<object>(runtime, target);

        var initialization = session.InitializeAsync("observeShortcut");
        await factoryStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        var disposal = session.DisposeAsync().AsTask();
        factoryCompletion.SetResult(observer);
        await drainStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        disposal.IsCompleted.Should().BeFalse();
        callback!.Value.Should().BeSameAs(target);
        observer.DisposeCalls.Should().Be(0);
        module.DisposeCalls.Should().Be(0);
        drainCompletion.SetResult(null);
        await Task.WhenAll(initialization, disposal).WaitAsync(TestContext.Current.CancellationToken);
        await session.DisposeAsync();

        observer.InvocationIdentifiers.Should().Equal("dispose");
        observer.DisposeCalls.Should().Be(1);
        module.DisposeCalls.Should().Be(1);
        AssertCallbackDisposed(callback);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisposeAsync_WhenSelectionCallIsAdmitted_ShouldDrainItBeforeStoppingObserver(bool canceled)
    {
        DotNetObjectReference<object>? callback = null;
        var selectionStarted = NewCompletion<bool>();
        var selectionCompletion = NewCompletion<object?>();
        var observer = new ControlledJsReference();
        var module = new ControlledJsReference((identifier, arguments) =>
        {
            if (identifier == "observeShortcut")
            {
                callback = (DotNetObjectReference<object>)arguments![^1]!;
                return ValueTask.FromResult<object?>(observer);
            }

            identifier.Should().Be("scrollSelection");
            selectionStarted.SetResult(true);
            return new ValueTask<object?>(selectionCompletion.Task);
        });
        var runtime = new ControlledJsRuntime(() => ValueTask.FromResult<IJSObjectReference>(module));
        var session = new NavigationSearchInteropSession<object>(runtime, new object());
        await session.InitializeAsync("observeShortcut");

        var selection = session.ScrollSelectionAsync(default, 1).AsTask();
        await selectionStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        var disposal = session.DisposeAsync().AsTask();

        disposal.IsCompleted.Should().BeFalse();
        observer.InvocationIdentifiers.Should().BeEmpty();
        callback!.Value.Should().NotBeNull();
        if (canceled) selectionCompletion.SetCanceled(TestContext.Current.CancellationToken);
        else selectionCompletion.SetResult(null);
        await Task.WhenAll(selection, disposal).WaitAsync(TestContext.Current.CancellationToken);
        await session.ScrollSelectionAsync(default, 2);

        module.InvocationIdentifiers.Should().Equal("observeShortcut", "scrollSelection");
        observer.InvocationIdentifiers.Should().Equal("dispose");
        module.DisposeCalls.Should().Be(1);
        AssertCallbackDisposed(callback);
    }

    [Fact]
    public async Task DisposeAsync_WhenBrowserCancelsReferenceCleanup_ShouldReleaseCallbackAndComplete()
    {
        DotNetObjectReference<object>? callback = null;
        var observer = new ControlledJsReference(
            invoke: (_, _) => ValueTask.FromException<object?>(new TaskCanceledException()),
            dispose: () => ValueTask.FromException(new TaskCanceledException()));
        var module = new ControlledJsReference(
            (identifier, arguments) =>
            {
                identifier.Should().Be("observeShortcut");
                callback = (DotNetObjectReference<object>)arguments![^1]!;
                return ValueTask.FromResult<object?>(observer);
            },
            () => ValueTask.FromException(new TaskCanceledException()));
        var runtime = new ControlledJsRuntime(() => ValueTask.FromResult<IJSObjectReference>(module));
        var session = new NavigationSearchInteropSession<object>(runtime, new object());
        await session.InitializeAsync("observeShortcut");

        await session.DisposeAsync();

        observer.InvocationIdentifiers.Should().Equal("dispose");
        observer.DisposeCalls.Should().Be(1);
        module.DisposeCalls.Should().Be(1);
        AssertCallbackDisposed(callback!);
    }

    [Fact]
    public async Task DisposeAsync_WhenInitializationFailsUnexpectedly_ShouldReleaseOwnershipAndPreserveFailure()
    {
        DotNetObjectReference<object>? callback = null;
        var failure = new InvalidOperationException("Observer factory failed.");
        var module = new ControlledJsReference((_, arguments) =>
        {
            callback = (DotNetObjectReference<object>)arguments![^1]!;
            return ValueTask.FromException<object?>(failure);
        });
        var runtime = new ControlledJsRuntime(() => ValueTask.FromResult<IJSObjectReference>(module));
        var session = new NavigationSearchInteropSession<object>(runtime, new object());

        var initializationFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => session.InitializeAsync("observeShortcut"));
        var disposalFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => session.DisposeAsync().AsTask());

        initializationFailure.Should().BeSameAs(failure);
        disposalFailure.Should().BeSameAs(failure);
        module.DisposeCalls.Should().Be(1);
        AssertCallbackDisposed(callback!);
    }

    private static TaskCompletionSource<T> NewCompletion<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void AssertCallbackDisposed(DotNetObjectReference<object> callback)
    {
        Action readCallback = () => _ = callback.Value;
        readCallback.Should().Throw<ObjectDisposedException>();
    }
}
