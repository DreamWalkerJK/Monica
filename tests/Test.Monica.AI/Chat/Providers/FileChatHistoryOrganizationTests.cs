using System.Text;
using AwesomeAssertions;
using Monica.AI.Chat.Models;
using Monica.AI.Chat.Providers;

namespace Test.Monica.AI.Chat.Providers;

public sealed class FileChatHistoryOrganizationTests
{
    public static bool IsWindows => OperatingSystem.IsWindows();

    [Fact]
    public async Task SetPinnedAsync_WhenTranscriptIsCheckpointed_ShouldRetainPinAcrossRestartAndKeepDeterministicOrder()
    {
        using var fixture = new FileChatStorageFixture();
        var ct = TestContext.Current.CancellationToken;
        var first = FileChatStorageFixture.Snapshot("a");
        await fixture.History.SaveSessionAsync(fixture.Partition, first, 0, ct);
        await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot("b"), 1, ct);
        await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot("c"), 2, ct);
        var pinned = await fixture.History.SetPinnedAsync(fixture.Partition, "c", true, 3, ct);
        var snapshot = (await fixture.History.LoadSessionAsync(fixture.Partition, "c", ct))!;

        var saved = await fixture.History.SaveSessionAsync(fixture.Partition, snapshot with { Title = "Checkpoint" }, pinned.Revision, ct);
        var catalog = await fixture.ReopenHistory().GetCatalogAsync(fixture.Partition, ct);

        saved.IsPersisted.Should().BeTrue();
        catalog.Sessions.Select(item => item.SessionId).Should().Equal("c", "a", "b");
        catalog.Sessions[0].IsPinned.Should().BeTrue();
        catalog.Sessions[0].UpdatedAt.Should().Be(first.UpdatedAt);
        catalog.Sessions[0].Revision.Should().Be(saved.Revision);
        catalog.Sessions[0].Title.Should().Be("Checkpoint");
    }

    [Fact]
    public async Task SetArchivedAsync_WhenCurrentConversationIsPinned_ShouldRetainContentAndRestoreWithoutSelectionOrPin()
    {
        using var fixture = new FileChatStorageFixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot(), 0, ct);
        var reference = await SaveAttachmentAsync(fixture, "session-one", ct);
        await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot(attachment: reference) with { Revision = 1 }, 1, ct);
        await fixture.History.SetCurrentSessionAsync(fixture.Partition, "session-one", 2, ct);
        await fixture.History.SetPinnedAsync(fixture.Partition, "session-one", true, 3, ct);

        var archived = await fixture.History.SetArchivedAsync(fixture.Partition, ["session-one"], true, 4, ct);
        var reopened = fixture.ReopenHistory();
        var catalog = await reopened.GetCatalogAsync(fixture.Partition, ct);
        var summary = catalog.Sessions.Should().ContainSingle().Subject;
        catalog.CurrentSessionId.Should().BeNull();
        summary.IsPinned.Should().BeFalse();
        summary.ArchivedAt.Should().NotBeNull();
        summary.Revision.Should().Be(2);
        (await reopened.LoadSessionAsync(fixture.Partition, "session-one", ct))!
            .Turns.Single().UserMessage.Parts.Last().Attachment.Should().Be(reference);
        (await fixture.Attachments.ReadAsync(fixture.Partition, "session-one", reference.Id, ct)).ExtractedText.Should().Be("Retained evidence");

        Func<Task> select = () => reopened.SetCurrentSessionAsync(fixture.Partition, "session-one", archived.Revision, ct);
        Func<Task> pin = () => reopened.SetPinnedAsync(fixture.Partition, "session-one", true, archived.Revision, ct);
        await select.Should().ThrowAsync<InvalidOperationException>();
        await pin.Should().ThrowAsync<InvalidOperationException>();

        var unchanged = await reopened.SetArchivedAsync(fixture.Partition, ["session-one"], true, archived.Revision, ct);
        (await reopened.GetCatalogAsync(fixture.Partition, ct)).Sessions.Single().ArchivedAt.Should().Be(summary.ArchivedAt);
        await reopened.SetArchivedAsync(fixture.Partition, ["session-one"], false, unchanged.Revision, ct);
        var restored = await fixture.ReopenHistory().GetCatalogAsync(fixture.Partition, ct);
        restored.CurrentSessionId.Should().BeNull();
        restored.Sessions.Single().ArchivedAt.Should().BeNull();
        restored.Sessions.Single().IsPinned.Should().BeFalse();
        restored.Sessions.Single().Revision.Should().Be(2);
    }

    [Fact]
    public async Task SaveSessionAsync_WhenRunningConversationWasArchivedElsewhere_ShouldRejectCheckpointEvenAfterCatalogRefresh()
    {
        using var fixture = new FileChatStorageFixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot(), 0, ct);
        var running = (await fixture.History.LoadSessionAsync(fixture.Partition, "session-one", ct))!;
        var archived = await fixture.ReopenHistory().SetArchivedAsync(fixture.Partition, ["session-one"], true, 1, ct);

        var staleCatalog = await fixture.History.SaveSessionAsync(fixture.Partition, running with { Title = "Late response" }, 1, ct);
        var refreshedCatalog = await fixture.History.SaveSessionAsync(fixture.Partition, running with { Title = "Late response" }, archived.Revision, ct);

        staleCatalog.IsConflict.Should().BeTrue();
        refreshedCatalog.IsConflict.Should().BeTrue();
        var catalog = await fixture.ReopenHistory().GetCatalogAsync(fixture.Partition, ct);
        catalog.Revision.Should().Be(archived.Revision);
        catalog.Sessions.Single().ArchivedAt.Should().NotBeNull();
        (await fixture.History.LoadSessionAsync(fixture.Partition, "session-one", ct))!.Title.Should().Be(running.Title);
    }

    [Fact]
    public async Task SetArchivedAsync_WhenPartitionDiffers_ShouldNotReadOrMutateOtherOwnersOrganization()
    {
        using var fixture = new FileChatStorageFixture();
        var ct = TestContext.Current.CancellationToken;
        var other = new ChatHistoryPartition("workspace/user-two");
        await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot(), 0, ct);
        await fixture.History.SaveSessionAsync(other, FileChatStorageFixture.Snapshot(), 0, ct);
        await fixture.History.SetPinnedAsync(other, "session-one", true, 1, ct);
        await fixture.History.SaveSessionAsync(other, FileChatStorageFixture.Snapshot("other-only"), 2, ct);
        await fixture.History.SetArchivedAsync(fixture.Partition, ["session-one"], true, 1, ct);

        Func<Task> archiveOther = () => fixture.History.SetArchivedAsync(fixture.Partition, ["other-only"], true, 2, ct);
        await archiveOther.Should().ThrowAsync<KeyNotFoundException>();
        var otherCatalog = await fixture.History.GetCatalogAsync(other, ct);
        otherCatalog.Revision.Should().Be(3);
        otherCatalog.Sessions.Single(item => item.SessionId == "session-one").IsPinned.Should().BeTrue();
        otherCatalog.Sessions.Should().OnlyContain(item => item.ArchivedAt == null);
    }

    [Theory]
    [InlineData("pin")]
    [InlineData("archive")]
    [InlineData("restore")]
    [InlineData("delete")]
    public async Task Organization_WhenCatalogRevisionIsStale_ShouldRejectWithoutChangingPersistedState(string operation)
    {
        using var fixture = new FileChatStorageFixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot(), 0, ct);
        await fixture.History.SetArchivedAsync(fixture.Partition, ["session-one"], true, 1, ct);

        var result = operation switch
        {
            "pin" => await fixture.History.SetPinnedAsync(fixture.Partition, "session-one", true, 1, ct),
            "archive" => await fixture.History.SetArchivedAsync(fixture.Partition, ["session-one"], true, 1, ct),
            "restore" => await fixture.History.SetArchivedAsync(fixture.Partition, ["session-one"], false, 1, ct),
            _ => await fixture.History.DeleteArchivedSessionsAsync(fixture.Partition, ["session-one"], 1, ct)
        };

        result.IsConflict.Should().BeTrue();
        result.FailureReason.Should().Be(ChatHistoryWriteFailureReason.Conflict);
        result.Revision.Should().Be(2);
        var catalog = await fixture.History.GetCatalogAsync(fixture.Partition, ct);
        catalog.Revision.Should().Be(2);
        catalog.Sessions.Single().ArchivedAt.Should().NotBeNull();
        (await fixture.History.LoadSessionAsync(fixture.Partition, "session-one", ct)).Should().NotBeNull();
    }

    [Fact]
    public async Task SetArchivedAsync_WhenSelectionIncludesMissingConversation_ShouldRejectCompleteBatch()
    {
        using var fixture = new FileChatStorageFixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot(), 0, ct);
        await fixture.History.SetPinnedAsync(fixture.Partition, "session-one", true, 1, ct);
        await fixture.History.SetCurrentSessionAsync(fixture.Partition, "session-one", 2, ct);

        Func<Task> archive = () => fixture.History.SetArchivedAsync(fixture.Partition, ["session-one", "missing"], true, 3, ct);
        await archive.Should().ThrowAsync<KeyNotFoundException>();

        var catalog = await fixture.History.GetCatalogAsync(fixture.Partition, ct);
        catalog.Revision.Should().Be(3);
        catalog.CurrentSessionId.Should().Be("session-one");
        catalog.Sessions.Single().IsPinned.Should().BeTrue();
        catalog.Sessions.Single().ArchivedAt.Should().BeNull();
    }

    [Theory]
    [InlineData("active")]
    [InlineData("missing")]
    public async Task DeleteArchivedSessionsAsync_WhenSelectionContainsIneligibleConversation_ShouldKeepCompleteBatch(string ineligibleId)
    {
        using var fixture = new FileChatStorageFixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot(), 0, ct);
        await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot("active"), 1, ct);
        await fixture.History.SetArchivedAsync(fixture.Partition, ["session-one"], true, 2, ct);

        Func<Task> delete = () => fixture.History.DeleteArchivedSessionsAsync(fixture.Partition, ["session-one", ineligibleId], 3, ct);
        if (ineligibleId == "active") await delete.Should().ThrowAsync<InvalidOperationException>();
        else await delete.Should().ThrowAsync<KeyNotFoundException>();

        var catalog = await fixture.History.GetCatalogAsync(fixture.Partition, ct);
        catalog.Revision.Should().Be(3);
        catalog.Sessions.Should().HaveCount(2);
        (await fixture.History.LoadSessionAsync(fixture.Partition, "session-one", ct)).Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteArchivedSessionsAsync_WhenSelectionIsValid_ShouldRemoveSnapshotsAndAttachmentsWithoutResurrection()
    {
        using var fixture = new FileChatStorageFixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot("one"), 0, ct);
        await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot("two"), 1, ct);
        var one = await SaveAttachmentAsync(fixture, "one", ct);
        var two = await SaveAttachmentAsync(fixture, "two", ct);
        await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot("one", one) with { Revision = 1 }, 2, ct);
        await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot("two", two) with { Revision = 2 }, 3, ct);
        await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot("active"), 4, ct);
        var stale = (await fixture.History.LoadSessionAsync(fixture.Partition, "one", ct))!;
        await fixture.History.SetArchivedAsync(fixture.Partition, ["one", "two"], true, 5, ct);

        var deleted = await fixture.History.DeleteArchivedSessionsAsync(fixture.Partition, ["one", "two", "one"], 6, ct);
        var reopened = fixture.ReopenHistory();
        var catalog = await reopened.GetCatalogAsync(fixture.Partition, ct);

        deleted.IsPersisted.Should().BeTrue();
        deleted.Warning.Should().BeNull();
        catalog.Revision.Should().Be(7);
        catalog.Sessions.Should().ContainSingle().Which.SessionId.Should().Be("active");
        Directory.Exists(fixture.Files.GetPath(ChatStoragePaths.Session(fixture.Partition, "one"))).Should().BeFalse();
        Directory.Exists(fixture.Files.GetPath(ChatStoragePaths.Session(fixture.Partition, "two"))).Should().BeFalse();
        (await reopened.LoadSessionAsync(fixture.Partition, "one", ct)).Should().BeNull();
        (await reopened.SaveSessionAsync(fixture.Partition, stale, catalog.Revision, ct)).IsConflict.Should().BeTrue();
        Func<Task> readAttachment = () => fixture.Attachments.ReadAsync(fixture.Partition, "two", two.Id, ct);
        await readAttachment.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact(SkipUnless = nameof(IsWindows), Skip = "Windows file sharing supplies the deterministic deletion failure.")]
    public async Task DeleteArchivedSessionsAsync_WhenFileIsLocked_ShouldWarnAndRetryAfterRestartWithoutReusingPendingId()
    {
        using var fixture = new FileChatStorageFixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot(), 0, ct);
        var attachment = await SaveAttachmentAsync(fixture, "session-one", ct);
        await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot(attachment: attachment) with { Revision = 1 }, 1, ct);
        await fixture.History.SetArchivedAsync(fixture.Partition, ["session-one"], true, 2, ct);
        var attachmentPath = fixture.Files.GetPath(ChatStoragePaths.Attachment(fixture.Partition, "session-one", attachment.Id) + ".json");
        var sessionDirectory = fixture.Files.GetPath(ChatStoragePaths.Session(fixture.Partition, "session-one"));
        ChatHistoryWriteResult deleted;

        // Windows denies deletion while this handle omits FileShare.Delete; the failure is owned by this scenario.
        await using (var locked = new FileStream(attachmentPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            deleted = await fixture.History.DeleteArchivedSessionsAsync(fixture.Partition, ["session-one"], 3, ct);
            deleted.IsPersisted.Should().BeTrue();
            deleted.Warning.Should().NotBeNullOrWhiteSpace();
            (await fixture.ReopenHistory().GetCatalogAsync(fixture.Partition, ct)).Sessions.Should().BeEmpty();
            (await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot(), deleted.Revision, ct))
                .IsConflict.Should().BeTrue();
            File.Exists(attachmentPath).Should().BeTrue();
        }

        var beforeRetry = await File.ReadAllTextAsync(fixture.Files.GetPath(ChatStoragePaths.Manifest(fixture.Partition)), ct);
        var catalog = await fixture.ReopenHistory().GetCatalogAsync(fixture.Partition, ct);
        catalog.Revision.Should().Be(deleted.Revision);
        Directory.Exists(sessionDirectory).Should().BeFalse();
        (await File.ReadAllTextAsync(fixture.Files.GetPath(ChatStoragePaths.Manifest(fixture.Partition)), ct)).Should().Be(beforeRetry);

        var reused = await fixture.ReopenHistory().SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot(), catalog.Revision, ct);
        reused.IsPersisted.Should().BeTrue();
        (await fixture.ReopenHistory().GetCatalogAsync(fixture.Partition, ct)).Sessions.Should().ContainSingle();
        (await fixture.ReopenHistory().LoadSessionAsync(fixture.Partition, "session-one", ct)).Should().NotBeNull();
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("session-one")]
    public async Task GetCatalogAsync_WhenDeletionQueueIsInvalid_ShouldRejectBeforeDeletingFiles(string pendingId)
    {
        using var fixture = new FileChatStorageFixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot(), 0, ct);
        var path = ChatStoragePaths.Manifest(fixture.Partition);
        var catalog = (await fixture.Files.ReadAsync<FileChatCatalog>(path, ct))!;
        catalog.PendingDeletionSessionIds.Add(pendingId);
        await fixture.Files.WriteAsync(path, catalog, ct);

        Func<Task> read = () => fixture.ReopenHistory().GetCatalogAsync(fixture.Partition, ct);
        await read.Should().ThrowAsync<InvalidDataException>();

        Directory.Exists(fixture.Files.GetPath(ChatStoragePaths.Session(fixture.Partition, "session-one"))).Should().BeTrue();
    }

    private static async Task<ChatAttachmentReference> SaveAttachmentAsync(FileChatStorageFixture fixture, string sessionId, CancellationToken ct)
    {
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("Retained evidence"));
        return await fixture.Attachments.SaveAsync(fixture.Partition, sessionId, "evidence.txt", "text/plain", content, ct);
    }
}
