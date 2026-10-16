using Monica.DevOps.FileOps.Models;
using Monica.DevOps.FileOps.State;
using Test.Monica.DevOps.FileOps.Support;
using Xunit;

namespace Test.Monica.DevOps.FileOps.State;

public sealed partial class FileOpsWorkspaceControllerTests
{
    private static readonly TimeSpan ObservationTimeout = TimeSpan.FromSeconds(10);
    private static readonly FileOpsDiscardConfirmation AcceptDiscard = (_, _) => Task.FromResult(true);

    [Fact]
    public async Task CreateDirectoryAsync_WhenNameDialogIsPending_ShouldReserveAdmissionBeforeCallbackAndCancelWithoutMutation()
    {
        var service = new ControlledFileOpsServices();
        var name = Signal<string?>();
        await using var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        var callbackObservedBusy = false;
        var creation = state.CreateDirectoryAsync((context, _) =>
        {
            callbackObservedBusy = state.IsBusy;
            Assert.Equal(ControlledFileOpsServices.Root, context.ParentPath);
            return name.Task;
        });
        try
        {
            Assert.True(callbackObservedBusy);
            Assert.True(state.IsBusy);
            Assert.Empty(service.DirectoryCreates);
            Assert.Equal(FileOpsOperationDisposition.Rejected,
                await state.NavigateAsync(Path.Combine(ControlledFileOpsServices.Root, "other"), AcceptDiscard));
            Assert.Equal(FileOpsOperationDisposition.Rejected,
                await state.CreateDirectoryAsync((_, _) => throw new InvalidOperationException("Overlapping confirmation must not open")));
            Assert.Single(service.Browses);
            name.SetResult(null);
            Assert.Equal(FileOpsOperationDisposition.Cancelled, await ObserveAsync(creation));
            Assert.Empty(service.DirectoryCreates);
            Assert.Null(state.LastCommand);
            Assert.False(state.IsBusy);
        }
        finally { name.TrySetResult(null); }
    }

    [Fact]
    public async Task CreateDirectoryAsync_WhenSourceSnapshotChangesDuringConfirmation_ShouldCreateAndRefreshCapturedParent()
    {
        var service = new ControlledFileOpsServices();
        var snapshot = ControlledFileOpsServices.DirectorySnapshot(ControlledFileOpsServices.Root);
        service.Browse = (_, _) => Task.FromResult(snapshot);
        var name = Signal<string?>();
        await using var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        var creation = state.CreateDirectoryAsync((_, _) => name.Task);
        try
        {
            snapshot.CurrentPath = Path.Combine(ControlledFileOpsServices.Root, "changed-source");
            state.Browse!.CurrentPath = Path.Combine(ControlledFileOpsServices.Root, "changed-public-copy");
            name.SetResult("new-folder");
            Assert.Equal(FileOpsOperationDisposition.Completed, await ObserveAsync(creation));
            Assert.Equal(ControlledFileOpsServices.Root, Assert.Single(service.DirectoryCreates).Request.ParentPath);
            Assert.Equal("new-folder", service.DirectoryCreates[0].Request.DirectoryName);
            Assert.Equal(ControlledFileOpsServices.Root, service.Browses.Last().Path);
        }
        finally { name.TrySetResult(null); }
    }

    [Fact]
    public async Task DeleteEntryAsync_WhenCallerAndConfirmationCopiesChange_ShouldDeleteFrozenOriginalPath()
    {
        var service = new ControlledFileOpsServices();
        var entry = ControlledFileOpsServices.Entry(Path.Combine(ControlledFileOpsServices.Root, "alpha.txt"));
        var originalPath = entry.FullPath;
        var decision = Signal<bool?>();
        FileOpsDeleteConfirmation? confirmation = null;
        await using var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        var deletion = state.DeleteEntryAsync(entry, (context, _) => { confirmation = context; return decision.Task; });
        try
        {
            Assert.Empty(service.SingleDeletes);
            entry.FullPath = Path.Combine(ControlledFileOpsServices.Root, "caller-replaced.txt");
            Assert.Single(confirmation!.Entries).FullPath = Path.Combine(ControlledFileOpsServices.Root, "dialog-replaced.txt");
            decision.SetResult(false);
            Assert.Equal(FileOpsOperationDisposition.Completed, await ObserveAsync(deletion));
            var call = Assert.Single(service.SingleDeletes);
            Assert.Equal(originalPath, call.Request.Path);
            Assert.False(call.Request.Recursive);
        }
        finally { decision.TrySetResult(null); }
    }

    [Fact]
    public async Task DeleteSelectedAsync_WhenSelectionAndConfirmationCopyChange_ShouldDeleteCapturedDisplayedSubsetOnly()
    {
        var service = new ControlledFileOpsServices();
        var decision = Signal<bool?>();
        FileOpsDeleteConfirmation? confirmation = null;
        await using var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        var alpha = state.SortedEntries.Single(entry => entry.Name == "alpha.txt");
        var beta = state.SortedEntries.Single(entry => entry.Name == "beta.txt");
        state.ToggleEntrySelection(alpha, true);
        var deletion = state.DeleteSelectedAsync((context, _) => { confirmation = context; return decision.Task; });
        try
        {
            Assert.Equal(alpha.FullPath, Assert.Single(confirmation!.Entries).FullPath);
            state.ToggleEntrySelection(beta, true);
            state.ClearSelection();
            confirmation!.Entries[0].FullPath = beta.FullPath;
            decision.SetResult(true);
            Assert.Equal(FileOpsOperationDisposition.Completed, await ObserveAsync(deletion));
            Assert.Equal(new[] { alpha.FullPath }, Assert.Single(service.BatchDeletes).Request.Paths);
        }
        finally { decision.TrySetResult(null); }
    }

