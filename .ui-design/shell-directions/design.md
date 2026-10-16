# Monica shell design directions

Entry: [index.html](index.html). From the repository root, start the local preview:

```powershell
python -m http.server 63140 --bind 127.0.0.1 --directory .
```

Open `http://127.0.0.1:63140/.ui-design/shell-directions/index.html`. Serving the repository root makes the shared local font paths available. The equivalent command from another directory uses `--directory D:/Repositories/WorkTree2/MoLibrary`.

These are standalone design prototypes with sample data. **B / Grove is selected.** The next prototype applies Grove to the actual FIPS2022 Flight host: [Grove with real host information](../grove-real-shell/index.html), with [design notes](../grove-real-shell/design.md). Production APIs have not changed.

## Three directions

| Direction | Visual thesis | Palette and typography | Best fit |
| --- | --- | --- | --- |
| A — Lumen | An open, structured workspace makes the composed application easy to scan. | Cool white and cobalt; IBM Plex Sans for interface and hierarchy, monospace for technical evidence. | A versatile default across operational and configuration pages. |
| B — Grove | A calm workspace makes infrastructure feel considered and approachable. | Warm stone and forest green; editorial serif hierarchy with Plex interface text. | A distinctive identity with a softer reading rhythm. |
| C — Onyx | Architectural structure gives technical information a precise, focused presence. | Layered graphite with a restrained accent; strong typographic hierarchy and ruled, denser groups. | A more technical visual identity and sustained inspection. |

Each direction supports sidebar navigation, top navigation, and light/dark appearance. The gallery provides review-only comparison controls and an embedded interactive preview; each direction can also open as a standalone page. These comparison controls are outside the proposed product shell. In the product, navigation layout and appearance live together in Settings → Appearance, with no prominent layout switch. Search, module filtering, detail dialogs, and representative page navigation make the proposed language reviewable beyond a static image. Fonts and other assets remain local.

## Navigation and preference contract

Both layouts use the same navigation tree: **category → group → leaf page**. A group may contain further expandable branches. Opening a category or expanding a branch browses destinations without changing the active route; only selecting a leaf navigates. Search, breadcrumbs, active-page indicators, and authorization filtering derive from this same tree.

- **Expanded sidebar:** a 64px category rail sits beside a 226px context panel. The panel names the browsed category and presents grouped links and expandable branches. Leaf selection identifies the current page, while category browsing remains independent of it.
- **Collapsed sidebar:** collapsing hides the context panel and leaves the category rail. Selecting a category opens a temporary context flyout with the same hierarchy. Leaf selection or dismissal closes the flyout while preserving the saved collapsed preference. The user can explicitly expand the panel again.
- **Top navigation:** one 56px row contains the brand, a flexible category area, and fixed tools. It does not reserve a second navigation or breadcrumb row. Category buttons open grouped dropdown panels on click. Nested branches expand inside the panel rather than requiring hover cascades. A width-aware More control contains categories that do not fit, preserving their groups, branches, leaf destinations, and active-page indication. At 760px and below, the category area gives way to the combined mobile drawer. Width measurement runs after local fonts load; if resizing changes an open menu’s overflow membership, the panel closes and keyboard focus moves to a visible control.
- **Mobile navigation:** both layouts use a combined drawer that exposes categories and their nested pages. The drawer closes after leaf selection, including selection of the current page. Navigation remains reachable without compressing the content stage.
- **Appearance settings:** navigation layout and light/dark appearance are independent preferences presented together. Changes are staged until Apply; Cancel discards the pending changes. Apply persists preferences across refreshes and preserves the active route, module filter, search text, and in-progress page state. Moving between layouts also preserves the remembered sidebar collapse preference.
- Navigation disclosures expose their expanded state and controlled panel; active leaf links identify the current page. Escape and outside click dismiss transient panels; Escape returns focus to the trigger. Selecting a leaf restores focus to the visible category or mobile navigation trigger. Hidden navigation is removed from keyboard traversal. Opening the current page initially reveals its ancestor branches.

## Other interaction contracts

- Search opens a useful page destination; module filters change visible rows, and detail actions open readable overlays. Sample actions must clearly remain local to the preview.
- Selected, hover, focus, pressed, and disabled states need distinct treatments. Status uses text or icons alongside color; nonessential transitions honor reduced motion.
- Operational content uses the available width. Tables own local overflow; structural surfaces and repeated rows stay visually distinct from independently actionable cards.

## Future implementation handoff

The selected prototype should become the reference for composition, typography, density, spacing, surface roles, and responsive behavior. Record any intentional changes to the existing Precision language when the user chooses a direction. Map prototype roles to the framework instead of copying literal colors into components:

| Prototype role | Monica / MudBlazor target |
| --- | --- |
| Page canvas | `PaletteLight/PaletteDark.Background`; `--mud-palette-background` |
| Ordinary surface and subtle section fill | `Surface`, `BackgroundGray`; `--mud-palette-surface`, `--mud-palette-background-gray` |
| Sidebar | `DrawerBackground`, `DrawerText`, `DrawerIcon` |
| Top bar | `AppbarBackground`, `AppbarText` |
| Identity, selected navigation, primary action, focus | `Primary` and its contrast/light/dark roles; a shared focus treatment |
| Main and supporting text | `TextPrimary`, `TextSecondary`, `TextDisabled` |
| Separators and control outlines | `Divider`, `DividerLight`, `LinesDefault`, `LinesInputs` |
| Success, warning, error, information | Matching semantic palette roles and existing `--mo-color-state-*-soft-*` tokens |
| Body, display, diagnostic typography | Shared typography roles and local WOFF2 assets with explicit offline/CJK fallbacks |

Keep shell dimensions, navigation placement, and viewport scrolling in shell/layout styles; keep shared component appearance in theme styles. Add supplemental semantic tokens only where existing roles cannot express a reusable distinction.

Preserve the current registry-driven category/page ordering, localized labels and icons, authorization filtering, and active-route matching. Both navigation modes must support arbitrary contributed modules, long translated labels, nested groups, and overflow rather than assuming the prototype's short navigation list. Retain host identity, version, access to Appearance settings, language controls, and conditional host-provided actions; do not introduce inert search, notifications, or account controls when a host supplies no behavior. The gallery's review switches do not become product controls.

Before an implementation is accepted, compare its selected prototype at wide, intermediate, and narrow viewports in both appearances. Check width use, surface hierarchy, typography, menu/keyboard behavior, overflow, and local asset loading. Verification results belong in a separate record of checks actually performed.
