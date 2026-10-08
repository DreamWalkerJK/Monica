namespace Monica.UI.Theming;

/// <summary>
/// Identifies the built-in Monica UI themes that can be selected by shell configuration or by the browser user.
/// </summary>
public enum MonicaThemeKind
{
    /// <summary>
    /// Monica's Precision theme with vivid signal colors and clear operational hierarchy.
    /// </summary>
    Default = 0,

    /// <summary>
    /// The baseline MudBlazor palette and component geometry with Monica's shared density and identity.
    /// </summary>
    MudBlazor = 1,

    /// <summary>
    /// A dark-teal editorial dashboard theme.
    /// </summary>
    HermesTeal = 2,

    /// <summary>
    /// A terminal-green hacker console theme.
    /// </summary>
    VibeUsageMatrix = 3,

    /// <summary>
    /// A Material Design 3 inspired theme.
    /// </summary>
    MaterialDesign3 = 4,

    /// <summary>
    /// A Fluent Design inspired theme.
    /// </summary>
    FluentDesign = 5,

    /// <summary>
    /// A light, fresh mint-color theme.
    /// </summary>
    Fresh = 6,

    /// <summary>
    /// A playful pastel macaron theme.
    /// </summary>
    MacaronSweetheart = 7,

    /// <summary>
    /// An ink-wash landscape theme.
    /// </summary>
    InkLandscape = 8,

    /// <summary>
    /// A minimalist ink painting theme.
    /// </summary>
    ZenInk = 9,

    /// <summary>
    /// An inked comic theme with bold print-paper surfaces.
    /// </summary>
    ComicBurst = 10,

    /// <summary>
    /// Warm stone surfaces, forest accents, and editorial page identity.
    /// </summary>
    Grove = 11
}
