# Monica Theme CSS Guide

## Current Architecture

Monica's theme system is split between C# theme definitions and theme CSS.

### C# theme layer

- Theme definitions live under `Monica.UI/Theming/Definitions/`
- Each theme implements `IThemeDefinition`
- Each built-in theme is identified by `MonicaThemeKind`; theme definitions expose `Kind`, not a string name
- `ThemeCatalog` registers the available definitions
- `ThemeState` owns the active theme kind, dark-mode state, and generated CSS/data-theme identifiers
- `ModuleShellUIOption.DefaultTheme` and `ModuleShellUIOption.DefaultDarkMode` provide the first-visit browser defaults; persisted browser preferences still win after the user changes theme settings
- `ThemeProviderHost` renders the wrapper that applies:
  - `class="mo-theme-{theme-token}-{mode}"`
  - `data-theme="{theme-token}-{mode}"`

The theme token is generated from `MonicaThemeKind.ToCssToken()`, for example:

- `MonicaThemeKind.MudBlazor` -> `mud-blazor`
- `MonicaThemeKind.MaterialDesign3` -> `material-design-3`
- `MonicaThemeKind.VibeUsageMatrix` -> `vibe-usage-matrix`

### CSS theme layer

- Theme CSS lives under `Monica.UI/wwwroot/css/themes/`
- `Monica.UI/wwwroot/css/mo-theme-main.css` imports the per-theme CSS files
- CSS should define theme tokens, surfaces, borders, typography accents, and shared component structure
- Color palettes, typography defaults, and MudBlazor tokens should still be rooted in the `MudTheme` returned by the C# definition

## Theme Authoring Workflow

1. Add a new `MonicaThemeKind` enum value.
2. Add a new `IThemeDefinition` under `Monica.UI/Theming/Definitions/` and return that enum value from `Kind`.
3. Register it in `ThemeCatalog`.
4. Add localized name and description entries for theme selection UI under `Theme:Options:{EnumValue}:Name` and `Theme:Options:{EnumValue}:Description`.
5. Create `Monica.UI/wwwroot/css/themes/mo-theme-{theme-token}.css`.
6. Import the new CSS file from `Monica.UI/wwwroot/css/mo-theme-main.css`.
7. If the theme needs a custom markdown/code-block presentation, add the corresponding markdown CSS file and register the code-block theme through the definition.
8. Verify persisted theme switching through `ThemeState` and browser storage.

## CSS Structure Rules

### 1. Define tokens on both the wrapper class and the `data-theme` root

```css
:root[data-theme="example-light"],
.mo-theme-example-light {
    --mo-example-panel: rgba(255, 255, 255, 0.92);
}

:root[data-theme="example-dark"],
.mo-theme-example-dark {
    --mo-example-panel: rgba(12, 18, 22, 0.92);
}
```

This keeps the theme usable for:

- normal app runtime
- DOM inspection
- browser-level overrides
- tools that read only `data-theme`

### 2. Keep palette ownership in C#

Use the C# `MudTheme` definition for:

- palette colors
- typography defaults
- shadows
- layout tokens that MudBlazor consumes directly

Use CSS for:

- custom background treatments
- surface shells
- borders and radii
- component-specific visual language
- light/dark-only decorative behavior

### 3. Isolate theme previews from application wrapper styles

Do not attach global `.mo-theme-*` wrapper classes to individual preview icons or cards. Those classes can own viewport-sized `::before`/`::after` decoration and broad descendant surface rules; applying one to a small preview can repaint the surrounding dialog.

Use component-scoped `data-theme` selectors and explicit palette/brand variables for the preview subtree. `MonicaIcon` accepts `ThemeKind` and `IsDarkMode` for this purpose while retaining its own `mo-brand-icon` class. Its preview must not inherit application-wrapper decoration. Verify all Appearance cards in light and dark modes, including expressive themes as both previews and the active surrounding theme.

### 4. Prefer shared MudBlazor selectors over page-specific hooks

Theme CSS should target stable shared structures such as:

