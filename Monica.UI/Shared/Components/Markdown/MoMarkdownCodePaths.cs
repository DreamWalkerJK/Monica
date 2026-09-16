using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace Monica.UI.Shared.Components.Markdown;

/// <summary>Recognizes file paths in parsed Markdown prose and inline code.</summary>
internal static partial class MoMarkdownCodePaths
{
    // Require a directory separator and a known file extension. The left boundary excludes URL fragments.
    [GeneratedRegex(@"(?<![\w./\\:@-])(?:[A-Za-z]:[\\/]|[\\/]{1,2}|~[\\/])?(?:[\w.-]+[\\/])+[\w.-]+\.(?:cs|csproj|razor|css|scss|less|js|jsx|ts|tsx|json|md|markdown|xml|yml|yaml|sh|bash|ps1|py|sql|sln|slnx|props|targets|config|txt|cmd|bat|fs|fsproj|vb|vbproj|java|kt|go|rs|rb|php|c|h|cpp|hpp|html|htm|vue|svelte|toml|ini|proto)(?::\d+(?::\d+)?)?(?![\w-]|\.[\w])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FilePathPattern();

    internal static bool IsFilePath(string text)
    {
        var match = FilePathPattern().Match(text);
        return match.Success && match.Index == 0 && match.Length == text.Length;
    }

    internal static RenderFragment? CreateContent(string text)
    {
        var matches = FilePathPattern().Matches(text);
        if (matches.Count == 0)
        {
            return null;
        }

        return builder =>
        {
            var offset = 0;
            foreach (Match match in matches)
            {
                builder.AddContent(0, text[offset..match.Index]);
                builder.OpenRegion(1);
                MoMudMarkdown.RenderCopyableCode(builder, match.Value);
                builder.CloseRegion();
                offset = match.Index + match.Length;
            }

            builder.AddContent(2, text[offset..]);
        };
    }
}
