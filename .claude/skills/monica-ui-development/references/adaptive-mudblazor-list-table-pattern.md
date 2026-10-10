# Adaptive MudBlazor List/Table Pattern

Use this reference when a Monica UI list/table has long values that crowd columns, overlap adjacent content, or create an unnecessary initial horizontal scrollbar.

The goal is not to restyle MudBlazor. The goal is a predictable, theme-first layout where important operational columns remain readable and long identifiers/names truncate safely with full-value access.

## When to Use This Pattern

Use this pattern for dense `MudTable` or `MudDataGrid` pages when:

- Long keys, names, instance IDs, cron expressions, timestamps, or statuses can overlap neighboring columns.
- A page should fit the initial viewport better, but still allow horizontal scroll if the viewport is truly too narrow.
- The user asks for adaptive clipping/ellipsis instead of hard-coded string truncation.
- Existing `table-layout: fixed`, inline `max-width`, or ad-hoc widths are not enough.
- You need alignment consistency between badges, icons, plain text, and action buttons.

Prefer a smaller fix for ordinary card/content overflow. For example, a System Info version/hash field usually only needs a scoped wrapper with `min-width: 0`, `max-width: 100%`, `overflow-wrap: anywhere`, `word-break: break-word`, and a full-value tooltip.

## Non-Negotiable Rules

1. Do not hard-code visible substrings such as `InstanceId[..8] + "..."` for layout. Render the full value and let CSS ellipsis control the visible text.
2. Provide full-value access when clipping is possible. Use `MudTooltip` for concise values, or an explicitly named, keyboard-accessible native inspector for long technical identities. Keep the exact encoded value in the rendered data and search contract.
3. Use scoped `.razor.css`; do not add broad/global CSS for one table.
4. Keep visuals theme-first. Use MudBlazor defaults and `var(--mud-palette-*)`; do not introduce gradients, custom colors, fonts, or decorative effects for a layout fix.
5. Avoid new shared copy components or broad copy affordances unless the user explicitly asks. If copy is requested, keep it targeted and MudBlazor-native.
6. Verify in the browser. Builds do not catch overlap, baseline drift, or empty-row centering issues.

For a full page redesign, use the shared [working-interface language](monica-workbench-design-language.md) to choose between a compact column table and readable multi-line evidence rows before applying this CSS-grid technique. This reference addresses adaptive column sizing rather than prescribing a single registry composition.

## Scroll only when the content requires it

Do not give every table an unconditional `min-width` or fixed-width column contract. First fit the actual headers, values and native sort/filter/action controls with natural or fractional columns, wrapping, shrinkable value owners, or the component's responsive rows. Enable local `overflow: auto` only when useful content still cannot fit after that adaptation; a width chosen for a dense example does not justify a scrollbar for empty or sparse results.

Render a padded, dedicated empty or no-match state at the available width instead of inheriting a populated table's wide column contract. If retaining native `NoRecordsContent`, reset populated-only minimum widths for that state and preserve its full-width message and usable native controls. Keep one owner per scrollable region: do not put independently scrolling wrappers around a native table container that already owns the same overflow. A dialog can own vertical scrolling while a genuinely wide table owns horizontal scrolling, but its table wrapper must not create a second unnecessary vertical scroll area.

Inspect the actual empty-row span and visible native headings when using fixed table layout or a `colgroup`. A mix of percentage widths and pixel-subtracting calculations can collapse a column in the empty state while populated rows appear correct. Check each visible heading's allocated width, label and native control fit, and the complete column boundary against the available table width. No scrollbar is not proof that the columns remain usable. Adapt to the table owner's width rather than the outer viewport.

Treat the native pager as part of this width contract. Its toolbar can wrap inside a narrow dialog even at a desktop viewport width. Adapt to the table's available width, allow the toolbar's height to grow, and reset native action margins with matching selector specificity when they push controls beyond the surface. Verify the complete rows-per-page group, result range and navigation actions after reflow. A fixed toolbar height can overlap the final row or bottom corner without producing document overflow.

