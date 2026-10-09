using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Monica.Framework.UI.UILogging.Models;
using Monica.Modules;
using Xunit;

namespace Test.Monica.Framework.Logging.UILogging.Models;

/// <summary>Protects retained evidence, physical-line positions, filtering and frozen caller ownership.</summary>
public sealed class ScreenLogBufferTests
{
    [Fact]
    public void Reset_WhenHistoryExceedsRetention_ShouldKeepExactAbsolutePositionsForTheLatestDisplayWindow()
    {
        var buffer = CreateBuffer();
        var raw = Enumerable.Range(1, 275).Select(static number => $"physical line {number}").ToArray();

        var snapshot = buffer.Reset(raw);

        snapshot.TotalStoredLines.Should().Be(200);
        snapshot.Lines.Should().HaveCount(100);
        snapshot.Lines.Select(static line => line.Raw).Should().Equal(raw[175..]);
        snapshot.Lines.Select(static line => line.AbsoluteLineNumber).Should().Equal(Enumerable.Range(176, 100).Select(static number => (long?)number));
        snapshot.Lines.Select(static line => line.BufferIndex).Should().Equal(Enumerable.Range(101, 100).Select(static number => (int?)number));
    }

    [Fact]
    public void ApplyFilter_WhenTheCallerKeepsAnEarlierSnapshot_ShouldNeverChangeItsMatchingPositionsOrContent()
    {
        var buffer = CreateBuffer();
        var before = buffer.Reset(["one", "needle", "three"], 10);

        var filtered = buffer.ApplyFilter(LogFilterState.Create("NEEDLE", true));
        buffer.Append("later needle");

        filtered.Lines.Should().ContainSingle().Which.AbsoluteLineNumber.Should().Be(11);
        filtered.Lines[0].FilteredIndex.Should().Be(1);
        filtered.Lines[0].IsMatch.Should().BeTrue();
        before.Lines.Select(static line => line.Raw).Should().Equal("one", "needle", "three");
        before.Lines.Select(static line => line.AbsoluteLineNumber).Should().Equal(10, 11, 12);
        before.Lines.Should().OnlyContain(static line => !line.IsMatch && line.FilteredIndex == null);
        before.Filter.IsActive.Should().BeFalse();
        before.Version.Should().BeLessThan(filtered.Version);
        buffer.Snapshot().Version.Should().BeGreaterThan(filtered.Version);
    }

    [Fact]
    public void ApplyFilter_WhenKeywordAppearsOnlyInTheHeader_ShouldFindThePhysicalLineWithoutInventingMessageHighlights()
    {
        var buffer = CreateBuffer();
        const string raw = "[2026-10-09 08:00:00.000 +00:00 ERR TID7] ordinary message";
        buffer.Reset([raw]);

        var snapshot = buffer.ApplyFilter(LogFilterState.Create("TID7", true));

        snapshot.Lines.Should().ContainSingle().Which.Raw.Should().Be(raw);
        snapshot.Lines[0].IsMatch.Should().BeTrue();
        snapshot.Lines[0].MatchSegments.Should().BeNullOrEmpty();
    }

    [Fact]
    public void ApplyFilter_WhenSeverityAndKeywordAreIndependent_ShouldRetainOnlyTheRequestedSeverityAndMatchScope()
    {
        var buffer = CreateBuffer();
        string[] raw = [
            "[2026-10-09 08:00:00.000 +00:00 INF TID1] needle",
            "[2026-10-09 08:00:01.000 +00:00 ERR TID2] ordinary",
            "[2026-10-09 08:00:02.000 +00:00 ERR TID3] needle" ];
        buffer.Reset(raw);

        var highlightOnly = buffer.ApplyFilter(LogFilterState.Create("needle", false, LogSeverity.Error));
        var captureOnly = buffer.ApplyFilter(LogFilterState.Create("needle", true, LogSeverity.Error));

        highlightOnly.Lines.Select(static line => line.Raw).Should().Equal(raw[1], raw[2]);
        highlightOnly.Lines.Select(static line => line.IsMatch).Should().Equal(false, true);
        captureOnly.Lines.Select(static line => line.Raw).Should().Equal(raw[2]);
        highlightOnly.Lines.Select(static line => line.IsMatch).Should().Equal(false, true);
    }

    [Fact]
    public void ExportCurrent_WhenTheViewIsFiltered_ShouldExportAllRetainedRawPhysicalLinesIncludingWhitespace()
    {
        var buffer = CreateBuffer();
        string[] raw = ["literal %20 # — 日志", "   ", "needle 😀", ""];
        buffer.Reset(raw);
        buffer.ApplyFilter(LogFilterState.Create("needle", true));

        var export = buffer.ExportCurrent();

        export.Should().Contain("# Total Stored Lines: 4");
        export.Should().EndWith(string.Join(Environment.NewLine, raw) + Environment.NewLine);
        buffer.Snapshot().Lines.Should().ContainSingle().Which.Raw.Should().Be("needle 😀");
    }

