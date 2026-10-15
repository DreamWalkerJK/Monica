using Monica.UI.Shared.Components.ContextMenu;
using Monica.UI.Shared.Components.ContextMenu.Support;
using Xunit;

namespace Test.Monica.UI.Shared.Components.ContextMenu.Support;

public sealed class ContextMenuSessionTests
{
    [Fact]
    public async Task Selection_WhenAnotherOpeningReplacesBlockedAction_ShouldRetainContextAndSkipOldTail()
    {
        var gate = NewGate();
        var trace = new List<string>();
        var fixture = new Fixture(context => { trace.Add("leaf:" + context); return gate.Task; });
        fixture.Set("A", 1, selected: _ => { trace.Add("selected:A"); return Task.CompletedTask; });
        await fixture.Session.ReconcileAsync();
        var admitted = fixture.Session.SelectAsync(fixture.Session.RenderedOpening!, fixture.Item);
        fixture.Set("B", 2);
        gate.SetResult();
        await admitted;
        await fixture.Session.ReconcileAsync();

        Assert.Equal(["leaf:A"], trace);
        Assert.Equal("B", fixture.Session.RenderedOpening!.Context);
        Assert.Equal(0, fixture.CloseRequests);
    }

    [Fact]
    public async Task Selection_WhenContextReturnsToA_ShouldNotRevalidateTheOriginalOpening()
    {
        var gate = NewGate();
        var fixture = new Fixture(_ => gate.Task);
        fixture.Set("A", 1);
        await fixture.Session.ReconcileAsync();
        var original = fixture.Session.RenderedOpening!;
        var admitted = fixture.Session.SelectAsync(original, fixture.Item);
        fixture.Set("B", 2);
        fixture.Set("A", 3);
        gate.SetResult();
        await admitted;
        await fixture.Session.ReconcileAsync();

        Assert.NotEqual(original.Id, fixture.Session.RenderedOpening!.Id);
        Assert.Equal(0, fixture.CloseRequests);
    }

    [Fact]
    public async Task Selection_WhenLeafHidesItsOwnMenu_ShouldKeepCapturedSuccessfulCallbackOrder()
    {
        var gate = NewGate();
        var trace = new List<string>();
        var fixture = new Fixture(_ => gate.Task);
        fixture.Set("A", 1, selected: _ => { trace.Add("selected"); return Task.CompletedTask; },
            close: () => { trace.Add("close"); return Task.CompletedTask; });
        await fixture.Session.ReconcileAsync();
        var admitted = fixture.Session.SelectAsync(fixture.Session.RenderedOpening!, fixture.Item);
        fixture.Set("A", 1, visible: false, selected: _ => throw new InvalidOperationException("Replacement callback"));
        trace.Add("leaf");
        gate.SetResult();
        await admitted;

        Assert.Equal(["leaf", "selected", "close"], trace);
    }

