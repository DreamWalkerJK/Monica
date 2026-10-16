using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Monica.UI.Shell.Components.Layout;
using Monica.UI.Shell.Models;
using Monica.UI.Shell.Support;
using MudBlazor;
using Xunit;

namespace Test.Monica.UI.Shell.Components.Layout;

public sealed class NavigationSearchSessionTests
{
    [Fact]
    public async Task RequestAsync_WhenNativeResultPrecedesRendererDisposal_ShouldDrainCapturedChildBeforeExactNavigation()
    {
        var service = new RecordingDialogService();
        var reference = new RecordingReference(service);
        var releaseChild = NewCompletion();
        var child = new ControlledContent(() => releaseChild.Task);
        var lease = new NavigationSearchSession.DialogLease(reference, child);
        var shown = 0;
        var navigated = new List<NavigationSearchEntry>();
        var navigationCompleted = NewCompletion();
        await using var session = new NavigationSearchSession(() =>
        {
            shown++;
            return Task.FromResult(lease);
        }, DispatchImmediately, selected =>
        {
            navigated.Add(selected);
            navigationCompleted.SetResult();
        });
        var entry = Entry("operations/exact-destination");

        var first = session.RequestAsync();
        var repeated = session.RequestAsync();
        reference.InjectDialog(new object());
        reference.Dismiss(DialogResult.Ok(entry));
        await child.Started.WaitAsync(TestContext.Current.CancellationToken);
        var repeatedDuringDrain = session.RequestAsync();

        first.Should().BeSameAs(repeated).And.BeSameAs(repeatedDuringDrain);
        first.IsCompletedSuccessfully.Should().BeTrue();
        navigated.Should().BeEmpty();
        releaseChild.SetResult();
        await navigationCompleted.Task.WaitAsync(TestContext.Current.CancellationToken);

        shown.Should().Be(1);
        child.DisposeCalls.Should().Be(1);
        navigated.Should().ContainSingle().Which.Should().BeSameAs(entry);
        navigated[0].Item.Href.Should().Be("operations/exact-destination");
    }

    [Fact]
    public async Task RequestAsync_WhenOpeningIsHeld_ShouldShareItsAcknowledgementAndCompleteBeforeResultAndChildRetirement()
    {
        var show = NewCompletion<NavigationSearchSession.DialogLease>();
        var service = new RecordingDialogService();
        service.OnDialogCloseRequested += static (reference, result) => reference.Dismiss(result);
        var reference = new RecordingReference(service);
        var releaseChild = NewCompletion();
        var child = new ControlledContent(() => releaseChild.Task);
        var shown = 0;
        var session = new NavigationSearchSession(() =>
        {
            shown++;
            return show.Task;
        }, DispatchImmediately, _ => { });

        var first = session.RequestAsync();
        var repeatedWhileOpening = session.RequestAsync();
        first.Should().BeSameAs(repeatedWhileOpening);
        first.IsCompleted.Should().BeFalse();
        show.SetResult(new NavigationSearchSession.DialogLease(reference, child));
        await first.WaitAsync(TestContext.Current.CancellationToken);

        reference.Result.IsCompleted.Should().BeFalse();
        child.DisposeCalls.Should().Be(0);
        var repeatedWhileOpen = session.RequestAsync();
        repeatedWhileOpen.Should().BeSameAs(first);
        repeatedWhileOpen.IsCompletedSuccessfully.Should().BeTrue();
        shown.Should().Be(1);

        var disposal = session.DisposeAsync().AsTask();
        await child.Started.WaitAsync(TestContext.Current.CancellationToken);
        first.IsCompletedSuccessfully.Should().BeTrue();
        disposal.IsCompleted.Should().BeFalse();
        releaseChild.SetResult();
        await disposal.WaitAsync(TestContext.Current.CancellationToken);

        service.CloseCalls.Should().Be(1);
        child.DisposeCalls.Should().Be(1);
    }

