# Compose the shell and protect feature pages

For a custom page's native modal, `Monica.UI.Shared.Components.Dialogs.MoDialogHeading` supplies a visible localized heading and names the enclosing dialog when placed in `TitleContent`. Pass the page's localized `Text`; the shell's normal dialog provider retains focus and dismissal ownership. See [native dialog heading authoring](../../monica-ui-development/references/blazor-best-practices.md#14-native-dialog-headings) for component and local-asset lifetime requirements rather than copying feature-specific interop into the host.

For related operational routes within a page family, `Monica.UI.Shared.Components.Navigation.MoWorkbenchNavigation` supplies the shared contextual navigation treatment. Pass a localized accessible `Label` and native anchors as `ChildContent`; the feature owns each destination, localized text, and exact `aria-current="page"` selection. It does not register routes, evaluate authorization, or turn them into local tab state. Register the actual destinations with the shell and apply their access policy separately. Follow the [contextual navigation contract](../../monica-ui-development/references/monica-workbench-design-language.md#keep-contextual-navigation-consistent) when authoring their contents.

The shell owns native navigation observers through route and circuit retirement. It drains browser users and observer callbacks before releasing their .NET target and module references. Reload or circuit closure can cancel final reference release; this is expected only after the owner retires. A live cancellation and unexpected cleanup failure remain observable, while independent references still receive cleanup. Hosts do not need a menu-specific exception filter. Custom shell integrations should follow the [interop lifetime contract](../../monica-ui-development/references/blazor-best-practices.md#22-bound-async-work-to-the-component-lifetime) rather than suppressing every JavaScript cancellation.

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

## Use shared cursor menus

`MoContextMenu<TItem>` uses MudBlazor's menu and popover primitives for cursor placement, nested menus, dismissal, and action callbacks. Supply the mouse event's page coordinates through `InitialX` and `InitialY`, retain the selected `ContextItem`, and clear `Visible` in the `OnClose` callback. Its native focus trap exists only while the menu is actually open, admits keyboard input at the container, and restores prior focus on dismissal. ArrowDown enters the native item sequence; pointer admission does not preselect an item independently of the menu's internal navigation state. Keep a keyboard-accessible alternative for important actions; a right-click menu alone does not make them discoverable or keyboard accessible.

The shared global stylesheet owns a non-interactive, one-pixel menu anchor. Preserve its measurable rectangle: MudBlazor checks the anchor's viewport visibility before applying cursor coordinates, so `display: contents` can leave the popup offscreen inside a scrolled owner. A feature's isolated CSS cannot style the portaled popup through its local wrapper. Verify actual anchor and popup geometry after scrolling, near viewport edges, and when reopening at another cursor position; leave positioning and dismissal with the primitive rather than adding a second overlay implementation.

## Read the module workbench

`AddModuleSystemUI()` supplies the five `/module-system-dashboard` sections and their inspectors; it requires Module System, localization, and the shell. The operational page policy covers the root and section aliases, and each diagnostics read rechecks access. A page-owned session retains selection, filters, and an imported baseline while navigating between its sections. Leaving the page disposes that session. Refresh reads the host-owned snapshot; it does not rerun module callbacks, resolve application services, or rescan assemblies.

Overview distinguishes tracked application startup from Monica composition. Module contribution totals combine callback and startup-work costs, which can overlap in wall-clock time; they are not mutually exclusive shares of application startup. The causal ribbon preserves every recorded occurrence; the bounded waterfall discloses omitted spans and has a table alternative. Dependency autocomplete and the edge table complement the graphical explorer. Discovery match counts describe type-query observations and may exceed enumerated type counts; its assembly facets may overlap.

Opening a module through its portable `?module=` ID shows the detail drawer; closing it removes that query without clearing synchronized selection. Module and Help inspectors support modal keyboard focus and Escape dismissal. Configuration is a lazy, bounded, read-only projection with the host's sensitivity policy. Export produces a sanitized portable baseline without option values, assembly paths, raw failures, or stack traces. Import compares a compatible JSON file of at most 10 MiB in the page session; it does not mutate host composition. Remove baseline clears that comparison.

## Read Service Discovery evidence

`AddServiceDiscoveryUI()` requires Service Discovery, localization, and the shell. Configure the underlying role and storage through the hosting skill's [Service Discovery contract](../../monica-infra-hosting/references/service-discovery.md). The UI contributes five separate Monitor destinations:

| Route | Workspace |
| --- | --- |
| `/service-discovery-manage` | Registry: searchable service identity, a paged service ledger, and instance inspection. |
| `/service-discovery-manage/service-map` | Service map: graphical membership and a native service roster. |
| `/service-discovery-manage/dependencies` | Dependencies: declared subdomain relationships and a native relationship ledger. |
| `/service-discovery-manage/subdomains` | Subdomains: catalog identity, owned services, and external dependants. |
| `/service-discovery-manage/current-host` | Current host: local identity, registration, configuration, and election evidence. |

The subdomain directory accepts an escaped `?domain=` catalog name, matched without regard to case. An absent catalog entry is distinct from a failed catalog read. The graphical views retain native lists so service inspection and dependency navigation do not require pointer access to a graph.

Opening a service creates a detached, frozen native inspector. Its originating capture time and the time it was frozen are separate facts; periodic page refresh does not rewrite the open inspector. Instance registrations, retained UI-observed disappearances, complete metadata, and release evidence remain inspectable. Configuration and local election state are explicitly scoped to the querying host, including when the selected service is remote. For membership, failure, lease, retention, and force-delete semantics, read [captured diagnostics and provider reads](../../monica-infra-hosting/references/service-discovery.md#captured-diagnostics-and-provider-reads).

Registry and current-host pages own their cancellable refresh loops; graph and directory captures refresh on demand. Failed reads retain the previous successful capture with a failure notice rather than displaying a successful empty registry. The host owns access to these diagnostics and their underlying APIs; navigation visibility does not replace authorization.

For embedded workspaces migrating from the former tabbed page, navigate through the dedicated routes instead of `ServiceDiscoveryPageState.SelectedDomain` or `ActiveTabIndex`. The scoped registry/current-host state no longer implements `IDisposable` or exposes timer/callback setup (`Initialize`, `ConfigureAutoRefresh`, or `UpdateAutoRefresh`). The consuming component owns and drains its cancellable polling loop before retirement; it must not dispose borrowed state. Set the registry's filter properties and call `ApplyFilters()`, or use `SetSelectedDomains(...)` for deliberate domain selection. Empty status or domain selections mean no matches. `SelectAllDomains()` follows domain membership arriving in subsequent successful captures; an explicitly cleared selection remains empty after recovery.

Source and checks: `Monica.UI/Modules/ModuleShellUI.cs`, `ModuleSystemUI.cs`, `Monica.UI/Shell/Models/NavigationCategoryDefinition.cs`, `Monica.UI/Shell/Support/INavigationRegistryBuilder.cs`, `NavigationTreeBuilder.cs`, `ShellIcons.cs`, `OperationalPageAccessPolicy.cs`, `Monica.UI/Shell/Components/Layout/NavBar.razor`, its code-behind, `NavigationPopover.razor`, `NavigationPopoverInteropSession.cs`, `tests/Test.Monica.UI/Shell/Components/Layout/NavigationPopoverInteropSessionTests.cs`, `Monica.UI/Shell/State/ShellState.cs`, `Monica.UI/Pages/ModuleSystemPage.razor`, `Monica.UI/UIModuleSystem/State/ModuleSystemWorkbenchSession.cs`, `Monica.ServiceDiscovery/Modules/ModuleServiceDiscoveryUI.cs`, `Monica.ServiceDiscovery/Pages/UIServiceDiscoveryPage.razor`, its section pages, `Monica.ServiceDiscovery/UIServiceDiscovery/Components/DiscoveryInspectorHost.razor`, and `tests/Test.Monica.UI/Shell/Support/PageRegistryTests.cs`.
