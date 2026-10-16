using AwesomeAssertions;
using Test.Monica.AI.Chat.Providers;

namespace Test.Monica.AI.Storage.Providers;

public sealed class AIFileStoreTests
{
    [Fact]
    public async Task WriteAsync_WhenSerializationFails_ShouldPreservePublishedDocument()
    {
        using var fixture = new FileChatStorageFixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Files.WriteAsync("atomic.json", new { Value = "original" }, ct);

        Func<Task> write = () => fixture.Files.WriteAsync("atomic.json", new UnserializableDocument(), ct);
        await write.Should().ThrowAsync<InvalidOperationException>();

        var restored = await fixture.Files.ReadAsync<Dictionary<string, string>>("atomic.json", ct);
        restored!["value"].Should().Be("original");
        Directory.GetFiles(fixture.Options.Value.StorageRootPath, "*.tmp").Should().BeEmpty();
    }

    [Theory]
    [InlineData("../escaped.json")]
    [InlineData("nested/../../escaped.json")]
    public void GetPath_WhenPathEscapesRoot_ShouldReject(string path)
    {
        using var fixture = new FileChatStorageFixture();
        Action resolve = () => fixture.Files.GetPath(path);

        resolve.Should().Throw<ArgumentException>();
    }

    private sealed class UnserializableDocument
    {
        public string Value => throw new InvalidOperationException("Serialization interrupted.");
    }
}