    [Fact]
    public async Task RequestAsync_WhenPreviousDialogHasDrained_ShouldAllowAnotherOpening()
    {
        var service = new RecordingDialogService();
        var firstReference = new RecordingReference(service);
        var secondReference = new RecordingReference(service);
        var firstChild = new ControlledContent();
        var secondChild = new ControlledContent();
        var shown = 0;
        await using var session = new NavigationSearchSession(
            () => Task.FromResult(++shown == 1
                ? new NavigationSearchSession.DialogLease(firstReference, firstChild)
                : new NavigationSearchSession.DialogLease(secondReference, secondChild)),
            DispatchImmediately, _ => { });

        firstReference.Dismiss(DialogResult.Cancel());
        var first = session.RequestAsync();
        await first.WaitAsync(TestContext.Current.CancellationToken);
        secondReference.Dismiss(DialogResult.Cancel());
        var second = session.RequestAsync();
        await second.WaitAsync(TestContext.Current.CancellationToken);

        first.Should().NotBeSameAs(second);
        shown.Should().Be(2);
        firstChild.DisposeCalls.Should().Be(1);
        secondChild.DisposeCalls.Should().Be(1);
    }

    [Fact]
    public async Task DisposeAsync_WhenCustomCallbackIsPending_ShouldCoalesceAndDrainItsActualTaskWithoutOpeningNativeDialog()
    {
        var callback = NewCompletion();
        var customCalls = 0;
        var shown = 0;
        var session = new NavigationSearchSession(() =>
        {
            shown++;
            throw new InvalidOperationException("The host callback owns this request.");
        }, DispatchImmediately, _ => { });
        Task CustomSearch()
        {
            customCalls++;
            return callback.Task;
        }

        var first = session.RequestAsync(CustomSearch);
        var repeated = session.RequestAsync(CustomSearch);
        var disposal = session.DisposeAsync().AsTask();
        var retiredRequest = session.RequestAsync(CustomSearch);

        first.Should().BeSameAs(repeated);
        first.IsCompleted.Should().BeFalse();
        disposal.IsCompleted.Should().BeFalse();
        retiredRequest.IsCompletedSuccessfully.Should().BeTrue();
        callback.SetResult();
        await Task.WhenAll(first, disposal).WaitAsync(TestContext.Current.CancellationToken);

        customCalls.Should().Be(1);
        shown.Should().Be(0);
        session.DisposeAsync().AsTask().Should().BeSameAs(disposal);
    }

