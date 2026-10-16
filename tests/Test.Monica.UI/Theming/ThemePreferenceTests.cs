using System.Text.Json;
using AwesomeAssertions;
using Monica.UI.Theming;
using Xunit;

namespace Test.Monica.UI.Theming;

public sealed class ThemePreferenceTests
{
    [Fact]
    public void Deserialize_WhenReadingExistingBrowserJson_ShouldPreserveNumericIdentityAndMode()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        const string stored = "{\"themeKind\":11,\"isDarkMode\":true}";

        var preference = JsonSerializer.Deserialize<ThemePreference>(stored, options);

        preference.Should().Be(new ThemePreference(MonicaThemeKind.Grove, true));
        JsonSerializer.Serialize(preference, options).Should().Be(stored);
        ThemePreference.BrowserStorageKey.Should().Be("mo:theme:data");
    }

    [Fact]
    public void Normalize_WhenStoredNumericIdentityIsUnknown_ShouldKeepModeAndUseDefaultTheme()
    {
        var preference = new ThemePreference((MonicaThemeKind)999, true);

        preference.Normalize().Should().Be(new ThemePreference(MonicaThemeKind.Default, true));
    }
}
