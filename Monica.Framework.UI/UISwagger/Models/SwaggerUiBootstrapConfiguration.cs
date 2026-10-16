using Monica.UI.Theming;

namespace Monica.Framework.UI.UISwagger.Models;

internal sealed record SwaggerUiBootstrapConfiguration(
    IReadOnlyList<SwaggerUiNavigationLink> Buttons,
    ThemePreference Defaults,
    IReadOnlyList<SwaggerUiThemeDefinition> Themes,
    SwaggerUiStorageConfiguration Storage,
    SwaggerUiLabels Labels,
    string Culture);

internal sealed record SwaggerUiNavigationLink(string Name, string Href, string? Description, int Order);

internal sealed record SwaggerUiThemeDefinition(
    MonicaThemeKind ThemeKind,
    string Token,
    IReadOnlyDictionary<string, string> Light,
    IReadOnlyDictionary<string, string> Dark);

internal sealed record SwaggerUiStorageConfiguration(string Key, string ModuleUrl);

internal sealed record SwaggerUiLabels(string Navigation, string SwitchToLight, string SwitchToDark, string Close, string Copy);
