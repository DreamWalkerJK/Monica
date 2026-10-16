using Monica.DevOps.FileOps.Models;
using Monica.DevOps.FileOps.State;
using Test.Monica.DevOps.FileOps.Support;
using Xunit;

namespace Test.Monica.DevOps.FileOps.State;

public sealed partial class FileOpsWorkspaceControllerTests
{
    [Fact]
    public async Task UploadAsync_WhenInputOpeningIsPending_ShouldUseCapturedParentMaximumAndOverwritePolicy()
    {
        var service = new ControlledFileOpsServices();
        var config = new FileOpsRuntimeConfig
        {
            AllowedRoots = [ControlledFileOpsServices.Root], StartupPath = ControlledFileOpsServices.Root,
            MaxUploadFileSizeBytes = 64 * 1024
        };
        var directory = ControlledFileOpsServices.DirectorySnapshot(ControlledFileOpsServices.Root);
        service.ReadConfig = _ => Task.FromResult(config);
        service.Browse = (_, _) => Task.FromResult(directory);
        var opening = Signal<Stream>();
        var stream = new TrackingDownloadStream();
        await using var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        long capturedMaximum = 0;
        var upload = state.UploadAsync("captured.txt", 3, (maximum, _) => { capturedMaximum = maximum; return opening.Task; });
        try
        {
            config.MaxUploadFileSizeBytes = 128 * 1024;
            directory.CurrentPath = Path.Combine(ControlledFileOpsServices.Root, "later-directory");
            opening.SetResult(stream);
            await ObserveAsync(upload);
            var call = Assert.Single(service.Uploads);
            Assert.Equal(ControlledFileOpsServices.Root, call.Parent);
            Assert.Equal("captured.txt", call.Name);
            Assert.Equal(64 * 1024, capturedMaximum);
            Assert.True(call.Overwrite);
            Assert.False(call.Token.CanBeCanceled);
            Assert.Equal(1, stream.DisposeCount);
            Assert.False(stream.CanRead);
        }
        finally { opening.TrySetResult(stream); }
    }

    [Fact]
    public async Task SaveAsync_WhenDraftIsApplied_ShouldReadActualTargetAgainAndAcceptFreshMetadataWithoutNestedAdmission()
    {
        var service = new ControlledFileOpsServices();
        await using var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        var alpha = state.SortedEntries.Single(entry => entry.Name == "alpha.txt");
        await ObserveAsync(state.OpenEntryAsync(alpha, AcceptDiscard));
        state.SetDraft("applied\ntext");
        service.ReadText = (path, _) => Task.FromResult(ControlledFileOpsServices.TextSnapshot(path, "applied\ntext", 12, 2));

        Assert.Equal(FileOpsOperationDisposition.Completed, await ObserveAsync(state.SaveAsync()));

        Assert.Equal(alpha.FullPath, Assert.Single(service.TextSaves).Request.Path);
        Assert.Equal("applied\ntext", service.TextSaves[0].Request.Content);
        Assert.Equal(new[] { alpha.FullPath, alpha.FullPath }, service.TextReads.Select(call => call.Path));
        Assert.Equal(12, state.InspectedFile!.SizeBytes);
        Assert.Equal(2, state.InspectedFile!.LineCount);
        Assert.Equal("applied\ntext", state.BaselineText);
        Assert.Equal("applied\ntext", state.DraftText);
        Assert.False(state.IsDirty);
        Assert.False(state.MetadataStale);
        Assert.True(state.LastCommand!.Succeeded);
        Assert.Equal(FileOpsOperationKind.SaveText, state.LastCommand!.Kind);
    }

    [Fact]
    public async Task SaveAsync_WhenNewerDraftAppearsDuringProducer_ShouldPersistCapturedContentAndKeepNewerDraftDirty()
    {
        var service = new ControlledFileOpsServices();
        var saved = Signal<bool>();
        service.SaveText = (_, _) => saved.Task;
        await using var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        var alpha = state.SortedEntries.Single(entry => entry.Name == "alpha.txt");
        await ObserveAsync(state.OpenEntryAsync(alpha, AcceptDiscard));
        state.SetDraft("captured draft");
        var save = state.SaveAsync();
        try
        {
            Assert.False(save.IsCompleted);
            Assert.Equal("captured draft", Assert.Single(service.TextSaves).Request.Content);
            Assert.False(service.TextSaves[0].Token.CanBeCanceled);
            state.SetDraft("newer same-path draft");
            service.ReadText = (path, _) => Task.FromResult(ControlledFileOpsServices.TextSnapshot(path, "captured draft"));
            saved.SetResult(true);
            await ObserveAsync(save);
            Assert.Equal("newer same-path draft", state.DraftText);
            Assert.True(state.IsDirty);
            Assert.Equal(alpha.FullPath, state.InspectedFile!.FullPath);
            Assert.Equal(2, service.TextReads.Count);
        }
        finally { saved.TrySetResult(true); }
    }