    [Theory]
    [InlineData(null, "# Filter: Keyword=needle, OnlyCapture=True")]
    [InlineData(LogSeverity.Error, "# Filter: Keyword=needle, OnlyCapture=True, Severity=Error")]
    public void ExportCurrent_WhenSeverityIsOnlyOptionallyConfigured_ShouldPreserveLegacyFramingAndAddSeverityOnlyWhenPresent(
        LogSeverity? severity, string expectedHeader)
    {
        var buffer = CreateBuffer();
        buffer.Reset(["needle"]);
        buffer.ApplyFilter(LogFilterState.Create("needle", true, severity));

        var export = buffer.ExportCurrent();

        var lines = export.Split(Environment.NewLine, StringSplitOptions.None);
        lines[0].Should().StartWith("# Exported at ");
        lines[1].Should().Be(expectedHeader);
        lines[2].Should().Be("# Total Stored Lines: 1");
        lines[3].Should().BeEmpty();
        lines[4].Should().Be("needle");
        lines[5].Should().BeEmpty();
        lines.Should().HaveCount(6);
    }

    [Fact]
    public void WithPositionAndContextTarget_WhenCalledOnARetainedLine_ShouldReturnDetachedCopies()
    {
        var original = LogLineViewModel.FromRaw("needle");

        var located = original.WithPosition(42, 7, 1);
        var contextual = located.WithContextTarget(true);
        var matched = contextual.WithFilter(LogFilterState.Create("needle", true));

        original.AbsoluteLineNumber.Should().BeNull();
        original.IsContextTarget.Should().BeFalse();
        original.IsMatch.Should().BeFalse();
        located.AbsoluteLineNumber.Should().Be(42);
        located.IsContextTarget.Should().BeFalse();
        located.IsMatch.Should().BeFalse();
        contextual.IsContextTarget.Should().BeTrue();
        contextual.IsMatch.Should().BeFalse();
        matched.IsContextTarget.Should().BeTrue();
        matched.IsMatch.Should().BeTrue();
        matched.GetCopyText(LogCopyFormat.WithLineNumber).Should().Be("Line 42: needle");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\t")]
    public void FromRaw_WhenAPhysicalLineContainsOnlyWhitespace_ShouldPreserveExactCopyText(string raw)
    {
        var line = LogLineViewModel.FromRaw(raw);

        line.Raw.Should().Be(raw);
        line.GetCopyText(LogCopyFormat.Raw).Should().Be(raw);
        line.Severity.Should().Be(LogSeverity.Unknown);
    }

    [Fact]
    public void Append_WhenPrependingDiscardedTheNewestRetainedLines_ShouldPreserveTheNextObservedAbsoluteLineNumber()
    {
        var buffer = CreateBuffer();
        buffer.Reset(Enumerable.Range(1, 300).Select(static number => $"line {number}"));
        var earlier = buffer.Prepend(Enumerable.Range(51, 50).Select(static number => $"line {number}"), 51);

        var after = buffer.Append("line 301");

        after.TotalStoredLines.Should().Be(200);
        after.Lines[^1].Raw.Should().Be("line 301");
        after.Lines[^1].AbsoluteLineNumber.Should().Be(301);
        after.Lines[^2].AbsoluteLineNumber.Should().Be(250);
        after.RetainedLines.Select(static line => line.AbsoluteLineNumber).Should().NotContain(251);
        earlier.FirstRetainedLineNumber.Should().Be(51);
        earlier.LastRetainedLineNumber.Should().Be(250);
        earlier.Lines[^1].AbsoluteLineNumber.Should().Be(150);
    }

    [Theory]
    [InlineData(-1, 1, 2)]
    [InlineData(0, 1, 2)]
    [InlineData(1, 1, 2)]
    [InlineData(100, 100, 200)]
    public void Constructor_WhenTheDisplayLimitIsConfigured_ShouldExposeEffectiveBoundedCapacities(int configured, int display, int retained)
    {
        var buffer = new ScreenLogBuffer(Options.Create(new ModuleLoggingUIOption { MaxDisplayLines = configured }));

        buffer.MaxDisplayLines.Should().Be(display);
        buffer.MaxRetainedLines.Should().Be(retained);
    }

    [Fact]
    public void Snapshot_WhenTheCallerAttemptsCollectionMutation_ShouldProtectVisibleAndRetainedEvidence()
    {
        var buffer = CreateBuffer();
        var snapshot = buffer.Reset(["source evidence"]);

        var changeVisible = () => ((IList<LogLineViewModel>)snapshot.Lines)[0] = LogLineViewModel.FromRaw("replacement");
        var changeRetained = () => ((IList<LogLineViewModel>)snapshot.RetainedLines)[0] = LogLineViewModel.FromRaw("replacement");

        changeVisible.Should().Throw<NotSupportedException>();
        changeRetained.Should().Throw<NotSupportedException>();
        snapshot.Lines[0].Raw.Should().Be("source evidence");
        snapshot.RetainedLines[0].Raw.Should().Be("source evidence");
        buffer.Snapshot().Lines[0].Raw.Should().Be("source evidence");
    }

    private static ScreenLogBuffer CreateBuffer() =>
        new(Options.Create(new ModuleLoggingUIOption { MaxDisplayLines = 100 }));
}