Measure `clientWidth`/`scrollWidth` and `clientHeight`/`scrollHeight` on the native container, its wrapper and the containing page or dialog; also inspect the actual scrollbar in screenshots. Check empty, sparse and populated data at phone, intermediate and desktop widths, including every dialog tab, the final row and pager, and after native sorting, filtering or focusing the last action. Titles, Close actions and keyboard focus must stay accessible. Repair the column or owner geometry rather than using `overflow: hidden`, hidden scrollbar paint or clipped content to make the check pass. Rounded-corner paint containment must preserve necessary scrolling and focus visibility.

## Verify the actual responsive threshold

Read `MudTableBase.cs`, `Styles/components/_table.scss`, and `Styles/abstracts/_variables.scss` in the installed MudBlazor source before aligning local cell CSS with `Breakpoint`. In v9.0.0, `Breakpoint.Xs` emits `mud-xs-table`, which stacks at `max-width: 600px`; `Breakpoint.Sm` emits `mud-sm-table`, which stacks at `960px`. Match stacked spacing, `DataLabel` behavior, and first-cell treatment to that actual threshold, and verify immediately above and below it. Selecting `Sm` does not mean the table will remain a column table until 600px.

When converting column rows to a stacked grid, reset the wide cell-width contract with matching selector specificity. A class-specific `width: 37%` can beat a generic narrow `tbody td { width: auto; }`, leaving the identity inside a thin strip even when its grid cell spans the full row. Inspect computed cell widths and the actual text-to-row insets after reflow; a page with no overflow can still waste most of its usable width. Retain visible measurement-group labels when hiding the wide header, especially when several groups reuse captions such as Mean/Latest or contain unavailable values.

Native XS cells are flex rows: a reserved `DataLabel` width, cell padding and the value's or action group's minimum content width can still widen the table inside a narrow dialog. When that combination cannot fit, stack the native label and value locally at the affected width, allow values and action groups to shrink or wrap, and retain the real `DataLabel` and native controls. Check the container's width and scroll position both before and after focusing or clicking the last action. Browser scrolling to reveal that action can clip previously inset labels; an initial padding check alone misses the defect.

Bound opaque machine-key previews with CSS when their provenance would dominate working rows; preserve the full searchable value and expose exact identity in the inspector. Do not cap meaningful descriptions or replace identities with shortened string data. Inspect populated host records rather than assuming friendly fixture keys represent real metadata density.

## MudTable CSS-Grid Layout

Wrap the table in a page-specific container that owns the column contract:

```razor
<div class="job-definitions-table-wrapper">
    <MudTable Items="@_definitions" Class="job-definitions-table">
        ...
    </MudTable>
</div>
```

```css
.job-definitions-table-wrapper {
    --table-columns: minmax(12rem, 1.2fr) minmax(14rem, 1.4fr) minmax(12rem, 1fr) minmax(9rem, auto) minmax(11rem, auto);
    overflow-x: auto;
}

.job-definitions-table-wrapper ::deep .mud-table-root {
    display: grid;
    grid-template-columns: var(--table-columns);
    min-width: 0;
}

.job-definitions-table-wrapper ::deep thead,
.job-definitions-table-wrapper ::deep tbody,
.job-definitions-table-wrapper ::deep tr {
    display: contents;
}

.job-definitions-table-wrapper ::deep th,
.job-definitions-table-wrapper ::deep td {
    display: flex;
    align-items: center;
    min-width: 0;
    overflow: hidden;
}
```

### Column Sizing Guidance

- Give long text columns flexible `minmax(..., fr)` tracks.
- Give operational columns enough fixed/auto minimum width so they do not disappear: cron expression, next/last execution time, status, and actions are common examples.
- Keep the action column wide enough for all visible buttons. Do not solve overlap by hiding actions.
- Tune each table shape separately. Recurring definitions, trigger definitions, instances, and project units usually need different `--table-columns`.
- Use page-level full width (`MaxWidth="MaxWidth.False"`) for dense list pages when the default container is the real bottleneck.

## Ellipsis Value Wrappers

Inside any cell that can truncate, wrap the text/link/tooltip target with a block-level value class:

```razor
<MudTooltip Text="@context.JobKey">
    <span class="job-definitions-table__ellipsis">@context.JobKey</span>
</MudTooltip>
```

```css
.job-definitions-table__ellipsis {
    display: block;
    min-width: 0;
    max-width: 100%;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}
```

Important details:

