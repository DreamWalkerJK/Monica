using MudBlazor;

namespace Monica.UI.Theming;

/// <summary>
/// Provides a restrained elevation scale for Monica's built-in themes.
/// </summary>
internal static class MonicaShadowDefaults
{
    /// <summary>
    /// Creates all 26 elevation entries required by MudBlazor, with compact contact shadows
    /// for controls and bounded depth for menus and dialogs. Palette ink remains theme-owned.
    /// </summary>
    internal static Shadow Create() => new()
    {
        Elevation =
        [
            "none",
            "0 1px 2px -1px color-mix(in srgb, var(--mud-palette-black) 10%, transparent)",
            "0 2px 4px -2px color-mix(in srgb, var(--mud-palette-black) 12%, transparent)",
            "0 2px 6px -2px color-mix(in srgb, var(--mud-palette-black) 12%, transparent)",
            "0 3px 8px -3px color-mix(in srgb, var(--mud-palette-black) 14%, transparent)",
            "0 3px 8px -3px color-mix(in srgb, var(--mud-palette-black) 14%, transparent)",
            "0 4px 10px -4px color-mix(in srgb, var(--mud-palette-black) 16%, transparent)",
            "0 4px 10px -4px color-mix(in srgb, var(--mud-palette-black) 16%, transparent)",
            "0 4px 12px -4px color-mix(in srgb, var(--mud-palette-black) 18%, transparent)",
            "0 4px 12px -4px color-mix(in srgb, var(--mud-palette-black) 18%, transparent)",
            "0 5px 14px -5px color-mix(in srgb, var(--mud-palette-black) 20%, transparent)",
            "0 5px 14px -5px color-mix(in srgb, var(--mud-palette-black) 20%, transparent)",
            "0 6px 16px -6px color-mix(in srgb, var(--mud-palette-black) 22%, transparent)",
            "0 6px 16px -6px color-mix(in srgb, var(--mud-palette-black) 22%, transparent)",
            "0 6px 18px -6px color-mix(in srgb, var(--mud-palette-black) 22%, transparent)",
            "0 6px 18px -6px color-mix(in srgb, var(--mud-palette-black) 22%, transparent)",
            "0 8px 20px -8px color-mix(in srgb, var(--mud-palette-black) 24%, transparent)",
            "0 8px 20px -8px color-mix(in srgb, var(--mud-palette-black) 24%, transparent)",
            "0 8px 22px -8px color-mix(in srgb, var(--mud-palette-black) 24%, transparent)",
            "0 8px 22px -8px color-mix(in srgb, var(--mud-palette-black) 24%, transparent)",
            "0 10px 24px -10px color-mix(in srgb, var(--mud-palette-black) 26%, transparent)",
            "0 10px 24px -10px color-mix(in srgb, var(--mud-palette-black) 26%, transparent)",
            "0 10px 26px -10px color-mix(in srgb, var(--mud-palette-black) 26%, transparent)",
            "0 10px 26px -10px color-mix(in srgb, var(--mud-palette-black) 26%, transparent)",
            "0 12px 28px -12px color-mix(in srgb, var(--mud-palette-black) 28%, transparent)",
            "0 12px 28px -12px color-mix(in srgb, var(--mud-palette-black) 28%, transparent)"
        ]
    };
}
