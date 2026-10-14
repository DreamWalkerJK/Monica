using Microsoft.JSInterop;
using Monica.DevOps.FileOps.Dialogs;
using Monica.DevOps.FileOps.Models;
using Monica.DevOps.FileOps.State;
using Monica.DevOps.FileOps.Support;
using MudBlazor;
using NSubstitute;
using Test.Monica.DevOps.FileOps.Support;
using Xunit;

namespace Test.Monica.DevOps.FileOps.State;

public sealed partial class FileOpsWorkspaceControllerTests
{
    [Fact]
    public async Task DisposeAsync_WhenRendererPublicationIsPending_ShouldDrainItsActualTaskAndSuppressLaterNotifications()
    {
        var service = new ControlledFileOpsServices();
        var publication = Signal<bool>();
        var notifications = 0;
        FileOpsWorkspaceController? state = null;
        state = new FileOpsWorkspaceController(service.CreateFacade(), async () =>
        {
            notifications++;
            Assert.True(state!.IsBusy);
            Assert.Equal(FileOpsOperationDisposition.Rejected, await state.RefreshAsync());
            await publication.Task;
        });
        var initialization = state.InitializeAsync();
        var retirement = state.DisposeAsync().AsTask();
        try
        {
            Assert.False(initialization.IsCompleted);
            Assert.False(retirement.IsCompleted);
            Assert.Equal(1, notifications);
            Assert.Equal(0, service.ConfigReads);
            publication.SetResult(true);
            await ObserveAsync(retirement);
            Assert.Equal(FileOpsOperationDisposition.Retired, await ObserveAsync(initialization));
            Assert.Equal(1, notifications);
            Assert.Equal(0, service.ConfigReads);
        }
        finally
        {
            publication.TrySetResult(true);
            await ObserveAsync(state.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenFinalPublicationIsPending_ShouldPublishSettledBusyRejectReentryAndDrainItsActualTask()
    {
        var service = new ControlledFileOpsServices();
        var publication = Signal<bool>();
        var publicationEntered = Signal<bool>();
        var busyAtPublication = new List<bool>();
        FileOpsOperationDisposition? reentrantDisposition = null;
        FileOpsWorkspaceController? state = null;
        state = new FileOpsWorkspaceController(service.CreateFacade(), async () =>
        {
            busyAtPublication.Add(state!.IsBusy);
            if (busyAtPublication.Count != 2) return;
            reentrantDisposition = await state.RefreshAsync();
            publicationEntered.SetResult(true);
            await publication.Task;
        });
        var initialization = state.InitializeAsync();
        try
        {
            await ObserveAsync(publicationEntered.Task);
            Assert.Equal(new[] { true, false }, busyAtPublication);
            Assert.Equal(FileOpsOperationDisposition.Rejected, reentrantDisposition);
            Assert.False(initialization.IsCompleted);

            var retirement = state.DisposeAsync().AsTask();
            Assert.False(retirement.IsCompleted);
            Assert.False(initialization.IsCompleted);
            publication.SetResult(true);
            await ObserveAsync(retirement);
            Assert.Equal(FileOpsOperationDisposition.Retired, await ObserveAsync(initialization));
            Assert.Equal(2, busyAtPublication.Count);
            Assert.Equal(1, service.ConfigReads);
            Assert.Single(service.Browses);
        }
        finally
        {
            publication.TrySetResult(true);
            await ObserveAsync(state.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenNativeRetirementFails_ShouldStillDrainActualReadAndKeepFailureObservable()
    {
        var service = new ControlledFileOpsServices();
        var browse = Signal<FileOpsBrowseResult>();
        service.Browse = (_, _) => browse.Task;
        var state = new FileOpsWorkspaceController(service.CreateFacade(), retireCallbacks: () =>
            Task.FromException(new InvalidOperationException("Native owner teardown failed")));
        var initialization = state.InitializeAsync();
        var retirement = state.DisposeAsync().AsTask();
        Assert.False(retirement.IsCompleted);
        browse.SetResult(ControlledFileOpsServices.DirectorySnapshot(ControlledFileOpsServices.Root));

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => ObserveAsync(retirement));
        Assert.Contains("Native owner teardown failed", failure.Message);
        Assert.Equal(FileOpsOperationDisposition.Retired, await ObserveAsync(initialization));
        Assert.Null(state.Browse);
        Assert.Same(retirement, state.DisposeAsync().AsTask());
    }

    [Fact]
    public async Task DisposeAsync_WhenReadIgnoresCancellation_ShouldDrainActualProducerWithoutPublishingLateEvidence()
    {
        var service = new ControlledFileOpsServices();
        var browse = Signal<FileOpsBrowseResult>();
        service.Browse = (_, _) => browse.Task;
        var notifications = 0;
        var state = new FileOpsWorkspaceController(service.CreateFacade(), () => { notifications++; return Task.CompletedTask; });
        var initialization = state.InitializeAsync();
        var retirement = state.DisposeAsync().AsTask();
        try
        {
            Assert.True(state.IsRetired);
            Assert.False(retirement.IsCompleted);
            Assert.Same(retirement, state.DisposeAsync().AsTask());
            Assert.True(Assert.Single(service.Browses).Token.IsCancellationRequested);
            var atRetirement = notifications;
            browse.SetResult(ControlledFileOpsServices.DirectorySnapshot(Path.Combine(ControlledFileOpsServices.Root, "late")));
            await ObserveAsync(retirement);
            Assert.Equal(FileOpsOperationDisposition.Retired, await ObserveAsync(initialization));
            Assert.Null(state.Browse);
            Assert.Equal(atRetirement, notifications);
            Assert.Equal(FileOpsOperationDisposition.Retired, await state.RefreshAsync());
            Assert.Single(service.Browses);
        }
        finally
        {
            browse.TrySetResult(ControlledFileOpsServices.DirectorySnapshot(ControlledFileOpsServices.Root));
            await ObserveAsync(state.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenStartedSaveIsPending_ShouldKeepMutationUncancelledDrainItAndStopFurtherReadsAndNotifications()
    {
        var service = new ControlledFileOpsServices();
        var saved = Signal<bool>();
        service.SaveText = (_, _) => saved.Task;
        var notifications = 0;
        var state = new FileOpsWorkspaceController(service.CreateFacade(), () => { notifications++; return Task.CompletedTask; });
        await ObserveAsync(state.InitializeAsync());
        await ObserveAsync(state.OpenEntryAsync(state.SortedEntries[0], AcceptDiscard));
        state.SetDraft("captured pending save");
        var saving = state.SaveAsync();
        var retirement = state.DisposeAsync().AsTask();
        try
        {
            Assert.False(Assert.Single(service.TextSaves).Token.CanBeCanceled);
            Assert.False(retirement.IsCompleted);
            var atRetirement = notifications;
            Assert.Equal(FileOpsOperationDisposition.Retired, await state.SaveAsync());
            saved.SetResult(true);
            await ObserveAsync(retirement);
            Assert.Equal(FileOpsOperationDisposition.Retired, await ObserveAsync(saving));
            Assert.Equal(atRetirement, notifications);
            Assert.Single(service.TextSaves);
            Assert.Single(service.TextReads);
            Assert.Single(service.Browses);
        }
        finally
        {
            saved.TrySetResult(true);
            await ObserveAsync(state.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenDialogIsStillOpening_ShouldRetireNativeOwnerBeforeDrainingConfirmationAndPreventMutation()
    {
        var service = new ControlledFileOpsServices();
        var opening = Signal<IDialogReference>();
        var result = new TaskCompletionSource<DialogResult?>();
        var reference = Substitute.For<IDialogReference>();
        reference.Result.Returns(result.Task);
        reference.When(dialog => dialog.Close(Arg.Any<DialogResult>())).Do(call => result.SetResult(call.Arg<DialogResult>()));
        var dialogs = Substitute.For<IDialogService>();
        dialogs.ShowAsync<FileOpsCreateDirectoryDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(opening.Task);
        var native = new FileOpsDialogOwner(dialogs, action => { action(); return Task.CompletedTask; });
        var state = new FileOpsWorkspaceController(service.CreateFacade(), retireCallbacks: () => native.DisposeAsync().AsTask());
        await ObserveAsync(state.InitializeAsync());
        var creation = state.CreateDirectoryAsync(async (_, _) =>
        {
            var nativeResult = await native.ShowAsync<FileOpsCreateDirectoryDialog>("Create", new DialogParameters());
            return nativeResult is { Canceled: false, Data: string name } ? name : null;
        });
        var retirement = state.DisposeAsync().AsTask();
        try
        {
            Assert.False(retirement.IsCompleted);
            Assert.Empty(service.DirectoryCreates);
            opening.SetResult(reference);
            await ObserveAsync(retirement);
            Assert.Equal(FileOpsOperationDisposition.Retired, await ObserveAsync(creation));
            Assert.Empty(service.DirectoryCreates);
            reference.Received(1).Close(Arg.Any<DialogResult>());
        }
        finally
        {
            opening.TrySetResult(reference);
            await ObserveAsync(state.DisposeAsync().AsTask());
            await ObserveAsync(native.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenDownloadDescriptorArrivesLate_ShouldReleaseStreamWithoutStartingBrowserCallback()
    {
        var service = new ControlledFileOpsServices();
        var descriptor = Signal<FileOpsDownloadDescriptor>();
        service.OpenDownload = (_, _) => descriptor.Task;
        var stream = new TrackingDownloadStream();
        var browserCalls = 0;
        var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        var path = state.SortedEntries[0].FullPath;
        var download = state.DownloadAsync(path, (_, _) => { browserCalls++; return Task.FromResult(true); });
        var retirement = state.DisposeAsync().AsTask();
        try
        {
            Assert.False(retirement.IsCompleted);
            descriptor.SetResult(ControlledFileOpsServices.Descriptor(path, stream));
            await ObserveAsync(retirement);
            Assert.Equal(FileOpsOperationDisposition.Retired, await ObserveAsync(download));
            Assert.Equal(0, browserCalls);
            Assert.Equal(1, stream.DisposeCount);
            Assert.False(stream.CanRead);
        }
        finally
        {
            descriptor.TrySetResult(ControlledFileOpsServices.Descriptor(path, stream));
            await ObserveAsync(state.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenUploadInputArrivesLate_ShouldReleaseInputAndNeverStartFilesystemMutation()
    {
        var service = new ControlledFileOpsServices();
        var opening = Signal<Stream>();
        var stream = new TrackingDownloadStream();
        var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        var upload = state.UploadAsync("late-input.txt", 3, (maximum, token) =>
        {
            Assert.Equal(state.RuntimeConfig!.MaxUploadFileSizeBytes, maximum);
            Assert.False(token.CanBeCanceled);
            return opening.Task;
        });
        var retirement = state.DisposeAsync().AsTask();
        try
        {
            Assert.False(retirement.IsCompleted);
            opening.SetResult(stream);
            await ObserveAsync(retirement);
            Assert.Equal(FileOpsOperationDisposition.Retired, await ObserveAsync(upload));
            Assert.Empty(service.Uploads);
            Assert.Equal(1, stream.DisposeCount);
            Assert.False(stream.CanRead);
        }
        finally
        {
            opening.TrySetResult(stream);
            await ObserveAsync(state.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DownloadAsync_WhenBrowserCallbackFails_ShouldReleaseOwnedDescriptorStream()
    {
        var service = new ControlledFileOpsServices();
        var stream = new TrackingDownloadStream();
        service.OpenDownload = (path, _) => Task.FromResult(ControlledFileOpsServices.Descriptor(path, stream));
        await using var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        var failure = new JSException("Download callback failed");

        var operation = state.DownloadAsync(state.SortedEntries[0].FullPath, (_, _) => Task.FromException<bool>(failure));
        var actual = await Assert.ThrowsAsync<JSException>(() => ObserveAsync(operation));
        Assert.Same(failure, actual);

        Assert.Equal(1, stream.DisposeCount);
        Assert.False(stream.CanRead);
        Assert.False(state.IsBusy);
        Assert.Null(state.LastCommand);
    }

    [Fact]
    public async Task DownloadAsync_WhenBrowserReportsNoInitiation_ShouldCancelWithoutRetainingEarlierSuccessAndReleaseDescriptor()
    {
        var service = new ControlledFileOpsServices();
        await using var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());
        var path = state.SortedEntries[0].FullPath;
        Assert.Equal(FileOpsOperationDisposition.Completed, await state.DownloadAsync(path, (_, _) => Task.FromResult(true)));
        Assert.True(state.LastCommand!.Succeeded);
        var stream = new TrackingDownloadStream();
        service.OpenDownload = (target, _) => Task.FromResult(ControlledFileOpsServices.Descriptor(target, stream));

        Assert.Equal(FileOpsOperationDisposition.Cancelled, await state.DownloadAsync(path, (_, _) => Task.FromResult(false)));

        Assert.Null(state.LastCommand);
        Assert.Equal(1, stream.DisposeCount);
        Assert.False(stream.CanRead);
    }

    [Fact]
    public async Task DownloadAsync_WhenInvocationAndStreamCleanupBothFail_ShouldPreserveBothActualFailures()
    {
        var service = new ControlledFileOpsServices();
        var callbackFailure = new JSException("Actual browser callback failed");
        var cleanupFailure = new IOException("Actual descriptor release failed");
        var stream = new FailingDownloadStream(cleanupFailure);
        service.OpenDownload = (path, _) => Task.FromResult(ControlledFileOpsServices.Descriptor(path, stream));
        await using var state = new FileOpsWorkspaceController(service.CreateFacade());
        await ObserveAsync(state.InitializeAsync());

        var failure = await Assert.ThrowsAsync<AggregateException>(() => state.DownloadAsync(
            state.SortedEntries[0].FullPath, (_, _) => Task.FromException<bool>(callbackFailure)));

        Assert.Contains(callbackFailure, failure.Flatten().InnerExceptions);
        Assert.Contains(cleanupFailure, failure.Flatten().InnerExceptions);
        Assert.Equal(1, stream.DisposeCount);
        Assert.False(stream.CanRead);
        Assert.False(state.IsBusy);
        Assert.Null(state.LastCommand);
    }
}
