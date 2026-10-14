namespace Monica.UI.Shell.Support;

/// <summary>
/// Owns the browser key prefix shared by Monica's storage service and standalone UI integrations.
/// </summary>
public static class BrowserStorageKeys
{
    /// <summary>
    /// Prefix applied to every key written through <see cref="IBrowserStorage"/>.
    /// </summary>
    public const string Prefix = "mo:";

    /// <summary>
    /// Converts an unprefixed service key into its actual local or session storage key.
    /// </summary>
    /// <param name="key">The service key, without the <see cref="Prefix"/> prefix.</param>
    /// <returns>The key used by the browser storage JavaScript module.</returns>
    public static string GetPrefixedKey(string key) => $"{Prefix}{key}";
}
