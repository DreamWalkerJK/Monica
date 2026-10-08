# Monica working-interface design language

Use this reference when designing, implementing, or reviewing an operational Monica page. It captures reusable principles from the accepted health workbench without making that page's readiness panel and registry a template for every module. Theme identity remains owned by [Precision Default](monica-precision-design-language.md) and [Grove](monica-grove-design-language.md); component mechanics remain owned by the implementation references.

## Start from the operator's task

Inspect the real page, its state owner, registered actions, facade data, authorization, and representative runtime values before choosing a composition. Write one sentence explaining what the operator needs to understand or accomplish. Identify the first decision, the evidence supporting it, and the next meaningful action. Design the hierarchy around those relationships rather than the number of properties available to display.

Different tasks need different compositions:

| Task | Composition to consider |
| --- | --- |
| Readiness and diagnostics | Current decision, affected evidence, then the full inspectable registry |
| Configuration and editing | Selection or context, grouped editable fields, validation, then save/apply state |
| Discovery and exploration | Search or category selection, comparable results, then contextual details |
| Execution and logs | Scope and time selection, ordered events or records, then evidence for the selected event |
| Reading and documentation | Navigation and a readable content measure, with supporting actions kept quiet |

These are starting points, not required regions. Do not add an attention panel to a page with no actionable attention state, or a headline statistic where the actual task is editing. A page should communicate its subject even when its title is hidden.

## Present honest decisions and evidence

Distinguish summary scope from filtered results. A whole-host state, all-registration count, or aggregate snapshot must not silently change meaning when the user filters the visible registry. Label scopes when they differ. Put a clear path from a concerning summary to the exact record or details that explain it.

Keep missing coverage, loading, stale evidence, query failure, access denial, no records, and no filter matches distinct. Zero records do not prove success. A live snapshot does not establish history, availability, a trend, or a percentage. Do not add simulated metrics, fabricated comparison periods, or decorative charts that the facade cannot support. Keep bounded, sanitized facade evidence encoded as text.

## Make hierarchy through type and spacing

Keep page identity concise: an optional quiet category label, a clear title, a short task description when useful, and small host or scope metadata. Group the primary action and last-update information where they can be found without competing with the title. Avoid a tall marketing hero on an operational page.

Use the selected theme's interface, display, and diagnostic families. Major identity headings may use the theme's display role; controls, repeated evidence, section titles, and table headers use its interface role. Monospace is useful for identifiers, durations, code, and diagnostic values, not for every sentence.

The health workbench provides a useful initial density:

| Role | Starting treatment |
| --- | --- |
| Page identity | 28–36px, weight 500, restrained line height |
| Section heading | 16–18px, weight 500 |
| Names and explanatory evidence | About 13px, weight 400; selected names may use 500 |
| Secondary labels, timestamps, tags, and counts | 10–11px where they remain comfortably readable |
| Major current decision | One larger statement when that decision merits priority |

Tune these values for the actual content, theme metrics, CJK text, viewport, and accessibility needs. They are role-based starting points, not global CSS overrides. Do not shrink descriptions into metadata to fit a layout. Reserve 600 or heavier weights for a small number of labels or critical emphasis instead of making every heading, row, and control bold. Verify the loaded local font and fallback in computed styles.

Use a consistent spacing rhythm: enough separation between page sections, less within a related group, and compact gaps between a value and its label. The accepted workbench uses roughly 24px section gaps and 20–28px surface padding on desktop, reducing local padding on phones. Let content and available width determine the final measurements. Alignment and readable line lengths matter more than matching every pixel value.

## Keep ordinary surfaces quiet

Use the full width supplied by the shell for working pages. Limit only local explanatory or reading text when needed. Assign canvas, ordinary surface, contextual surface, raised overlay, and semantic-state roles before styling. A border, a restrained tonal difference, and whitespace usually provide sufficient separation for ordinary evidence.

Repeated facts belong in rows, lists, definition groups, contiguous statistic strips, or tables. Use an independent card only for an independently actionable object or a genuinely distinct content group. Integrate filters with the results they control; avoid separating a filter bar and its registry into competing floating cards. A name, readable description, and quieter tags can form one row rather than several columns of truncated fragments.

Use small semantic dots, labels, or tonal badges when they aid scanning. Reinforce color with text or a recognizable icon. A zero attention count can remain neutral rather than signaling a live warning. Avoid a large saturated state banner when a prominent statement and a small severity cue already communicate the decision.

Keep working surfaces at low or zero elevation. Reserve stronger shadows for overlays that require separation. Structural panels and nested facts do not acquire the shared card-hover hook simply because they have a border. Keep shape and spacing component-owned; theme defaults must not force rounded pills or oversized controls over that geometry. See [theme CSS](theme-css-guide.md) for shared geometry and [Precision Default](monica-precision-design-language.md) for card hooks and decorative limits.

