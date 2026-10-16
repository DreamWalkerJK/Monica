using MudBlazor;

namespace Monica.UI.Theming.Definitions;

/// <summary>
/// Grove: warm stone surfaces, forest accents, and editorial page identity.
/// </summary>
public sealed class GroveTheme : ThemeDefinitionBase
{
    public override MonicaThemeKind Kind => MonicaThemeKind.Grove;
    public override string DisplayName => "Grove";
    public override string Description => "Warm stone surfaces, forest accents, and clear operational typography.";

    public override CodeBlockTheme LightCodeBlockTheme => CodeBlockTheme.Github;
    public override CodeBlockTheme DarkCodeBlockTheme => CodeBlockTheme.GithubDark;

    public override MudTheme CreateTheme() => new()
    {
        PaletteLight = CreateLightPalette(),
        PaletteDark = CreateDarkPalette(),
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
            DrawerWidthLeft = "290px",
            DrawerWidthRight = "290px",
            DrawerMiniWidthLeft = "64px",
            DrawerMiniWidthRight = "64px",
            AppbarHeight = "56px"
        },
        Shadows = MonicaShadowDefaults.Create(),
        Typography = MonicaTypographyDefaults.CreateGrove()
    };

    private static PaletteLight CreateLightPalette() => new()
    {
        Primary = "#335d43",
        PrimaryLighten = "#4f7758",
        PrimaryDarken = "#294b36",
        PrimaryContrastText = "#f6f8ef",

        Secondary = "#706345",
        SecondaryLighten = "#8c7e5f",
        SecondaryDarken = "#574c33",
        SecondaryContrastText = "#fffffb",

        Tertiary = "#dce7d4",
        TertiaryContrastText = "#335d43",

        Info = "#396877",
        InfoLighten = "#578694",
        InfoDarken = "#2b505c",
        InfoContrastText = "#fffffb",

        Success = "#39734e",
        SuccessLighten = "#548e65",
        SuccessDarken = "#285838",
        SuccessContrastText = "#fffffb",

        Warning = "#885f24",
        WarningLighten = "#a77b38",
        WarningDarken = "#694818",
        WarningContrastText = "#fffffb",

        Error = "#a54e41",
        ErrorLighten = "#bf6d5f",
        ErrorDarken = "#843b30",
        ErrorContrastText = "#fffffb",

        Dark = "#223f33",
        DarkLighten = "#335647",
        DarkDarken = "#182d24",
        DarkContrastText = "#f0f3e7",

        Background = "#f5f5ef",
        BackgroundGray = "#eeefe6",
        Surface = "#fffffb",

        DrawerBackground = "#eaece2",
        DrawerText = "#233c31",
        DrawerIcon = "#63725f",

        AppbarBackground = "#f5f5ef",
        AppbarText = "#233c31",

        TextPrimary = "#233c31",
        TextSecondary = "#63725f",
        TextDisabled = "#939e8d",

        ActionDefault = "#63725f",
        ActionDisabled = "#a8b09e",
        ActionDisabledBackground = "#e6e9de",

        Divider = "#dce0d3",
        DividerLight = "#e8ebdf",
        LinesDefault = "#dce0d3",
        LinesInputs = "#c5cdbd",

        TableStriped = "#f8f9f2",
        TableHover = "#edf2e7",

        OverlayDark = "rgba(24, 45, 36, 0.48)",
        OverlayLight = "rgba(245, 245, 239, 0.78)",
        HoverOpacity = 0.055,

        GrayDefault = "#939e8d",
        GrayLight = "#c5cdbd",
        GrayLighter = "#eeefe6",
        GrayDark = "#63725f",
        GrayDarker = "#3e5545"
    };

    private static PaletteDark CreateDarkPalette() => new()
    {
        Primary = "#b5ce9a",
        PrimaryLighten = "#c7dcaf",
        PrimaryDarken = "#98b57e",
        PrimaryContrastText = "#1a291e",

        Secondary = "#d2c29b",
        SecondaryLighten = "#e4d6b6",
        SecondaryDarken = "#b8a77c",
        SecondaryContrastText = "#2b291e",

        Tertiary = "#364a34",
        TertiaryContrastText = "#dce7d4",

        Info = "#9bc8d1",
        InfoLighten = "#b7dce2",
        InfoDarken = "#7baab4",
        InfoContrastText = "#172c31",

        Success = "#b2d4a2",
        SuccessLighten = "#c9e5bd",
        SuccessDarken = "#93b883",
        SuccessContrastText = "#1a291e",

        Warning = "#dbb77a",
        WarningLighten = "#edd09b",
        WarningDarken = "#bd975b",
        WarningContrastText = "#302615",

        Error = "#e8a094",
        ErrorLighten = "#f5bdb3",
        ErrorDarken = "#c97f73",
        ErrorContrastText = "#39231e",

        Dark = "#223d30",
        DarkLighten = "#335647",
        DarkDarken = "#172b20",
        DarkContrastText = "#f0f3e7",

        Background = "#131d19",
        BackgroundGray = "#25332a",
        Surface = "#1c2822",

        DrawerBackground = "#17231c",
        DrawerText = "#e7ecdd",
        DrawerIcon = "#a0af9c",

        AppbarBackground = "#131d19",
        AppbarText = "#e7ecdd",

        TextPrimary = "#e7ecdd",
        TextSecondary = "#a0af9c",
        TextDisabled = "#6b7c68",

        ActionDefault = "#a0af9c",
        ActionDisabled = "#61715e",
        ActionDisabledBackground = "#2d3b30",

        Divider = "#354439",
        DividerLight = "#2c3b30",
        LinesDefault = "#354439",
        LinesInputs = "#51624d",

        TableStriped = "#1a251e",
        TableHover = "#2a3b2c",

        OverlayDark = "rgba(8, 16, 11, 0.7)",
        OverlayLight = "rgba(28, 40, 34, 0.65)",
        HoverOpacity = 0.08,

        GrayDefault = "#83937d",
        GrayLight = "#a0af9c",
        GrayLighter = "#ced7c3",
        GrayDark = "#61715e",
        GrayDarker = "#354439"
    };

}