    [Fact]
    public async Task DisposeAsync_WhenCustomCallbackIsCanceled_ShouldPreserveItsCancellation()
    {
        var callback = NewCompletion();
        var cancellation = new CancellationToken(canceled: true);
        var session = new NavigationSearchSession(
            () => throw new InvalidOperationException("The host callback owns this request."),
            DispatchImmediately, _ => { });
        var request = session.RequestAsync(() => callback.Task);
        var disposal = session.DisposeAsync().AsTask();

        callback.SetCanceled(cancellation);
        var requestCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => request.WaitAsync(TestContext.Current.CancellationToken));
        var disposalCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => disposal.WaitAsync(TestContext.Current.CancellationToken));

        request.IsCanceled.Should().BeTrue();
        disposal.IsCanceled.Should().BeTrue();
        requestCancellation.CancellationToken.Should().Be(cancellation);
        disposalCancellation.CancellationToken.Should().Be(cancellation);
    }

    [Fact]
    public async Task RequestAsync_WhenHostReturnsToBuiltInSearch_ShouldUseTheCurrentRequestChoice()
    {
        var service = new RecordingDialogService();
        var reference = new RecordingReference(service);
        var child = new ControlledContent();
        var shown = 0;
        var customCalls = 0;
        await using var session = new NavigationSearchSession(() =>
        {
            shown++;
            return Task.FromResult(new NavigationSearchSession.DialogLease(reference, child));
        }, DispatchImmediately, _ => { });

        await session.RequestAsync(() =>
        {
            customCalls++;
            return Task.CompletedTask;
        });
        var builtIn = session.RequestAsync();
        reference.Dismiss(DialogResult.Cancel());
        await builtIn.WaitAsync(TestContext.Current.CancellationToken);
        await child.Started.WaitAsync(TestContext.Current.CancellationToken);

        customCalls.Should().Be(1);
        shown.Should().Be(1);
        child.DisposeCalls.Should().Be(1);
    }

    [Fact]
    public async Task DisposeAsync_WhenShowCompletesLate_ShouldCloseActualReferenceAndDrainItsCapturedChild()
    {
        var show = NewCompletion<NavigationSearchSession.DialogLease>();
        var releaseChild = NewCompletion();
        var service = new RecordingDialogService();
        service.OnDialogCloseRequested += static (reference, result) => reference.Dismiss(result);
        var reference = new RecordingReference(service);
        var child = new ControlledContent(() => releaseChild.Task);
        var navigated = new List<NavigationSearchEntry>();
        var session = new NavigationSearchSession(() => show.Task, DispatchImmediately, navigated.Add);

        var request = session.RequestAsync();
        var disposal = session.DisposeAsync().AsTask();
        request.IsCompleted.Should().BeFalse();
        disposal.IsCompleted.Should().BeFalse();
        show.SetResult(new NavigationSearchSession.DialogLease(reference, child));
        await child.Started.WaitAsync(TestContext.Current.CancellationToken);

        service.CloseCalls.Should().Be(1);
        (await reference.Result)!.Canceled.Should().BeTrue();
        request.IsCompletedSuccessfully.Should().BeTrue();
        disposal.IsCompleted.Should().BeFalse();
        releaseChild.SetResult();
        await Task.WhenAll(request, disposal).WaitAsync(TestContext.Current.CancellationToken);

        child.DisposeCalls.Should().Be(1);
        reference.DismissCalls.Should().Be(1);
        navigated.Should().BeEmpty();
    }

    [Fact]
    public async Task DisposeAsync_WhenCloseDispatchIsHeld_ShouldKeepWaitingForTheActualNativeResult()
    {
        var service = new RecordingDialogService();
        service.OnDialogCloseRequested += static (reference, result) => reference.Dismiss(result);
        var reference = new RecordingReference(service);
        var child = new ControlledContent();
        var dispatchStarted = NewCompletion();
        var dispatchFinished = NewCompletion();
        Action? queuedClose = null;
        var session = CreateSession(reference, child, dispatch: action =>
        {
            queuedClose = action;
            dispatchStarted.SetResult();
            return dispatchFinished.Task;
        });

        var request = session.RequestAsync();
        var disposal = session.DisposeAsync().AsTask();
        await dispatchStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        reference.Result.IsCompleted.Should().BeFalse();
        request.IsCompletedSuccessfully.Should().BeTrue();
        disposal.IsCompleted.Should().BeFalse();
        child.DisposeCalls.Should().Be(0);
        queuedClose!();
        dispatchFinished.SetResult();
        await Task.WhenAll(request, disposal).WaitAsync(TestContext.Current.CancellationToken);

        child.DisposeCalls.Should().Be(1);
        service.CloseCalls.Should().Be(1);
    }

    [Fact]
    public async Task DisposeAsync_WhenNativeProviderHasUnsubscribed_ShouldRequestCloseBeforeFallbackDismiss()
    {
        var service = new RecordingDialogService();
        Action<IDialogReference, DialogResult?> provider = static (reference, result) => reference.Dismiss(result);
        service.OnDialogCloseRequested += provider;
        service.OnDialogCloseRequested -= provider;
        var reference = new RecordingReference(service);
        var child = new ControlledContent();
        var session = CreateSession(reference, child);
        var request = session.RequestAsync();

        await session.DisposeAsync().AsTask().WaitAsync(TestContext.Current.CancellationToken);
        await request.WaitAsync(TestContext.Current.CancellationToken);

        service.CloseCalls.Should().Be(1);
        reference.DismissCalls.Should().Be(1);
        reference.CloseCallsAtDismiss.Should().Equal(1);
        (await reference.Result)!.Canceled.Should().BeTrue();
        child.DisposeCalls.Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisposeAsync_WhenNativeCloseReentersRetirementAndDispatchStillRuns_ShouldDrainTheReservedClose(bool faultDispatch)
    {
        var service = new RecordingDialogService();
        var reference = new RecordingReference(service);
        var child = new ControlledContent();
        var dispatchFinished = NewCompletion();
        var dispatchStarted = NewCompletion();
        NavigationSearchSession? session = null;
        Task? reentrantDisposal = null;
        service.OnDialogCloseRequested += (nativeReference, result) =>
        {
            nativeReference.Dismiss(result);
            reentrantDisposal = Assert.IsType<NavigationSearchSession>(session).DisposeAsync().AsTask();
        };
        session = CreateSession(reference, child, dispatch: action =>
        {
            action();
            dispatchStarted.SetResult();
            return dispatchFinished.Task;
        });
        var request = session.RequestAsync();

        var disposal = session.DisposeAsync().AsTask();
        await dispatchStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        reentrantDisposal.Should().BeSameAs(disposal);
        disposal.IsCompleted.Should().BeFalse();
        var failure = new InvalidOperationException("Close dispatch failed after native result completion.");
        if (faultDispatch) dispatchFinished.SetException(failure);
        else dispatchFinished.SetResult();
        await request.WaitAsync(TestContext.Current.CancellationToken);
        if (faultDispatch)
        {
            (await Assert.ThrowsAsync<InvalidOperationException>(() => disposal.WaitAsync(TestContext.Current.CancellationToken)))
                .Should().BeSameAs(failure);
        }
        else
        {
            await disposal.WaitAsync(TestContext.Current.CancellationToken);
        }

        service.CloseCalls.Should().Be(1);
        reference.DismissCalls.Should().Be(1);
        child.DisposeCalls.Should().Be(1);
    }

    [Fact]
    public async Task DisposeAsync_WhenCloseDispatchFaultsBeforeChildFinishes_ShouldDrainTheChildIndependently()
    {
        var service = new RecordingDialogService();
        service.OnDialogCloseRequested += static (reference, result) => reference.Dismiss(result);
        var reference = new RecordingReference(service);
        var releaseChild = NewCompletion();
        var child = new ControlledContent(() => releaseChild.Task);
        var failure = new InvalidOperationException("Close dispatch failed.");
        var session = CreateSession(reference, child, dispatch: action =>
        {
            action();
            return Task.FromException(failure);
        });
        var request = session.RequestAsync();

        var disposal = session.DisposeAsync().AsTask();
        await child.Started.WaitAsync(TestContext.Current.CancellationToken);
        disposal.IsCompleted.Should().BeFalse();
        releaseChild.SetResult();
        await request.WaitAsync(TestContext.Current.CancellationToken);

        (await Assert.ThrowsAsync<InvalidOperationException>(() => disposal.WaitAsync(TestContext.Current.CancellationToken)))
            .Should().BeSameAs(failure);
        child.DisposeCalls.Should().Be(1);
    }

    [Fact]
    public async Task RequestAsync_WhenChildRetirementReentersLauncherRetirement_ShouldDrainWithoutSelfAwaitOrNavigation()
    {
        var service = new RecordingDialogService();
        var reference = new RecordingReference(service);
        var releaseChild = NewCompletion();
        NavigationSearchSession? session = null;
        Task? reentrantDisposal = null;
        var child = new ControlledContent(() =>
        {
            reentrantDisposal = Assert.IsType<NavigationSearchSession>(session).DisposeAsync().AsTask();
            return releaseChild.Task;
        });
        var navigated = new List<NavigationSearchEntry>();
        session = CreateSession(reference, child, navigate: navigated.Add);
        var request = session.RequestAsync();

        reference.Dismiss(DialogResult.Ok(Entry("destination")));
        await child.Started.WaitAsync(TestContext.Current.CancellationToken);
        var disposal = session.DisposeAsync().AsTask();
        reentrantDisposal.Should().BeSameAs(disposal);
        request.IsCompletedSuccessfully.Should().BeTrue();
        disposal.IsCompleted.Should().BeFalse();
        releaseChild.SetResult();
        await Task.WhenAll(request, disposal).WaitAsync(TestContext.Current.CancellationToken);

        navigated.Should().BeEmpty();
        child.DisposeCalls.Should().Be(1);
    }

    [Fact]
    public async Task RequestAsync_WhenRetirementWinsQueuedNavigation_ShouldSuppressItAndDrainTheDispatch()
    {
        var service = new RecordingDialogService();
        var reference = new RecordingReference(service);
        var child = new ControlledContent();
        var dispatchStarted = NewCompletion();
        var dispatchFinished = NewCompletion();
        Action? queuedNavigation = null;
        var navigated = new List<NavigationSearchEntry>();
        var session = CreateSession(reference, child, action =>
        {
            queuedNavigation = action;
            dispatchStarted.SetResult();
            return dispatchFinished.Task;
        }, navigated.Add);
        var request = session.RequestAsync();

        reference.Dismiss(DialogResult.Ok(Entry("destination")));
        await dispatchStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        var disposal = session.DisposeAsync().AsTask();
        disposal.IsCompleted.Should().BeFalse();
        queuedNavigation!();
        dispatchFinished.SetResult();
        await Task.WhenAll(request, disposal).WaitAsync(TestContext.Current.CancellationToken);

        navigated.Should().BeEmpty();
        child.DisposeCalls.Should().Be(1);
    }

    [Fact]
    public async Task RequestAsync_WhenNavigationReentersRetirement_ShouldCompleteItsAlreadyAdmittedChain()
    {
        var service = new RecordingDialogService();
        var reference = new RecordingReference(service);
        var child = new ControlledContent();
        NavigationSearchSession? session = null;
        Task? reentrantDisposal = null;
        var navigated = new List<NavigationSearchEntry>();
        var navigationCompleted = NewCompletion();
        session = CreateSession(reference, child, navigate: entry =>
        {
            navigated.Add(entry);
            reentrantDisposal = Assert.IsType<NavigationSearchSession>(session).DisposeAsync().AsTask();
            navigationCompleted.SetResult();
        });
        var request = session.RequestAsync();
        var selected = Entry("exact-route");

        reference.Dismiss(DialogResult.Ok(selected));
        await request.WaitAsync(TestContext.Current.CancellationToken);
        await navigationCompleted.Task.WaitAsync(TestContext.Current.CancellationToken);
        await Assert.IsAssignableFrom<Task>(reentrantDisposal).WaitAsync(TestContext.Current.CancellationToken);

        navigated.Should().ContainSingle().Which.Should().BeSameAs(selected);
        child.DisposeCalls.Should().Be(1);
    }

    [Fact]
    public async Task RequestAsync_WhenAcquiredContentIsMissing_ShouldCloseReferenceWithoutNavigating()
    {
        var service = new RecordingDialogService();
        service.OnDialogCloseRequested += static (reference, result) => reference.Dismiss(result);
        var reference = new RecordingReference(service);
        var navigated = new List<NavigationSearchEntry>();
        await using var session = CreateSession(reference, content: null, navigate: navigated.Add);

        await session.RequestAsync().WaitAsync(TestContext.Current.CancellationToken);

        service.CloseCalls.Should().Be(1);
        reference.DismissCalls.Should().Be(1);
        navigated.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RequestAsync_WhenChildRetirementFailsAfterOpening_ShouldRetainFailureAcrossReopening(bool canceled)
    {
        var service = new RecordingDialogService();
        service.OnDialogCloseRequested += static (reference, result) => reference.Dismiss(result);
        var firstReference = new RecordingReference(service);
        var secondReference = new RecordingReference(service);
        var failure = new InvalidOperationException("Search dialog retirement failed.");
        var cancellation = new CancellationToken(canceled: true);
        var firstChild = new ControlledContent(() => canceled
            ? Task.FromCanceled(cancellation)
            : Task.FromException(failure));
        var secondChild = new ControlledContent();
        var navigated = new List<NavigationSearchEntry>();
        var shown = 0;
        var session = new NavigationSearchSession(
            () => Task.FromResult(++shown == 1
                ? new NavigationSearchSession.DialogLease(firstReference, firstChild)
                : new NavigationSearchSession.DialogLease(secondReference, secondChild)),
            DispatchImmediately, navigated.Add);

        // A settled native result makes the post-acknowledgement child failure complete in the opening call itself.
        firstReference.Dismiss(DialogResult.Ok(Entry("destination")));
        var first = session.RequestAsync();
        await first.WaitAsync(TestContext.Current.CancellationToken);
        var second = session.RequestAsync();
        await second.WaitAsync(TestContext.Current.CancellationToken);
        var disposal = session.DisposeAsync().AsTask();
        if (canceled)
        {
            var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => disposal.WaitAsync(TestContext.Current.CancellationToken));
            exception.CancellationToken.Should().Be(cancellation);
            disposal.IsCanceled.Should().BeTrue();
        }
        else
        {
            (await Assert.ThrowsAsync<InvalidOperationException>(() => disposal.WaitAsync(TestContext.Current.CancellationToken)))
                .Should().BeSameAs(failure);
        }

        first.IsCompletedSuccessfully.Should().BeTrue();
        second.IsCompletedSuccessfully.Should().BeTrue();
        second.Should().NotBeSameAs(first);
        shown.Should().Be(2);
        navigated.Should().BeEmpty();
        firstChild.DisposeCalls.Should().Be(1);
        secondChild.DisposeCalls.Should().Be(1);
    }

    [Fact]
    public async Task DisposeAsync_WhenHeldShowFails_ShouldObserveItsActualFailure()
    {
        var show = NewCompletion<NavigationSearchSession.DialogLease>();
        var session = new NavigationSearchSession(() => show.Task, DispatchImmediately, _ => { });
        var request = session.RequestAsync();
        var disposal = session.DisposeAsync().AsTask();
        var failure = new InvalidOperationException("Native search opening failed.");

        disposal.IsCompleted.Should().BeFalse();
        show.SetException(failure);
        (await Assert.ThrowsAsync<InvalidOperationException>(() => request.WaitAsync(TestContext.Current.CancellationToken)))
            .Should().BeSameAs(failure);
        (await Assert.ThrowsAsync<InvalidOperationException>(() => disposal.WaitAsync(TestContext.Current.CancellationToken)))
            .Should().BeSameAs(failure);
    }

    private static NavigationSearchSession CreateSession(
        IDialogReference reference,
        IAsyncDisposable? content,
        Func<Action, Task>? dispatch = null,
        Action<NavigationSearchEntry>? navigate = null) => new(
            () => Task.FromResult(new NavigationSearchSession.DialogLease(reference, content)),
            dispatch ?? DispatchImmediately,
            navigate ?? (_ => { }));

    private static Task DispatchImmediately(Action action)
    {
        action();
        return Task.CompletedTask;
    }

    private static NavigationSearchEntry Entry(string route)
    {
        var page = new PageDefinition(route, typeof(TestPage), UIRegistryText.Literal(route), accessPolicyType: null);
        var item = new NavigationItem(page, icon: null, categoryId: BuiltInNavigationCategoryIds.Uncategorized, order: 0);
        return new NavigationSearchEntry(item, route, string.Empty, string.Empty, string.Empty, Icon: null);
    }

    private static TaskCompletionSource NewCompletion() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static TaskCompletionSource<T> NewCompletion<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class RecordingDialogService : DialogService
    {
        internal int CloseCalls { get; private set; }

        public override void Close(IDialogReference dialog, DialogResult? result)
        {
            CloseCalls++;
            base.Close(dialog, result);
        }
    }

    private sealed class RecordingReference : DialogReference
    {
        private readonly RecordingDialogService _service;

        internal RecordingReference(RecordingDialogService service) : base(Guid.NewGuid(), service)
        {
            _service = service;
        }

        internal int DismissCalls { get; private set; }
        internal List<int> CloseCallsAtDismiss { get; } = [];

        public override bool Dismiss(DialogResult? result)
        {
            DismissCalls++;
            CloseCallsAtDismiss.Add(_service.CloseCalls);
            return base.Dismiss(result);
        }
    }

    private sealed class ControlledContent(Func<Task>? retire = null) : IAsyncDisposable
    {
        private readonly TaskCompletionSource _started = NewCompletion();
        internal Task Started => _started.Task;
        internal int DisposeCalls { get; private set; }

        public ValueTask DisposeAsync()
        {
            DisposeCalls++;
            var retirement = retire?.Invoke() ?? Task.CompletedTask;
            _started.TrySetResult();
            return new ValueTask(retirement);
        }
    }

    private sealed class TestPage : ComponentBase;
}
