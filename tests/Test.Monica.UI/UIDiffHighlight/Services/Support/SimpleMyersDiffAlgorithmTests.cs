using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Monica.Modules;
using Monica.UI.UIDiffHighlight.Models;
using Monica.UI.UIDiffHighlight.Services;
using Monica.UI.UIDiffHighlight.Services.Support;
using Xunit;

namespace Test.Monica.UI.UIDiffHighlight.Services.Support;

public sealed class SimpleMyersDiffAlgorithmTests
{
    [Theory]
    [InlineData("abc", "abcXYZ", EDiffLineType.Added, 3, "XYZ")]
    [InlineData("abcXYZ", "abc", EDiffLineType.Deleted, 3, "XYZ")]
    [InlineData("A😀", "A😀尾🚀", EDiffLineType.Added, 3, "尾🚀")]
    [InlineData("A😀尾🚀", "A😀", EDiffLineType.Deleted, 3, "尾🚀")]
    public void ComputeCharacterDiff_WhenOnlySuffixChanges_ShouldReturnCompleteSideRelativeUtf16Range(
        string oldText, string newText, EDiffLineType type, int start, string content)
    {
        var ranges = new SimpleMyersDiffAlgorithm().ComputeCharacterDiff(oldText, newText, new());

        AssertRanges(ranges, (type, start, content.Length, content));
    }

    [Fact]
    public void ComputeCharacterDiff_WhenNoCharactersAreShared_ShouldReturnBothCompleteSides()
    {
        var ranges = new SimpleMyersDiffAlgorithm().ComputeCharacterDiff("abc", "XYZ", new());

        AssertRanges(ranges, (EDiffLineType.Deleted, 0, 3, "abc"), (EDiffLineType.Added, 0, 3, "XYZ"));
    }

    [Fact]
    public void ComputeCharacterDiff_WhenInteriorReplacementAndSuffixAdditionCoexist_ShouldRetainBothChanges()
    {
        var ranges = new SimpleMyersDiffAlgorithm().ComputeCharacterDiff("abc", "axcZ", new());

        AssertRanges(ranges,
            (EDiffLineType.Deleted, 1, 1, "b"),
            (EDiffLineType.Added, 1, 1, "x"),
            (EDiffLineType.Added, 3, 1, "Z"));
    }

    [Fact]
    public void ComputeCharacterDiff_WhenChangesAreSeparatedBySharedCharacters_ShouldKeepDistinctCoordinates()
    {
        var ranges = new SimpleMyersDiffAlgorithm().ComputeCharacterDiff("a1b2c3d", "aXbYcZd", new());

        AssertRanges(ranges,
            (EDiffLineType.Deleted, 1, 1, "1"), (EDiffLineType.Added, 1, 1, "X"),
            (EDiffLineType.Deleted, 3, 1, "2"), (EDiffLineType.Added, 3, 1, "Y"),
            (EDiffLineType.Deleted, 5, 1, "3"), (EDiffLineType.Added, 5, 1, "Z"));
    }

