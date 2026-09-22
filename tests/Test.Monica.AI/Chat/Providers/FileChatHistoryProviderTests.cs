using System.Text;
using AwesomeAssertions;
using Monica.AI.Chat.Models;
using Monica.AI.Chat.Providers;

namespace Test.Monica.AI.Chat.Providers;

public sealed class FileChatHistoryProviderTests
{
    [Fact]
    public async Task SaveSessionAsync_WhenReopened_ShouldRetainTranscriptSelectionAndAttachment()
    {
        using var fixture = new FileChatStorageFixture();
        var ct = TestContext.Current.CancellationToken;
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("Persistent evidence"));
        var reference = await fixture.Attachments.SaveAsync(fixture.Partition, "session-one", "evidence.txt", "text/plain", content, ct);
        var saved = await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot(attachment: reference), 0, ct);
        var selected = await fixture.History.SetCurrentSessionAsync(fixture.Partition, "session-one", saved.Revision, ct);

        var reopened = fixture.ReopenHistory();
        var catalog = await reopened.GetCatalogAsync(fixture.Partition, ct);
        var snapshot = await reopened.LoadSessionAsync(fixture.Partition, "session-one", ct);
        var attachment = await fixture.Attachments.ReadAsync(fixture.Partition, "session-one", reference.Id, ct);

        saved.IsPersisted.Should().BeTrue();
        catalog.Revision.Should().Be(selected.Revision);
        catalog.CurrentSessionId.Should().Be("session-one");
        catalog.Sessions.Should().ContainSingle();
        snapshot!.Turns.Single().UserMessage.Parts.Last().Attachment.Should().Be(reference);
        attachment.ExtractedText.Should().Be("Persistent evidence");
    }

    [Fact]
    public async Task SaveSessionAsync_WhenTwoWritersHaveSameRevision_ShouldPublishOnlyOne()
    {
        using var fixture = new FileChatStorageFixture();
        var ct = TestContext.Current.CancellationToken;
        var writes = await Task.WhenAll(
            fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot("one"), 0, ct),
            fixture.ReopenHistory().SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot("two"), 0, ct));

        writes.Count(result => result.IsPersisted).Should().Be(1);
        writes.Count(result => result.Status == ChatHistoryWriteStatus.Conflict).Should().Be(1);
        var catalog = await fixture.History.GetCatalogAsync(fixture.Partition, ct);
        catalog.Revision.Should().Be(1);
        catalog.Sessions.Should().ContainSingle();
    }

    [Fact]
    public async Task LoadSessionAsync_WhenPartitionDiffers_ShouldNotExposeSessionOrSelection()
    {
        using var fixture = new FileChatStorageFixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot(), 0, ct);
        var other = new ChatHistoryPartition("workspace/user-two");

        (await fixture.History.LoadSessionAsync(other, "session-one", ct)).Should().BeNull();
        (await fixture.History.GetCatalogAsync(other, ct)).Sessions.Should().BeEmpty();
        Func<Task> select = () => fixture.History.SetCurrentSessionAsync(other, "session-one", 0, ct);
        await select.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task SaveSessionAsync_WhenAttachmentIsMissing_ShouldKeepPreviouslyPublishedRevision()
    {
        using var fixture = new FileChatStorageFixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot(), 0, ct);
        var missing = new ChatAttachmentReference
        {
            Id = "missing", FileName = "gone.txt", MediaType = "text/plain", Kind = ChatAttachmentKind.Document, Size = 10
        };

        Func<Task> save = () => fixture.History.SaveSessionAsync(fixture.Partition,
            FileChatStorageFixture.Snapshot(attachment: missing) with { Revision = 1 }, 1, ct);
        await save.Should().ThrowAsync<FileNotFoundException>();
        (await fixture.History.GetCatalogAsync(fixture.Partition, ct)).Revision.Should().Be(1);
        (await fixture.History.LoadSessionAsync(fixture.Partition, "session-one", ct))!
            .Turns.Single().UserMessage.Parts.Should().ContainSingle();
    }

    [Fact]
    public async Task SaveSessionAsync_WhenIdContainsPathSegments_ShouldRemainInsideItsHashedPartition()
    {
        using var fixture = new FileChatStorageFixture();
        var ct = TestContext.Current.CancellationToken;
        const string id = "../../outside/session";
        var snapshot = FileChatStorageFixture.Snapshot(id);
        await fixture.History.SaveSessionAsync(fixture.Partition, snapshot, 0, ct);
        await fixture.History.SaveSessionAsync(fixture.Partition, snapshot with { Title = "New title", Revision = 1 }, 1, ct);

        var loaded = await fixture.History.LoadSessionAsync(fixture.Partition, id, ct);
        loaded!.Title.Should().Be("New title");
        var sessionDirectory = fixture.Files.GetPath(ChatStoragePaths.Session(fixture.Partition, id));
        Directory.GetFiles(sessionDirectory, "snapshot-*.json").Should().ContainSingle();
        Path.GetRelativePath(fixture.Options.Value.StorageRootPath, sessionDirectory).Should().NotStartWith("..");
    }

    [Fact]
    public async Task SaveSessionAsync_WhenCatalogWasRefreshedButSessionIsStale_ShouldRejectOverwriteAndResurrection()
    {
        using var fixture = new FileChatStorageFixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot(), 0, ct);
        var original = (await fixture.History.LoadSessionAsync(fixture.Partition, "session-one", ct))!;
        await fixture.History.SaveSessionAsync(fixture.Partition, original with { Title = "Edited elsewhere" }, 1, ct);

        var stale = await fixture.History.SaveSessionAsync(fixture.Partition, original, 2, ct);
        stale.IsConflict.Should().BeTrue();
        (await fixture.History.LoadSessionAsync(fixture.Partition, "session-one", ct))!.Title.Should().Be("Edited elsewhere");
        await fixture.History.DeleteSessionAsync(fixture.Partition, "session-one", 2, ct);
        var resurrect = await fixture.History.SaveSessionAsync(fixture.Partition, original, 3, ct);
        resurrect.IsConflict.Should().BeTrue();
        (await fixture.History.GetCatalogAsync(fixture.Partition, ct)).Sessions.Should().BeEmpty();
    }
}
