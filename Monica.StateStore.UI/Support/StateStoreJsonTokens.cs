using System.Text.RegularExpressions;

namespace Monica.StateStore.UI.Support;

internal static partial class StateStoreJsonTokens
{
    [GeneratedRegex(""" "(?:\\.|[^"\\])*"(?:\s*:)?|\b(?:true|false|null)\b|-?\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\b """, RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant, 200)]
    public static partial Regex Pattern();
}
