# Monica working-interface design language

Use this reference when designing, implementing, or reviewing an operational Monica page. It captures reusable principles from accepted page work without making the health workbench's readiness panel, registry, or restrained surface treatment a template for every module. Choose an intentional visual direction for each page; neither minimalism nor a decorative hero is mandatory. Theme identity remains owned by [Precision Default](monica-precision-design-language.md) and [Grove](monica-grove-design-language.md); component mechanics remain owned by the implementation references.

## Start from the operator's task

Inspect the real page, its state owner, registered actions, facade data, authorization, and representative runtime values before choosing a composition. Write one sentence explaining what the operator needs to understand or accomplish. Identify the first decision, the evidence supporting it, and the next meaningful action. Design the hierarchy around those relationships rather than the number of properties available to display.

Different tasks need different compositions:

| Task | Composition to consider |
| --- | --- |
| Readiness and diagnostics | Current decision, affected evidence, then the full inspectable registry |
| Configuration and editing | Selection or context, grouped editable fields, validation, then save/apply state |
| Discovery and exploration | Search or category selection, comparable results, then contextual details |
| Execution and logs | Scope and time selection, ordered events or records, then evidence for the selected event |
| Reading and documentation | Navigation and a readable content measure, with supporting actions given appropriate priority |

These are starting points, not required regions. Do not add an attention panel to a page with no actionable attention state, or a headline statistic where the actual task is editing. A page should communicate its subject even when its title is hidden.

## Present honest decisions and evidence

Distinguish summary scope from filtered results. A whole-host state, all-registration count, or aggregate snapshot must not silently change meaning when the user filters the visible registry. Label scopes when they differ. Put a clear path from a concerning summary to the exact record or details that explain it.

Keep missing coverage, loading, stale evidence, query failure, access denial, no records, and no filter matches distinct. Zero records do not prove success. A live snapshot does not establish history, availability, a trend, or a percentage. Do not add simulated metrics, fabricated comparison periods, or decorative charts that the facade cannot support. Keep bounded, sanitized facade evidence encoded as text.

## Make hierarchy through composition, type, and spacing

Give page identity appropriate prominence: a category label when useful, a clear title, task context, and host or scope metadata. An identity or feature region can carry stronger color, larger display type, an asymmetric composition, or a deliberate graphic treatment when it makes the page more legible and distinctive. Group actions and last-update information where they can be found. Keep the amount of introductory space proportionate to the task rather than forbidding or requiring a prominent hero.

Use the selected theme's interface, display, and diagnostic families. Major identity headings may use the theme's display role; controls, repeated evidence, section titles, and table headers use its interface role. Monospace is useful for identifiers, durations, code, and diagnostic values, not for every sentence.

The accepted health workbench provides one useful density example, not a required scale for all page families:

| Role | Starting treatment |
| --- | --- |
| Page identity | 28–36px, weight 500, deliberate line height |
| Section heading | 16–18px, weight 500 |
| Names and explanatory evidence | About 13px, weight 400; selected names may use 500 |
| Secondary labels, timestamps, tags, and counts | 10–11px where they remain comfortably readable |
| Major current decision | One larger statement when that decision merits priority |

Tune these values for the actual content, theme metrics, CJK text, viewport, and accessibility needs. They are role-based starting points, not global CSS overrides. Do not shrink descriptions into metadata to fit a layout. Use weight and scale contrast intentionally; a more expressive title or key value can be heavier while repeated evidence remains easy to scan. Avoid making every heading, row, and control equally bold. Verify the loaded local font and fallback in computed styles.

Use a consistent spacing rhythm: enough separation between page sections, less within a related group, and compact gaps between a value and its label. The accepted workbench uses roughly 24px section gaps and 20–28px surface padding on desktop, reducing local padding on phones. Let content and available width determine the final measurements. Alignment and readable line lengths matter more than matching every pixel value.

## Give surfaces distinct roles and character

