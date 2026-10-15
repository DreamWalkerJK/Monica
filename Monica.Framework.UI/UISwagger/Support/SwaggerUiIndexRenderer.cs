using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Microsoft.Net.Http.Headers;
using Monica.Framework.UI.Localization;
using Monica.Framework.UI.UISwagger.Models;
using Monica.UI.Theming;
using Monica.WebApi.Swagger.Abstractions;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Monica.Framework.UI.UISwagger.Support;

/// <summary>
/// Adds per-request localization and mounted URLs to the host's original Swagger index template.
/// </summary>
internal sealed class SwaggerUiIndexRenderer(
    IHttpContextAccessor httpContextAccessor,
    IStringLocalizer<SwaggerUiResource> localizer,
    ThemePreference defaults,
    IReadOnlyList<SwaggerNavigationButton> buttons) : ISwaggerUIOptionsEnhancer
{
    internal const string CONFIGURATION_ELEMENT_ID = "mo-swagger-config";
    private const string HEAD_CONTENT_PLACEHOLDER = "%(HeadContent)";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IReadOnlyList<SwaggerUiThemeDefinition> _themes = SwaggerUiThemeCatalog.Create();

    void ISwaggerUIOptionsEnhancer.Apply(SwaggerUIOptions options) => Configure(options);

    internal void Configure(SwaggerUIOptions options)
    {
        var originalIndex = options.IndexStream;
        options.IndexStream = () => Render(originalIndex);
    }

    internal Stream Render(Func<Stream> originalIndex)
    {
        string template;
        using (var original = originalIndex())
        using (var reader = new StreamReader(
                   original, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
        {
            template = reader.ReadToEnd();
        }

        var context = httpContextAccessor.HttpContext;
        var pathBase = context?.Request.PathBase ?? PathString.Empty;
        if (context is not null)
        {
            var existingVary = context.Response.Headers.Vary.ToString().Split(',');
            if (!existingVary.Any(static value => value.Trim() is "*"
                    || value.Trim().Equals("Cookie", StringComparison.OrdinalIgnoreCase)))
            {
                context.Response.Headers.Append(HeaderNames.Vary, "Cookie");
            }
        }

        var configuration = new SwaggerUiBootstrapConfiguration(
            Array.AsReadOnly(buttons.Select(button => new SwaggerUiNavigationLink(
                    button.Name,
                    SwaggerUiUrlResolver.Navigation(pathBase, button.Path),
                    button.Description,
                    button.Order))
                .ToArray()),
            defaults,
            _themes,
            new SwaggerUiStorageConfiguration(
                ThemePreference.BrowserStorageKey,
                SwaggerUiUrlResolver.Asset(pathBase, "Monica.UI/js/mo-browser-storage.js")),
            new SwaggerUiLabels(
                localizer["Navigation:Label"],
                localizer["Actions:SwitchToLight"],
                localizer["Actions:SwitchToDark"],
                localizer["Actions:Close"],
                localizer["Actions:Copy"]),
            CultureInfo.CurrentUICulture.Name);

        // Keep the HTML-safe serializer independent from a host's relaxed API JSON escaping settings.
        // Native middleware expands %(...) across the template after this factory returns. Encode percent
        // within JSON so caller text cannot be reinterpreted as a native template placeholder.
        var json = JsonSerializer.Serialize(configuration, JsonOptions).Replace("%", "\\u0025", StringComparison.Ordinal);
        var fontsUrl = SwaggerUiUrlResolver.Asset(pathBase, "Monica.UI/css/mo-fonts.css");
        var stylesUrl = SwaggerUiUrlResolver.Asset(pathBase, "Monica.Framework.UI/UISwagger/custom.css");
        var scriptUrl = SwaggerUiUrlResolver.Asset(pathBase, "Monica.Framework.UI/UISwagger/custom.js");
        var injection = $$"""

            <script id="{{CONFIGURATION_ELEMENT_ID}}" type="application/json">{{json}}</script>
            <link rel="stylesheet" href="{{System.Net.WebUtility.HtmlEncode(fontsUrl)}}" />
            <link rel="stylesheet" href="{{System.Net.WebUtility.HtmlEncode(stylesUrl)}}" />
            <script src="{{System.Net.WebUtility.HtmlEncode(scriptUrl)}}"></script>
            """;

        var insertion = template.IndexOf(HEAD_CONTENT_PLACEHOLDER, StringComparison.Ordinal);
        if (insertion >= 0)
        {
            insertion += HEAD_CONTENT_PLACEHOLDER.Length;
        }
        else
        {
            insertion = template.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
            if (insertion < 0)
            {
                insertion = template.Length;
            }
        }

        return new MemoryStream(Encoding.UTF8.GetBytes(template.Insert(insertion, injection)), writable: false);
    }
}
