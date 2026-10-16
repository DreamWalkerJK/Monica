using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Monica.AI.AgentCapabilities.Models;
using Monica.AI.AgentCapabilities.Services;
using Test.Monica.AI.Chat.Providers;

namespace Test.Monica.AI.AgentCapabilities.Services;

public sealed class FileAgentCapabilityStateStoreTests
{
    [Fact]
    public async Task UpdateAsync_WhenIndependentWritersChangeDifferentEntries_ShouldRetainBothAfterReopen()
    {
        using var fixture = new FileChatStorageFixture();
        var first = new FileAgentCapabilityStateStore(fixture.Files, NullLogger<FileAgentCapabilityStateStore>.Instance);
        var second = new FileAgentCapabilityStateStore(fixture.Files, NullLogger<FileAgentCapabilityStateStore>.Instance);
        var ct = TestContext.Current.CancellationToken;

        await Task.WhenAll(
            first.UpdateAsync(state => state.SetEntryEnabled(AgentCapabilityKind.Skill, "analysis", false), ct),
            second.UpdateAsync(state => state.SetEntryEnabled(AgentCapabilityKind.Mcp, "search", false), ct));
        var restored = await new FileAgentCapabilityStateStore(fixture.Files, NullLogger<FileAgentCapabilityStateStore>.Instance).LoadAsync(ct);

        restored.IsEntryEnabled(AgentCapabilityKind.Skill, "analysis").Should().BeFalse();
        restored.IsEntryEnabled(AgentCapabilityKind.Mcp, "search").Should().BeFalse();
        restored.Revision.Should().Be(3);
    }

    [Fact]
    public async Task LoadAsync_WhenStateIsCorrupt_ShouldNotSilentlyEnableTools()
    {
        using var fixture = new FileChatStorageFixture();
        var path = fixture.Files.GetPath("capabilities.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, "{broken", TestContext.Current.CancellationToken);
        var store = new FileAgentCapabilityStateStore(fixture.Files, NullLogger<FileAgentCapabilityStateStore>.Instance);

        Func<Task> read = () => store.LoadAsync(TestContext.Current.CancellationToken);

        await read.Should().ThrowAsync<JsonException>();
        (await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken)).Should().Be("{broken");
    }
}
