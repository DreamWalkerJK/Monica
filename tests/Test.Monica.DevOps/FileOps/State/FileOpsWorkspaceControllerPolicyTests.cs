using Monica.DevOps.FileOps.Models;
using Monica.DevOps.FileOps.State;
using Test.Monica.DevOps.FileOps.Support;
using Xunit;

namespace Test.Monica.DevOps.FileOps.State;

public sealed partial class FileOpsWorkspaceControllerTests
{
    [Fact]
    public async Task SavePolicyAsync_WhenDirtyInvalidationIsRefused_ShouldPreservePolicyTargetAndDraftWithoutMutation()
    {
        var service = new ControlledFileOpsServices();
        await using var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        var entry = state.SortedEntries[0];
        await ObserveAsync(state.OpenEntryAsync(entry));
        state.SetDraft("retained dirty content");
        var proposed = state.RuntimeConfig!;
        proposed.AllowWriteOperations = false;

        var disposition = await state.SavePolicyAsync(proposed, (request, _) =>
        {
            Assert.Equal(FileOpsDiscardReason.PolicyReplacement, request.Reason);
            Assert.Equal(entry.FullPath, request.FullPath);
            return Task.FromResult(false);
        });

        Assert.Equal(FileOpsOperationDisposition.Cancelled, disposition);
        Assert.Empty(service.ConfigSaves);
        Assert.True(state.RuntimeConfig!.AllowWriteOperations);
        Assert.Equal(entry.FullPath, state.InspectedFile!.FullPath);
        Assert.Equal("retained dirty content", state.DraftText);
        Assert.True(state.IsDirty);
    }

    [Fact]
    public async Task SavePolicyAsync_WhenMutableDraftChangesDuringConfirmation_ShouldPersistCapturedNormalizedDraftAndAdoptActualReadback()
    {
        var service = new ControlledFileOpsServices();
        var decision = Signal<bool>();
        var readback = Signal<FileOpsRuntimeConfig>();
        var persisted = Signal<bool>();
        await using var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        await ObserveAsync(state.OpenEntryAsync(state.SortedEntries[0]));
        state.SetDraft("dirty content");
        var proposed = state.RuntimeConfig!;
        proposed.AllowWriteOperations = false;
        proposed.MaxTextFileSizeBytes = 1;
        proposed.MaxUploadFileSizeBytes = 1;
        proposed.MaxDownloadFileSizeBytes = 1;
        proposed.MaxDirectoryEntries = 1;
        proposed.EditableExtensions = [" TXT "];
        service.ReadConfig = _ => readback.Task;
        service.SaveConfig = (_, _) => { persisted.SetResult(true); return Task.CompletedTask; };
        var saving = state.SavePolicyAsync(proposed, (_, _) => decision.Task);
        try
        {
            proposed.AllowWriteOperations = true;
            proposed.AllowedRoots.Clear();
            proposed.EditableExtensions.Clear();
            proposed.MaxTextFileSizeBytes = 100_000;
            decision.SetResult(true);
            await ObserveAsync(persisted.Task);
            var saved = Assert.Single(service.ConfigSaves);
            Assert.False(saved.Token.CanBeCanceled);
            Assert.False(saved.Config.AllowWriteOperations);
            Assert.Equal(new[] { ControlledFileOpsServices.Root }, saved.Config.AllowedRoots);
            Assert.Equal(new[] { ".txt" }, saved.Config.EditableExtensions);
            Assert.Equal(4096, saved.Config.MaxTextFileSizeBytes);
            Assert.Equal(65_536, saved.Config.MaxUploadFileSizeBytes);
            Assert.Equal(65_536, saved.Config.MaxDownloadFileSizeBytes);
            Assert.Equal(50, saved.Config.MaxDirectoryEntries);
            var authoritative = saved.Config.Clone();
            authoritative.MaxDirectoryEntries = 75;
            readback.SetResult(authoritative);
            await ObserveAsync(saving);
            authoritative.MaxDirectoryEntries = 300;
            state.RuntimeConfig!.AllowedRoots.Clear();

            Assert.Equal(75, state.RuntimeConfig!.MaxDirectoryEntries);
            Assert.Equal(new[] { ControlledFileOpsServices.Root }, state.RuntimeConfig!.AllowedRoots);
            Assert.Null(state.InspectedFile);
            Assert.False(state.IsDirty);
            Assert.True(state.LastCommand!.Succeeded);
        }
        finally
        {
            decision.TrySetResult(false);
            readback.TrySetResult(new FileOpsRuntimeConfig { AllowedRoots = [ControlledFileOpsServices.Root], StartupPath = ControlledFileOpsServices.Root });
        }
    }

