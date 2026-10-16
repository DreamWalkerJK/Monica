using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Monica.DevOps.Terminal.Support;
using Xunit;

namespace Test.Monica.DevOps.Terminal.Support;

public sealed class TerminalScrollInteropSessionTests
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task RetirementJoinsLateImportAndReleasesItsModuleWithoutCreatingController()
    {
        var import = Signal<IJSObjectReference>();
        var module = new ControlledReference();
        var runtime = new ControlledRuntime { Import = () => import.Task };
        var session = new TerminalScrollInteropSession(runtime);
        var update = session.UpdateAsync(default);
        var retirement = session.DisposeAsync().AsTask();
        try
        {
            Assert.False(retirement.IsCompleted);
            Assert.False(update.IsCompleted);
            Assert.Same(retirement, session.DisposeAsync().AsTask());
            Assert.False(await session.UpdateAsync(default));
            import.SetResult(module);
            await retirement.WaitAsync(Deadline, TestContext.Current.CancellationToken);
            Assert.False(await update.WaitAsync(Deadline, TestContext.Current.CancellationToken));
            Assert.Equal(0, module.Creates);
            Assert.Equal(1, module.Releases);
        }
        finally { import.TrySetResult(module); await session.DisposeAsync().AsTask().WaitAsync(Deadline, TestContext.Current.CancellationToken); }
    }

    [Fact]
    public async Task RetirementJoinsLateControllerThenShutsItDownBeforeReleasingBothReferences()
    {
        var factory = Signal<IJSObjectReference>();
        var controller = new ControlledReference();
        var module = new ControlledReference { Create = () => factory.Task };
        var session = new TerminalScrollInteropSession(new ControlledRuntime { Import = () => Task.FromResult<IJSObjectReference>(module) });
        var update = session.UpdateAsync(default);
        var retirement = session.DisposeAsync().AsTask();
        try
        {
            Assert.False(retirement.IsCompleted);
            Assert.Equal(0, controller.Releases);
            factory.SetResult(controller);
            await retirement.WaitAsync(Deadline, TestContext.Current.CancellationToken);
            Assert.False(await update.WaitAsync(Deadline, TestContext.Current.CancellationToken));
            Assert.Equal(0, controller.Updates);
            Assert.Equal(1, controller.Shutdowns);
            Assert.Equal(1, controller.Releases);
            Assert.Equal(1, module.Releases);
        }
        finally { factory.TrySetResult(controller); await session.DisposeAsync().AsTask().WaitAsync(Deadline, TestContext.Current.CancellationToken); }
    }

    [Fact]
    public async Task PendingUseAndShutdownRemainOwnedUntilTheirActualCompletion()
    {
        var invocation = Signal<bool>();
        var shutdown = Signal<bool>();
        var shutdownStarted = Signal<bool>();
        var controller = new ControlledReference
        {
            Update = () => invocation.Task,
            Shutdown = () => { shutdownStarted.TrySetResult(true); return shutdown.Task; }
        };
        var module = Module(controller);
        var session = new TerminalScrollInteropSession(Runtime(module));
        var update = session.UpdateAsync(default, true);
        var retirement = session.DisposeAsync().AsTask();
        try
        {
            Assert.False(retirement.IsCompleted);
            Assert.Equal(0, controller.Shutdowns);
            invocation.SetResult(true);
            await shutdownStarted.Task.WaitAsync(Deadline, TestContext.Current.CancellationToken);
            Assert.False(retirement.IsCompleted);
            Assert.Equal(0, controller.Releases);
            Assert.Equal(0, module.Releases);
            shutdown.SetResult(true);
            await retirement.WaitAsync(Deadline, TestContext.Current.CancellationToken);
            Assert.False(await update.WaitAsync(Deadline, TestContext.Current.CancellationToken));
            Assert.Equal(1, controller.Releases);
            Assert.Equal(1, module.Releases);
            Assert.True(controller.LastReveal);
        }
        finally
        {
            invocation.TrySetResult(true); shutdown.TrySetResult(true);
            await session.DisposeAsync().AsTask().WaitAsync(Deadline, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task PendingRendererCallbackIsJoinedBeforeReleasingItsDotNetTarget()
    {
        var output = new ElementReference("transcript-output");
        var publication = Signal<bool>();
        var publicationStarted = Signal<bool>();
        var controller = new ControlledReference();
        var module = Module(controller);
        var callbacks = 0;
        var session = new TerminalScrollInteropSession(Runtime(module), async (_, _, _) =>
        {
            callbacks++;
            publicationStarted.TrySetResult(true);
            await publication.Task;
        });
        await session.UpdateAsync(output);
        Assert.Equal(output, module.OutputRoot);
        var callback = module.Receiver!.Value.ReceiveStateAsync(true, true, true);
        await publicationStarted.Task.WaitAsync(Deadline, TestContext.Current.CancellationToken);
        var retirement = session.DisposeAsync().AsTask();
        try
        {
            Assert.False(retirement.IsCompleted);
            Assert.Equal(0, controller.Shutdowns);
            Assert.Same(session, module.Receiver.Value);
            await session.ReceiveStateAsync(false, false, false);
            Assert.Equal(1, callbacks);
            publication.SetResult(true);
            await callback.WaitAsync(Deadline, TestContext.Current.CancellationToken);
            await retirement.WaitAsync(Deadline, TestContext.Current.CancellationToken);
            Assert.Throws<ObjectDisposedException>(() => module.Receiver.Value);
            Assert.Equal(1, controller.Shutdowns);
            Assert.Equal(1, module.Releases);
        }
        finally { publication.TrySetResult(true); await session.DisposeAsync().AsTask().WaitAsync(Deadline, TestContext.Current.CancellationToken); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ControllerCleanupFailureDoesNotSkipControllerOrModuleRelease(bool releaseFails)
    {
        var failure = new JSException("Unexpected live controller failure");
        var controller = new ControlledReference
        {
            Shutdown = () => releaseFails ? Task.CompletedTask : Task.FromException(failure),
            Release = () => releaseFails ? Task.FromException(failure) : Task.CompletedTask
        };
        var module = Module(controller);
        var session = new TerminalScrollInteropSession(Runtime(module));
        await session.UpdateAsync(default);
        var actual = await Assert.ThrowsAsync<JSException>(() => session.DisposeAsync().AsTask());
        Assert.Same(failure, actual);
        Assert.Equal(1, controller.Releases);
        Assert.Equal(1, module.Releases);
        await Assert.ThrowsAsync<JSException>(() => session.DisposeAsync().AsTask());
        Assert.Equal(1, controller.Shutdowns);
    }

    [Fact]
    public async Task LiveJavascriptFaultPropagatesWhileExpectedDisconnectDoesNot()
    {
        var fault = new JSException("Live browser failure");
        var controller = new ControlledReference { Update = () => Task.FromException<bool>(fault) };
        var module = Module(controller);
        await using var session = new TerminalScrollInteropSession(Runtime(module));
        Assert.Same(fault, await Assert.ThrowsAsync<JSException>(() => session.UpdateAsync(default)));
        controller.Update = () => Task.FromException<bool>(new JSDisconnectedException("Circuit gone"));
        Assert.False(await session.UpdateAsync(default));
        controller.Shutdown = () => Task.FromException(new JSDisconnectedException("Circuit gone"));
        await session.DisposeAsync();
        Assert.Equal(1, module.Releases);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ImportCancellationRemainsObservableWithoutAnOwnedCancellationReason(bool retire)
    {
        var import = Signal<IJSObjectReference>();
        var session = new TerminalScrollInteropSession(new ControlledRuntime { Import = () => import.Task });
        var update = session.UpdateAsync(default);
        var retirement = retire ? session.DisposeAsync().AsTask() : null;
        import.SetCanceled(TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => update.WaitAsync(Deadline, TestContext.Current.CancellationToken));
        if (retire)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => retirement!.WaitAsync(Deadline, TestContext.Current.CancellationToken));
            Assert.Same(retirement, session.DisposeAsync().AsTask());
        }
        else
        {
            await session.DisposeAsync();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateAsync_WhenControllerCreationFails_ShouldReuseAndDrainReceiverThroughRetirement(bool canceled)
    {
        var factory = Signal<IJSObjectReference>();
        var publication = Signal<bool>();
        var publicationStarted = Signal<bool>();
        var controller = new ControlledReference();
        var attempts = 0;
        var callbacks = 0;
        var module = new ControlledReference
        {
            Create = () => ++attempts == 1 ? factory.Task : Task.FromResult<IJSObjectReference>(controller)
        };
        var session = new TerminalScrollInteropSession(Runtime(module), async (_, _, _) =>
        {
            callbacks++;
            publicationStarted.TrySetResult(true);
            await publication.Task;
        });
        var failedUpdate = session.UpdateAsync(default);
        var receiver = Assert.IsType<DotNetObjectReference<TerminalScrollInteropSession>>(module.Receiver);
        try
        {
            if (canceled)
            {
                factory.SetCanceled(TestContext.Current.CancellationToken);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    failedUpdate.WaitAsync(Deadline, TestContext.Current.CancellationToken));
            }
            else
            {
                var failure = new JSException("Controlled controller creation failure");
                factory.SetException(failure);
                Assert.Same(failure, await Assert.ThrowsAsync<JSException>(() =>
                    failedUpdate.WaitAsync(Deadline, TestContext.Current.CancellationToken)));
            }
            Assert.Same(session, receiver.Value);
            Assert.Equal(0, module.Releases);
            Assert.Equal(0, controller.Updates);

            await session.UpdateAsync(default, true).WaitAsync(Deadline, TestContext.Current.CancellationToken);
            Assert.Same(receiver, module.Receiver);
            Assert.Equal(2, module.Creates);
            Assert.Equal(1, controller.Updates);
            Assert.True(controller.LastReveal);

            var callback = receiver.Value.ReceiveStateAsync(true, true, true);
            await publicationStarted.Task.WaitAsync(Deadline, TestContext.Current.CancellationToken);
            var retirement = session.DisposeAsync().AsTask();
            Assert.False(retirement.IsCompleted);
            Assert.Same(session, receiver.Value);
            Assert.Equal(0, controller.Shutdowns);
            await session.ReceiveStateAsync(false, false, false);
            Assert.Equal(1, callbacks);

            publication.SetResult(true);
            await callback.WaitAsync(Deadline, TestContext.Current.CancellationToken);
            await retirement.WaitAsync(Deadline, TestContext.Current.CancellationToken);
            Assert.Throws<ObjectDisposedException>(() => receiver.Value);
            Assert.Equal(1, controller.Shutdowns);
            Assert.Equal(1, controller.Releases);
            Assert.Equal(1, module.Releases);
            Assert.Same(retirement, session.DisposeAsync().AsTask());
        }
        finally
        {
            factory.TrySetResult(controller);
            publication.TrySetResult(true);
            await session.DisposeAsync().AsTask().WaitAsync(Deadline, TestContext.Current.CancellationToken);
        }
    }

    [Theory]
    [InlineData("create")]
    [InlineData("use")]
    [InlineData("shutdown")]
    public async Task LateUnownedCancellationPreservesFailureAndIndependentlyReleasesAcquiredReferences(string phase)
    {
        var factory = Signal<IJSObjectReference>();
        var use = Signal<bool>();
        var shutdown = Signal<bool>();
        var controller = new ControlledReference
        {
            Update = () => phase == "use" ? use.Task : Task.FromResult(false),
            Shutdown = () => phase == "shutdown" ? shutdown.Task : Task.CompletedTask
        };
        var module = new ControlledReference
        {
            Create = () => phase == "create" ? factory.Task : Task.FromResult<IJSObjectReference>(controller)
        };
        var session = new TerminalScrollInteropSession(Runtime(module));
        var update = session.UpdateAsync(default);
        if (phase == "shutdown") await update.WaitAsync(Deadline, TestContext.Current.CancellationToken);
        var retirement = session.DisposeAsync().AsTask();
        try
        {
            Assert.False(retirement.IsCompleted);
            Assert.Equal(0, module.Releases);
            Assert.Same(session, module.Receiver!.Value);
            if (phase == "create") factory.SetCanceled(TestContext.Current.CancellationToken);
            else if (phase == "use") use.SetCanceled(TestContext.Current.CancellationToken);
            else shutdown.SetCanceled(TestContext.Current.CancellationToken);
            if (phase != "shutdown")
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => update.WaitAsync(Deadline, TestContext.Current.CancellationToken));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => retirement.WaitAsync(Deadline, TestContext.Current.CancellationToken));
            Assert.Equal(1, module.Releases);
            Assert.Throws<ObjectDisposedException>(() => module.Receiver.Value);
            Assert.Equal(phase == "create" ? 0 : 1, controller.Releases);
            Assert.Equal(phase == "create" ? 0 : 1, controller.Shutdowns);
            Assert.Same(retirement, session.DisposeAsync().AsTask());
        }
        finally
        {
            factory.TrySetResult(controller); use.TrySetResult(false); shutdown.TrySetResult(true);
            try { await session.DisposeAsync().AsTask().WaitAsync(Deadline, TestContext.Current.CancellationToken); }
            catch (OperationCanceledException) { }
        }
    }

    private static TaskCompletionSource<T> Signal<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static ControlledReference Module(ControlledReference controller) => new() { Create = () => Task.FromResult<IJSObjectReference>(controller) };
    private static ControlledRuntime Runtime(ControlledReference module) => new() { Import = () => Task.FromResult<IJSObjectReference>(module) };

    private sealed class ControlledRuntime : IJSRuntime
    {
        internal Func<Task<IJSObjectReference>> Import { get; init; } = default!;
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, default, args);
        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Assert.Equal("import", identifier);
            Assert.Equal(TerminalScrollInteropSession.MODULE_PATH, args![0]);
            return (TValue)(object)await Import();
        }
    }

    private sealed class ControlledReference : IJSObjectReference
    {
        internal Func<Task<IJSObjectReference>> Create { get; init; } = default!;
        internal Func<Task<bool>> Update { get; set; } = () => Task.FromResult(false);
        internal Func<Task> Shutdown { get; set; } = () => Task.CompletedTask;
        internal Func<Task> Release { get; init; } = () => Task.CompletedTask;
        internal int Creates { get; private set; }
        internal int Updates { get; private set; }
        internal int Shutdowns { get; private set; }
        internal int Releases { get; private set; }
        internal bool LastReveal { get; private set; }
        internal ElementReference OutputRoot { get; private set; }
        internal DotNetObjectReference<TerminalScrollInteropSession>? Receiver { get; private set; }
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, default, args);
        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "createTerminalScroller")
            {
                Creates++;
                Assert.Equal(2, args!.Length);
                OutputRoot = Assert.IsType<ElementReference>(args[0]);
                Receiver = Assert.IsType<DotNetObjectReference<TerminalScrollInteropSession>>(args[1]);
                return (TValue)(object)await Create();
            }
            if (identifier == "update")
            {
                Updates++;
                LastReveal = Assert.IsType<bool>(args![0]);
                return (TValue)(object)await Update();
            }
            Assert.Equal("shutdown", identifier);
            Shutdowns++;
            await Shutdown();
            return default!;
        }
        public async ValueTask DisposeAsync() { Releases++; await Release(); }
    }
}
