using System.Collections.Frozen;
using Monica.Framework.UI.UISwagger.Models;
using Monica.UI.Theming;
using MudBlazor;

namespace Monica.Framework.UI.UISwagger.Support;

/// <summary>
/// Projects the real Monica theme factories onto the standalone Swagger presentation roles.
/// </summary>
internal static class SwaggerUiThemeCatalog
{
    internal static IReadOnlyList<SwaggerUiThemeDefinition> Create()
    {
        var themes = ThemeCatalog.GetAvailableThemes()
            .OrderBy(static entry => entry.Kind)
            .Select(static entry =>
            {
                var theme = ThemeCatalog.GetTheme(entry.Kind).CreateTheme();
                return new SwaggerUiThemeDefinition(
                    entry.Kind,
                    entry.Kind.ToCssToken(),
                    CreateVariables(theme.PaletteLight, theme.Typography),
                    CreateVariables(theme.PaletteDark, theme.Typography));
            })
            .ToArray();

        return Array.AsReadOnly(themes);
    }

    private static FrozenDictionary<string, string> CreateVariables(Palette palette, Typography typography)
    {
        var variables = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["--mo-swagger-background"] = palette.Background.ToString(),
            ["--mo-swagger-surface"] = palette.Surface.ToString(),
            ["--mo-swagger-surface-raised"] = palette.Surface.ToString(),
            ["--mo-swagger-surface-muted"] = palette.BackgroundGray.ToString(),
            ["--mo-swagger-text"] = palette.TextPrimary.ToString(),
            ["--mo-swagger-text-secondary"] = palette.TextSecondary.ToString(),
            ["--mo-swagger-text-disabled"] = palette.TextDisabled.ToString(),
            ["--mo-swagger-border"] = palette.Divider.ToString(),
            ["--mo-swagger-border-strong"] = palette.LinesInputs.ToString(),
            ["--mo-swagger-primary"] = palette.Primary.ToString(),
            ["--mo-swagger-primary-text"] = palette.PrimaryContrastText.ToString(),
            ["--mo-swagger-primary-hover"] = palette.PrimaryDarken,
            ["--mo-swagger-primary-surface"] = TonalSurface("primary"),
            ["--mo-swagger-secondary"] = palette.Secondary.ToString(),
            ["--mo-swagger-secondary-text"] = palette.SecondaryContrastText.ToString(),
            ["--mo-swagger-secondary-surface"] = TonalSurface("secondary"),
            ["--mo-swagger-success"] = palette.Success.ToString(),
            ["--mo-swagger-success-text"] = palette.SuccessContrastText.ToString(),
            ["--mo-swagger-success-surface"] = TonalSurface("success"),
            ["--mo-swagger-warning"] = palette.Warning.ToString(),
            ["--mo-swagger-warning-text"] = palette.WarningContrastText.ToString(),
            ["--mo-swagger-warning-surface"] = TonalSurface("warning"),
            ["--mo-swagger-danger"] = palette.Error.ToString(),
            ["--mo-swagger-danger-text"] = palette.ErrorContrastText.ToString(),
            ["--mo-swagger-danger-surface"] = TonalSurface("danger"),
            ["--mo-swagger-info"] = palette.Info.ToString(),
            ["--mo-swagger-info-text"] = palette.InfoContrastText.ToString(),
            ["--mo-swagger-info-surface"] = TonalSurface("info"),
            ["--mo-swagger-action"] = palette.ActionDefault.ToString(),
            ["--mo-swagger-action-disabled"] = palette.ActionDisabled.ToString(),
            ["--mo-swagger-action-disabled-background"] = palette.ActionDisabledBackground.ToString(),
            ["--mo-swagger-overlay"] = palette.OverlayDark,
            ["--mo-swagger-font-body"] = FormatFontFamily(typography.Default.FontFamily),
            ["--mo-swagger-font-display"] = FormatFontFamily(typography.H1.FontFamily ?? typography.Default.FontFamily),
            ["--mo-swagger-font-code"] = "var(--mo-font-family-mono)"
        };

        if (typography.Default.FontSize is { } bodySize)
        {
            variables["--mo-swagger-font-body-size"] = bodySize;
        }

        if (typography.Default.FontWeight is { } bodyWeight)
        {
            variables["--mo-swagger-font-body-weight"] = bodyWeight;
        }

        if (typography.Default.LineHeight is { } bodyLineHeight)
        {
            variables["--mo-swagger-font-body-line-height"] = bodyLineHeight;
        }

        return variables.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static string TonalSurface(string role) =>
        $"color-mix(in srgb, var(--mo-swagger-{role}) 10%, var(--mo-swagger-surface))";

    private static string FormatFontFamily(string[]? families) => string.Join(
        ", ",
        (families ?? []).Select(static family => family.Contains(' ')
            ? $"'{family.Replace("\\", "\\\\").Replace("'", "\\'")}'"
            : family));
}