    [Fact]
    public async Task Selection_WhenSelectedNotificationIsSuperseded_ShouldNotCloseTheNewOpening()
    {
        var gate = NewGate();
        var fixture = new Fixture();
        fixture.Set("A", 1, selected: _ => gate.Task);
        await fixture.Session.ReconcileAsync();
        var admitted = fixture.Session.SelectAsync(fixture.Session.RenderedOpening!, fixture.Item);
        fixture.Set("B", 2);
        gate.SetResult();
        await admitted;

        Assert.Equal(0, fixture.CloseRequests);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Selection_WhenTheLeafFaults_ShouldObserveTheOriginalFailureWithoutClosing(bool superseded)
    {
        var gate = NewGate();
        var expected = new InvalidOperationException("Action failed");
        var fixture = new Fixture(_ => gate.Task);
        fixture.Set("A", 1);
        await fixture.Session.ReconcileAsync();
        var admitted = fixture.Session.SelectAsync(fixture.Session.RenderedOpening!, fixture.Item);
        if (superseded) fixture.Set("B", 2);
        gate.SetException(expected);

        Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => admitted));
        Assert.Equal(0, fixture.CloseRequests);
    }

    [Fact]
    public async Task Selection_WhenItemNotificationFaults_ShouldNotImplySuccessfulDismissal()
    {
        var expected = new InvalidOperationException("Notification failed");
        var fixture = new Fixture();
        fixture.Set("A", 1, selected: _ => Task.FromException(expected));
        await fixture.Session.ReconcileAsync();

        Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Session.SelectAsync(fixture.Session.RenderedOpening!, fixture.Item)));
        Assert.Equal(0, fixture.CloseRequests);
    }

    [Fact]
    public async Task Reconciliation_WhenHiddenDuringOpen_ShouldWaitForOpenThenCloseWithoutReopening()
    {
        var gate = NewGate();
        var fixture = new Fixture { DuringOpen = _ => gate.Task };
        fixture.Set("A", 1);
        var first = fixture.Session.ReconcileAsync();
        fixture.Set("A", 1, visible: false);
        Assert.Same(first, fixture.Session.ReconcileAsync());
        Assert.Equal(["open:10,20"], fixture.NativeTrace);
        gate.SetResult();
        await first;

        Assert.Equal(["open:10,20", "close"], fixture.NativeTrace);
        Assert.Null(fixture.Session.RenderedOpening);
        Assert.Equal(0, fixture.CloseRequests);
    }

    [Fact]
    public async Task Reconciliation_WhenTargetsChangeDuringClose_ShouldOpenOnlyTheLatestCoordinates()
    {
        var fixture = new Fixture();
        fixture.Set("A", 1);
        await fixture.Session.ReconcileAsync();
        var gate = NewGate();
        fixture.DuringClose = () => gate.Task;
        fixture.Set("B", 2, position: new(30, 40));
        var admitted = fixture.Session.ReconcileAsync();
        fixture.Set("C", 3, position: new(50, 60));
        Assert.Same(admitted, fixture.Session.ReconcileAsync());
        gate.SetResult();
        await admitted;

        Assert.Equal(["open:10,20", "close", "open:50,60"], fixture.NativeTrace);
        Assert.Equal("C", fixture.Session.RenderedOpening!.Context);
    }

    [Fact]
    public async Task Reconciliation_WhenSameCoordinatesHaveNewAdmission_ShouldResetNativeTree()
    {
        var fixture = new Fixture();
        fixture.Set("A", 1);
        await fixture.Session.ReconcileAsync();
        var first = fixture.Session.RenderedOpening!;
        fixture.Set("A", 2);
        await fixture.Session.ReconcileAsync();

        Assert.NotEqual(first.Id, fixture.Session.RenderedOpening!.Id);
        Assert.NotEqual(first.PresentationRevision, fixture.Session.RenderedOpening.PresentationRevision);
        Assert.Equal(["open:10,20", "close", "open:10,20"], fixture.NativeTrace);
    }

    [Fact]
    public async Task Reconciliation_WhenOnlyPositionChanges_ShouldRemountPresentationWithoutRetargetingActionIdentity()
    {
        var fixture = new Fixture();
        fixture.Set("A", 1);
        await fixture.Session.ReconcileAsync();
        var first = fixture.Session.RenderedOpening!;
        fixture.Set("A", 1, position: new(30, 40));
        await fixture.Session.ReconcileAsync();

        Assert.Equal(first.Id, fixture.Session.RenderedOpening!.Id);
        Assert.NotEqual(first.PresentationRevision, fixture.Session.RenderedOpening.PresentationRevision);
        Assert.Equal(["open:10,20", "close", "open:30,40"], fixture.NativeTrace);
    }

    [Fact]
    public async Task Reconciliation_WhenNativeCloseFaults_ShouldReleaseSuppressionAndObserveFailure()
    {
        var expected = new InvalidOperationException("Native close failed");
        var fixture = new Fixture();
        fixture.Set("A", 1);
        await fixture.Session.ReconcileAsync();
        fixture.DuringClose = () => Task.FromException(expected);
        fixture.Set("A", 1, position: new(30, 40));

        Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(fixture.Session.ReconcileAsync));
        await fixture.Session.NativeOpenChangedAsync(false);
        Assert.Equal(1, fixture.CloseRequests);
    }

    [Fact]
    public async Task Retirement_WhenOpenIsPending_ShouldDrainItWithoutStartingAnotherNativeOperation()
    {
        var gate = NewGate();
        var fixture = new Fixture { DuringOpen = _ => gate.Task };
        fixture.Set("A", 1);
        var admitted = fixture.Session.ReconcileAsync();
        var retirement = fixture.Session.DisposeAsync().AsTask();
        Assert.False(retirement.IsCompleted);
        fixture.Set("B", 2);
        gate.SetResult();
        await Task.WhenAll(admitted, retirement);
        await fixture.Session.DisposeAsync();

        Assert.True(fixture.Session.IsRetired);
        Assert.Null(fixture.Session.RenderedOpening);
        Assert.Equal(["open:10,20"], fixture.NativeTrace);
    }

    [Fact]
    public async Task Retirement_WhenCallerActionIsPending_ShouldLeaveProducerObservedAndSkipItsUiTail()
    {
        var gate = NewGate();
        var fixture = new Fixture(_ => gate.Task);
        fixture.Set("A", 1);
        await fixture.Session.ReconcileAsync();
        var admitted = fixture.Session.SelectAsync(fixture.Session.RenderedOpening!, fixture.Item);
        await fixture.Session.DisposeAsync();
        Assert.False(admitted.IsCompleted);
        gate.SetResult();
        await admitted;

        Assert.Equal(0, fixture.CloseRequests);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("remove")]
    [InlineData("reorder")]
    [InlineData("replace")]
    public async Task Reconciliation_WhenExistingListMembershipChanges_ShouldRefreshTheNativePresentation(string change)
    {
        var fixture = new Fixture();
        var first = new EqualContextMenuItem { Text = "First" };
        var second = new EqualContextMenuItem { Text = "Second" };
        fixture.Items.Clear();
        fixture.Items.AddRange([first, second]);
        fixture.Set("A", 1);
        await fixture.Session.ReconcileAsync();
        var original = fixture.Session.RenderedOpening!;

        switch (change)
        {
            case "add": fixture.Items.Add(new EqualContextMenuItem { Text = "Added" }); break;
            case "remove": fixture.Items.RemoveAt(0); break;
            case "reorder": fixture.Items.Reverse(); break;
            case "replace": fixture.Items[0] = new EqualContextMenuItem { Text = "Replacement" }; break;
            default: throw new ArgumentOutOfRangeException(nameof(change));
        }

        var expected = fixture.Items.ToArray();
        fixture.Set("A", 1);
        await fixture.Session.ReconcileAsync();
        var refreshed = fixture.Session.RenderedOpening!;

        Assert.Equal(original.Id, refreshed.Id);
        Assert.NotEqual(original.PresentationRevision, refreshed.PresentationRevision);
        Assert.Equal(expected.Length, refreshed.Items.Count);
        for (var index = 0; index < expected.Length; index++)
            Assert.Same(expected[index], refreshed.Items[index]);
        Assert.Equal(["open:10,20", "close", "open:10,20"], fixture.NativeTrace);
    }

    [Fact]
    public async Task Selection_WhenExistingListChangesWithinOpening_ShouldKeepCapturedActionAndCallbacks()
    {
        var gate = NewGate();
        var trace = new List<string>();
        var fixture = new Fixture(context => { trace.Add("leaf:" + context); return gate.Task; });
        fixture.Set("A", 1,
            selected: _ => { trace.Add("selected:A"); return Task.CompletedTask; },
            close: () => { trace.Add("close:A"); return Task.CompletedTask; });
        await fixture.Session.ReconcileAsync();
        var admitted = fixture.Session.SelectAsync(fixture.Session.RenderedOpening!, fixture.Item);

        fixture.Items.Add(new ContextMenuItem<string> { Text = "Added" });
        fixture.Set("A", 1,
            selected: _ => throw new InvalidOperationException("Replacement item callback"),
            close: () => throw new InvalidOperationException("Replacement close callback"));
        await fixture.Session.ReconcileAsync();
        gate.SetResult();
        await admitted;

        Assert.Equal(["leaf:A", "selected:A", "close:A"], trace);
    }

    private sealed class EqualContextMenuItem : ContextMenuItem<string>
    {
        public override bool Equals(object? obj) => obj is EqualContextMenuItem;
        public override int GetHashCode() => 0;
    }

    private static TaskCompletionSource NewGate() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class Fixture
    {
        public Fixture(Func<string?, Task>? action = null)
        {
            Item = new ContextMenuItem<string> { Text = "Action", OnClick = action };
            Items = [Item];
            Session = new ContextMenuSession<string>(OpenAsync, CloseAsync);
        }

        public ContextMenuSession<string> Session { get; }
        public ContextMenuItem<string> Item { get; }
        public List<ContextMenuItem<string>> Items { get; }
        public List<string> NativeTrace { get; } = [];
        public int CloseRequests { get; private set; }
        public Func<ContextMenuPosition, Task>? DuringOpen { get; init; }
        public Func<Task>? DuringClose { get; set; }

        public void Set(string context, long revision, bool visible = true,
            ContextMenuPosition? position = null,
            Func<ContextMenuItem<string>, Task>? selected = null, Func<Task>? close = null)
            => Session.SetParameters(visible, revision, position ?? new(10, 20), Items, context,
                selected ?? (_ => Task.CompletedTask), close ?? (() => { CloseRequests++; return Task.CompletedTask; }));

        private async Task OpenAsync(ContextMenuPosition position)
        {
            NativeTrace.Add($"open:{position.PageX},{position.PageY}");
            await Session.NativeOpenChangedAsync(true);
            if (DuringOpen is not null) await DuringOpen(position);
        }

        private async Task CloseAsync()
        {
            NativeTrace.Add("close");
            if (DuringClose is not null) await DuringClose();
            await Session.NativeOpenChangedAsync(false);
        }
    }
}