- `min-width: 0` is required on both the flex cell and the value wrapper.
- `display: block` on the value wrapper avoids inline elements ignoring the available width.
- If the clipped value is a link, put the ellipsis class on the link or on a block wrapper inside the link.
- If `MudTooltip` introduces an extra wrapper, inspect the DOM and ensure the element receiving ellipsis has the constrained width.

## MudDataGrid Notes

For `MudDataGrid`, prefer the same wrapper principle but adapt selectors to the actual runtime DOM. Add `HeaderClass` and `CellClass` where useful, then style those classes through the scoped wrapper with `::deep`.

```razor
<MudDataGrid Items="@_units" Class="project-units-monitor-grid">
    <Columns>
        <PropertyColumn Property="x => x.UnitName" HeaderClass="project-units-monitor-grid__name" CellClass="project-units-monitor-grid__name" />
    </Columns>
</MudDataGrid>
```

Before writing selectors, inspect the live DOM or generated scoped CSS. MudBlazor class names and internal wrappers can differ from assumptions.

## Empty Rows Under CSS-Grid Tables

MudTable empty rows can become left-aligned or span only one grid column after converting rows to `display: contents`. Fix the generated empty row explicitly:

```css
.job-definitions-table-wrapper ::deep tr:has(.mud-table-empty-row) {
    display: contents;
}

.job-definitions-table-wrapper ::deep .mud-table-empty-row {
    grid-column: 1 / -1;
    width: 100%;
    justify-content: center;
    text-align: center;
}
```

Verify this visually for every tab/filter that can be empty.

Spanning the empty row fixes its alignment, not the width of its scroll owner. Apply the [content-driven scrolling contract](#scroll-only-when-the-content-requires-it) before accepting an empty table with a horizontal scrollbar.

## Alignment for Badges, Icons, and Plain Text

- Keep cells as flex containers with `align-items: center` so badges, icon buttons, and plain text share a consistent vertical line.
- If a badge/button column needs a slight left/right nudge, scope the tweak to the actual badge/button wrapper or a `:has(...)` selector.
- Do not apply padding shifts to the whole column when plain `-` values are also rendered there; that makes empty values look misaligned.
- Action stacks should use `align-items: center` and should not wrap unexpectedly at the target viewport.

## Verification Checklist

Before acceptance or PR:

1. Build affected projects with zero warnings.
2. Inspect the native container and its wrapper at desktop, intermediate and phone widths. Check both scroll dimensions and the actual visible bars with empty, sparse and populated results; confirm overflow exists only when useful content needs it.
3. Resize immediately above and below the actual table breakpoint, open every relevant dialog tab, and confirm each needed scroll direction has one designated owner. Check title/Close availability and keyboard focus after sorting, filtering and the last action.
4. Confirm long key/name/ID values show ellipsis, not overlap.
5. Confirm clipped values have full-value access through `MudTooltip` or the named keyboard-accessible inspector; verify actual technical identities, not only short fixture values.
6. Confirm operational columns remain readable: cron, next/last time, status, duration, actions. Inspect native filter/order controls too: the actual selected value must retain its meaning and direction at the narrowest owner in each theme, rather than merely fitting the outer control. A theme's larger input type can require a scoped compact font or a shorter localized label.
7. Confirm badges, icons, plain text, and actions align on one visual row.
8. Check empty states for centered full-width messages.
9. Save screenshots for populated and empty/narrow cases when the task is UI-facing.
10. Inspect the diff for overreach: no unrelated styling, no new shared copy components, no hard-coded truncation, and no broad global CSS.

## Common Pitfalls

- `display: contents` makes layout work but removes the row box; empty rows need explicit spanning, and row-level styles such as hover/background must be re-applied to cells, for example with `tr:hover td`.
- Ellipsis fails without `min-width: 0` on flex/grid children.
- Tooltip wrappers can become the real width owner; inspect the DOM if ellipsis does not appear.
- Inline elements do not reliably truncate; use block-level wrappers.
- `table-layout: fixed` alone often hides content rather than solving the column contract. The CSS-grid pattern can also conflict with `FixedHeader="true"` because sticky header positioning needs stable parent boxes; verify fixed-header behavior before combining the two.
- Overly small min widths on operational columns cause status/actions to disappear first, which is usually worse than truncating keys/names.
- CSS isolation will not style MudBlazor internals unless the selector crosses from a real scoped wrapper with `::deep`.
