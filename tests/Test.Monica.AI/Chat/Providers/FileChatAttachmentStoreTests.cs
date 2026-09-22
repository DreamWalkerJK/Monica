using System.IO.Compression;
using System.Text;
using AwesomeAssertions;
using Monica.AI.Chat.Models;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Test.Monica.AI.Chat.Providers;

public sealed class FileChatAttachmentStoreTests
{
    [Fact]
    public async Task DeleteAsync_WhenAttachmentIsRetained_ShouldRequireDeletingConversation()
    {
        using var fixture = new FileChatStorageFixture();
        var ct = TestContext.Current.CancellationToken;
        await using var stream = new MemoryStream("retained text"u8.ToArray());
        var reference = await fixture.Attachments.SaveAsync(fixture.Partition, "session-one", "document.txt", "image/png", stream, ct);
        reference.MediaType.Should().Be("text/plain");
        await fixture.History.SaveSessionAsync(fixture.Partition, FileChatStorageFixture.Snapshot(attachment: reference), 0, ct);

        Func<Task> remove = () => fixture.Attachments.DeleteAsync(fixture.Partition, "session-one", reference.Id, ct);
        await remove.Should().ThrowAsync<InvalidOperationException>();
        await fixture.History.DeleteSessionAsync(fixture.Partition, "session-one", 1, ct);
        Func<Task> read = () => fixture.Attachments.ReadAsync(fixture.Partition, "session-one", reference.Id, ct);
        await read.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ReadAsync_WhenPartitionOrSessionDiffers_ShouldNotExposeAttachment()
    {
        using var fixture = new FileChatStorageFixture();
        var ct = TestContext.Current.CancellationToken;
        await using var stream = new MemoryStream("private text"u8.ToArray());
        var reference = await fixture.Attachments.SaveAsync(fixture.Partition, "session-one", "document.txt", "text/plain", stream, ct);

        Func<Task> crossUser = () => fixture.Attachments.ReadAsync(new ChatHistoryPartition("workspace/user-two"), "session-one", reference.Id, ct);
        Func<Task> crossSession = () => fixture.Attachments.ReadAsync(fixture.Partition, "session-two", reference.Id, ct);
        await crossUser.Should().ThrowAsync<FileNotFoundException>();
        await crossSession.Should().ThrowAsync<FileNotFoundException>();
        await fixture.Attachments.DeleteAsync(fixture.Partition, "session-one", reference.Id, ct);
        Func<Task> removed = () => fixture.Attachments.ReadAsync(fixture.Partition, "session-one", reference.Id, ct);
        await removed.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task SaveAsync_WhenDocxContainsParagraphs_ShouldExtractReadableText()
    {
        using var fixture = new FileChatStorageFixture();
        var ct = TestContext.Current.CancellationToken;
        await using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            await using var writer = new StreamWriter(zip.CreateEntry("word/document.xml").Open(), Encoding.UTF8);
            await writer.WriteAsync("""
                <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:body><w:p><w:r><w:t>First paragraph</w:t></w:r></w:p>
                  <w:p><w:r><w:t>Second paragraph</w:t></w:r></w:p></w:body>
                </w:document>
                """);
        }
        stream.Position = 0;

        var reference = await fixture.Attachments.SaveAsync(fixture.Partition, "session-one", "source.docx", "", stream, ct);
        var attachment = await fixture.Attachments.ReadAsync(fixture.Partition, "session-one", reference.Id, ct);

        reference.HasExtractedText.Should().BeTrue();
        attachment.ExtractedText.Should().Contain("First paragraph").And.Contain("Second paragraph");
        stream.CanRead.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_WhenPdfContainsText_ShouldRetainOriginalBytesAndExtractText()
    {
        using var fixture = new FileChatStorageFixture();
        var ct = TestContext.Current.CancellationToken;
        var document = new PdfDocumentBuilder();
        var page = document.AddPage(PageSize.A4);
        var font = document.AddStandard14Font(Standard14Font.Helvetica);
        page.AddText("PDF evidence survives restart", 12, new PdfPoint(30, 700), font);
        var bytes = document.Build();
        await using var stream = new MemoryStream(bytes);

        var reference = await fixture.Attachments.SaveAsync(fixture.Partition, "session-one", "evidence.pdf", "application/pdf", stream, ct);
        var attachment = await fixture.Attachments.ReadAsync(fixture.Partition, "session-one", reference.Id, ct);

        reference.HasExtractedText.Should().BeTrue();
        attachment.ExtractedText.Should().Contain("PDF evidence survives restart");
        attachment.Data.Should().Equal(bytes);
    }

    [Fact]
    public async Task SaveAsync_WhenExtractedDocumentExceedsLimit_ShouldRejectWithoutTruncation()
    {
        using var fixture = new FileChatStorageFixture();
        fixture.Options.Value.MaxExtractedDocumentCharacters = 4;
        await using var stream = new MemoryStream("longer document"u8.ToArray());
        Func<Task> save = () => fixture.Attachments.SaveAsync(fixture.Partition, "session-one", "source.txt", "text/plain", stream,
            TestContext.Current.CancellationToken);

        await save.Should().ThrowAsync<InvalidDataException>();
    }

    [Theory]
    [InlineData("pretend.png")]
    [InlineData("pretend.pdf")]
    [InlineData("script.exe")]
    public async Task SaveAsync_WhenFileTypeIsInvalid_ShouldReject(string name)
    {
        using var fixture = new FileChatStorageFixture();
        await using var stream = new MemoryStream("not the claimed content"u8.ToArray());
        Func<Task> save = () => fixture.Attachments.SaveAsync(fixture.Partition, "session-one", name, "text/plain", stream,
            TestContext.Current.CancellationToken);

        await save.Should().ThrowAsync<Exception>();
    }
}
