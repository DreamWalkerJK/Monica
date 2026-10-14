using System.Globalization;

namespace Monica.UI.Shell.Support;

/// <summary>
/// Matches and ranks prepared navigation destinations without changing access or localization state.
/// </summary>
internal static class NavigationSearchMatcher
{
    private const CompareOptions COMPARISON_OPTIONS = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    /// <summary>
    /// Finds entries that contain every query word in their title, category, description, or registered route.
    /// Route searches accept either form of a destination with or without its leading slash.
    /// </summary>
    /// <param name="entries">The authorized, localized destinations in their preferred catalog order.</param>
    /// <param name="query">A whitespace-separated query. Empty queries preserve catalog order.</param>
    /// <param name="culture">The culture whose text comparisons apply to the localized metadata.</param>
    /// <param name="maximumResults">The result limit. Defaults to 30; zero and negative values return no results.</param>
    /// <returns>
    /// Matches ranked by title relevance before category, description, and route relevance.
    /// Equal scores preserve the original catalog order.
    /// </returns>
    internal static IReadOnlyList<NavigationSearchEntry> Search(
        IReadOnlyList<NavigationSearchEntry> entries,
        string? query,
        CultureInfo culture,
        int maximumResults = 30)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(culture);

        if (maximumResults <= 0 || entries.Count == 0)
        {
            return [];
        }

        var words = query?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (words.Length == 0)
        {
            return entries.Take(maximumResults).ToList().AsReadOnly();
        }

        var normalizedQuery = string.Join(' ', words);
        var matches = new List<(NavigationSearchEntry Entry, int Score, int Order)>();
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            var score = Score(entry, words, normalizedQuery, culture.CompareInfo);
            if (score >= 0)
            {
                matches.Add((entry, score, index));
            }
        }

        return matches
            .OrderByDescending(static match => match.Score)
            .ThenBy(static match => match.Order)
            .Take(maximumResults)
            .Select(static match => match.Entry)
            .ToList()
            .AsReadOnly();
    }

    private static int Score(
        NavigationSearchEntry entry,
        IReadOnlyList<string> words,
        string query,
        CompareInfo comparison)
    {
        var score = 0;
        var route = "/" + entry.Item.Href.TrimStart('/');
        foreach (var word in words)
        {
            var titleIndex = comparison.IndexOf(entry.Title, word, COMPARISON_OPTIONS);
            if (titleIndex >= 0)
            {
                score += titleIndex == 0 ? 120 : 100;
            }
            else if (comparison.IndexOf(entry.CategoryBreadcrumb, word, COMPARISON_OPTIONS) >= 0)
            {
                score += 60;
            }
            else if (comparison.IndexOf(entry.Description, word, COMPARISON_OPTIONS) >= 0)
            {
                score += 30;
            }
            else if (comparison.IndexOf(route, word, COMPARISON_OPTIONS) >= 0)
            {
                score += 10;
            }
            else
            {
                return -1;
            }
        }

        if (comparison.Compare(entry.Title, query, COMPARISON_OPTIONS) == 0)
        {
            score += 1_000;
        }
        else if (comparison.IsPrefix(entry.Title, query, COMPARISON_OPTIONS))
        {
            score += 400;
        }
        else if (comparison.IndexOf(entry.Title, query, COMPARISON_OPTIONS) >= 0)
        {
            score += 200;
        }

        return score;
    }
}
