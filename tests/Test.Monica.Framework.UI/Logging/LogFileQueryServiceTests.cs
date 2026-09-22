using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Monica.Framework.UI.UILogging.Support;
using Monica.Modules;
using Xunit;

namespace Test.Monica.Framework.UI.Logging;

/// <summary>
/// Verifies log-directory containment independently of the HTTP and filesystem boundaries.
/// </summary>
public sealed class LogFileQueryServiceTests
{
    [Fact]
    public void ResolveFilePath_WhenSiblingDirectorySharesRootPrefix_ShouldRejectTraversalAndAcceptNestedFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "monica-containment", "logs");
        var service = new LogFileQueryService(
            Options.Create(new ModuleLoggingOption()),
            Options.Create(new ModuleLoggingUIOption { LogDirectory = root }),
            NullLogger<LogFileQueryService>.Instance);

        var traverseSibling = () => service.ResolveFilePath("../logs-backup/private.log");

        traverseSibling.Should().Throw<InvalidOperationException>();
        service.ResolveFilePath("archive/日志 %20 #.log")
            .Should().Be(Path.GetFullPath(Path.Combine(root, "archive", "日志 %20 #.log")));
    }
}
