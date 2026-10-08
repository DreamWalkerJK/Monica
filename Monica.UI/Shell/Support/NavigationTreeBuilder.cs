using Monica.Core.Localization.Abstractions;
using Monica.UI.Shell.Models;

namespace Monica.UI.Shell.Support;

/// <summary>
/// Resolves one host's navigation hierarchy after page access has been evaluated.
/// </summary>
internal static class NavigationTreeBuilder
{
    /// <summary>
    /// Builds ordered root categories and removes branches with no authorized navigation destinations.
    /// </summary>
    internal static IReadOnlyList<NavigationGroup> Build(
        IReadOnlyList<NavigationCategoryDefinition> categories,
        IReadOnlyList<NavigationItem> authorizedItems,
        ILocalizationCatalog localizationCatalog)
    {
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(authorizedItems);
        ArgumentNullException.ThrowIfNull(localizationCatalog);

        var itemsByCategory = authorizedItems.ToLookup(static item => item.CategoryId);
        var childrenByParent = categories
            .Where(static category => category.ParentId is not null)
            .ToLookup(static category => category.ParentId!);

        return Resolve(categories.Where(static category => category.ParentId is null));

        IReadOnlyList<NavigationGroup> Resolve(IEnumerable<NavigationCategoryDefinition> definitions)
        {
            var groups = new List<NavigationGroup>();
            foreach (var definition in definitions
                         .OrderBy(static category => category.Order)
                         .ThenBy(static category => category.Id.Value, StringComparer.Ordinal))
            {
                var items = itemsByCategory[definition.Id]
                    .OrderBy(static item => item.Order)
                    .ThenBy(static item => item.Href, StringComparer.OrdinalIgnoreCase)
                    .ToList()
                    .AsReadOnly();
                var children = Resolve(childrenByParent[definition.Id]);
                if (items.Count == 0 && children.Count == 0)
                {
                    continue;
                }

                groups.Add(new NavigationGroup(
                    definition.Id,
                    definition.ResolveDisplayName(localizationCatalog),
                    items,
                    children,
                    definition.Icon ?? items.FirstOrDefault()?.Icon ?? children.FirstOrDefault()?.Icon));
            }

            return groups.AsReadOnly();
        }
    }

    /// <summary>
    /// Enumerates every navigation destination in the resolved tree, preserving category and item order.
    /// </summary>
    internal static IEnumerable<NavigationItem> Flatten(IEnumerable<NavigationGroup> groups)
    {
        foreach (var group in groups)
        {
            foreach (var item in group.Items)
            {
                yield return item;
            }

            foreach (var item in Flatten(group.Children))
            {
                yield return item;
            }
        }
    }
}