    [Fact]
    public async Task SaveAsync_WhenRereadFailsButDirectoryRefreshSucceeds_ShouldKeepAppliedBaselineAndExplicitMetadataFailure()
    {
        var service = new ControlledFileOpsServices();
        await using var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        var alpha = state.SortedEntries.Single(entry => entry.Name == "alpha.txt");
        await ObserveAsync(state.OpenEntryAsync(alpha, AcceptDiscard));
        var acceptedSize = state.InspectedFile!.SizeBytes;
        state.SetDraft("persisted replacement");
        service.ReadText = (_, _) => Task.FromException<FileOpsFileTextContent>(
            new IOException("Saved file metadata could not be reread"));

        await ObserveAsync(state.SaveAsync());

        Assert.True(state.LastCommand!.Succeeded);
        Assert.Equal(FileOpsOperationKind.SaveText, state.LastCommand!.Kind);
        Assert.False(state.LastRead!.Succeeded);
        Assert.Equal(FileOpsOperationKind.ReadText, state.LastRead!.Kind);
        Assert.Contains("Saved file metadata could not be reread", state.LastRead!.Message);
        Assert.Equal("persisted replacement", state.BaselineText);
        Assert.Equal("persisted replacement", state.DraftText);
        Assert.False(state.IsDirty);
        Assert.True(state.MetadataStale);
        Assert.False(state.DiskContentDiffers);
        Assert.Equal(acceptedSize, state.InspectedFile!.SizeBytes);
        Assert.False(state.BrowseStale);

        service.ReadText = (path, _) => Task.FromResult(ControlledFileOpsServices.TextSnapshot(path, "persisted replacement", 21));
        await ObserveAsync(state.RefreshAsync());
        Assert.True(state.LastRead!.Succeeded);
        Assert.False(state.MetadataStale);
        Assert.Equal(21, state.InspectedFile!.SizeBytes);
    }

    [Fact]
    public async Task SaveAsync_WhenRereadSeesExternalChange_ShouldExposeDiskDifferenceWithoutReplacingSavedDraft()
    {
        var service = new ControlledFileOpsServices();
        await using var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        await ObserveAsync(state.OpenEntryAsync(state.SortedEntries[0], AcceptDiscard));
        state.SetDraft("applied user content");
        service.ReadText = (path, _) => Task.FromResult(ControlledFileOpsServices.TextSnapshot(path, "external content after save"));

        await ObserveAsync(state.SaveAsync());

        Assert.Equal("applied user content", Assert.Single(service.TextSaves).Request.Content);
        Assert.Equal("applied user content", state.BaselineText);
        Assert.Equal("applied user content", state.DraftText);
        Assert.Equal("external content after save", state.InspectedFile!.Content);
        Assert.False(state.IsDirty);
        Assert.True(state.DiskContentDiffers);
        Assert.False(state.MetadataStale);
        Assert.True(state.LastCommand!.Succeeded);
    }

    [Fact]
    public async Task CreateDirectoryAsync_WhenMutationSucceedsButBrowseFails_ShouldKeepSuccessfulCommandAndAcceptedEvidence()
    {
        var service = new ControlledFileOpsServices();
        await using var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        var acceptedPaths = state.SortedEntries.Select(entry => entry.FullPath).ToArray();
        service.Browse = (_, _) => Task.FromException<FileOpsBrowseResult>(new IOException("Post-create browse failed"));

        await ObserveAsync(state.CreateDirectoryAsync((_, _) => Task.FromResult<string?>("created")));

        Assert.True(state.LastCommand!.Succeeded);
        Assert.Equal(FileOpsOperationKind.CreateDirectory, state.LastCommand!.Kind);
        Assert.False(state.LastRead!.Succeeded);
        Assert.Equal(FileOpsOperationKind.Browse, state.LastRead!.Kind);
        Assert.Contains("Post-create browse failed", state.LastRead!.Message);
        Assert.Equal(acceptedPaths, state.SortedEntries.Select(entry => entry.FullPath));
        Assert.False(state.IsBusy);
    }

    [Fact]
    public async Task DeleteSelectedAsync_WhenBatchFailsAndRefreshAlsoFails_ShouldRefreshCapturedDirectoryAndKeepDistinctFailures()
    {
        var service = new ControlledFileOpsServices();
        await using var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        state.SelectAll(true);
        service.DeleteEntries = (_, _) => Task.FromException<FileOpsBatchDeleteResult>(
            new IOException("Batch stopped after earlier deletion"));
        service.Browse = (_, _) => Task.FromException<FileOpsBrowseResult>(new IOException("Post-delete browse failed"));

        await ObserveAsync(state.DeleteSelectedAsync((_, _) => Task.FromResult<bool?>(true)));

        Assert.False(state.LastCommand!.Succeeded);
        Assert.Contains("Batch stopped after earlier deletion", state.LastCommand!.Message);
        Assert.False(state.LastRead!.Succeeded);
        Assert.Contains("Post-delete browse failed", state.LastRead!.Message);
        Assert.Equal(2, service.Browses.Count);
        Assert.Equal(ControlledFileOpsServices.Root, service.Browses.Last().Path);
        Assert.Equal(2, state.SortedEntries.Count);
        Assert.False(state.IsBusy);
    }
}
