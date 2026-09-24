# Monica UI audit categories

## Findings

Scan for these violations in order of severity:

### P0 — Unsafe async component lifetime or JS interop ownership

Indicators:
- `OnAfterRenderAsync`, an event callback, a navigation callback, timer, or background task can remain incomplete while `DisposeAsync` disposes an `IJSObjectReference`, `DotNetObjectReference`, cancellation source, or other resource that its continuation later uses
- A nullable `IJSObjectReference` field is treated as usable merely because it is non-null; disposal leaves the disposed reference published, or initialization can publish a newly imported reference after disposal
- An async continuation touches component state, requests a render, or invokes JavaScript after an `await` without a component-lifetime cancellation/disposal check when the awaited operation can outlive the component
- Cancellation can abandon a resource-producing operation, such as a JS module import, without observing and disposing a late-created result
- Fire-and-forget work is untracked, uncancelable, or allowed to report exceptions outside the renderer's observed lifecycle task
- A component-owned callback/render delegate, event subscription, timer, or cancellation source remains attached after disposal begins
- A disposable transient service is injected into a component at all, or a scoped disposable intended to match component lifetime is resolved from the longer-lived app/circuit scope; manual disposal adds a second conflicting owner
- Multiple render/event paths can initialize, invoke, replace, or dispose the same JS module without explicit single ownership, serialization, or idempotent teardown
- `DotNetObjectReference.Create(...)` is passed inline to JavaScript or otherwise not retained and deterministically disposed by its .NET owner; JavaScript retaining the reference does not transfer disposal ownership unless an explicit, verified ownership contract says so
- `DotNetObjectReference` is disposed before every JS observer, event listener, animation-frame callback, or other callback producer that can invoke it has been stopped
- Per-component JavaScript state is stored in module globals or located with document-global selectors, so overlapping old/new component instances can mutate or tear down each other's state; queued animation frames, timers, or observers survive the JS session's cleanup
- `DisposeAsync` invokes JavaScript to mutate or clean up DOM that the renderer may already have removed; DOM cleanup should be owned by client-side `MutationObserver` logic
- Server-side JS module calls or disposal ignore expected circuit loss (`JSDisconnectedException`), or code broadly swallows `ObjectDisposedException` instead of repairing the ownership race

Fix: give the component/state owner one explicit lifetime. Mark it disposed and cancel ordinary lifetime-bound work before teardown; stop scheduling work and detach callbacks/render delegates; recheck lifetime after incomplete awaits; prevent new resource acquisitions; serialize with or drain existing users; then atomically detach shared references for teardown. Always observe resource-producing operations and dispose late-created references instead of publishing them. Stop JS callback producers before disposing their `DotNetObjectReference`, and dispose every resource once. Do not register component-consumed disposable transients. Prefer a non-disposable factory that creates a component-owned resource. Use an explicit component-owned DI scope only after inspecting the full dependency graph and proving it does not require services bound to the existing Blazor circuit scope. Keep JS session state per component/root and cancel all queued work during session cleanup. Use client-side `MutationObserver` for DOM cleanup. Catch `JSDisconnectedException` where server-circuit loss is expected. Do not catch `ObjectDisposedException` from a locally owned interop reference; repair the ownership and teardown ordering.

### P1 — Custom interactive markup replacing MudBlazor primitives

Indicators:
- Custom dropdown/flyout/overlay markup with manual `@onmouseenter` / `@onmouseleave` / `@onclick` state machines
- Manual `_isOpen`, `_isHovered`, delayed-close `CancellationTokenSource` patterns
- Custom `<div class="dropdown-menu">` / `<div class="flyout-menu">` / `<div class="popover-panel">` instead of `MudMenu`, `MudPopover`, `MudDialog`, `MudDrawer`

Fix: replace with MudBlazor primitives. Reference implementation: `NavBarDropdown.razor`, `NavBarMore.razor` in `Monica.UI/Shell/Components/Layout/`.

### P2 — Broken layout containment, sizing, or scroll ownership

Indicators:
- An operational dashboard, workbench, administration page, or diagnostics page uses an arbitrary page-shell `width` or `max-width`, leaving usable shell width empty
- A `FullWidth` dialog, drawer, tab panel, or preview has a descendant `width`, `max-width`, grid track, or intrinsic `inline`/`fit-content` size that leaves usable space empty
- A flex/grid child that must shrink omits `min-width: 0`, or a grid uses `1fr` where `minmax(0, 1fr)` is required
- A component-isolated selector targets a MudBlazor render root it cannot reach; use a scoped native wrapper or a reachable `::deep` descendant selector
- Long hashes, identifiers, JSON, tables, or code stretch an ancestor instead of wrapping or scrolling inside the intended local surface
- The page or dialog becomes the horizontal scroll owner when only a table, diff, or code surface should scroll
- A fixed/minimum height leaves unexplained blank space instead of using content-driven height with a viewport-aware cap

Fix: trace the layout from the dialog/page surface to the failing descendant and assign width, shrink, wrap, and overflow responsibility explicitly. Make operational page roots consume the owner's full available width; apply readable line-length limits to local text regions instead of the page shell. Remove contradictory internal width caps, use `width: 100%`, `min-width: 0`, `minmax(0, 1fr)`, `overflow-wrap`, or a local scroll surface only where each property expresses the intended contract. Keep long identifiers fully accessible for inspection and copying. Justify every retained page-level width cap or fixed/minimum height with a concrete editorial, readability, interaction, or viewport requirement.

