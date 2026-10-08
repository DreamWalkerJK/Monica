# Compose the shell and protect feature pages

Use a Web host. Register selected feature UI modules in `AddMonica(...)`, then complete the same application's lifecycle with `UseMonica()` and `MapMonica()` so the shell can map static assets, antiforgery, Razor components, feature endpoints, and additional page assemblies. `AddUIShell()` installs interactive server services, MudBlazor, browser storage, theme and navigation preference state, and the startup page catalog; feature UI modules contribute their pages.

```csharp
using Monica.Core.Modularity.Extensions;
using Monica.Modules;
using Monica.UI.Shell.Models;

var builder = WebApplication.CreateBuilder(args);
builder.AddMonica(monica =>
{
    monica.AddUIShell(options =>
    {
        options.OperationalPageAccess.AuthorizationPolicy = "OperationsViewer";
        options.DefaultNavigationLayout = ShellNavigationLayout.Sidebar;
    });
    monica.AddModuleSystemUI();
});

var app = builder.Build();
app.UseMonica();
app.MapMonica();
app.Run();
```

The host must register the named ASP.NET Core `OperationsViewer` policy and the authentication/authorization services it needs. Many feature UI modules, including `AddModuleSystemUI()`, declare their own shell dependency, so explicit `AddUIShell()` is needed here to set a host-wide option, not merely to satisfy that dependency. Select the underlying operational capability as well as its UI module; inspect each feature module's `Describe(...)` to avoid duplicating its transitive registrations. Built-in feature pages include module and system information, logging, DI, health, jobs, AI chat, knowledge, and RAG.

Operational pages are accessible by default in Development. Outside Development, a missing effective `OperationalPageAccess.AuthorizationPolicy` denies access unless an individual page supplies a policy override. `OperationalPageAccess.DebugMode` bypasses access checks in every environment; use it only in an explicitly trusted diagnostic host. `EnableDebug` is separate: it controls detailed Blazor/SignalR errors and defaults to true in DEBUG builds and false otherwise. Feature pages must evaluate their access policy before protected work; hiding a navigation link alone is not authorization.

UI modules contribute routes and navigation during `Describe(...)` through `ModuleShellUIOption.ConfigureNavigation(...)`. `INavigationRegistryBuilder.RegisterPage<TPage>` or `RegisterLocalizedPage<TPage,TResource>` records a route, optional stable category ID, label, icon, and access-policy type. Localized pages and categories need the module's resource marker registered with Localization. Contributions are startup-only and the registry seals during endpoint configuration. Duplicate routes, unknown navigation categories, missing category parents, category cycles, or a late contribution fail rather than silently replacing a page. A route that works through in-app navigation but 404s on refresh points first to missing `MapMonica()` or an unregistered page assembly.

Categories support multiple levels through the optional `parentId` argument on `RegisterCategory(...)` and `RegisterLocalizedCategory<TResource>(...)`. A missing parent argument creates a root category; `icon` optionally supplies that category's shell icon. Parent contributions may arrive later in startup, but every reference must resolve before the registry is sealed. Use stable category IDs independently of translated labels. The catalog publishes a flat immutable list of definitions with parent metadata; the shell evaluates each page's access policy before constructing its navigation tree and prunes branches with no authorized destinations.

Categories and pages accept an optional `description` on literal registrations or `descriptionKey` on localized registrations. Explain category capabilities and page purpose with module-owned text; localized descriptions use the same registered `TResource` as the label. Omitted descriptions remain null, supplied text or keys must be nonblank, and a conflicting category description rejects duplicate registration. `NavigationCategoryDefinition` and `PageDefinition` retain immutable `UIRegistryText? Description` metadata and resolve it through `ResolveDescription(ILocalizationCatalog)` for the current host. The authorized tree exposes resolved `NavigationGroup.Description` only for retained branches; descriptions do not create destinations or bypass access policies.

For example, a feature module can add a localized branch beneath the shell's Module category in its navigation callback:

```csharp
module.Require<ModuleLocalization, ModuleLocalizationOption>(
    static options => options.AddResource<ExampleResource>());
module.Require<ModuleShellUI, ModuleShellUIOption>(static options =>
    options.ConfigureNavigation(static registry =>
    {
        var diagnostics = registry.RegisterLocalizedCategory<ExampleResource>(
            "example.diagnostics",
            "Navigation:Diagnostics",
            order: 20,
            parentId: BuiltInNavigationCategoryIds.Module,
            icon: Icons.Material.Filled.AccountTree,
            descriptionKey: "Navigation:DiagnosticsDescription");
        registry.RegisterLocalizedPage<ExampleDiagnosticsPage, ExampleResource>(
            "/example-diagnostics",
            "Navigation:Overview",
            icon: Icons.Material.Filled.Dashboard,
            categoryId: diagnostics,
            addToNav: true,
            navOrder: 10,
            accessPolicyType: typeof(ExampleDiagnosticsAccessPolicy),
            descriptionKey: "Navigation:OverviewDescription");
    }));
```