- `.mud-card`
- `.mud-dialog`
- `.mud-data-grid`
- `.mud-tabs`
- `.mud-input-outlined`

Do not rely on page-only classes when the intent is a reusable theme behavior. If a temporary page-specific hook is added during diagnosis, remove it after the shared selector is in place.

Native dialog title actions are rendered by the dialog provider outside the feature's isolated content wrapper. Style a shared title action through its stable native selector in shared theme CSS; keep feature content and layout in isolated CSS. An absolutely positioned native Close must anchor to the actual title or dialog rather than rely on an animation transform to establish its containing block. Verify its bounds in ordinary and reduced-motion settings, including themes that remove animation. Do not assume `ActionDefault` provides readable contrast for an enabled icon on every surface. Check the actual icon color against its normal, hovered and selected backgrounds across themes; preserve explicit semantic colors and disabled states.

### 5. Keep theme CSS component-oriented

A good theme file usually looks like:

1. theme tokens
2. shell/background
3. navigation/chrome
4. shared surfaces
5. forms
6. tables/data grids
7. dialogs, menus, snackbars, overlays

Avoid scattering one-off page fixes throughout the theme file.

### 6. Keep shared button geometry overridable

Global `.mud-button` rules must not force pill radii, padding, or minimum height with `!important`. Appearance preview cards and compact shell triggers own their geometry; broad forced button paint can turn cards into pills and make real trigger widths differ from the navigation measurement rail. Set modest theme defaults without overriding component-owned shape or spacing. Inspect compiled isolation selectors and live computed styles when a local rule loses, and verify Appearance cards, ordinary actions, and Topbar overflow together.

## High-Risk Component Areas

### Drawer positioning belongs to its layout owner

Theme surface rules may paint drawers, but must preserve their fixed or absolute positioning. A broad `position: relative` rule can bring even a closed temporary drawer into the shell grid, consume a content row, and collapse the page. Keep decorative positioning separate from shared paint and exclude `.mud-drawer` from every matching branch. Excluding only an explicit drawer selector is insufficient: a drawer also matches elevated `.mud-paper` selectors.

Verify open and closed temporary drawers, both shell layouts, and a narrow viewport. Inspect computed drawer positioning and the main content height on more than one page; a visible shell with an empty content area can be a layout regression rather than a page loading failure.

### Hidden measurement elements can enlarge the document

Keep navigation measurement rails rendered at their natural width so overflow decisions remain accurate, but position the whole rail outside the viewport horizontally as well as vertically. `visibility: hidden` and an offscreen `top` do not prevent a wide absolute element from expanding the document's horizontal scroll area. Verify document width with a collapsed Sidebar near the desktop breakpoint and a wide or monospace theme, then confirm Topbar still moves destinations into More as space decreases.

### `MudTabs` with `ApplyEffectsToContainer="true"`

MudBlazor applies rounded, outlined, and elevation classes to the root `.mud-tabs` element in this mode.

If the theme needs a visible tab shell, inspect and style:

- the root `.mud-tabs`
- `.mud-tabs-tabbar`
- `.mud-tabs-panels`

Do not assume styling only the tabbar is enough.

Native tab scrolling measures each tab's bounding rectangle. In MudBlazor v9, `GetAllTabsSize` and `GetLengthOfPanelItems` sum those widths for horizontal tabs or heights for vertical tabs; they do not include margins or the wrapper's gaps. Do not add inline margins or horizontal gaps between horizontal tabs, or block margins or vertical gaps between vertical tabs. Otherwise, the scroll arrow can disable before the final caption is visible, and selecting that tab still leaves its text clipped. Use padding inside the measured tab for spacing along its scroll axis; cross-axis spacing must also fit its containing surface.

Verify the first, middle and last tabs through native clicks and scroll arrows at narrow widths, in both orientations when supported. Check the actual arrow state, the complete selected caption and the native strip's scroll position after layout settles. A partially exposed tab can remain a valid click target and center after selection; prove the complete selected caption rather than rejecting that native interaction or weakening final visibility checks. Keep `MinimumTabWidth` within the available strip when possible, and repair theme geometry instead of hiding arrows or scrollbars.

