using MudBlazor;

namespace Monica.UI.Theming.Definitions;

/// <summary>
/// The default Monica Precision theme for vivid, information-dense operational interfaces.
/// </summary>
public sealed class DefaultTheme : ThemeDefinitionBase
{
    public override MonicaThemeKind Kind => MonicaThemeKind.Default;
    public override string DisplayName => "默认主题";
    public override string Description => "以鲜明信号色、技术字体和清晰信息层次构建的精密运维界面";

    public override CodeBlockTheme LightCodeBlockTheme => CodeBlockTheme.Github;
    public override CodeBlockTheme DarkCodeBlockTheme => CodeBlockTheme.GithubDark;

    public override MudTheme CreateTheme() => new()
    {
        PaletteLight = CreateLightPalette(),
        PaletteDark = CreateDarkPalette(),
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
            DrawerWidthLeft = "260px",
            DrawerWidthRight = "260px",
            DrawerMiniWidthLeft = "64px",
            DrawerMiniWidthRight = "64px",
            AppbarHeight = "56px"
        },
        Shadows = new Shadow { Elevation = CreateElevationScale() },
        Typography = MonicaTypographyDefaults.CreatePrecision()
    };

    private static PaletteLight CreateLightPalette() => new()
    {
        Primary = "#7257f5",
        PrimaryLighten = "#8b74ff",
        PrimaryDarken = "#5841d2",
        PrimaryContrastText = "#ffffff",

        Secondary = "#3f779c",
        SecondaryLighten = "#5b96ba",
        SecondaryDarken = "#2e5c7b",
        SecondaryContrastText = "#ffffff",

        Tertiary = "#eeeafd",
        TertiaryContrastText = "#4e3cb2",

        Info = "#118fc9",
        InfoLighten = "#3baddb",
        InfoDarken = "#0b70a0",
        InfoContrastText = "#ffffff",

        Success = "#0b9f73",
        SuccessLighten = "#2db88d",
        SuccessDarken = "#087d5a",
        SuccessContrastText = "#ffffff",

        Warning = "#b97806",
        WarningLighten = "#d89525",
        WarningDarken = "#925e05",
        WarningContrastText = "#ffffff",

        Error = "#d5445e",
        ErrorLighten = "#e86b82",
        ErrorDarken = "#b1324b",
        ErrorContrastText = "#ffffff",

        Dark = "#151b2b",
        DarkLighten = "#293247",
        DarkDarken = "#0d1220",
        DarkContrastText = "#f4f6fb",

        Background = "#f1f3f7",
        BackgroundGray = "#eef1f6",
        Surface = "#ffffff",

        DrawerBackground = "#ffffff",
        DrawerText = "#516078",
        DrawerIcon = "#68768c",

        AppbarBackground = "rgba(255, 255, 255, 0.94)",
        AppbarText = "#151b2b",

        TextPrimary = "#151b2b",
        TextSecondary = "#516078",
        TextDisabled = "#7f8ca2",

        ActionDefault = "#68768c",
        ActionDisabled = "#aeb7c6",
        ActionDisabledBackground = "#e8ecf2",

        Divider = "#d9deea",
        DividerLight = "#e8ebf1",
        LinesDefault = "#d9deea",
        LinesInputs = "#c3cada",

        TableStriped = "#f8f9fb",
        TableHover = "#f5f3ff",

        OverlayDark = "rgba(21, 27, 43, 0.58)",
        OverlayLight = "rgba(255, 255, 255, 0.78)",
        HoverOpacity = 0.055,

        GrayDefault = "#7f8ca2",
        GrayLight = "#c3cada",
        GrayLighter = "#eef1f6",
        GrayDark = "#516078",
        GrayDarker = "#303a4d"
    };

    // Dark mode restores the deep Zinc + Indigo scheme of Monica's former default theme
    // (demo.monica.dpdns.org): a near-black Zinc 950 canvas with Zinc 900 surfaces,
    // Indigo primary, and Tailwind-scale signal colors.
    private static PaletteDark CreateDarkPalette() => new()
    {
        Primary = "#6366f1", // Indigo 500
        PrimaryLighten = "#818cf8", // Indigo 400
        PrimaryDarken = "#4f46e5", // Indigo 600
        PrimaryContrastText = "#ffffff",

        Secondary = "#a1a1aa", // Zinc 400
        SecondaryLighten = "#d4d4d8", // Zinc 300
        SecondaryDarken = "#71717a", // Zinc 500
        SecondaryContrastText = "#18181b", // Zinc 900

        Tertiary = "#27272a", // Zinc 800
        TertiaryContrastText = "#f4f4f5", // Zinc 50

        Info = "#3b82f6", // Blue 500
        InfoLighten = "#60a5fa", // Blue 400
        InfoDarken = "#2563eb", // Blue 600
        InfoContrastText = "#ffffff",

        Success = "#10b981", // Emerald 500
        SuccessLighten = "#34d399", // Emerald 400
        SuccessDarken = "#059669", // Emerald 600
        SuccessContrastText = "#ffffff",

        Warning = "#f59e0b", // Amber 500
        WarningLighten = "#fbbf24", // Amber 400
        WarningDarken = "#d97706", // Amber 600
        WarningContrastText = "#18181b", // Zinc 900

        Error = "#ef4444", // Red 500
        ErrorLighten = "#f87171", // Red 400
        ErrorDarken = "#dc2626", // Red 600
        ErrorContrastText = "#ffffff",

        Dark = "#f4f4f5", // Zinc 50
        DarkLighten = "#ffffff",
        DarkDarken = "#d4d4d8", // Zinc 300
        DarkContrastText = "#09090b", // Zinc 950

        Background = "#09090b", // Zinc 950
        BackgroundGray = "#18181b", // Zinc 900
        Surface = "#18181b", // Zinc 900

        DrawerBackground = "#18181b",
        DrawerText = "#e4e4e7", // Zinc 200
        DrawerIcon = "#a1a1aa", // Zinc 400

        AppbarBackground = "rgba(9, 9, 11, 0.94)",
        AppbarText = "#f4f4f5", // Zinc 50

        TextPrimary = "#f4f4f5", // Zinc 50
        TextSecondary = "#a1a1aa", // Zinc 400
        TextDisabled = "#52525b", // Zinc 600

        ActionDefault = "#a1a1aa",
        ActionDisabled = "#3f3f46", // Zinc 700
        ActionDisabledBackground = "#27272a", // Zinc 800

        Divider = "#27272a", // Zinc 800
        DividerLight = "#18181b", // Zinc 900
        LinesDefault = "#27272a", // Zinc 800
        LinesInputs = "#3f3f46", // Zinc 700

        TableStriped = "#09090b", // Zinc 950
        TableHover = "#27272a", // Zinc 800

        OverlayDark = "rgba(0, 0, 0, 0.8)",
        OverlayLight = "rgba(24, 24, 27, 0.5)",
        HoverOpacity = 0.08,

        GrayDefault = "#71717a", // Zinc 500
        GrayLight = "#a1a1aa", // Zinc 400
        GrayLighter = "#d4d4d8", // Zinc 300
        GrayDark = "#52525b", // Zinc 600
        GrayDarker = "#3f3f46" // Zinc 700
    };

    private static string[] CreateElevationScale() =>
    [
        "none",
        "0 1px 2px rgba(10, 16, 28, 0.06)",
        "0 2px 6px -1px rgba(10, 16, 28, 0.08)",
        "0 4px 10px -2px rgba(10, 16, 28, 0.09)",
        "0 6px 14px -3px rgba(10, 16, 28, 0.10)",
        "0 8px 18px -4px rgba(10, 16, 28, 0.11)",
        "0 10px 22px -5px rgba(10, 16, 28, 0.12)",
        "0 12px 26px -6px rgba(10, 16, 28, 0.13)",
        "0 14px 30px -7px rgba(10, 16, 28, 0.14)",
        "0 16px 34px -8px rgba(10, 16, 28, 0.15)",
        "0 18px 38px -9px rgba(10, 16, 28, 0.16)",
        "0 20px 42px -10px rgba(10, 16, 28, 0.17)",
        "0 22px 46px -11px rgba(10, 16, 28, 0.18)",
        "0 24px 50px -12px rgba(10, 16, 28, 0.19)",
        "0 24px 54px -12px rgba(10, 16, 28, 0.20)",
        "0 26px 58px -13px rgba(10, 16, 28, 0.21)",
        "0 26px 62px -13px rgba(10, 16, 28, 0.22)",
        "0 28px 64px -14px rgba(10, 16, 28, 0.23)",
        "0 28px 68px -14px rgba(10, 16, 28, 0.24)",
        "0 30px 70px -15px rgba(10, 16, 28, 0.25)",
        "0 30px 72px -15px rgba(10, 16, 28, 0.26)",
        "0 30px 74px -15px rgba(10, 16, 28, 0.27)",
        "0 30px 76px -16px rgba(10, 16, 28, 0.28)",
        "0 30px 78px -16px rgba(10, 16, 28, 0.29)",
        "0 30px 80px -16px rgba(10, 16, 28, 0.30)",
        "0 32px 84px -17px rgba(10, 16, 28, 0.32)"
    ];

}