    [Fact]
    public async Task NavigateAsync_WhenDiscardIsCanceled_ShouldRetainExactDirtyTargetAndAvoidBrowse()
    {
        var service = new ControlledFileOpsServices();
        await using var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        var alpha = state.SortedEntries.Single(entry => entry.Name == "alpha.txt");
        await ObserveAsync(state.OpenEntryAsync(alpha, AcceptDiscard));
        state.SetDraft("unsaved draft");
        var revision = state.DraftRevision;
        FileOpsDiscardRequest? captured = null;

        var result = await state.NavigateAsync(Path.Combine(ControlledFileOpsServices.Root, "other"), (request, _) =>
        {
            captured = request;
            return Task.FromResult(false);
        });

        Assert.Equal(FileOpsOperationDisposition.Cancelled, result);
        Assert.Equal(alpha.FullPath, captured!.FullPath);
        Assert.Equal(FileOpsDiscardReason.Navigate, captured.Reason);
        Assert.Equal(revision, captured.DraftRevision);
        Assert.Equal(alpha.FullPath, state.InspectedFile!.FullPath);
        Assert.Equal("unsaved draft", state.DraftText);
        Assert.True(state.IsDirty);
        Assert.Single(service.Browses);
    }

    [Fact]
    public async Task NavigateAsync_WhenDraftChangesAwayAndBackDuringDiscard_ShouldRejectOlderApprovalByRevision()
    {
        var service = new ControlledFileOpsServices();
        var decision = Signal<bool>();
        await using var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        var alpha = state.SortedEntries.Single(entry => entry.Name == "alpha.txt");
        await ObserveAsync(state.OpenEntryAsync(alpha, AcceptDiscard));
        state.SetDraft("draft A");
        var originalRevision = state.DraftRevision;
        var navigation = state.NavigateAsync(Path.Combine(ControlledFileOpsServices.Root, "other"), (_, _) => decision.Task);
        try
        {
            state.SetDraft("draft B");
            state.SetDraft("draft A");
            Assert.True(state.DraftRevision > originalRevision);
            decision.SetResult(true);
            Assert.Equal(FileOpsOperationDisposition.Cancelled, await ObserveAsync(navigation));
            Assert.Equal("draft A", state.DraftText);
            Assert.Equal(alpha.FullPath, state.InspectedFile!.FullPath);
            Assert.Single(service.Browses);
        }
        finally { decision.TrySetResult(false); }
    }

    [Fact]
    public async Task RefreshAsync_WhenTruncatedInventoryOmitsInspectedFile_ShouldKeepDraftAndUseDirectFileEvidence()
    {
        var service = new ControlledFileOpsServices();
        await using var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        var alpha = state.SortedEntries.Single(entry => entry.Name == "alpha.txt");
        var beta = state.SortedEntries.Single(entry => entry.Name == "beta.txt");
        await ObserveAsync(state.OpenEntryAsync(alpha, AcceptDiscard));
        state.SetDraft("unsaved alpha text");
        state.ToggleEntrySelection(alpha, true);
        var capped = ControlledFileOpsServices.DirectorySnapshot(ControlledFileOpsServices.Root, beta);
        capped.IsTruncated = true;
        service.Browse = (_, _) => Task.FromResult(capped);
        service.ReadText = (path, _) => Task.FromResult(ControlledFileOpsServices.TextSnapshot(path, "fresh external disk text"));

        await ObserveAsync(state.RefreshAsync());

        Assert.Equal(beta.FullPath, Assert.Single(state.SortedEntries).FullPath);
        Assert.Empty(state.SelectedPaths);
        Assert.Equal(alpha.FullPath, state.InspectedFile!.FullPath);
        Assert.Equal("fresh external disk text", state.InspectedFile!.Content);
        Assert.Equal("unsaved alpha text", state.DraftText);
        Assert.True(state.IsDirty);
        Assert.True(state.DiskContentDiffers);
        Assert.False(state.MetadataStale);
        Assert.Equal(2, service.TextReads.Count);
    }

    [Fact]
    public async Task DeleteEntryAsync_WhenTargetIsUnrelatedToDirtyInspector_ShouldKeepDraftAndInspectorIdentity()
    {
        var service = new ControlledFileOpsServices();
        await using var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        var alpha = state.SortedEntries.Single(entry => entry.Name == "alpha.txt");
        var beta = state.SortedEntries.Single(entry => entry.Name == "beta.txt");
        await ObserveAsync(state.OpenEntryAsync(alpha, AcceptDiscard));
        state.SetDraft("unsaved alpha text");
        service.Browse = (_, _) => Task.FromResult(ControlledFileOpsServices.DirectorySnapshot(ControlledFileOpsServices.Root, alpha));

        await ObserveAsync(state.DeleteEntryAsync(beta, (context, _) =>
        {
            Assert.False(context.AffectsDirtyDraft);
            return Task.FromResult<bool?>(true);
        }));

        Assert.Equal(beta.FullPath, Assert.Single(service.SingleDeletes).Request.Path);
        Assert.Equal(alpha.FullPath, state.InspectedFile!.FullPath);
        Assert.Equal("unsaved alpha text", state.DraftText);
        Assert.True(state.IsDirty);
    }

    private static TaskCompletionSource<T> Signal<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static Task ObserveAsync(Task task) => task.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);
    private static Task<T> ObserveAsync<T>(Task<T> task) => task.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);
}
