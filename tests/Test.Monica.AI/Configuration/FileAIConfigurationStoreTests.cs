using AwesomeAssertions;
using Monica.AI.Configuration.Models;
using Monica.AI.Configuration.Providers;
using Test.Monica.AI.Support;

namespace Test.Monica.AI.Configuration;

public sealed class FileAIConfigurationStoreTests
{
    [Fact]
    public async Task WriteAsync_WhenIndependentStoresRace_ShouldCommitExactlyOneRevision()
    {
        using var workspace = new ConfigurationTestWorkspace();
        var secondStore = new FileAIConfigurationStore(workspace.Files);
        var document = new AIConfigurationDocument
        {
            Providers = [new AIPersistedProvider { Configuration = AIConfigurationFacadeTests.Provider("saved") }]
        };

        var attempts = await Task.WhenAll(AttemptAsync(workspace.Store), AttemptAsync(secondStore));

        attempts.Count(static succeeded => succeeded).Should().Be(1);
        workspace.Store.Read().Revision.Should().Be(1);
        Directory.EnumerateFiles(workspace.RootPath, "*.tmp", SearchOption.AllDirectories).Should().BeEmpty();
        return;

        async Task<bool> AttemptAsync(FileAIConfigurationStore store)
        {
            try
            {
                await store.WriteAsync(document, 0, TestContext.Current.CancellationToken);
                return true;
            }
            catch (AIConfigurationConflictException) { return false; }
        }
    }

    [Fact]
    public async Task WriteAsync_WhenCancelledBeforeCommit_ShouldKeepPreviousSnapshot()
    {
        using var workspace = new ConfigurationTestWorkspace();
        await workspace.Store.WriteAsync(new AIConfigurationDocument(), 0, TestContext.Current.CancellationToken);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var action = () => workspace.Store.WriteAsync(new AIConfigurationDocument(), 1, cancelled.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
        workspace.Store.Read().Revision.Should().Be(1);
    }

    [Fact]
    public void Read_WhenDocumentIsCorrupt_ShouldSurfaceFailureInsteadOfDroppingSettings()
    {
        using var workspace = new ConfigurationTestWorkspace();
        var path = workspace.Files.GetPath("configuration/providers.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{incomplete");

        var action = () => workspace.Store.Read();

        action.Should().Throw<System.Text.Json.JsonException>();
    }
}
