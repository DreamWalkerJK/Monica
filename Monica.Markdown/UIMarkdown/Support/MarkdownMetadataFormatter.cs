using System.Globalization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Converters;

namespace Monica.Markdown.UIMarkdown.Support;

/// <summary>Formats front-matter values as plain text without discarding their nested structure.</summary>
internal static class MarkdownMetadataFormatter
{
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithQuotingNecessaryStrings()
        .WithNewLine("\n")
        .WithYamlFormatter(new YamlFormatter())
        .WithTypeConverter(new DateTimeOffsetConverter())
        .WithTypeConverter(new DateOnlyConverter(formats: ["O"]))
        .WithTypeConverter(new TimeOnlyConverter(formats: ["O"]))
        .Build();

    /// <summary>
    /// Displays strings verbatim, null as <c>null</c>, scalars with invariant formatting,
    /// and collections or mappings as YAML containing their complete values.
    /// </summary>
    /// <remarks>
    /// The result is text, not trusted markup. The rendering component must retain normal HTML encoding.
    /// YAML output retains its final newlines because they can belong to an authored multiline value.
    /// </remarks>
    internal static string FormatValue(object? value) => value switch
    {
        null => "null",
        string text => text,
        char character => character.ToString(),
        bool boolean => boolean ? "true" : "false",
        DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset date => date.ToString("O", CultureInfo.InvariantCulture),
        DateOnly date => date.ToString("O", CultureInfo.InvariantCulture),
        TimeOnly time => time.ToString("O", CultureInfo.InvariantCulture),
        TimeSpan duration => duration.ToString("c", CultureInfo.InvariantCulture),
        IFormattable scalar => scalar.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
        _ => Serializer.Serialize(value)
    };
}