Prefer recognizable outlined icons for compact actions and evidence. Use existing local shell vectors or suitable MudBlazor outlined icons according to the context; elegance comes from consistent weight, size, alignment, and restraint rather than replacing an entire icon library. Icon-only actions need accessible names. Do not add decorative icons to every fact or leaf row.

Preserve Default's existing palette and every selectable theme's identity. Components express roles through MudBlazor palette variables or approved Monica tokens, while themes supply paint and font roles. Do not hardcode the Grove palette into a shared component, introduce external runtime fonts, or flatten all themes into one visual style.

## Design responsive and scroll ownership explicitly

The shell owns the available page region and main scroll area. The page uses that region rather than calculating a second viewport height from the AppBar. Give shrinking flex/grid children `min-width: 0`; height-constrained children may also need `min-height: 0`. Long identifiers wrap or scroll in their smallest intended owner, with full-value access preserved. A details body may own vertical scrolling while its heading and close action remain available. A page-owned persistent drawer stays inside the available content region rather than anchoring to the viewport origin; verify open and closed containment in both shell layouts. A temporary overlay has its own explicit placement and focus contract.

Verify the actual MudBlazor source and runtime DOM before coupling responsive CSS to a component breakpoint. In v9.0.0, `MudTableBase.cs` maps `Breakpoint.Xs` to `mud-xs-table`; `_table.scss` stacks that class at `max-width: 600px`. `Breakpoint.Sm` maps to `mud-sm-table` and stacks at `960px`. The enum name is not the media-query threshold. Read `Styles/abstracts/_variables.scss` and the selected component's styles for the installed version. Keep local stacked-cell spacing and responsive labels synchronized with that behavior. Do not convert a usable tablet table into phone rows accidentally.

When a layout rule appears ineffective, compare compiled isolation selectors with the live element and its real wrappers. A class on an inner `MudSelect` input does not necessarily constrain the outer select's flex item. Use a scoped native geometry wrapper and reachable `::deep` selectors before escalating specificity. See [CSS isolation](css-isolation-fix-workflow.md) and [adaptive lists/tables](adaptive-mudblazor-list-table-pattern.md).

Measure both `scrollWidth/clientWidth` and `scrollHeight/clientHeight` at the smallest scroll owner, its children, and relevant ancestors. An `overflow-x: auto` tab strip can gain a vertical scrollbar when a child extends below its box; a negative bottom margin is enough to cause this even though the page has no horizontal overflow. Repair the child/container geometry rather than hiding all scrollbars. Permit intentional scrolling only in the designated direction and region. Check intermediate widths, short viewports, long values, and empty results as well as the first wide screenshot.

## Preserve interaction and runtime state

Use native buttons and links for simple actions and disclosures; retain MudBlazor primitives for menus, popovers, dialogs, and drawers. Provide visible hover, selected, pressed, and keyboard-focus states without making informational surfaces look clickable. Keep motion purposeful and honor reduced motion; an immediately appearing context panel may be appropriate for frequent navigation.

An overlay's visual treatment does not prove that it contains focus, handles Escape, or restores focus. Verify the exact primitive's behavior. A temporary details drawer that needs dialog behavior must expose an accessible identity, keep focus within its open content, close through its supported paths, and restore focus appropriately. Mount focus trapping only while open. Verify close button, Escape, backdrop, and route-change behavior without broad key suppression.

Keep polling, cancellation, permission checks, filter combinations, selection reconciliation, and stale-data behavior with their existing state owner. Refresh should preserve active filters and reconcile selected details against the new snapshot. A visual refactor must not create a second polling loop or move authorization solely into markup. See [lifecycle guidance](blazor-best-practices.md) and [auto-refresh ownership](auto-refresh-page-pattern.md).

## Accept each page on real evidence

For a staged redesign, make each page or related page family independently reviewable. Record its task, retained behavior, intentional composition, and limitations in `.ui-design/{feature}/`; keep session logs and raw browser reports in `.tmp/`. Capture the actual bridge implementation, including populated, filtered/empty, detail, and narrow states where relevant. Preserve real failures instead of masking them for screenshots.

Compare the design and implementation through composition, evidence density, typography, spacing, surface roles, and interactions. Inspect Default and Grove in both modes and exercise representative rounded or expressive themes. When shared theme behavior changes, cover every selectable theme in both modes. Verify Topbar and Sidebar containment, a phone, the table's actual breakpoint boundary, and a useful intermediate width. A screenshot checks appearance; fresh browser errors, computed metrics, and interaction checks establish behavior and containment. Follow repository build/test policy; do not introduce UI rendering tests solely for a visual redesign.

Fix structural and behavioral differences before adding polish. Reject a page that invents data, hides meaningful evidence, overuses cards or semantic color, shrinks working text into metadata, has incidental scrollbars, loses keyboard access, or copies the health page's regions without serving its own task. Update this shared reference only when a later accepted page establishes reusable guidance; page-specific choices stay with their owning design report.
