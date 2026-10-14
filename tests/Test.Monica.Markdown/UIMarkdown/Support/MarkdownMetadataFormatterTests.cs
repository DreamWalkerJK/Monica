using System.Globalization;
using AwesomeAssertions;
using Monica.Markdown.Providers.FileSystem;
using Monica.Markdown.UIMarkdown.Support;
using YamlDotNet.Serialization;

namespace Test.Monica.Markdown.UIMarkdown.Support;

public sealed class MarkdownMetadataFormatterTests
{
    [Fact]
    public void FormatValue_WhenValueIsScalar_ShouldDisplayCompleteInvariantValue()
    {
        (object? Value, string Expected)[] values =
        [
            (null, "null"),
            (string.Empty, string.Empty),
            ("  运行手册 <strong>literal HTML</strong>\n", "  运行手册 <strong>literal HTML</strong>\n"),
            ('界', "界"),
            (true, "true"),
            (false, "false"),
            (42, "42"),
            (-7L, "-7"),
            (1234.500m, "1234.500"),
            (1.25d, "1.25"),
            (new DateTime(2026, 1, 15, 12, 34, 56, DateTimeKind.Utc).AddTicks(1234567), "2026-01-15T12:34:56.1234567Z"),
            (new DateTimeOffset(2026, 1, 15, 12, 34, 56, TimeSpan.FromHours(8)).AddTicks(1234567), "2026-01-15T12:34:56.1234567+08:00"),
            (new DateOnly(2026, 1, 15), "2026-01-15"),
            (new TimeOnly(12, 34, 56).Add(TimeSpan.FromTicks(1234567)), "12:34:56.1234567"),
            (TimeSpan.FromMinutes(90), "01:30:00")
        ];
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

            foreach (var sample in values)
                MarkdownMetadataFormatter.FormatValue(sample.Value).Should().Be(sample.Expected);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void FormatValue_WhenFrontMatterHasNestedListsAndMappings_ShouldPreserveParsedStructureAndKeys()
    {
        var frontMatter = MarkdownFrontMatterParser.Parse("""
            ---
            title: "Public review"
            review:
              Owner Team: "运行实践小组"
              reviewers:
                - name: "Lin"
                  regions: ["Shanghai", "杭州"]
                - name: "Morgan"
                  regions: []
              thresholds:
                latency: "12.50"
                enabled: "false"
                missing: null
              rendered example: '<img src="x" onerror="example()">'
              empty mapping: {}
              nested lists:
                - ["canary", "recovery"]
                - ["true", "null", "0123"]
            ---
            # Public review
            """)!;
        var original = frontMatter.RawMetadata["review"];

        var text = MarkdownMetadataFormatter.FormatValue(original);
        var restored = new DeserializerBuilder().Build().Deserialize<object>(text);

        restored.Should().BeEquivalentTo(original, options => options.WithStrictOrdering());
        text.Should().Contain("Owner Team:").And.Contain("运行实践小组").And.Contain("杭州")
            .And.Contain("<img src=\"x\" onerror=\"example()\">")
            .And.NotContain("System.Collections");
    }

    [Fact]
    public void FormatValue_WhenValueIsParsedTagList_ShouldPreserveEveryTagAndOrder()
    {
        var frontMatter = MarkdownFrontMatterParser.Parse("""
            ---
            tags: [operations, "跨区域恢复", "null", "0123"]
            ---
            """)!;

        var text = MarkdownMetadataFormatter.FormatValue(frontMatter.RawMetadata["tags"]);
        var restored = new DeserializerBuilder().Build().Deserialize<List<string>>(text);

        restored.Should().Equal("operations", "跨区域恢复", "null", "0123");
        text.Should().NotContain("System.Collections");
    }

    [Fact]
    public void FormatValue_WhenNestedValuesAreTypedScalars_ShouldUseInvariantNumbersAndDates()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var value = new Dictionary<string, object?>
            {
                ["amount"] = 1234.500m,
                ["enabled"] = false,
                ["timestamp"] = new DateTime(2026, 1, 15, 12, 34, 56, DateTimeKind.Utc).AddTicks(1234567),
                ["offset"] = new DateTimeOffset(2026, 1, 15, 12, 34, 56, TimeSpan.FromHours(8)).AddTicks(1234567),
                ["date"] = new DateOnly(2026, 1, 15),
                ["time"] = new TimeOnly(12, 34, 56).Add(TimeSpan.FromTicks(1234567)),
                ["nothing"] = null
            };

            var text = MarkdownMetadataFormatter.FormatValue(value);

            text.Should().Contain("1234.500").And.Contain("false")
                .And.Contain("2026-01-15T12:34:56.1234567Z")
                .And.Contain("2026-01-15T12:34:56.1234567+08:00")
                .And.Contain("2026-01-15").And.Contain("12:34:56.1234567")
                .And.Contain("nothing:").And.NotContain("System.");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void FormatValue_WhenCollectionIsEmpty_ShouldDisplayItsActualStructure()
    {
        var listText = MarkdownMetadataFormatter.FormatValue(new List<object?>());
        var mappingText = MarkdownMetadataFormatter.FormatValue(new Dictionary<string, object?>());

        listText.Trim().Should().Be("[]");
        mappingText.Trim().Should().Be("{}");
    }

    [Fact]
    public void FormatValue_WhenNestedTextHasWhitespaceAndLongContent_ShouldPreserveEveryCharacter()
    {
        var prose = new string('界', 12000) + "\n\n";
        var original = new Dictionary<string, string>
        {
            ["说明"] = prose,
            ["literal markup"] = "<script>example()</script>",
            ["whitespace"] = "  leading and trailing  \n\n"
        };

        var text = MarkdownMetadataFormatter.FormatValue(original);
        var restored = new DeserializerBuilder().Build().Deserialize<Dictionary<string, string>>(text);

        restored.Should().BeEquivalentTo(original);
        text.Should().NotContain("System.Collections");
    }
}
