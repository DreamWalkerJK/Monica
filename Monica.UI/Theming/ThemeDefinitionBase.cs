using MudBlazor;

namespace Monica.UI.Theming;

/// <summary>
/// Base contract for Monica theme metadata and palette creation.
/// </summary>
public abstract class ThemeDefinitionBase : IThemeDefinition
{
    /// <summary>
    /// Theme identity.
    /// </summary>
    public abstract MonicaThemeKind Kind { get; }
    
    /// <summary>
    /// Theme display name.
    /// </summary>
    public abstract string DisplayName { get; }
    
    /// <summary>
    /// Theme description.
    /// </summary>
    public abstract string Description { get; }
    
    /// <summary>
    /// Creates a MudBlazor theme, including code-highlight colors on Markdown palettes.
    /// </summary>
    /// <returns>Configured MudTheme instance</returns>
    public abstract MudTheme CreateTheme();
}
