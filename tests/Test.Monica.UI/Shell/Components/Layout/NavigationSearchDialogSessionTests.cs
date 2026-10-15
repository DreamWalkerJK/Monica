using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Monica.UI.Shell.Components.Layout;
using Monica.UI.Shell.Models;
using Monica.UI.Shell.Support;
using Xunit;

namespace Test.Monica.UI.Shell.Components.Layout;

public sealed class NavigationSearchDialogSessionTests
{
    [Fact]
    public async Task RefreshAsync_WhenAuthenticationInvalidatesPendingCatalog_ShouldKeepOnlyTheNewSnapshot()
    {
        var firstRead = NewCompletion<IReadOnlyList<NavigationSearchEntry>>();
        var secondRead = NewCompletion<IReadOnlyList<NavigationSearchEntry>>();
        var reads = 0;
        await using var session = CreateSession([], createEntries: _ => ++reads == 1 ? firstRead.Task : secondRead.Task);

        var oldRefresh = session.RefreshAsync();
        session.InvalidateAuthentication();
        var newRefresh = session.RefreshAsync();
        var currentEntry = Entry("current");
        secondRead.SetResult([currentEntry]);
        var current = await newRefresh.WaitAsync(TestContext.Current.CancellationToken);
        firstRead.SetResult([Entry("obsolete")]);

        (await oldRefresh.WaitAsync(TestContext.Current.CancellationToken)).Should().BeNull();
        current.Should().NotBeNull();
        current!.Entries.Should().Equal(currentEntry);
        session.IsCurrent(current.Revision).Should().BeTrue();
    }

    [Fact]
    public async Task ActivateAsync_WhenQueuedRowBelongsToAnOlderSnapshot_ShouldRejectItBeforeEvaluatingAccess()
    {
        var entry = Entry("shared-destination");
        var evaluations = 0;
        await using var session = CreateSession([entry], authorizeEntry: (_, _) =>
        {
            evaluations++;
            return Task.FromResult(true);
        });
        var oldSnapshot = (await session.RefreshAsync())!;
        session.InvalidateAuthentication();
        var current = (await session.RefreshAsync())!;
        var opened = new List<NavigationSearchEntry>();

        var obsolete = await session.ActivateAsync(entry, oldSnapshot.Revision, opened.Add);
        var copiedEntry = await session.ActivateAsync(entry with { }, current.Revision, opened.Add);

        obsolete.Should().Be(NavigationSearchDialogSession.ActivationResult.Rejected);
        copiedEntry.Should().Be(NavigationSearchDialogSession.ActivationResult.Rejected);
        evaluations.Should().Be(0);
        opened.Should().BeEmpty();
        session.IsCurrent(current.Revision).Should().BeTrue();
    }

    [Fact]
    public async Task ActivateAsync_WhenOldAllowCompletesAfterAuthenticationInvalidation_ShouldPreserveNewSnapshot()
    {
        var entry = Entry("protected");
        var authorization = NewCompletion<bool>();
        await using var session = CreateSession([entry], authorizeEntry: (_, _) => authorization.Task);
        var oldSnapshot = (await session.RefreshAsync())!;
        var opened = new List<NavigationSearchEntry>();

        var activation = session.ActivateAsync(entry, oldSnapshot.Revision, opened.Add);
        session.InvalidateAuthentication();
        var current = (await session.RefreshAsync())!;
        authorization.SetResult(true);

        (await activation.WaitAsync(TestContext.Current.CancellationToken))
            .Should().Be(NavigationSearchDialogSession.ActivationResult.Rejected);
        opened.Should().BeEmpty();
        session.IsCurrent(current.Revision).Should().BeTrue();
    }

    [Fact]
    public async Task ActivateAsync_WhenAuthenticationChangesBeforeQueuedRendererCommit_ShouldRejectTheOldAllow()
    {
        var entry = Entry("protected");
        var dispatchStarted = NewCompletion<bool>();
        var dispatchCompletion = NewCompletion<bool>();
        Action? queuedCommit = null;
        await using var session = CreateSession([entry], dispatch: action =>
        {
            queuedCommit = action;
            dispatchStarted.SetResult(true);
            return dispatchCompletion.Task;
        });
        var snapshot = (await session.RefreshAsync())!;
        var opened = new List<NavigationSearchEntry>();

        var activation = session.ActivateAsync(entry, snapshot.Revision, opened.Add);
        await dispatchStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        session.InvalidateAuthentication();
        queuedCommit!();
        dispatchCompletion.SetResult(true);

        (await activation.WaitAsync(TestContext.Current.CancellationToken))
            .Should().Be(NavigationSearchDialogSession.ActivationResult.Rejected);
        opened.Should().BeEmpty();
    }

