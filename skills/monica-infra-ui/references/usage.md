# UI modules in a Web host

## Shell and feature pages

```csharp
using Monica.Core.Modularity.Extensions;
using Monica.Modules;

var builder = WebApplication.CreateBuilder(args);
builder.AddMonica(monica =>
{
    monica.AddUIShell(options =>
    {
        options.DefaultDarkMode = false;
        options.OperationalPageAccess.AuthorizationPolicy = "OperationsViewer";
    });
    monica.AddModuleSystemUI();
});

var app = builder.Build();
app.UseMonica();
app.MapMonica();
app.Run();
```

`AddUIShell()` registers MudBlazor services, Razor components with interactive server rendering, the page catalog, browser storage, theme state, and shell endpoints. `ModuleShellUI` requires a Web host. Feature UI modules implement `IUIModule`; many, such as `AddModuleSystemUI()`, declare their own shell and infrastructure dependencies, so explicit shell registration is useful when configuring host-wide options but is not required just to satisfy that dependency. The shell maps static assets, antiforgery, Razor components, and additional page assemblies. A UI route that works through in-app navigation but returns 404 on refresh often points to missing shell endpoint mapping or an assembly registration issue; check `MapMonica()` and the UI module's `Describe(...)` navigation contribution.

Built-in pages include module system and system info, logging, dependency injection, health checks, job scheduler, AI chat, knowledge base, RAG, and other feature-specific UIs. Register the feature UI method, for example `AddLoggingUI()` or `AddAIUI()`, after selecting the underlying operational capability. Each UI module's `Describe(...)` states its transitive dependencies; read it before duplicating configuration.

Operational pages are accessible by default in Development. Outside Development, a missing effective `OperationalPageAccess.AuthorizationPolicy` denies access unless a page overrides it. `OperationalPageAccess.DebugMode` bypasses these checks in every environment, while `EnableDebug` controls detailed Blazor/SignalR errors; they are different switches. Register the named ASP.NET Core policy on the host when using an access policy.

## Markdown documents

```csharp
builder.AddMonica(monica =>
{
    monica.AddMarkdown()
        .AddDocumentGroup("handbook", "Handbook", "docs/handbook")
        .EnableMultilingualDocuments();
    monica.AddMarkdownUI();
});
```

`AddMarkdown()` supplies the document catalog, search, and `MarkdownFacade`; its default provider reads the filesystem. `AddDocumentGroup(key, title, basePath)` is the explicit discovery root. `EnableMultilingualDocuments()` treats top-level culture folders such as `en-US` and `zh-CN` as language roots. `AddMarkdownUI()` adds the viewer page and requires the Markdown module, shell, and localization. It also enables shell Markdown rendering. Its local image asset endpoint is enabled by default at `/markdown-ui/assets/{groupKey}` and validates the referenced image path and extension; disable it with `EnableLocalImageAssetEndpoint = false` if the viewer does not need local images.

The shell alone can render basic Markdown components when `EnableMarkdown = true`; the Markdown document module and viewer are needed for searchable, grouped documents.

## Theme and localization

`ModuleShellUIOption.DefaultTheme` defaults to `MonicaThemeKind.Default`; `DefaultDarkMode` defaults to `false`. They affect a browser only until the user stores a theme preference. `ShowLanguageSwitcher` defaults to `true`. `AddLocalization()` defaults to `zh-CN` and supports `zh-CN` and `en-US`; UI feature modules normally declare their own resource markers through the localization dependency. If a custom page is added, use the module's `Describe(...)` to contribute its resource and `ConfigureNavigation(...)` with `RegisterLocalizedPage<TPage,TResource>(...)`, keeping stable category IDs separate from translated labels. For resource layout and strict validation, follow `$monica-ui-localization`.

For first-party UI styling, use the `--mud-palette-*` variables and the small `--mo-color-*` contract in `Monica.UI/wwwroot/css/mo-theme-main.css`. Component implementation, MudBlazor v9 API details, CSS isolation, and browser verification belong to `$monica-ui-development`.

## Source checks

Current contracts live in `Monica.UI/Modules/ModuleShellUI.cs`, `Monica.UI/Modules/OperationalPageAccessOption.cs`, `Monica.UI/Shell/Support/INavigationRegistryBuilder.cs`, `Monica.Core/Modules/ModuleLocalization.cs`, and `Monica.Markdown/Modules/ModuleMarkdown.cs` and `ModuleMarkdownUI.cs`. Inspect the chosen feature UI module's `Describe(...)` before assuming what it adds.