Use the full width supplied by the shell for working pages. Limit only local explanatory or reading text when needed. Assign canvas, identity, ordinary evidence, contextual, raised-overlay, and semantic-state roles before styling. Choose color fields, inverse surfaces, tonal contrast, borders, depth, and spacing to make those roles visibly distinct. Do not flatten every region into the same neutral bordered panel. A page may be restrained or expressive; judge whether its visual treatment strengthens the task and hierarchy rather than enforcing quietness as a quality rule.

Repeated facts belong in rows, lists, definition groups, contiguous statistic strips, or tables. Use an independent card for an independently actionable object or a genuinely distinct content group, and design the group's character rather than wrapping every field in another container. Integrate filters with the results they control. A name, readable description, and secondary tags can form one row rather than several columns of truncated fragments. Richer feature groups, contrasting identity panels, and varied section compositions remain valid when their roles are clear.

Choose semantic dots, labels, badges, banners, or contrasting state surfaces according to the decision's importance and urgency. Reinforce color with text or a recognizable icon. Keep severity meaning accurate; for example, a zero attention count does not imply a live warning. Use identity and primary palette roles for expressive composition instead of borrowing error or success colors as unrelated decoration.

Set elevation and effects according to the intended spatial hierarchy and theme. Depth can help a feature surface or interactive group stand out, but large repeated shadows must not obscure evidence or make every region compete. Gradients, patterns, and illustration can contribute to an intentional page direction when legibility and semantic states remain clear; none is a default requirement. Structural panels and nested facts do not acquire the shared card-hover hook simply because they have a border. Keep shape and spacing component-owned; theme defaults must not force rounded pills or oversized controls over that geometry. See [theme CSS](theme-css-guide.md) for shared geometry and [Precision Default](monica-precision-design-language.md) for card hooks and theme fidelity.

Choose recognizable icons with consistent weight, size, and alignment. Existing local shell vectors and suitable MudBlazor outlined icons serve compact actions and evidence well; a feature region may use a more prominent icon treatment when it establishes useful identity. Icon-only actions need accessible names. Avoid adding an icon to every fact or leaf row without a useful purpose.

Preserve Default's existing palette and every selectable theme's identity. Components express roles through MudBlazor palette variables or approved Monica tokens, while themes supply paint and font roles. Do not hardcode the Grove palette into a shared component, introduce external runtime fonts, or flatten all themes into one visual style.

Check small-text contrast against the rendered background in every theme and color mode, including inverse identity metadata, selected filters, severity captions and expanded inspectors. Raw Primary, Info or Warning may suit an action or accent but fail as small text on a tonal surface. A component-local mix toward TextPrimary can retain the hue while restoring legibility; measure the actual surface and cascading styles rather than prescribing one percentage for every role. Verify the control's rendered state selector against the installed dependency: a selected MudBlazor 9 ChipSet child can use `mud-chip-selected` with a text variant, so a filled-only correction may never apply. Distinguish actual browser measurements from palette-derived predictions for states the host does not expose.

## Design responsive and scroll ownership explicitly

The shell owns the available page region and main scroll area. The page uses that region rather than calculating a second viewport height from the AppBar. Give shrinking flex/grid children `min-width: 0`; height-constrained children may also need `min-height: 0`. Long identifiers wrap or scroll in their smallest intended owner, with full-value access preserved. A details body may own vertical scrolling while its heading and close action remain available. A page-owned persistent drawer stays inside the available content region rather than anchoring to the viewport origin; verify open and closed containment in both shell layouts. A temporary overlay has its own explicit placement and focus contract.

Verify the actual MudBlazor source and runtime DOM before coupling responsive CSS to a component breakpoint. In v9.0.0, `MudTableBase.cs` maps `Breakpoint.Xs` to `mud-xs-table`; `_table.scss` stacks that class at `max-width: 600px`. `Breakpoint.Sm` maps to `mud-sm-table` and stacks at `960px`. The enum name is not the media-query threshold. Read `Styles/abstracts/_variables.scss` and the selected component's styles for the installed version. Keep local stacked-cell spacing and responsive labels synchronized with that behavior. Do not convert a usable tablet table into phone rows accidentally.

