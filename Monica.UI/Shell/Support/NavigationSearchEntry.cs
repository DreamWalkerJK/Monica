using Monica.UI.Shell.Models;

namespace Monica.UI.Shell.Support;

/// <summary>
/// Describes one authorized navigation destination after its display metadata has been localized.
/// </summary>
/// <param name="Item">The registered navigation destination used for access checks and navigation.</param>
/// <param name="Title">The localized page title.</param>
/// <param name="Description">The localized page summary, category summary, or destination fallback.</param>
/// <param name="CategoryBreadcrumb">The localized category ancestry used for display and searching.</param>
/// <param name="RootCategory">The localized root category shown as a result badge.</param>
/// <param name="Icon">The root category icon, registered page icon, or shell folder fallback.</param>
internal sealed record NavigationSearchEntry(
    NavigationItem Item,
    string Title,
    string Description,
    string CategoryBreadcrumb,
    string RootCategory,
    string? Icon);