    [Fact]
    public async Task ActivateAsync_WhenRepeatedRequestsShareTheDisplayedSnapshot_ShouldOpenOnlyOnce()
    {
        var entry = Entry("destination");
        var authorization = NewCompletion<bool>();
        var evaluations = 0;
        await using var session = CreateSession([entry], authorizeEntry: (_, _) =>
        {
            evaluations++;
            return authorization.Task;
        });
        var snapshot = (await session.RefreshAsync())!;
        var opened = new List<NavigationSearchEntry>();

        var activation = session.ActivateAsync(entry, snapshot.Revision, opened.Add);
        var duplicate = await session.ActivateAsync(entry, snapshot.Revision, opened.Add);
        authorization.SetResult(true);
        var result = await activation.WaitAsync(TestContext.Current.CancellationToken);
        var queuedAfterClose = await session.ActivateAsync(entry, snapshot.Revision, opened.Add);

        result.Should().Be(NavigationSearchDialogSession.ActivationResult.Opened);
        duplicate.Should().Be(NavigationSearchDialogSession.ActivationResult.Rejected);
        queuedAfterClose.Should().Be(NavigationSearchDialogSession.ActivationResult.Rejected);
        evaluations.Should().Be(1);
        opened.Should().Equal(entry);
    }

    [Fact]
    public async Task ActivateAsync_WhenCurrentPolicyDenies_ShouldReturnDeniedWithoutConsumingTheSnapshot()
    {
        var entry = Entry("protected");
        await using var session = CreateSession([entry], authorizeEntry: (_, _) => Task.FromResult(false));
        var snapshot = (await session.RefreshAsync())!;
        var opened = new List<NavigationSearchEntry>();

        var result = await session.ActivateAsync(entry, snapshot.Revision, opened.Add);

        result.Should().Be(NavigationSearchDialogSession.ActivationResult.Denied);
        session.IsCurrent(snapshot.Revision).Should().BeTrue();
        opened.Should().BeEmpty();
    }

    [Fact]
    public async Task DisposeAsync_WhenInitialCatalogIgnoresCancellation_ShouldWaitForTheActualRead()
    {
        var catalog = NewCompletion<IReadOnlyList<NavigationSearchEntry>>();
        var session = CreateSession([], createEntries: _ => catalog.Task);
        var refresh = session.RefreshAsync();

        var disposal = session.DisposeAsync().AsTask();
        session.LifetimeToken.IsCancellationRequested.Should().BeTrue();
        disposal.IsCompleted.Should().BeFalse();
        (await session.RefreshAsync()).Should().BeNull();
        catalog.SetResult([Entry("late")]);

        (await refresh.WaitAsync(TestContext.Current.CancellationToken)).Should().BeNull();
        await disposal.WaitAsync(TestContext.Current.CancellationToken);
        session.DisposeAsync().AsTask().Should().BeSameAs(disposal);
    }

