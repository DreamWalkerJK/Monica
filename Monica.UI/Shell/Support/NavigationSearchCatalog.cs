using Microsoft.Extensions.Localization;
using Monica.Core.Localization.Abstractions;
using Monica.UI.Icons;
using Monica.UI.Localization;
using Monica.UI.Shell.Models;

namespace Monica.UI.Shell.Support;

/// <summary>Builds a localized search snapshot from the current circuit's authorized navigation.</summary>
internal sealed class NavigationSearchCatalog(
    IPageCatalog pageCatalog,
    PageAccessEvaluator pageAccess,
    ILocalizationCatalog localizationCatalog,
    IStringLocalizer<NavigationSearchResource> localizer)
{
    /// <summary>Evaluates access before resolving labels or descriptions; route templates are not searchable destinations.</summary>
    internal async Task<IReadOnlyList<NavigationSearchEntry>> CreateEntriesAsync(CancellationToken cancellationToken)
    {
        var authorizedItems = new List<NavigationItem>();
        foreach (var item in pageCatalog.GetNavItems())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!item.Href.Contains('{') && !item.Href.Contains('}') &&
                await pageAccess.IsAuthorizedAsync(item, cancellationToken))
            {
                authorizedItems.Add(item);
            }
        }

        var roots = NavigationTreeBuilder.Build(
            pageCatalog.GetNavigationCategories(), authorizedItems, localizationCatalog);
        var entries = new List<NavigationSearchEntry>();
        foreach (var root in roots)
        {
            AddGroup(root, root, root.DisplayName);
        }

        return entries.AsReadOnly();

        void AddGroup(NavigationGroup group, NavigationGroup root, string breadcrumb)
        {
            foreach (var item in group.Items)
            {
                var title = item.ResolveText(localizationCatalog);
                var description = item.Page.ResolveDescription(localizationCatalog)
                    ?? group.Description ?? root.Description
                    ?? localizer["Search:FallbackDescription", title, breadcrumb].Value;
                entries.Add(new NavigationSearchEntry(item, title, description, breadcrumb, root.DisplayName,
                    root.Icon ?? item.Icon ?? MoIcons.Folder));
            }

            foreach (var child in group.Children)
            {
                AddGroup(child, root, $"{breadcrumb} / {child.DisplayName}");
            }
        }
    }
}
