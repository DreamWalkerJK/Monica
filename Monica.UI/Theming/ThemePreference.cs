using Monica.UI.Shell.Support;

namespace Monica.UI.Theming;

/// <summary>
/// The browser's persisted theme identity and color mode, shared by the shell and standalone Monica UI.
/// </summary>
/// <param name="ThemeKind">The numeric theme identity retained by the existing browser JSON contract.</param>
/// <param name="IsDarkMode">Whether to use the selected theme's dark palette.</param>
/// <remarks>
/// Browser storage serializes this record with camelCase property names and a numeric theme kind.
/// A saved preference takes precedence over host defaults; unavailable or malformed storage uses those defaults.
/// </remarks>
public sealed record ThemePreference(MonicaThemeKind ThemeKind, bool IsDarkMode)
{
    /// <summary>
    /// Unprefixed key passed to <see cref="IBrowserStorage"/> for the shared theme preference.
    /// </summary>
    public const string StorageKey = "theme:data";

    /// <summary>
    /// Actual browser key used by standalone integrations calling the shared storage JavaScript module.
    /// </summary>
    public static string BrowserStorageKey => BrowserStorageKeys.GetPrefixedKey(StorageKey);

    /// <summary>
    /// Resolves an unregistered numeric identity to <see cref="MonicaThemeKind.Default"/>, retaining color mode.
    /// </summary>
    /// <returns>This preference when its theme exists, or the default theme with the same color mode.</returns>
    public ThemePreference Normalize() => ThemeCatalog.ThemeExists(ThemeKind)
        ? this
        : new ThemePreference(MonicaThemeKind.Default, IsDarkMode);
}
