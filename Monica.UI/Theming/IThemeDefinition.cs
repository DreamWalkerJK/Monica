using MudBlazor;

namespace Monica.UI.Theming;

/// <summary>
/// Theme provider interface
/// </summary>
public interface IThemeDefinition
{
    /// <summary>
    /// Theme identity.
    /// </summary>
    MonicaThemeKind Kind { get; }
    
    /// <summary>
    /// Theme display name.
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// Theme description.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Creates a MudBlazor theme, including code-highlight colors on Markdown palettes.
    /// </summary>
    /// <returns>Configured MudTheme instance</returns>
    MudTheme CreateTheme();
}
