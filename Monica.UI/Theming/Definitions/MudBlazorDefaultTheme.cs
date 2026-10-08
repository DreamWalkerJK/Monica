using MudBlazor;

namespace Monica.UI.Theming.Definitions;

/// <summary>
/// MudBlazor palette and component theme with Monica's offline typography fallback.
/// </summary>
public class MudBlazorDefaultTheme : ThemeDefinitionBase
{
    public override MonicaThemeKind Kind => MonicaThemeKind.MudBlazor;
    public override string DisplayName => "MudBlazor默认主题";
    public override string Description => "保留MudBlazor视觉结构，并使用Monica离线基础字体";
    
    public override CodeBlockTheme LightCodeBlockTheme => CodeBlockTheme.Default;
    public override CodeBlockTheme DarkCodeBlockTheme => CodeBlockTheme.Dark;

    public override MudTheme CreateTheme()
    {
        return new MudTheme
        {
            PaletteLight = _lightPalette,
            PaletteDark = _darkPalette,
            LayoutProperties = new LayoutProperties { AppbarHeight = "56px", DrawerMiniWidthLeft = "64px", DrawerMiniWidthRight = "64px" },
            Typography = MonicaTypographyDefaults.CreateFallback(),
            Shadows = MonicaShadowDefaults.Create()
        };
    }
    private readonly PaletteLight _lightPalette = new()
    {
        Black = "#110e2d",
        Primary = "#594ae2",
        PrimaryContrastText = "#ffffff",
        Info = "#1667b8",
        InfoContrastText = "#ffffff",
        Success = "#278246",
        SuccessContrastText = "#ffffff",
        Warning = "#956217",
        WarningContrastText = "#ffffff",
        Error = "#c02e49",
        ErrorContrastText = "#ffffff",
        Background = "#f5f4fa",
        Surface = "#ffffff",
        AppbarText = "#424242",
        AppbarBackground = "rgba(255,255,255,0.8)",
        DrawerBackground = "#ffffff",
        GrayLight = "#e8e8e8",
        GrayLighter = "#f9f9f9",

    };

    private readonly PaletteDark _darkPalette = new()
    {
        Primary = "#9d91ff",
        PrimaryContrastText = "#171427",
        InfoContrastText = "#101b2d",
        SuccessContrastText = "#0b2513",
        WarningContrastText = "#2d210d",
        ErrorContrastText = "#2d1015",
        Surface = "#1e1e2d",
        Background = "#1a1a27",
        BackgroundGray = "#151521",
        AppbarText = "#eeedf5",
        AppbarBackground = "rgba(26,26,39,0.8)",
        DrawerBackground = "#1a1a27",
        ActionDefault = "#aca8c0",
        ActionDisabled = "#9999994d",
        ActionDisabledBackground = "#605f6d4d",
        TextPrimary = "#eeedf5",
        TextSecondary = "#b0adbf",
        TextDisabled = "#ffffff33",
        DrawerIcon = "#b0adbf",
        DrawerText = "#eeedf5",
        GrayLight = "#2a2833",
        GrayLighter = "#1e1e2d",
        Info = "#4a86ff",
        Success = "#3dcb6c",
        Warning = "#ffb545",
        Error = "#ff3f5f",
        LinesDefault = "#33323e",
        TableLines = "#33323e",
        Divider = "#292838",
        OverlayLight = "#1e1e2d80",
    };
}
