# Theme and language in the shell

Configure shell defaults inside the Web host's `builder.AddMonica(...)` callback. `DefaultTheme` is `MonicaThemeKind.Default`, which retains Monica's original Precision palette and theme identity. `DefaultDarkMode` is false, `DefaultNavigationLayout` is `ShellNavigationLayout.Sidebar`, and `ShowLanguageSwitcher` is true. Defaults apply only until a browser stores its own preference; changing a host default will not override a user's saved choice. The host still completes the UI lifecycle with `app.UseMonica()` and `app.MapMonica()`.

```csharp
using Monica.UI.Shell.Models;
using Monica.UI.Theming;

builder.AddMonica(monica =>
{
    monica.AddUIShell(options =>
    {
        options.DefaultTheme = MonicaThemeKind.Default;
        options.DefaultDarkMode = false;
        options.DefaultNavigationLayout = ShellNavigationLayout.Sidebar;
        options.ShowLanguageSwitcher = true;
    });
    monica.AddLocalization(options => options.DefaultCulture = "en-US");
});
```

The Appearance dialog combines theme selection, light/dark mode, and desktop navigation placement. Its theme cards preview both the palette and theme-aware Monica mark. Selections are staged until Apply; Cancel, Escape, and dismissal discard pending selections. Applying updates the active theme and navigation layout without navigating away from the current page. Sidebar collapse remains an independent shell preference. `ThemeProviderHost` persists the selected theme and color mode under `mo:theme:data`; `ShellState` persists layout and collapse under `mo:shell:preferences`. Both use injected `IBrowserStorage` after interactive rendering, with the runtime `mo:` prefix supplied by the storage service.

Grove is a separate optional theme identified by `MonicaThemeKind.Grove` and the `grove` CSS token. Choose it in Appearance or configure `options.DefaultTheme = MonicaThemeKind.Grove` for first-visit browsers. It uses warm stone surfaces, forest accents, IBM Plex Sans interface text, Playfair Display hierarchy, a CJK serif fallback for display text, and diagnostic monospace, with locally served WOFF2 assets. `MonicaThemeKind.Default` keeps its existing identity and persisted value; selecting the new shell layout does not change its palette.

The built-in alternatives remain selectable in both light and dark modes: Grove, MudBlazor, Hermes Teal, Vibe Usage Matrix, Material Design 3, Fluent Design, Fresh, Macaron Sweetheart, Ink Landscape, Zen Ink, and Comic Burst. They share the shell's dimensions, navigation behavior, readable surface roles, and interaction contract while retaining their own palette, typography, and expressive treatment.

The Default dark theme retains its original Indigo accent and Zinc surfaces. Its `PrimaryContrastText` is black so small labels on the bright primary accent remain readable; dark mode does not imply white text on every accent. Hosts do not need an Analytics-specific color override. Custom theme authors should check each foreground/background pair, including inverted primary surfaces and hover states, against the [palette contrast contract](../../monica-ui-development/references/theme-css-guide.md#2-keep-palette-ownership-in-c) while preserving the selected theme's identity.

Shared native dialog titles anchor their Close action independently of animation, including reduced-motion settings. Enabled title actions use readable text roles rather than assuming an accent-valued `ActionDefault` works on every surface. Hosts do not need an extra dialog CSS override for this behavior. For custom theme or component styling, follow the [theme CSS contract](../../monica-ui-development/references/theme-css-guide.md) and verify the native title and content together.

Native tab spacing remains inside each measured tab so responsive arrows and selected captions agree with the actual rendered width. Built-in themes retain their pill or underline treatment through measured spacing; hosts do not need an inspector-specific margin or gap override. When customizing tabs, use the same [theme CSS contract](../../monica-ui-development/references/theme-css-guide.md) and verify complete selected captions through the native arrows in both orientations.

Shared theme CSS removes a closed native tooltip from layout only while its `.mud-popover` lacks `.mud-popover-open`. MudBlazor can retain an invisible tooltip's positioned coordinates after a resize; opacity alone does not prevent that portal from enlarging the document. Open and reopened tooltips retain native positioning and visibility. Hosts do not need a page overflow override for this behavior; keep required content scrolling available and use the [portal containment audit](../../monica-ui-audit/references/audit-categories.md#p2--broken-layout-containment-sizing-or-scroll-ownership) when diagnosing document-level overflow.

The shell uses the shared `MonicaIcon` vector component rather than a fixed favicon image. Its colors, silhouette treatment, and surface geometry follow the selected theme and color mode; the same component appears in theme previews. Keep the application's configured name, ID, and version separate from this theme identity. Use the theme contract for component paint so a page remains readable under every selectable theme.

Built-in palettes distinguish enabled native icon paint through `ActionDefault` from inactive controls through `ActionDisabled`. Fresh light supplies a readable shared enabled-action role for native menus and icon buttons, so hosts do not need page-specific icon overrides. Custom palette authors should preserve this distinction and follow the [native action-role contrast contract](../../monica-ui-development/references/theme-css-guide.md#2-keep-palette-ownership-in-c) across actual surfaces and interaction states.

Localization defaults to `zh-CN` and supports `zh-CN` and `en-US`. `ModuleLocalizationOption.SupportedCultures` and `DefaultCulture` define the host profile; Web request culture persists through the localization cookie. The switcher affects whether users see a language control, not which resources are registered. Built-in UI modules contribute their own resource markers through a Localization dependency. For a custom UI module, contribute its marker in `Describe(...)` and use `ConfigureNavigation(...)` with `RegisterLocalizedPage<TPage,TResource>(...)`; keep route and category IDs stable while translating display labels. See [shell and access](shell-and-access.md) for when that page becomes routable and protected, and `$monica-ui-localization` for resource layout and validation.

For first-party styling, prefer `--mud-palette-*` variables and the small `--mo-color-*` contract in `Monica.UI/wwwroot/css/mo-theme-main.css`. The page-feature roles default to a primary-tinted surface, normal text, and a primary action; themes with a distinct feature treatment override the background/text pair together. This keeps large working surfaces readable without requiring the theme's full-strength action color to fill them. These roles do not change the configured palette or the colors of ordinary primary actions. Component construction, MudBlazor v9 APIs, CSS isolation, and browser checks belong to `$monica-ui-development`.

Source: `Monica.UI/Modules/ModuleShellUI.cs`, `Monica.Core/Modules/ModuleLocalization.cs`, `Monica.UI/Theming/Definitions/DefaultTheme.cs`, `GroveTheme.cs`, `FreshTheme.cs`, `Monica.UI/Shell/Components/Layout/ThemeDialog.razor`, `ThemeProviderHost.razor`, `Monica.UI/Shell/State/ShellState.cs`, `Monica.UI/Shared/Components/Branding/MonicaIcon.razor`, and `Monica.UI/wwwroot/css/themes/mo-theme-components.css`.