    [Theory]
    [InlineData("", "xyz", EDiffLineType.Added)]
    [InlineData("xyz", "", EDiffLineType.Deleted)]
    [InlineData("", "😀尾", EDiffLineType.Added)]
    [InlineData("😀尾", "", EDiffLineType.Deleted)]
    public void ComputeCharacterDiff_WhenOneSideIsEmpty_ShouldReturnEntireOtherSide(
        string oldText, string newText, EDiffLineType type)
    {
        var ranges = new SimpleMyersDiffAlgorithm().ComputeCharacterDiff(oldText, newText, new());
        var content = type == EDiffLineType.Added ? newText : oldText;

        AssertRanges(ranges, (type, 0, content.Length, content));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("unchanged😀", "unchanged😀")]
    public void ComputeCharacterDiff_WhenTextsAreIdentical_ShouldReturnNoRanges(string oldText, string newText)
    {
        new SimpleMyersDiffAlgorithm().ComputeCharacterDiff(oldText, newText, new()).Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ComputeCharacterDiff_WhenEmptySideComparisonExceedsLcsLimit_ShouldPreserveFastPath(bool addition)
    {
        var text = new string('x', 1001);
        var ranges = new SimpleMyersDiffAlgorithm().ComputeCharacterDiff(addition ? "" : text, addition ? text : "", new());

        AssertRanges(ranges, (addition ? EDiffLineType.Added : EDiffLineType.Deleted, 0, text.Length, text));
    }

    [Theory]
    [InlineData(EDiffHighlightMode.Character)]
    [InlineData(EDiffHighlightMode.Mixed)]
    public void Highlight_WhenEligibleLineHasAddedSuffix_ShouldPreserveInputsNumberingAndModifiedStatistics(EDiffHighlightMode mode)
    {
        var result = CreateService().Highlight("same\nabc", "same\nabcZ", new() { Mode = mode });

        result.Lines.Select(line => (line.Type, line.OldLineNumber, line.NewLineNumber, line.OldContent, line.NewContent))
            .Should().Equal(
                (EDiffLineType.Unchanged, 1, 1, "same", "same"),
                (EDiffLineType.Modified, 2, 2, "abc", "abcZ"));
        AssertRanges(result.Lines[1].CharacterDiffs!, (EDiffLineType.Added, 3, 1, "Z"));
        AssertStatistics(result.Statistics, 0, 0, 1, 1, 2, 2);
    }

    [Fact]
    public void Highlight_WhenConfigurationUsesDefaultLineMode_ShouldKeepSeparateAddedDeletedLinesAndStatistics()
    {
        var result = CreateService().Highlight("same\nabc", "same\nabcZ");

        result.Options.Mode.Should().Be(EDiffHighlightMode.Line);
        result.Lines.Select(line => (line.Type, line.OldLineNumber, line.NewLineNumber, line.OldContent, line.NewContent))
            .Should().Equal(
                (EDiffLineType.Unchanged, 1, 1, "same", "same"),
                (EDiffLineType.Deleted, 2, 0, "abc", ""),
                (EDiffLineType.Added, 0, 2, "", "abcZ"));
        result.Lines.Should().OnlyContain(line => line.CharacterDiffs == null);
        AssertStatistics(result.Statistics, 1, 1, 0, 1, 2, 2);
    }

    [Fact]
    public void ComputeDiff_WhenNormalizedLinesAreEqual_ShouldKeepOriginalTextAndNoCharacterRanges()
    {
        var lines = new SimpleMyersDiffAlgorithm().ComputeDiff(["  AbC  "], ["abc"],
            new() { Mode = EDiffHighlightMode.Character, IgnoreCase = true, IgnoreWhitespace = true });

        var line = lines.Should().ContainSingle().Subject;
        line.Type.Should().Be(EDiffLineType.Unchanged);
        line.OldContent.Should().Be("  AbC  ");
        line.NewContent.Should().Be("abc");
        line.OldLineNumber.Should().Be(1);
        line.NewLineNumber.Should().Be(1);
        line.CharacterDiffs.Should().BeNull();
    }

    [Theory]
    [InlineData(EDiffHighlightMode.Character)]
    [InlineData(EDiffHighlightMode.Mixed)]
    public void ComputeDiff_WhenConfiguredCharacterLimitIsExceeded_ShouldKeepSeparateLineChanges(EDiffHighlightMode mode)
    {
        var lines = new SimpleMyersDiffAlgorithm().ComputeDiff(["abc"], ["abcZ"],
            new() { Mode = mode, MaxCharacterDiffLength = 3 });

        lines.Select(line => line.Type).Should().Equal(EDiffLineType.Deleted, EDiffLineType.Added);
        lines.Should().OnlyContain(line => line.CharacterDiffs == null);
    }

    [Fact]
    public void ComputeDiff_WhenFixedLcsLimitIsExceeded_ShouldPreserveSkippedCharacterComparison()
    {
        var oldText = new string('a', 1001);
        var newText = oldText + "Z";
        var algorithm = new SimpleMyersDiffAlgorithm();
        var options = new DiffHighlightOptions { Mode = EDiffHighlightMode.Character, MaxCharacterDiffLength = 2000 };

        algorithm.ComputeCharacterDiff(oldText, newText, options).Should().BeEmpty();
        var lines = algorithm.ComputeDiff([oldText], [newText], options);

        lines.Select(line => line.Type).Should().Equal(EDiffLineType.Deleted, EDiffLineType.Added);
        lines.Should().OnlyContain(line => line.CharacterDiffs == null);
    }

    private static DiffHighlightService CreateService() => new(
        NullLogger<DiffHighlightService>.Instance,
        Options.Create(new ModuleDiffHighlightOption()),
        new SimpleMyersDiffAlgorithm());

    private static void AssertRanges(IEnumerable<DiffCharacterRange> actual,
        params (EDiffLineType Type, int Start, int Length, string Content)[] expected)
    {
        actual.Select(range => (range.Type, range.Start, range.Length, range.Content)).Should().Equal(expected);
    }

    private static void AssertStatistics(DiffStatistics actual, int added, int deleted, int modified, int unchanged,
        int oldLines, int newLines)
    {
        actual.AddedLines.Should().Be(added);
        actual.DeletedLines.Should().Be(deleted);
        actual.ModifiedLines.Should().Be(modified);
        actual.UnchangedLines.Should().Be(unchanged);
        actual.TotalChanges.Should().Be(added + deleted + modified);
        actual.TotalOldLines.Should().Be(oldLines);
        actual.TotalNewLines.Should().Be(newLines);
    }
}
