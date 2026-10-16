# Standalone Swagger workbench

`AddSwaggerUI()` composes Monica's native Swagger generation and localization, then enhances the standalone Swagger index with Monica themes and optional application destinations. It does not require the Blazor shell. Styling is enabled with an empty navigation list; `AddSwagger()` alone retains the native Swagger presentation. Document assignment, XML comments and endpoint access belong to the [Web API documentation contract](../../monica-infra-web/references/api-documentation-and-visibility.md).

```csharp
using Monica.Modules;
using Monica.UI.Theming;

builder.Services.AddEndpointsApiExplorer();
builder.AddMonica(monica =>
{
    monica.AddSwagger(options =>
    {
        options.BusinessDocumentTitle = "Ordering API";
        options.RoutePrefix = "docs/api";
    });
    monica.AddSwaggerUI()
        .AddNavigationButton("System information", "~/system-info", "Inspect this host", order: 10);
    monica.AddUIShell(options => options.DefaultTheme = MonicaThemeKind.Grove);
    monica.AddSystemInfoUI();
});
app.UseMonica();
app.MapMonica();
```

The example registers API discovery and the System Information destination explicitly; the shell supplies its page host and first-visit theme default. Swagger needs the host's API Explorer services: use its normal MVC/WebApi composition for controllers and `AddEndpointsApiExplorer()` for Minimal API metadata, including in a mixed host. A host using only the standalone workbench must expose the referenced Razor class library static assets, for example through its normal `app.MapStaticAssets()` composition. An HTTP 200 for the Swagger HTML does not establish that the Monica CSS, JavaScript, storage module or local fonts are available. Inspect those actual requests in a mounted deployment.

`NavigationButtons` defaults to an empty list. Disabled entries are omitted and enabled entries are ordered by `Order`, whose default is zero. `Name` and optional `Description` are caller-owned literal strings. An application-root path, with or without `~/` or a leading slash, resolves within the current request's PathBase; an absolute HTTP/HTTPS URL remains external. Supply the application path once rather than manually prepending PathBase. The resulting links retain native anchor navigation. Destinations are navigation, not permission grants.

The existing `SetAsRootRoute()` helper composes the shell and redirects `/` to the default `/swagger` path. It assumes the default prefix. With a custom or empty Swagger prefix, configure the root destination explicitly rather than applying that helper.

When a shell is composed, its `DefaultTheme` and `DefaultDarkMode` provide first-visit defaults. Without it, defaults are `MonicaThemeKind.Default` and light mode. The saved same-origin `ThemePreference` at `mo:theme:data` takes precedence; the shell and standalone workbench share its numeric theme identity and camelCase JSON contract. The native Swagger mode button selects the color mode of that theme. Palette and typography come from the existing ThemeCatalog factories and local font assets, not a second theme catalog in JavaScript. See [theme and language](theme-and-localization.md) for shell defaults and font coverage.

The enhancement runs after the host's Swagger customization and default document URL resolution, retaining the final options, original index factory, head content and native initializer. Document selection, contracts, authorization, request execution, response evidence, schemas and source actions remain owned by Swagger. Monica's added control labels follow the request culture; the native renderer's own vocabulary is not a complete Monica translation. Localized index responses vary by the localization cookie, while native asset caching remains intact.

Each HTML request evaluates the host's final index factory, buffers its template, and disposes the produced stream once. Native middleware owns the new enhanced stream. A custom factory must produce a fresh readable stream per request; a fully custom template must retain the native root, initializer and required scripts for the workbench to operate.

An interface prefix controls the HTML location, not the document endpoint namespace. Monica's unchanged generated document destinations resolve relative to the final native index location, preserving PathBase and nested or empty UI prefixes. A host extension that replaces a document URL retains ownership of that URL. `UseAuth` describes Bearer authorization in OpenAPI; it does not restrict access to the HTML or JSON documents. Apply the host's actual access policy separately.

Sources: `Monica.Framework.UI/Modules/ModuleSwaggerUI.cs`, `Monica.Framework.UI/UISwagger/Support/SwaggerUiIndexRenderer.cs`, `SwaggerUiUrlResolver.cs`, `SwaggerUiThemeCatalog.cs`, `Monica.UI/Theming/ThemePreference.cs`, and `Monica.WebApi/Swagger/Services/Support/SwaggerUIOptionConfigurator.cs`.