When a layout rule appears ineffective, compare compiled isolation selectors with the live element and its real wrappers. A class on an inner `MudSelect` input does not necessarily constrain the outer select's flex item. Use a scoped native geometry wrapper and reachable `::deep` selectors before escalating specificity. See [CSS isolation](css-isolation-fix-workflow.md) and [adaptive lists/tables](adaptive-mudblazor-list-table-pattern.md).

Measure both `scrollWidth/clientWidth` and `scrollHeight/clientHeight` at the smallest scroll owner, its children, and relevant ancestors. An `overflow-x: auto` tab strip can gain a vertical scrollbar when a child extends below its box; a negative bottom margin is enough to cause this even though the page has no horizontal overflow. Repair the child/container geometry rather than hiding all scrollbars. Permit intentional scrolling only in the designated direction and region. Check intermediate widths, short viewports, long values, and empty results as well as the first wide screenshot.

## Preserve interaction and runtime state

Use native buttons and links for simple actions and disclosures; retain MudBlazor primitives for menus, popovers, dialogs, and drawers. Provide visible hover, selected, pressed, and keyboard-focus states without making informational surfaces look clickable. Keep motion purposeful and honor reduced motion; an immediately appearing context panel may be appropriate for frequent navigation.

An overlay's visual treatment does not prove that it contains focus, handles Escape, or restores focus. Verify the exact primitive's behavior. A temporary details drawer that needs dialog behavior must expose an accessible identity, keep focus within its open content, close through its supported paths, and restore focus appropriately. Mount focus trapping only while open. Verify close button, Escape, backdrop, and route-change behavior without broad key suppression. Check the accessible name on the actual native dialog rather than an attributed inner wrapper. When a drill replaces its focused control, transfer focus to a stable native control inside the primitive's actual focus trap before clearing that content. A title or header can lie outside the trap even while it belongs to the dialog; verify the installed source and rendered boundaries.

Keep polling, cancellation, permission checks, filter combinations, selection reconciliation, and stale-data behavior with their existing state owner. Refresh should preserve active filters and reconcile selected details against the new snapshot. A visual refactor must not create a second polling loop or move authorization solely into markup. See [lifecycle guidance](blazor-best-practices.md) and [auto-refresh ownership](auto-refresh-page-pattern.md).

## Accept each page on real evidence

For a staged redesign, make each page or related page family independently reviewable. Record its task, retained behavior, intentional composition, and limitations in `.ui-design/{feature}/`; keep session logs and raw browser reports in `.tmp/`. Capture the actual bridge implementation, including populated, filtered/empty, detail, and narrow states where relevant. Preserve real failures instead of masking them for screenshots.

Compare the design and implementation through composition, evidence density, typography, spacing, surface roles, and interactions. Inspect Default and Grove in both modes and exercise representative rounded or expressive themes. When shared theme behavior changes, cover every selectable theme in both modes. Verify Topbar and Sidebar containment, a phone, the table's actual breakpoint boundary, and a useful intermediate width. A screenshot checks appearance; fresh browser errors, computed metrics, and interaction checks establish behavior and containment. Follow repository build/test policy; do not introduce UI rendering tests solely for a visual redesign.

Fix structural and behavioral differences before adding polish. Review the page's visual character as well as correctness: a technically contained page can still fail when its composition is generic, its hierarchy is flattened, or its identity is lost. Reject invented data, hidden evidence, misleading severity, unreadable working text, incidental scrollbars, lost keyboard access, or copied regions that do not serve the page's task. Retain the accepted health workbench as one successful example, not a universal requirement for neutral surfaces or understated presentation. Update this shared reference when accepted feedback establishes reusable guidance; page-specific choices stay with their owning design report.
