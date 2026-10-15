using AwesomeAssertions;
using Microsoft.JSInterop;
using Monica.UI.Shell.Components.Layout;
using Test.Monica.UI.UIModuleSystem;
using Xunit;

namespace Test.Monica.UI.Shell.Components.Layout;

public sealed class NavigationSearchInteropSessionTests
{
    [Fact]
    public async Task DisposeAsync_WhenPendingImportIsExternallyCanceled_ShouldPreserveCancellation()
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
        var externalCancellation = new CancellationToken(canceled: true);
        importCompletion.SetCanceled(externalCancellation);

        var initializationFailure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => initialization.WaitAsync(TestContext.Current.CancellationToken));
        var disposalFailure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => disposal.WaitAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => session.DisposeAsync().AsTask().WaitAsync(TestContext.Current.CancellationToken));
        await session.InitializeAsync("observeShortcut").WaitAsync(TestContext.Current.CancellationToken);

        initializationFailure.CancellationToken.Should().Be(externalCancellation);
        disposalFailure.CancellationToken.Should().Be(externalCancellation);
        initialization.IsCanceled.Should().BeTrue();
        disposal.IsCanceled.Should().BeTrue();
        runtime.ImportCalls.Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InitializeAsync_WhenFactoryIsExternallyCanceled_ShouldPropagateCancellation(bool retired)
    {
        var target = new object();
        DotNetObjectReference<object>? callback = null;
        var factoryStarted = NewCompletion<bool>();
        var factoryCompletion = NewCompletion<object?>();
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
        var disposal = retired ? session.DisposeAsync().AsTask() : null;
        var externalCancellation = new CancellationToken(canceled: true);
        factoryCompletion.SetCanceled(externalCancellation);

        var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => initialization.WaitAsync(TestContext.Current.CancellationToken));
        callback.Should().NotBeNull();
        if (!retired) callback!.Value.Should().BeSameAs(target);
        disposal ??= session.DisposeAsync().AsTask();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => disposal.WaitAsync(TestContext.Current.CancellationToken));

        failure.CancellationToken.Should().Be(externalCancellation);
        module.DisposeCalls.Should().Be(1);
        AssertCallbackDisposed(callback!);
    }

    [Fact]
    public async Task DisposeAsync_WhenObserverArrivesLate_ShouldDrainCallbacksBeforeReleasingReferences()
    {
        var target = new object();
        DotNetObjectReference<object>? callback = null;
        var importStarted = NewCompletion<bool>();
        var importCompletion = NewCompletion<IJSObjectReference>();
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
        var runtime = new ControlledJsRuntime(() =>
        {
            importStarted.SetResult(true);
            return new ValueTask<IJSObjectReference>(importCompletion.Task);
        });
        var session = new NavigationSearchInteropSession<object>(runtime, target);

        var initialization = session.InitializeAsync("observeShortcut");
        await importStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        var disposal = session.DisposeAsync().AsTask();
        importCompletion.SetResult(module);
        await factoryStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        disposal.IsCompleted.Should().BeFalse();
        callback!.Value.Should().BeSameAs(target);
        module.DisposeCalls.Should().Be(0);
        factoryCompletion.SetResult(observer);
        await drainStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        disposal.IsCompleted.Should().BeFalse();
        callback!.Value.Should().BeSameAs(target);
        observer.DisposeCalls.Should().Be(0);
        module.DisposeCalls.Should().Be(0);
        drainCompletion.SetResult(null);
        await Task.WhenAll(initialization, disposal).WaitAsync(TestContext.Current.CancellationToken);
        await session.DisposeAsync().AsTask().WaitAsync(TestContext.Current.CancellationToken);

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
        await session.InitializeAsync("observeShortcut").WaitAsync(TestContext.Current.CancellationToken);

        var selection = session.ScrollSelectionAsync(default, 1).AsTask();
        await selectionStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        var disposal = session.DisposeAsync().AsTask();

        disposal.IsCompleted.Should().BeFalse();
        observer.InvocationIdentifiers.Should().BeEmpty();
        callback!.Value.Should().NotBeNull();
        if (canceled)
        {
            selectionCompletion.SetCanceled(new CancellationToken(canceled: true));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => selection.WaitAsync(TestContext.Current.CancellationToken));
        }
        else
        {
            selectionCompletion.SetResult(null);
            await selection.WaitAsync(TestContext.Current.CancellationToken);
        }
        await disposal.WaitAsync(TestContext.Current.CancellationToken);
        await session.ScrollSelectionAsync(default, 2).AsTask().WaitAsync(TestContext.Current.CancellationToken);

        module.InvocationIdentifiers.Should().Equal("observeShortcut", "scrollSelection");
        observer.InvocationIdentifiers.Should().Equal("dispose");
        module.DisposeCalls.Should().Be(1);
        AssertCallbackDisposed(callback);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task DisposeAsync_WhenObserverCleanupIsExternallyCanceled_ShouldDrainAndPreserveCancellation(
        bool cancelDrain, bool cancelReference)
    {
        var target = new object();
        DotNetObjectReference<object>? callback = null;
        var drainStarted = NewCompletion<bool>();
        var drainCompletion = NewCompletion<object?>();
        var referenceStarted = NewCompletion<bool>();
        var referenceCompletion = NewCompletion<bool>();
        var observer = new ControlledJsReference(
            (_, _) =>
            {
                drainStarted.SetResult(true);
                return new ValueTask<object?>(drainCompletion.Task);
            },
            () =>
            {
                referenceStarted.SetResult(true);
                return new ValueTask(referenceCompletion.Task);
            });
        var module = new ControlledJsReference(
            (identifier, arguments) =>
            {
                identifier.Should().Be("observeShortcut");
                callback = (DotNetObjectReference<object>)arguments![^1]!;
                return ValueTask.FromResult<object?>(observer);
            });
        var runtime = new ControlledJsRuntime(() => ValueTask.FromResult<IJSObjectReference>(module));
        var session = new NavigationSearchInteropSession<object>(runtime, target);
        await session.InitializeAsync("observeShortcut").WaitAsync(TestContext.Current.CancellationToken);

        var disposal = session.DisposeAsync().AsTask();
        await drainStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        disposal.IsCompleted.Should().BeFalse();
        callback!.Value.Should().BeSameAs(target);
        observer.DisposeCalls.Should().Be(0);
        module.DisposeCalls.Should().Be(0);
        if (cancelDrain) drainCompletion.SetCanceled(new CancellationToken(canceled: true));
        else drainCompletion.SetResult(null);
        await referenceStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        disposal.IsCompleted.Should().BeFalse();
        callback.Value.Should().BeSameAs(target);
        module.DisposeCalls.Should().Be(0);
        if (cancelReference) referenceCompletion.SetCanceled(new CancellationToken(canceled: true));
        else referenceCompletion.SetResult(true);
        if (cancelDrain && cancelReference)
        {
            var failure = await Assert.ThrowsAsync<AggregateException>(
                () => disposal.WaitAsync(TestContext.Current.CancellationToken));
            failure.InnerExceptions.Should().HaveCount(2)
                .And.OnlyContain(exception => exception is OperationCanceledException);
        }
        else
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => disposal.WaitAsync(TestContext.Current.CancellationToken));
            disposal.IsCanceled.Should().BeTrue();
        }

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
            () => session.InitializeAsync("observeShortcut").WaitAsync(TestContext.Current.CancellationToken));
        var disposalFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => session.DisposeAsync().AsTask().WaitAsync(TestContext.Current.CancellationToken));

        initializationFailure.Should().BeSameAs(failure);
        disposalFailure.Should().BeSameAs(failure);
        module.DisposeCalls.Should().Be(1);
        AssertCallbackDisposed(callback!);
    }

    [Fact]
    public async Task DisposeAsync_WhenInitializationIsCanceledAndModuleCleanupFails_ShouldPreserveBoth()
    {
        DotNetObjectReference<object>? callback = null;
        var factoryStarted = NewCompletion<bool>();
        var factoryCompletion = NewCompletion<object?>();
        var releaseStarted = NewCompletion<bool>();
        var releaseCompletion = NewCompletion<bool>();
        var cleanupFailure = new InvalidOperationException("Module release failed.");
        var module = new ControlledJsReference((_, arguments) =>
        {
            callback = (DotNetObjectReference<object>)arguments![^1]!;
            factoryStarted.SetResult(true);
            return new ValueTask<object?>(factoryCompletion.Task);
        }, () =>
        {
            releaseStarted.SetResult(true);
            return new ValueTask(releaseCompletion.Task);
        });
        var runtime = new ControlledJsRuntime(() => ValueTask.FromResult<IJSObjectReference>(module));
        var session = new NavigationSearchInteropSession<object>(runtime, new object());

        var initialization = session.InitializeAsync("observeShortcut");
        await factoryStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        var disposal = session.DisposeAsync().AsTask();
        factoryCompletion.SetCanceled(new CancellationToken(canceled: true));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => initialization.WaitAsync(TestContext.Current.CancellationToken));
        await releaseStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        disposal.IsCompleted.Should().BeFalse();
        AssertCallbackDisposed(callback!);
        releaseCompletion.SetException(cleanupFailure);
        var observed = await Assert.ThrowsAsync<AggregateException>(
            () => disposal.WaitAsync(TestContext.Current.CancellationToken));

        observed.InnerExceptions.Should().HaveCount(2);
        observed.InnerExceptions[0].Should().BeAssignableTo<OperationCanceledException>();
        observed.InnerExceptions[1].Should().BeSameAs(cleanupFailure);
        module.DisposeCalls.Should().Be(1);
    }

    [Fact]
    public async Task DisposeAsync_WhenObserverAndModuleCleanupFail_ShouldPreserveEveryFailureAndReleaseCallback()
    {
        DotNetObjectReference<object>? callback = null;
        var drainFailure = new InvalidOperationException("Observer callback drain failed.");
        var referenceFailure = new ArgumentException("Observer reference release failed.");
        var moduleFailure = new InvalidOperationException("Module release failed.");
        var observer = new ControlledJsReference(
            (_, _) => ValueTask.FromException<object?>(drainFailure),
            () => ValueTask.FromException(referenceFailure));
        var module = new ControlledJsReference((_, arguments) =>
        {
            callback = (DotNetObjectReference<object>)arguments![^1]!;
            return ValueTask.FromResult<object?>(observer);
        }, () =>
        {
            AssertCallbackDisposed(callback!);
            return ValueTask.FromException(moduleFailure);
        });
        var runtime = new ControlledJsRuntime(() => ValueTask.FromResult<IJSObjectReference>(module));
        var session = new NavigationSearchInteropSession<object>(runtime, new object());
        await session.InitializeAsync("observeShortcut").WaitAsync(TestContext.Current.CancellationToken);

        var disposal = session.DisposeAsync().AsTask();
        var observed = await Assert.ThrowsAsync<AggregateException>(
            () => disposal.WaitAsync(TestContext.Current.CancellationToken));
        var repeated = await Assert.ThrowsAsync<AggregateException>(
            () => session.DisposeAsync().AsTask().WaitAsync(TestContext.Current.CancellationToken));

        observed.InnerExceptions.Should().Equal(drainFailure, referenceFailure, moduleFailure);
        repeated.Should().BeSameAs(observed);
        observer.InvocationIdentifiers.Should().Equal("dispose");
        observer.DisposeCalls.Should().Be(1);
        module.DisposeCalls.Should().Be(1);
        AssertCallbackDisposed(callback!);
    }

    [Fact]
    public async Task InitializeAsync_WhenFactoryRetiresSynchronously_ShouldRetainItsLateObserverUntilDrained()
    {
        var target = new object();
        DotNetObjectReference<object>? callback = null;
        NavigationSearchInteropSession<object>? session = null;
        Task? disposal = null;
        var factoryCompletion = NewCompletion<object?>();
        var observer = new ControlledJsReference();
        var module = new ControlledJsReference((_, arguments) =>
        {
            callback = (DotNetObjectReference<object>)arguments![^1]!;
            disposal = session!.DisposeAsync().AsTask();
            return new ValueTask<object?>(factoryCompletion.Task);
        });
        var runtime = new ControlledJsRuntime(() => ValueTask.FromResult<IJSObjectReference>(module));
        session = new NavigationSearchInteropSession<object>(runtime, target);

        var initialization = session.InitializeAsync("observeShortcut");

        disposal.Should().NotBeNull();
        disposal!.IsCompleted.Should().BeFalse();
        callback!.Value.Should().BeSameAs(target);
        module.DisposeCalls.Should().Be(0);
        factoryCompletion.SetResult(observer);
        await Task.WhenAll(initialization, disposal).WaitAsync(TestContext.Current.CancellationToken);

        observer.InvocationIdentifiers.Should().Equal("dispose");
        observer.DisposeCalls.Should().Be(1);
        module.DisposeCalls.Should().Be(1);
        AssertCallbackDisposed(callback);
    }

    [Fact]
    public async Task DisposeAsync_WhenObserverReentersRetirement_ShouldShareTheActualDrainCompletion()
    {
        DotNetObjectReference<object>? callback = null;
        NavigationSearchInteropSession<object>? session = null;
        Task? reentrantDisposal = null;
        var drainCompletion = NewCompletion<object?>();
        var observer = new ControlledJsReference((_, _) =>
        {
            reentrantDisposal = session!.DisposeAsync().AsTask();
            return new ValueTask<object?>(drainCompletion.Task);
        });
        var module = new ControlledJsReference((_, arguments) =>
        {
            callback = (DotNetObjectReference<object>)arguments![^1]!;
            return ValueTask.FromResult<object?>(observer);
        });
        var runtime = new ControlledJsRuntime(() => ValueTask.FromResult<IJSObjectReference>(module));
        session = new NavigationSearchInteropSession<object>(runtime, new object());
        await session.InitializeAsync("observeShortcut").WaitAsync(TestContext.Current.CancellationToken);

        var disposal = session.DisposeAsync().AsTask();

        reentrantDisposal.Should().BeSameAs(disposal);
        disposal.IsCompleted.Should().BeFalse();
        callback!.Value.Should().NotBeNull();
        observer.DisposeCalls.Should().Be(0);
        drainCompletion.SetResult(null);
        await disposal.WaitAsync(TestContext.Current.CancellationToken);

        observer.InvocationIdentifiers.Should().Equal("dispose");
        observer.DisposeCalls.Should().Be(1);
        module.DisposeCalls.Should().Be(1);
        AssertCallbackDisposed(callback);
    }

    [Theory]
    [InlineData("factory")]
    [InlineData("drain")]
    [InlineData("reference")]
    public async Task DisposeAsync_WhenBrowserDisconnects_ShouldPreserveDisconnectSemantics(string stage)
    {
        DotNetObjectReference<object>? callback = null;
        var observer = new ControlledJsReference(
            (_, _) => stage == "drain"
                ? ValueTask.FromException<object?>(new JSDisconnectedException("Browser disconnected."))
                : ValueTask.FromResult<object?>(null),
            () => stage == "reference"
                ? ValueTask.FromException(new JSDisconnectedException("Browser disconnected."))
                : ValueTask.CompletedTask);
        var module = new ControlledJsReference((_, arguments) =>
        {
            callback = (DotNetObjectReference<object>)arguments![^1]!;
            return stage == "factory"
                ? ValueTask.FromException<object?>(new JSDisconnectedException("Browser disconnected."))
                : ValueTask.FromResult<object?>(observer);
        });
        var runtime = new ControlledJsRuntime(() => ValueTask.FromResult<IJSObjectReference>(module));
        var session = new NavigationSearchInteropSession<object>(runtime, new object());

        await session.InitializeAsync("observeShortcut").WaitAsync(TestContext.Current.CancellationToken);
        await session.DisposeAsync().AsTask().WaitAsync(TestContext.Current.CancellationToken);

        observer.DisposeCalls.Should().Be(stage == "factory" ? 0 : 1);
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
