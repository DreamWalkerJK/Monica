using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Monica.Framework.UI.UILogging.State;
using Monica.Framework.UI.UILogging.Support;
using Monica.Logging.Services.Support;
using Monica.Modules;
using Xunit;

namespace Test.Monica.Framework.Logging.UILogging.Support;

/// <summary>Verifies the same file-location contract drives Serilog, the registry and independent readers.</summary>
public sealed class LogFileLocationTests
{
    [Fact]
    public void Resolve_WhenOnlyTheLoggingDirectoryIsConfigured_ShouldUseItsLogsChildForWriterAndReader()
    {
        var root = Path.Combine(Path.GetTempPath(), "monica-location-contract");
        var option = new ModuleLoggingOption { LogDirectory = root, LogFileName = "named.log" };
        var expected = Path.GetFullPath(Path.Combine(root, "Logs", "named.log"));

        LogFileLocation.Resolve(option).Should().Be(expected);
        CreateTail(option, new ModuleLoggingUIOption()).LogFilePath.Should().Be(expected);
        CreateQuery(option, new ModuleLoggingUIOption()).LogDirectory.Should().Be(Path.GetDirectoryName(expected));
    }

    [Fact]
    public void Resolve_WhenAnExplicitFilePathIsConfigured_ShouldIgnoreTheDirectoryAndFileName()
    {
        var explicitPath = Path.Combine(Path.GetTempPath(), "monica-explicit-location", "actual.log");
        var option = new ModuleLoggingOption
        {
            LogFilePath = explicitPath,
            LogDirectory = Path.Combine(Path.GetTempPath(), "ignored-directory"),
            LogFileName = "ignored-name.log"
        };

        LogFileLocation.Resolve(option).Should().Be(Path.GetFullPath(explicitPath));
        CreateTail(option, new ModuleLoggingUIOption()).LogFilePath.Should().Be(Path.GetFullPath(explicitPath));
        CreateQuery(option, new ModuleLoggingUIOption()).LogDirectory.Should().Be(Path.GetDirectoryName(explicitPath));
    }

    [Fact]
    public void Resolve_WhenTheUiDirectoryOverridesTheWriter_ShouldRetainTheWriterFileNameInsideThatDirectory()
    {
        var option = new ModuleLoggingOption { LogFilePath = Path.Combine(Path.GetTempPath(), "writer", "explicit.log") };
        var uiOption = new ModuleLoggingUIOption { LogDirectory = Path.Combine(Path.GetTempPath(), "reader") };

        CreateTail(option, uiOption).LogFilePath.Should().Be(Path.GetFullPath(Path.Combine(uiOption.LogDirectory, "explicit.log")));
        CreateQuery(option, uiOption).LogDirectory.Should().Be(Path.GetFullPath(uiOption.LogDirectory));
    }

    [Fact]
    public async Task ModuleLogging_WhenTwoHostsWriteToTheirOwnDirectories_ShouldResolveEachSinkAndRetireOnlyItsOwnWriter()
    {
        using var firstFactory = new LoggingUITestApplicationFactory(
            configureLogging: static option => { option.EnableFileSink = true; option.LogFileName = "first.log"; },
            configureLoggingUi: static option => option.LogDirectory = null);
        using var secondFactory = new LoggingUITestApplicationFactory(
            configureLogging: static option => { option.EnableFileSink = true; option.LogFileName = "second.log"; },
            configureLoggingUi: static option => option.LogDirectory = null);
        await using var first = await firstFactory.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        await using var second = await secondFactory.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        var firstService = first.Services.GetRequiredService<LoggingService>();
        var secondService = second.Services.GetRequiredService<LoggingService>();
        var firstPath = Path.Combine(firstFactory.LogDirectory, "Logs", "first.log");
        var secondPath = Path.Combine(secondFactory.LogDirectory, "Logs", "second.log");
        firstService.CurrentLogFilePath.Should().Be(firstPath);
        secondService.CurrentLogFilePath.Should().Be(secondPath);
        firstService.LogDirectory.Should().Be(Path.GetDirectoryName(firstPath));
        secondService.LogDirectory.Should().Be(Path.GetDirectoryName(secondPath));

        first.Services.GetRequiredService<ILogger<LogFileLocationTests>>().LogWarning("First host marker: reader-one");
        second.Services.GetRequiredService<ILogger<LogFileLocationTests>>().LogWarning("Second host marker: reader-two");
        await first.DisposeAsync(); // The Serilog async sink is flushed at its actual host lifetime boundary.
        second.Services.GetRequiredService<ILogger<LogFileLocationTests>>().LogWarning("Second host marker after first retired: reader-two-alive");
        await second.DisposeAsync();

        var firstText = await File.ReadAllTextAsync(firstPath, TestContext.Current.CancellationToken);
        var secondText = await File.ReadAllTextAsync(secondPath, TestContext.Current.CancellationToken);
        firstText.Should().Contain("reader-one").And.NotContain("reader-two");
        secondText.Should().Contain("reader-two").And.Contain("reader-two-alive").And.NotContain("reader-one");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(4294967295L)]
    public void LogTailService_WhenThePollingIntervalCannotBeScheduled_ShouldRejectItBeforeCreatingAReader(long milliseconds)
    {
        var construct = () => CreateTail(new ModuleLoggingOption(), new ModuleLoggingUIOption
        {
            PollingInterval = TimeSpan.FromMilliseconds(milliseconds)
        });

        construct.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static LogTailService CreateTail(ModuleLoggingOption option, ModuleLoggingUIOption uiOption) =>
        new(Options.Create(option), Options.Create(uiOption), NullLogger<LogTailService>.Instance);

    private static LogFileQueryService CreateQuery(ModuleLoggingOption option, ModuleLoggingUIOption uiOption) =>
        new(Options.Create(option), Options.Create(uiOption), NullLogger<LogFileQueryService>.Instance);
}