This example belongs in a UI module's `Describe(ModuleDescriptor module)` method. `ExampleResource` must implement `ILocalizationResource`, `ExampleDiagnosticsPage` must declare the matching Blazor route, and the host must register `ExampleDiagnosticsAccessPolicy` as an `IPageAccessPolicy`. A deeper branch uses the returned `diagnostics` identity as its parent. Register actual routable pages rather than deriving menu entries from URL segments. The built-in module diagnostics branch exposes Overview, Performance, Modules, Dependencies, and Discovery under the existing `/module-system-dashboard` routes; each entry retains the module's operational access policy. When destinations overlap, the shell selects the longest matching route so only one leaf is active.

`DefaultNavigationLayout` defaults to `ShellNavigationLayout.Sidebar`: a 64px category rail and collapsible 226px contextual sidebar. Built-in category icons use locally defined, lightweight SVG strokes; custom registered icons remain supported. Category icons come from registry metadata rather than translated labels, while leaf destinations use quiet dots. `ShellNavigationLayout.Topbar` uses one 56px row, shows category icons alongside their labels, and expands nested categories inside a popup with category identity and description above its destinations. A fine, hover-capable pointer opens Topbar categories and More without a click or a focus change; pointer travel into the popup keeps it open. Click, touch, and ArrowDown open with focus, Escape dismisses and restores the trigger, and native Tab can leave the menu. The Appearance dialog owns the infrequent layout switch; navigation placement and theme selection are independent preferences. At widths up to 929px, both preferences use temporary drawer navigation; selecting a desktop layout does not reserve a sidebar on a narrow screen. The shell owns viewport height and scrolling, while operational pages consume its available content width. Changing layout or collapsing the sidebar preserves the current route and routed page instance.

Selecting a category on the collapsed desktop rail immediately displays a temporary, full-height 226px context panel beside the 64px rail, without a slide or flyout animation. The page keeps its collapsed-layout width, and the rail remains available to switch categories. Opening or dismissing this panel does not persist an expanded sidebar. Destination selection, Escape, and outside dismissal close the panel; dismissal returns focus to its category trigger, while route navigation allows the destination heading to receive focus. Crossing into the mobile viewport closes the desktop context instead of retaining hidden open state; focus remains on visible navigation rather than returning to a hidden rail trigger. Preserve native keyboard access to category controls, nested group disclosure, and links. Verify both expanded and collapsed navigation in short viewports so the category rail scrolls and its controls remain reachable.

`ShellState` restores browser preferences after the first interactive render through `IBrowserStorage`. Layout and sidebar collapse persist under `mo:shell:preferences`; saved browser preferences override the configured default. Components consuming this circuit-scoped state must not initialize browser storage during prerendering or dispose the injected storage service.

`ModuleShellUIOption.AppName`, `AppId`, and `AppVersion` override the host's application identity in the shell. `EnableNavBarSearch` defaults to true and supplies a toolbar control and Ctrl/Cmd+K shortcut for the built-in registered-page search palette. Set it to false to remove both entry points, or configure `NavBarSearchAction` to replace the palette with a host-owned callback for both entry points. `MaxVisibleCategories` defaults to 10 and limits visible Topbar root categories; available width can move additional categories into More. Sidebar and temporary drawer navigation retain the complete authorized hierarchy. Theme and language defaults are in [theme and localization](theme-and-localization.md).

Search evaluates the current user's page policies before resolving navigation metadata, excludes hidden navigation pages and route templates with parameters, and refreshes its snapshot when authentication changes. Each result shows its localized title, description, and root category; nested branches also show their category path. A page description takes precedence over its immediate category description and then its root category description. Every whitespace-separated query token must match the title, description, category path, or route; exact and prefix title matches rank first using the current culture's case and accent rules. The input keeps focus while arrows select results, Enter rechecks access and opens the selected destination, and Escape or outside dismissal closes the palette. IME composition is left to the input, and Ctrl/Cmd+K does not interrupt another open dialog. This search is a navigation aid, not a replacement for route or API authorization.

Source and checks: `Monica.UI/Modules/ModuleShellUI.cs`, `ModuleSystemUI.cs`, `Monica.UI/Shell/Models/NavigationCategoryDefinition.cs`, `Monica.UI/Shell/Support/INavigationRegistryBuilder.cs`, `NavigationTreeBuilder.cs`, `ShellIcons.cs`, `OperationalPageAccessPolicy.cs`, `Monica.UI/Shell/Components/Layout/NavBar.razor`, its code-behind, `Monica.UI/Shell/State/ShellState.cs`, and `tests/Test.Monica.UI/Shell/Support/PageRegistryTests.cs`.
