using AwesomeAssertions;
using Monica.Framework.UI.UISwagger.Support;
using Monica.UI.Theming;
using MudBlazor;
using Xunit;

namespace Test.Monica.Framework.UI.UISwagger.Support;

public sealed class SwaggerUiThemeCatalogTests
{
    [Fact]
    public void Create_WhenProjectingAllThemes_ShouldRetainBothActualFactoryPalettesAndTypography()
    {
        var projected = SwaggerUiThemeCatalog.Create();
        projected.Select(static theme => theme.ThemeKind).Should().BeEquivalentTo(
            ThemeCatalog.GetAvailableThemes().Select(static entry => entry.Kind));

        foreach (var definition in projected)
        {
            var source = ThemeCatalog.GetTheme(definition.ThemeKind).CreateTheme();
            definition.Token.Should().Be(definition.ThemeKind.ToCssToken());
            AssertPalette(definition.Light, source.PaletteLight);
            AssertPalette(definition.Dark, source.PaletteDark);
            definition.Light["--mo-swagger-font-body"].Should().Contain(source.Typography.Default.FontFamily![0]);
            definition.Dark["--mo-swagger-font-display"].Should().Contain(source.Typography.H1.FontFamily![0]);
            definition.Light["--mo-swagger-font-body-size"].Should().Be(source.Typography.Default.FontSize);
        }
    }

    [Fact]
    public void Create_WhenFamiliesContainGenericFallbacks_ShouldKeepThoseFallbacksAsCssKeywords()
    {
        var defaults = SwaggerUiThemeCatalog.Create().Single(static theme => theme.ThemeKind == MonicaThemeKind.Default);
        var grove = SwaggerUiThemeCatalog.Create().Single(static theme => theme.ThemeKind == MonicaThemeKind.Grove);

        defaults.Light["--mo-swagger-font-body"].Should().EndWith(", sans-serif");
        defaults.Light["--mo-swagger-font-body"].Should().Contain("'IBM Plex Sans'");
        grove.Dark["--mo-swagger-font-display"].Should().EndWith(", serif");
        defaults.Light["--mo-swagger-font-code"].Should().Be("var(--mo-font-family-mono)");
    }

    private static void AssertPalette(IReadOnlyDictionary<string, string> actual, Palette source)
    {
        actual["--mo-swagger-background"].Should().Be(source.Background.ToString());
        actual["--mo-swagger-surface"].Should().Be(source.Surface.ToString());
        actual["--mo-swagger-text"].Should().Be(source.TextPrimary.ToString());
        actual["--mo-swagger-text-secondary"].Should().Be(source.TextSecondary.ToString());
        actual["--mo-swagger-primary"].Should().Be(source.Primary.ToString());
        actual["--mo-swagger-primary-text"].Should().Be(source.PrimaryContrastText.ToString());
        actual["--mo-swagger-danger"].Should().Be(source.Error.ToString());
        actual["--mo-swagger-danger-text"].Should().Be(source.ErrorContrastText.ToString());
        actual["--mo-swagger-border"].Should().Be(source.Divider.ToString());
    }
}