    [Fact]
    public async Task SavePolicyAsync_WhenRealServiceNormalizesInput_ShouldReadAuthoritativeLimitsAndRoots()
    {
        using var fixture = new FileOpsFilesystemFixture();
        await using var state = new FileOpsWorkspaceController(fixture.Facade);
        await ObserveAsync(state.InitializeAsync());
        var proposed = new FileOpsRuntimeConfig
        {
            AllowedRoots = [$" \"{fixture.Root}\" "], StartupPath = Path.Combine(fixture.Root, "missing"),
            MaxTextFileSizeBytes = 1, MaxUploadFileSizeBytes = 1, MaxDownloadFileSizeBytes = 1,
            MaxDirectoryEntries = 1, EditableExtensions = [" TXT ", ".txt"]
        };

        await ObserveAsync(state.SavePolicyAsync(proposed));

        Assert.True(state.LastCommand!.Succeeded);
        var actual = fixture.Store.GetCurrent();
        Assert.Equal(new[] { fixture.Root }, actual.AllowedRoots);
        Assert.Equal(fixture.Root, actual.StartupPath);
        Assert.Equal(4096, actual.MaxTextFileSizeBytes);
        Assert.Equal(65_536, actual.MaxUploadFileSizeBytes);
        Assert.Equal(65_536, actual.MaxDownloadFileSizeBytes);
        Assert.Equal(50, actual.MaxDirectoryEntries);
        Assert.Equal(new[] { ".txt" }, actual.EditableExtensions);
        Assert.Equal(actual.AllowedRoots, state.RuntimeConfig!.AllowedRoots);
        Assert.Equal(actual.StartupPath, state.RuntimeConfig!.StartupPath);
        Assert.Equal(actual.MaxDirectoryEntries, state.RuntimeConfig!.MaxDirectoryEntries);
    }

    [Fact]
    public async Task SavePolicyAsync_WhenNewerDraftAppearsDuringReadback_ShouldRetainOriginAndDraftInsteadOfRelocatingToStartup()
    {
        var service = new ControlledFileOpsServices();
        var readback = Signal<FileOpsRuntimeConfig>();
        await using var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        var entry = state.SortedEntries[0];
        await ObserveAsync(state.OpenEntryAsync(entry));
        state.SetDraft("approved older draft");
        var newRoot = Path.Combine(ControlledFileOpsServices.Root, "replacement-root");
        var proposed = state.RuntimeConfig!;
        proposed.AllowedRoots = [newRoot];
        proposed.StartupPath = newRoot;
        service.ReadConfig = _ => readback.Task;
        var saving = state.SavePolicyAsync(proposed, AcceptDiscard);
        try
        {
            Assert.Single(service.ConfigSaves);
            Assert.False(saving.IsCompleted);
            state.SetDraft("newer protected draft");
            readback.SetResult(proposed.Clone().Normalize());
            await ObserveAsync(saving);

            Assert.True(state.LastCommand!.Succeeded);
            Assert.Equal(new[] { newRoot }, state.RuntimeConfig!.AllowedRoots);
            Assert.Equal(ControlledFileOpsServices.Root, state.Browse!.CurrentPath);
            Assert.Equal(entry.FullPath, state.InspectedFile!.FullPath);
            Assert.Equal("newer protected draft", state.DraftText);
            Assert.True(state.IsDirty);
            Assert.True(state.MetadataStale);
            Assert.True(state.BrowseStale);
            Assert.Single(service.Browses);
            Assert.Single(service.TextReads);
        }
        finally { readback.TrySetResult(proposed.Clone()); }
    }

    [Fact]
    public async Task SavePolicyAsync_WhenApprovedPolicyInvalidatesTarget_ShouldClearCapturedDraftAndBrowseAuthoritativeStartup()
    {
        var service = new ControlledFileOpsServices();
        await using var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        await ObserveAsync(state.OpenEntryAsync(state.SortedEntries[0]));
        state.SetDraft("approved draft");
        var newRoot = Path.Combine(ControlledFileOpsServices.Root, "replacement-root");
        var proposed = state.RuntimeConfig!;
        proposed.AllowedRoots = [newRoot];
        proposed.StartupPath = newRoot;
        service.ReadConfig = _ => Task.FromResult(proposed.Clone().Normalize());

        await ObserveAsync(state.SavePolicyAsync(proposed, AcceptDiscard));

        Assert.True(state.LastCommand!.Succeeded);
        Assert.Null(state.InspectedFile);
        Assert.Null(state.InspectedEntry);
        Assert.Equal(string.Empty, state.DraftText);
        Assert.False(state.IsDirty);
        Assert.Equal(newRoot, state.Browse!.CurrentPath);
        Assert.Equal(newRoot, service.Browses.Last().Path);
    }
}