### `MudDataGrid` header controls

MudBlazor intentionally hides several header affordances until hover. If the theme causes the header to look blank, inspect:

- `.sort-direction-icon`
- `.column-options-icon`
- `.drag-icon-options`
- `.column-options .mud-menu .mud-icon-button-label`

When needed, provide a visible default state and a stronger hover state at the theme level.

### Tables flush with a surrounding card

Use `Square="true"` on a `MudTable` or `MudDataGrid` whose edges meet an enclosing card, such as a table immediately below the card's heading:

```razor
<MudTable T="JobOperationalSummary" Square="true" Elevation="0">
    ...
</MudTable>
```

The outer card owns the rounded outline and clipping. `components/mo-table.css`, imported by `mo-theme-main.css`, honors the generated `mud-table-square` class on both the table root and its direct `.mud-table-container`. The radius override is important because theme surface selectors can be more specific than MudBlazor's square rule. It changes only the shape; borders, header colors, and horizontal scrolling retain their existing behavior. Standalone tables keep their theme's default shape unless they explicitly request square corners.

Verify the root and scroll container both compute to zero radius in light and dark modes, while the enclosing card retains its radius. Include a rounded theme, a standalone table, and a narrow viewport with horizontal overflow in browser smoke checks.

### Snackbars and overlays

If you replace the default surface/background treatment, also verify:

- message text contrast
- action button contrast
- icon contrast
- success/info/warning/error variants

Changing only the container background is often not enough.

## Verification Workflow

1. Persist the target theme through `IBrowserStorage` or manual `localStorage` setup.
2. Verify the actual runtime state by checking:
   - wrapper class
   - `data-theme`
   - computed styles
3. Inspect the live DOM before editing selectors.
4. If component behavior is unclear, inspect MudBlazor source before guessing.
5. Verify both light and dark modes.
6. Verify the same component type on more than one page when the component is reused across the app.

## Manual Theme Debugging Note

When using Playwright or browser DevTools, remember that Monica's `IBrowserStorage` prefixes keys with `mo:`.

Application code reads:

```csharp
await BrowserStorage.GetAsync<ThemeData?>("theme:data", null);
```

Manual browser storage debugging must therefore set:

```text
mo:theme:data
```

not just `theme:data`.

## Layout Hooks (`mo-theme-main.css`)

Some components need sizing, scrolling, or positioning that MudBlazor's built-in parameters cannot express. These are handled through **layout hooks**: lightweight CSS classes injected via component parameters like `PopoverClass` or `ListClass`.

Layout hooks live in `mo-theme-main.css`, not in theme files or component CSS. They own only sizing, positioning, and scroll behavior. They never define colors, shadows, borders, or other visual styling.

### Current layout hooks

| Class | Purpose | Used by |
|-------|---------|---------|
| `mo-nav-menu-popover` | min-width for category menus (240px) | NavBarDropdown |
| `mo-nav-menu-popover-more` | min-width for overflow menu (220px) | NavBarMore |
| `mo-nav-menu-popover-compact` | fixed width for compact/mobile mode | NavBarMore (compact) |
| `mo-nav-menu-popover-scrollable` | max-height with viewport offset, scroll behavior | NavBarMore, NavBarDropdown submenus |
| `mo-nav-menu-popover-submenu` | max-height variant for nested submenus | NavBarMore submenus |
| `mo-nav-menu-list` | white-space nowrap for menu item text | All NavBar menus |

### When to add layout hooks

1. MudBlazor component parameters (`MaxHeight`, `MinWidth`, etc.) cannot express the required constraint.
2. The constraint is shared across multiple components or themes (not page-specific).
3. The hook defines only layout properties (`min-width`, `max-height`, `overflow`, `width`).

If the need is visual rather than layout, it belongs in theme CSS targeting standard MudBlazor selectors.

### Naming convention

Prefix with `mo-` to indicate Monica infrastructure. Use descriptive segments: `mo-{area}-{purpose}`.

## Related References

- `references/theme-authoring-pitfalls.md`
- `references/browser-storage-guide.md`
