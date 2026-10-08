using System.ComponentModel;
using AwesomeAssertions;
using Monica.Profiling.MemoryDiagnostics.Providers.DotNetTools;
using Xunit;

namespace Test.Monica.Profiling.MemoryDiagnostics.Providers.DotNetTools;

/// <summary>Verifies command and completion policies without launching a diagnostic process.</summary>
public sealed class DotNetGcDumpProviderTests
{
    [Fact]
    public void CreateStartInfo_WhenPathContainsSpacesAndLiteralSyntax_ShouldKeepOneOutputArgumentAndRedirectBothPipes()
    {
        const string path = "fixture host/capture \"literal\" & <tag>.gcdump";

        var startInfo = DotNetGcDumpProvider.CreateStartInfo(12345, path);

        startInfo.FileName.Should().Be("dotnet-gcdump");
        startInfo.Arguments.Should().BeEmpty();
        startInfo.ArgumentList.Should().Equal("collect", "-p", "12345", "-o", path);
        startInfo.UseShellExecute.Should().BeFalse();
        startInfo.RedirectStandardOutput.Should().BeTrue();
        startInfo.RedirectStandardError.Should().BeTrue();
    }

    [Theory]
    [InlineData("capture.gcdump", "capture.gcdump")]
    [InlineData("capture", "capture.gcdump")]
    [InlineData("capture.txt", "capture.txt.gcdump")]
    [InlineData("capture.GCDUMP", "capture.GCDUMP.gcdump")]
    public void ResolveOutputPath_WhenToolWouldAppendItsSuffix_ShouldReturnTheActualAbsoluteDestination(
        string requested, string generated)
    {
        DotNetGcDumpProvider.ResolveOutputPath(requested, 12345).Should().Be(Path.GetFullPath(generated));
    }

    [Fact]
    public void ResolveOutputPath_WhenDefaultIsRequestedRepeatedly_ShouldAvoidSameHostFilenameCollisions()
    {
        var first = DotNetGcDumpProvider.ResolveOutputPath(null, 12345);
        var second = DotNetGcDumpProvider.ResolveOutputPath(null, 12345);

        first.Should().NotBe(second);
        Path.IsPathFullyQualified(first).Should().BeTrue();
        Path.GetDirectoryName(first).Should().Be(Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath())));
        Path.GetExtension(first).Should().Be(".gcdump");
        Path.GetDirectoryName(second).Should().Be(Path.GetDirectoryName(first));
    }

    [Fact]
    public void CreateStartFailure_WhenExecutableIsMissing_ShouldPreserveTheNativeCauseAndDescribeHostPathPrerequisite()
    {
        var cause = new Win32Exception(2, "Controlled executable-not-found failure.");

        var failure = DotNetGcDumpProvider.CreateStartFailure(cause);

        failure.InnerException.Should().BeSameAs(cause);
        failure.Message.Should().Contain("host process PATH").And.Contain("dotnet tool install");
    }

    [Theory]
    [InlineData(5)]
    [InlineData(8)]
    public void CreateStartFailure_WhenNativeStartupFailsForAnotherReason_ShouldNotClaimTheToolIsMissing(int code)
    {
        var cause = new Win32Exception(code, "Controlled native startup failure.");

        var failure = DotNetGcDumpProvider.CreateStartFailure(cause);

        failure.InnerException.Should().BeSameAs(cause);
        failure.Message.Should().Contain($"native error {code}").And.Contain(cause.Message);
        failure.Message.Should().NotContain("could not be found").And.NotContain("dotnet tool install");
    }

    [Theory]
    [InlineData("Controlled stdout collection failure.", "Controlled stderr failure.")]
    [InlineData("Controlled stdout collection failure.", "")]
    [InlineData("", "")]
    public void ValidateCompletion_WhenExitFails_ShouldKeepExitCodeAndBothCapturedStreams(string stdout, string stderr)
    {
        var failure = Assert.Throws<InvalidOperationException>(() =>
            DotNetGcDumpProvider.ValidateCompletion(17, stdout, stderr, "unused.gcdump"));

        failure.Message.Should().Contain("exit code 17");
        failure.Message.Should().Contain($"Standard output:\n{stdout}");
        failure.Message.Should().Contain($"Standard error:\n{stderr}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ValidateCompletion_WhenExitSucceedsWithoutNonemptyArtifact_ShouldRejectFalseSuccess(bool createEmptyFile)
    {
        var directory = Directory.CreateTempSubdirectory("monica-gcdump-completion-");
        var path = Path.Combine(directory.FullName, "structural-output.gcdump");
        try
        {
            if (createEmptyFile)
            {
                File.WriteAllBytes(path, []);
            }

            var failure = Assert.Throws<InvalidOperationException>(() =>
                DotNetGcDumpProvider.ValidateCompletion(0, "Controlled completion.", "", path));

            failure.Message.Should().Contain("did not create a nonempty file").And.Contain(path);
            failure.Message.Should().Contain("Controlled completion.");
        }
        finally
        {
            File.Delete(path);
            directory.Delete();
        }
    }

    [Fact]
    public void ValidateCompletion_WhenExitSucceedsAndArtifactExists_ShouldReturnTheActualAbsoluteFilePath()
    {
        var directory = Directory.CreateTempSubdirectory("monica-gcdump-completion-");
        var path = Path.Combine(directory.FullName, "structural output.gcdump");
        try
        {
            // Structural bytes exercise only existence/length; this is not a diagnostic dump.
            File.WriteAllBytes(path, [1, 2, 3]);

            DotNetGcDumpProvider.ValidateCompletion(0, "Controlled completion.", "", path)
                .Should().Be(new FileInfo(path).FullName);
        }
        finally
        {
            File.Delete(path);
            directory.Delete();
        }
    }
}
