using Monica.Configuration.UI.Support;
using MudBlazor;

namespace Test.Monica.Configuration.UI.Support;

/// <summary>Exercises native result mechanics without rendering or a browser.</summary>
public sealed class ConfigurationDialogSessionLifetimeTests
{
    [Fact]
    public async Task DisposeAsync_WhenProviderHasUnsubscribed_ShouldDismissActualNativeReferenceAfterClose()
    {
        var service = new DialogService();
        var reference = new DialogReference(Guid.NewGuid(), service);
        var closeCalls = 0;
        service.OnDialogCloseRequested += (_, _) => closeCalls++;
        var session = new ConfigurationDialogSession(() => Task.FromResult<IDialogReference>(reference));
        var opening = session.OpenAsync();
        Assert.False(reference.Result.IsCompleted);

        var retirement = session.DisposeAsync().AsTask();
        await ObserveAsync(retirement);

        Assert.Equal(1, closeCalls);
        Assert.True((await ObserveAsync(opening))!.Canceled);
        Assert.Same(retirement, session.DisposeAsync().AsTask());
    }

    [Fact]
    public async Task DisposeAsync_WhenNativeResultCompletesInline_ShouldJoinReservedPendingDispatch()
    {
        var boundary = new NativeBoundary();
        var dispatch = Signal<bool>();
        var session = new ConfigurationDialogSession(() => Task.FromResult<IDialogReference>(boundary.Reference),
            dispatch: action => { action(); return dispatch.Task; });
        var opening = session.OpenAsync();
        var retirement = session.DisposeAsync().AsTask();
        try
        {
            Assert.True(boundary.Reference.Result.IsCompleted);
            Assert.False(opening.IsCompleted);
            Assert.False(retirement.IsCompleted);
            dispatch.SetResult(true);
            await ObserveAsync(retirement);
            Assert.True((await ObserveAsync(opening))!.Canceled);
            Assert.Equal(1, boundary.CloseCalls);
        }
        finally
        {
            dispatch.TrySetResult(true);
            await ObserveAsync(session.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenInlineResultPrecedesDispatchFailure_ShouldDrainCapturedComponentAndReportFailure()
    {
        var boundary = new NativeBoundary();
        var component = new PendingInspector();
        boundary.Reference.InjectDialog(component);
        var dispatch = Signal<bool>();
        var session = new ConfigurationDialogSession(() => Task.FromResult<IDialogReference>(boundary.Reference),
            dispatch: action => { action(); return dispatch.Task; });
        var opening = session.OpenAsync();
        var retirement = session.DisposeAsync().AsTask();
        try
        {
            await ObserveAsync(component.Started.Task);
            dispatch.SetException(new InvalidOperationException("native dispatch tail"));
            Assert.False(opening.IsCompleted);
            Assert.False(retirement.IsCompleted);
            component.Drained.SetResult(true);
            var openFailure = await Assert.ThrowsAnyAsync<Exception>(() => ObserveAsync(opening));
            var retirementFailure = await Assert.ThrowsAnyAsync<Exception>(() => ObserveAsync(retirement));
            Assert.Contains("native dispatch tail", openFailure.ToString());
            Assert.Contains("native dispatch tail", retirementFailure.ToString());
            Assert.Equal(1, boundary.CloseCalls);
            Assert.Same(retirement, session.DisposeAsync().AsTask());
        }
        finally { component.Drained.TrySetResult(true); dispatch.TrySetResult(true); }
    }

    [Fact]
    public async Task OpenAsync_WhenNativeResultChangesLiveComponentReference_ShouldDrainPreviouslyCapturedComponent()
    {
        var boundary = new NativeBoundary();
        var component = new PendingInspector();
        boundary.Reference.InjectDialog(component);
        var session = new ConfigurationDialogSession(() => Task.FromResult<IDialogReference>(boundary.Reference));
        var opening = session.OpenAsync();
        try
        {
            // Simulates removal/replacement before a late property lookup. Acquisition already captured ownership.
            boundary.Reference.InjectDialog(new object());
            var result = DialogResult.Cancel();
            boundary.Reference.Dismiss(result);
            await ObserveAsync(component.Started.Task);
            Assert.False(opening.IsCompleted);
            var retirement = session.DisposeAsync().AsTask();
            Assert.False(retirement.IsCompleted);
            component.Drained.SetResult(true);
            Assert.Same(result, await ObserveAsync(opening));
            await ObserveAsync(retirement);
            Assert.Equal(0, boundary.CloseCalls);
        }
        finally
        {
            component.Drained.TrySetResult(true);
            await ObserveAsync(session.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenShowProducesLateReference_ShouldCloseAndDrainActualComponentBeforeCompleting()
    {
        var produced = Signal<IDialogReference>();
        var boundary = new NativeBoundary();
        var component = new PendingInspector();
        boundary.Reference.InjectDialog(component);
        var session = new ConfigurationDialogSession(() => produced.Task);
        var opening = session.OpenAsync();
        var retirement = session.DisposeAsync().AsTask();
        try
        {
            Assert.False(retirement.IsCompleted);
            produced.SetResult(boundary.Reference);
            await ObserveAsync(component.Started.Task);
            Assert.True(boundary.Reference.Result.IsCompleted);
            Assert.False(opening.IsCompleted);
            Assert.False(retirement.IsCompleted);
            component.Drained.SetResult(true);
            Assert.True((await ObserveAsync(opening))!.Canceled);
            await ObserveAsync(retirement);
            Assert.Equal(1, boundary.CloseCalls);
        }
        finally
        {
            produced.TrySetResult(boundary.Reference);
            component.Drained.TrySetResult(true);
            await ObserveAsync(session.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenActualResultIsApplied_ShouldRetainResultUntilComponentReconciliationDrains()
    {
        using var ownerCancellation = new CancellationTokenSource();
        var boundary = new NativeBoundary();
        var component = new PendingInspector();
        boundary.Reference.InjectDialog(component);
        var session = new ConfigurationDialogSession(() => Task.FromResult<IDialogReference>(boundary.Reference), ownerCancellation.Token);
        var opening = session.OpenAsync();
        var actual = DialogResult.Ok("actual persisted outcome");
        try
        {
            boundary.Reference.Dismiss(actual);
            await ObserveAsync(component.Started.Task);
            ownerCancellation.Cancel();
            var retirement = session.DisposeAsync().AsTask();
            Assert.False(opening.IsCompleted);
            Assert.False(retirement.IsCompleted);
            component.Drained.SetResult(true);
            Assert.Same(actual, await ObserveAsync(opening));
            await ObserveAsync(retirement);
            Assert.Equal(0, boundary.CloseCalls);
        }
        finally
        {
            component.Drained.TrySetResult(true);
            await ObserveAsync(session.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenParentOwnsNestedInspector_ShouldCloseChildBeforeJoiningItsActualRead()
    {
        var childBoundary = new NativeBoundary();
        var pendingRead = new PendingInspector();
        childBoundary.Reference.InjectDialog(pendingRead);
        var child = new ConfigurationDialogSession(() => Task.FromResult<IDialogReference>(childBoundary.Reference));
        var childOpening = child.OpenAsync();
        var parentBoundary = new NativeBoundary();
        var parentComponent = new NestedInspector(child);
        parentBoundary.Reference.InjectDialog(parentComponent);
        var parent = new ConfigurationDialogSession(() => Task.FromResult<IDialogReference>(parentBoundary.Reference));
        var parentOpening = parent.OpenAsync();
        var retirement = parent.DisposeAsync().AsTask();
        try
        {
            await ObserveAsync(pendingRead.Started.Task);
            Assert.Equal(1, parentBoundary.CloseCalls);
            Assert.Equal(1, childBoundary.CloseCalls);
            Assert.False(parentOpening.IsCompleted);
            Assert.False(childOpening.IsCompleted);
            Assert.False(retirement.IsCompleted);
            Assert.Same(retirement, parent.DisposeAsync().AsTask());
            pendingRead.Drained.SetResult(true);
            Assert.True((await ObserveAsync(childOpening))!.Canceled);
            Assert.True((await ObserveAsync(parentOpening))!.Canceled);
            await ObserveAsync(retirement);
            Assert.Same(parentComponent.DisposeAsync().AsTask(), parentComponent.DisposeAsync().AsTask());
        }
        finally
        {
            pendingRead.Drained.TrySetResult(true);
            await ObserveAsync(parent.DisposeAsync().AsTask());
            await ObserveAsync(child.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenCloseAndComponentFail_ShouldObserveBothWithoutSkippingComponent()
    {
        var service = new DialogService();
        var reference = new DialogReference(Guid.NewGuid(), service);
        var component = new PendingInspector();
        reference.InjectDialog(component);
        service.OnDialogCloseRequested += (_, _) => throw new InvalidOperationException("native close failure");
        var session = new ConfigurationDialogSession(() => Task.FromResult<IDialogReference>(reference));
        var opening = session.OpenAsync();
        var retirement = session.DisposeAsync().AsTask();
        try
        {
            await ObserveAsync(component.Started.Task);
            Assert.True(reference.Result.IsCompleted);
            Assert.False(retirement.IsCompleted);
            component.Drained.SetException(new ApplicationException("component drain failure"));
            var openFailure = await Assert.ThrowsAnyAsync<Exception>(() => ObserveAsync(opening));
            var retirementFailure = await Assert.ThrowsAnyAsync<Exception>(() => ObserveAsync(retirement));
            Assert.Contains("native close failure", openFailure.ToString());
            Assert.Contains("component drain failure", openFailure.ToString());
            Assert.Contains("component drain failure", retirementFailure.ToString());
        }
        finally { component.Drained.TrySetResult(true); }
    }

    private sealed class NativeBoundary
    {
        internal DialogReference Reference { get; }
        internal int CloseCalls { get; private set; }
        internal NativeBoundary()
        {
            var service = new DialogService();
            Reference = new(Guid.NewGuid(), service);
            // Uses the real native reference's inline completion mechanics without a rendering provider.
            service.OnDialogCloseRequested += (reference, result) => { CloseCalls++; reference.Dismiss(result); };
        }
    }

    private sealed class PendingInspector : IAsyncDisposable
    {
        internal TaskCompletionSource<bool> Started { get; } = Signal<bool>();
        internal TaskCompletionSource<bool> Drained { get; } = Signal<bool>();
        public ValueTask DisposeAsync() { Started.TrySetResult(true); return new(Drained.Task); }
    }

    private sealed class NestedInspector(ConfigurationDialogSession child) : IAsyncDisposable
    {
        private Task? _retirement;
        public ValueTask DisposeAsync() => new(_retirement ??= child.DisposeAsync().AsTask());
    }

    private static TaskCompletionSource<T> Signal<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static Task ObserveAsync(Task task) => task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    private static Task<T> ObserveAsync<T>(Task<T> task) => task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
}