    [Fact]
    public async Task DisposeAsync_WhenCatalogAndActivationIgnoreCancellation_ShouldDrainBothProducers()
    {
        var entry = Entry("destination");
        var catalog = NewCompletion<IReadOnlyList<NavigationSearchEntry>>();
        var authorization = NewCompletion<bool>();
        var reads = 0;
        var session = CreateSession([entry],
            createEntries: _ => ++reads == 1 ? Task.FromResult<IReadOnlyList<NavigationSearchEntry>>([entry]) : catalog.Task,
            authorizeEntry: (_, _) => authorization.Task);
        var snapshot = (await session.RefreshAsync())!;
        var opened = new List<NavigationSearchEntry>();
        var activation = session.ActivateAsync(entry, snapshot.Revision, opened.Add);
        var refresh = session.RefreshAsync();

        var disposal = session.DisposeAsync().AsTask();
        disposal.IsCompleted.Should().BeFalse();
        (await session.ActivateAsync(entry, snapshot.Revision, opened.Add))
            .Should().Be(NavigationSearchDialogSession.ActivationResult.Rejected);
        catalog.SetResult([entry]);
        await refresh.WaitAsync(TestContext.Current.CancellationToken);
        disposal.IsCompleted.Should().BeFalse();
        authorization.SetResult(true);

        await Task.WhenAll(activation, disposal).WaitAsync(TestContext.Current.CancellationToken);
        (await activation).Should().Be(NavigationSearchDialogSession.ActivationResult.Rejected);
        opened.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisposeAsync_WhenOneProducerFails_ShouldDrainTheOtherAndPreserveFailure(bool failCatalog)
    {
        var entry = Entry("destination");
        var catalog = NewCompletion<IReadOnlyList<NavigationSearchEntry>>();
        var authorization = NewCompletion<bool>();
        var reads = 0;
        var session = CreateSession([entry],
            createEntries: _ => ++reads == 1 ? Task.FromResult<IReadOnlyList<NavigationSearchEntry>>([entry]) : catalog.Task,
            authorizeEntry: (_, _) => authorization.Task);
        var snapshot = (await session.RefreshAsync())!;
        var activation = session.ActivateAsync(entry, snapshot.Revision, _ => { });
        var refresh = session.RefreshAsync();
        var disposal = session.DisposeAsync().AsTask();
        var failure = new InvalidOperationException("Search boundary failed.");

        if (failCatalog) catalog.SetException(failure);
        else authorization.SetException(failure);
        var failedProducer = failCatalog ? (Task)refresh : activation;
        (await Assert.ThrowsAsync<InvalidOperationException>(() => failedProducer.WaitAsync(TestContext.Current.CancellationToken)))
            .Should().BeSameAs(failure);
        disposal.IsCompleted.Should().BeFalse();
        if (failCatalog) authorization.SetResult(true);
        else catalog.SetResult([entry]);
        var remainingProducer = failCatalog ? (Task)activation : refresh;
        await remainingProducer.WaitAsync(TestContext.Current.CancellationToken);

        (await Assert.ThrowsAsync<InvalidOperationException>(() => disposal.WaitAsync(TestContext.Current.CancellationToken)))
            .Should().BeSameAs(failure);
    }

    [Fact]
    public async Task DisposeAsync_WhenRendererCommitIsPending_ShouldDrainDispatchAndSuppressItsNativeAction()
    {
        var entry = Entry("destination");
        var dispatchStarted = NewCompletion<bool>();
        var dispatchCompletion = NewCompletion<bool>();
        Action? queuedCommit = null;
        var session = CreateSession([entry], dispatch: action =>
        {
            queuedCommit = action;
            dispatchStarted.SetResult(true);
            return dispatchCompletion.Task;
        });
        var snapshot = (await session.RefreshAsync())!;
        var opened = new List<NavigationSearchEntry>();
        var activation = session.ActivateAsync(entry, snapshot.Revision, opened.Add);
        await dispatchStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        var disposal = session.DisposeAsync().AsTask();
        disposal.IsCompleted.Should().BeFalse();
        queuedCommit!();
        dispatchCompletion.SetResult(true);
        await Task.WhenAll(activation, disposal).WaitAsync(TestContext.Current.CancellationToken);

        (await activation).Should().Be(NavigationSearchDialogSession.ActivationResult.Rejected);
        opened.Should().BeEmpty();
    }

    [Fact]
    public async Task ActivateAsync_WhenNativeCloseReentersRetirement_ShouldShareTheReservedCompletion()
    {
        var entry = Entry("destination");
        var session = CreateSession([entry]);
        var snapshot = (await session.RefreshAsync())!;
        Task? retirement = null;
        var retirementWasPending = false;

        var result = await session.ActivateAsync(entry, snapshot.Revision, _ =>
        {
            retirement = session.DisposeAsync().AsTask();
            retirementWasPending = !retirement.IsCompleted;
        });
        await retirement!.WaitAsync(TestContext.Current.CancellationToken);

        result.Should().Be(NavigationSearchDialogSession.ActivationResult.Opened);
        retirementWasPending.Should().BeTrue();
        session.DisposeAsync().AsTask().Should().BeSameAs(retirement);
    }

    private static NavigationSearchDialogSession CreateSession(
        IReadOnlyList<NavigationSearchEntry> entries,
        Func<CancellationToken, Task<IReadOnlyList<NavigationSearchEntry>>>? createEntries = null,
        Func<NavigationSearchEntry, CancellationToken, Task<bool>>? authorizeEntry = null,
        Func<Action, Task>? dispatch = null) => new(
            createEntries ?? (_ => Task.FromResult(entries)),
            authorizeEntry ?? ((_, _) => Task.FromResult(true)),
            dispatch ?? (action =>
            {
                action();
                return Task.CompletedTask;
            }));

    private static NavigationSearchEntry Entry(string route)
    {
        var page = new PageDefinition(route, typeof(TestPage), UIRegistryText.Literal(route), accessPolicyType: null);
        var item = new NavigationItem(page, icon: null, categoryId: BuiltInNavigationCategoryIds.Uncategorized, order: 0);
        return new NavigationSearchEntry(item, route, string.Empty, string.Empty, string.Empty, Icon: null);
    }

    private static TaskCompletionSource<T> NewCompletion<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class TestPage : ComponentBase;
}