### P3 — Theme bypass, visual-language drift, or prototype-fidelity loss

Indicators:
- Inline `Style=` / `style=` attributes for layout or visuals
- Hardcoded color, shadow, radius, or background values instead of MudBlazor parameters, `var(--mud-palette-*)`, or approved `var(--mo-color-*)` tokens
- Semantically distinct statuses, severities, categories, or progress states collapse into visually identical neutral surfaces when scanability requires differentiation
- Multiple hierarchy levels and semantic states rely on the same neutral surface plus one accent, producing a flat or monotonous page
- The page has no subject-specific design thesis or palette/type/layout/signature plan, and its composition could be reused unchanged for an unrelated AI administration product
- Light or dark mode is excessively dark, low-contrast, or monotone, so canvas, structural surfaces, borders, text hierarchy, or semantic states collapse together
- Success, warning, error, information, selection, and runtime states are not meaningfully differentiated when the task requires rapid scanning
- Repeated facts, properties, metrics, or records are each wrapped in cards instead of rows, lists, definition groups, or tables
- More than one visual system competes as the page's signature treatment, or a gradient/glow/pattern is not backed by the accepted prototype or continuous-data semantics
- A page shell, app bar, navigation surface, ordinary card, KPI tile, panel, filter, table, data group, or status surface uses a decorative gradient
- Actionable controls, rows, or cards lack visible hover, focus, pressed, or selected feedback
- Informational surfaces translate, scale, change the cursor, or use strong lift on hover, falsely implying an unavailable action; subtle border, tonal, or low-shadow spatial-focus feedback is valid
- Motion uses large travel, bouncing, repeated flourishes, continuous ambient effects, or ignores `prefers-reduced-motion`
- A surface stacks more than two decorative cues, such as top stripe + ring + shadow, or repeats stripes/rings across every card
- The same accent rail is repeated across unrelated card groups, replaces a clearer page-specific outline/icon/badge treatment, or appears where it encodes no additional status or category meaning
- Border radii fall outside the 6/8/12 scale, multiply a base radius, or grow across nested surfaces
- The implementation loses the prototype's typography, density, spacing rhythm, composition, surface roles, width utilization, or responsive reflow
- Interface, hierarchy, and diagnostic text have no deliberate font-role separation, or monospace is used broadly as decoration
- A font declared by the prototype/source is missing from local WOFF2 assets, lacks `@font-face`, fails offline, has no explicit fallback stack/CJK coverage, or is silently replaced by a fallback
- Component CSS repainting global MudBlazor behavior that should be consistent across modules
- Component CSS painting route `.active`, `:hover`, or `:focus` states for menu items, nav links, or list items that are themed globally
- Dark-mode overrides inside component CSS (`[data-theme*="dark"]` in `.razor.css`)

Fix: articulate the subject-specific thesis and palette/type/layout/signature plan; rebuild hierarchy with typography and spacing; assign explicit surface and semantic-color roles; keep ordinary surfaces structurally simple; convert repeated data cards to rows/lists/tables; enforce the 6/8/12 radius scale; restore useful hover/focus/pressed/selected feedback; and simplify flamboyant, repetitive, or misleading effects. Keep one coherent prototype-backed signature system concentrated in identity/readiness, with only limited non-competing recurrence, and use gradients elsewhere only for continuous-data encoding. Use short purposeful motion, test `prefers-reduced-motion`, vendor role-appropriate fonts locally as WOFF2, and verify both primary and fallback paths. Compare prototype and browser at widths 1440, 929, and 390 in light and dark modes, then self-critique for generic AI-dashboard styling. Keep component-owned layout and token-based presentation in component CSS; move only shared MudBlazor behavior to theme CSS.

### P4 — Private component classes that force theme coupling

Indicators:
- Custom CSS classes on menu surfaces, row items, or overlay panels (e.g. `.my-dropdown`, `.custom-flyout-item`) that are also targeted in theme files
- Theme files containing selectors for these private component classes
- Grep theme files for the private class name to confirm coupling

Fix: remove theme coupling. If the class is only used by the owning component, keep it in the component's `.razor.css`. Promote a hook to shared CSS only when multiple components intentionally share the same layout contract.

### P5 — Duplicate utility or raw CLR type-display logic

Indicators:
- Text-resolution helpers like `GetItemText(NavigationItem)` duplicated across components instead of using `NavigationItem.ResolveDisplayText(localizer)`
- Manual route-matching wrappers instead of `NavigationRouteMatcher.GetActiveClass()`
- Repeated localization key lookup patterns that could use shared model methods
- Ordinary UI labels, tooltips, table cells, or drawer details receive type names from `Type.FullName`, `Type.AssemblyQualifiedName`, or `Type.ToString()`, exposing CLR generic backticks, `[[...]]` assembly-qualified arguments, version, culture, or public-key-token metadata
- Razor components format `System.Type` locally instead of receiving one normalized display value from the producing diagnostics, facade, view-model, or state boundary

Fix: use the shared utilities in `Monica.UI/Shell/Support/` and methods on `Monica.UI/Shell/Models/NavigationItem`. Format user-visible CLR type identities with `Monica.Tool.Extensions.GenericTypeExtensions.GetCleanFullName()` at the producing boundary, then pass the clean string to Razor. Do not expose assembly-qualified type identity in ordinary UI. Showing an assembly name or identity remains valid when assembly loading, binding, inventory, or module provenance is the explicit subject of the view.


