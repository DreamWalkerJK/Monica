using Monica.Core.Localization.Abstractions;

namespace Monica.UI.Shell.Models;

/// <summary>
/// Describes the localized presentation, hierarchy, and deterministic order of a navigation category.
/// </summary>
public sealed class NavigationCategoryDefinition
{
    internal NavigationCategoryDefinition(
        NavigationCategoryId id,
        UIRegistryText displayName,
        int order,
        NavigationCategoryId? parentId = null,
        string? icon = null)
    {
        Id = id;
        DisplayName = displayName;
        Order = order;
        ParentId = parentId;
        Icon = icon;
    }

    /// <summary>
    /// Gets the stable category identity used for grouping.
    /// </summary>
    public NavigationCategoryId Id { get; }

    /// <summary>
    /// Gets the literal or resource-backed display name.
    /// </summary>
    public UIRegistryText DisplayName { get; }

    /// <summary>
    /// Gets the deterministic category order. Lower values render first.
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// Gets the parent category identity, or <see langword="null"/> for a top-level category.
    /// </summary>
    public NavigationCategoryId? ParentId { get; }

    /// <summary>
    /// Gets the optional icon used to represent the category in shell navigation.
    /// </summary>
    public string? Icon { get; }

    /// <summary>
    /// Resolves the category label through the specified host localization catalog.
    /// </summary>
    /// <param name="localizationCatalog">The localization catalog owned by the current host.</param>
    /// <returns>The localized label, or the registered fallback when the key is unavailable.</returns>
    public string ResolveDisplayName(ILocalizationCatalog localizationCatalog)
    {
        return DisplayName.Resolve(localizationCatalog);
    }

    internal bool HasSameRegistrationAs(NavigationCategoryDefinition other)
    {
        return Order == other.Order && DisplayName == other.DisplayName
            && ParentId == other.ParentId && Icon == other.Icon;
    }
}
