# Monica visual language

This reference owns the shared visual grammar of every Monica operational page: the signature region, color roles, scale tiers, surface layers, and the reusable component vocabulary. It applies under every selectable theme. The [working-interface language](monica-workbench-design-language.md) owns task analysis, evidence honesty, responsive containment, and acceptance; [Precision Default](monica-precision-design-language.md), [Grove](monica-grove-design-language.md), and the other theme definitions own the actual paint and fonts.

A page built from neutral bordered panels, small muted labels, and one accent fails this language even when it is contained, accessible, and correct. Restraint is not the baseline. Each page should look deliberately designed: a strong subject, several meaningful colors, clear scale contrast, and varied but related element shapes.

## Five principles

1. **One signature per page.** Give each view one feature region built from its real evidence and sized to own the first viewport. It might be a timeline, topology, distribution, ordered sequence, matrix, comparison, or a decisive statement with its supporting facts. A row of equal KPI boxes is not a signature.
2. **Color carries meaning, and there is more than one.** Before styling, write a role map that assigns palette roles to the page's real categories. Apply the same mapping in the signature, ledger meters, legends, path nodes, and inspector so a reader learns it once.
3. **Scale contrast.** Use one display-scale value or statement per view, structural titles, readable evidence, and comfortable metadata, as described under [scale tiers](#scale-tiers). Contrast comes from size and weight differences, not from bolding everything.
4. **Say the finding.** Under the feature value, write one sentence that states what the evidence means, with inline value marks and a link to the responsible record. Generate it from facade data, and omit it when the data supports no conclusion.
5. **Layered surfaces.** Use canvas, then ordinary surface, then a tinted feature wash, then soft semantic panels, then a raised overlay. Hairline dividers separate rows. Resting shadows stay nearly flat; pronounced depth belongs to overlays.

Choose the signature by task:

| Page task | Signature to consider |
| --- | --- |
| Startup, execution, timing | Lanes on a real time axis with scope brackets, a threshold marker, and selectable spans |
| Topology, dependencies, routing | Graph or layered map with role-colored nodes and a selected-context column |
| Inventory, registry | Distribution strip or ranked contribution ledger with proportional meters |
| Health, readiness, diagnostics | Decisive state statement with a contiguous fact grid and linked affected records |
| Configuration, editing | A selected-object identity band on a tonal wash above grouped fields; no invented chart |
| Logs, events, messages | A volume or severity strip over the real interval, above the ordered records |

If the facade has no quantitative evidence, make the signature structural: identity, grouping, and composition. Never fabricate a chart to fill the slot.

## Color system

Every page uses the neutral role plus at least two categorical palette roles whenever its data has categories. Map categories to palette roles; never color by index or build a rainbow. Adjacent segments of one category alternate between the solid role and its lighter mix rather than taking new hues.

| Role | Typical palette source | Use |
| --- | --- | --- |
| Identity / first category | `--mud-palette-primary` | Signature emphasis, selected state, the page's main category |
| Second category | `--mud-palette-info` or `--mud-palette-secondary` | A distinct contributing category, a second meter segment |
| Third category | `--mud-palette-secondary` when Info is already used, or a theme-supplied solid role | Only when the data has a third real category. Default's Tertiary is a soft tint, not a solid category color |
| Host / baseline | `--mud-palette-lines-inputs`, `--mud-palette-text-secondary` | Context not owned by the subject |
| States | Success, Warning, Error, with `--mo-color-state-*-soft-*` tokens | Only actual states, such as a budget exceeded or a failure |

Derive tonal variants from palette variables, never from literals:

```css
/* Solid: bars, marks, selected indicators */
background: var(--mud-palette-info);
/* Soft fill: inline value marks, pressed chips, icon tiles */
background: color-mix(in srgb, var(--mud-palette-info) 14%, var(--mud-palette-surface));
/* Wash: feature-region background tint */
background: color-mix(in srgb, var(--mud-palette-primary) 6%, var(--mud-palette-surface));
/* Ink on soft: readable small text inside a soft fill (measure 4.5:1) */
color: color-mix(in srgb, var(--mud-palette-info) 70%, var(--mud-palette-text-primary));
```

Percentages are starting points; measure the rendered pair in both modes and every theme. A theme may supply named soft or wash tokens when a mix fails its palette.

When a component derives fills and inks from a shared role variable such as `--mo-vl-role`, declare the fallback default with `:where()` so its specificity stays zero and any single-class role modifier (`mo-vl-role--info`, `mo-vl-role--warning`, …) wins regardless of rule order. An `:is()` default carries class-level specificity and silently repaints every mark, callout, chip, and status dot of a page in the primary hue, flattening the role map the page was designed around.

### Gradient policy

Allowed:

- A single-hue tonal wash, derived from one palette role and fading to the surface, behind a feature region or identity masthead.
- Gradients in visualizations that encode a continuous scale, with the mapping documented.
- Theme-owned brand marks and illustrations.

Not allowed: multi-hue decorative gradients, and gradients on controls, filters, table rows, dialogs, text, or behind dense evidence.

## Scale tiers

| Tier | Starting treatment |
| --- | --- |
| Display | One feature value per view: 56–84px, weight 250–350, tight negative tracking, tabular numerals, unit at roughly a third of the size in secondary ink |
| Page title | 26–32px, weight 600, tracking about −0.02em |
| Section title | 16–18px, weight 600 |
| Lede and evidence | 13–14px, weight 400; record names 13.5px at weight 500 |
| Metadata | 11.5–12.5px in secondary or tertiary ink; never below 11.5px Latin or 12px CJK |
| Diagnostic values | Durations, IDs, offsets, and ranks in the theme's diagnostic family at about 0.92em, right-aligned in columns |

Use the theme's interface, display, and diagnostic families. A theme with an editorial display role, such as Grove, may set the page title in that family. Display numerals stay in the interface family at a light weight so they read as data.

## Component vocabulary

Build these as shared Monica components or shared CSS roles once, then reuse them. Inspect existing shared components before adding one; page-local copies drift.

- **Masthead:** crumb, page title, a status line (status dot with a soft halo, revision, a copyable identity chip, capture time), and an action group with exactly one filled primary action.
- **Feature value and lede:** display numeral and unit, followed by the one-sentence finding with inline **value marks**: compact soft-fill chips with role ink around the key numbers.
- **Fact grid:** two-by-two or one-by-four contiguous facts sharing one border, with hairline internal separators, a short label, and a light 24px value. These are not separate cards.
- **Lanes:** a label column with name and duration, a track on a real axis with tick labels and faint grid lines, scope brackets naming aggregate intervals, a dashed marker line with an inverse flag for a threshold or ready instant, a hover crosshair with an exact readout, keyboard-selectable lanes, and a caption bar reporting the selected lane.
- **Two-role meter:** a 6px stacked bar against one stated shared scale, with role values captioned beneath in the diagnostic family.
- **Ranked ledger:** mono rank, record name, provenance metadata with capability badges, meter, right-aligned total, and a chevron affordance. Show the top N with an explicit expand control, and summarize the remainder in the footer.
- **Spine:** ordered nodes colored by role, joined by a 1.5px connector, each with a name, a sublabel, and a duration.
- **Dot distribution:** small squares per counted item, tinted by role intensity, with the exact count beside each row.
- **Semantic callout:** soft state fill, a tinted icon tile, a title, and a body that links the responsible record.
- **Filter chips:** pill toggles; pressed uses the primary soft fill. Status filters keep the [semantic filter contract](monica-workbench-design-language.md#preserve-semantic-color-in-status-filters).
- **Inspector sheet:** a temporary panel inset about 12px from the viewport edges, with a 16px radius and a lightly blurred scrim. Inside: a display total, a split meter with a legend, a key–value identity group, and relation chips that navigate to related records.
- **Icons:** 1.5px stroke outlines at 16–18px, consistent across shell and page.

## Theme mapping

The grammar is theme-independent and the paint is theme-owned. Components consume palette variables and `--mo-color-*` tokens only. Default supplies violet and info blue over cool surfaces, with Zinc and Indigo in dark mode. Grove supplies forest over warm stone. Expressive themes may restyle radius, border, and texture through theme CSS, but they keep the signature, the role map, and the scale tiers. Verify each page in Default and Grove in both modes, plus at least one expressive theme.

## Richness review

Before treating a page as done, compare its capture with the previous version and confirm each item:

- The first viewport shows the signature region populated with real evidence.
- At least two categorical roles plus neutral are used consistently across regions, with a legend where the mapping is not self-evident.
- Exactly one display-scale value or statement appears per view.
- A one-sentence finding is present wherever the data supports one.
- Repeated facts appear as rows, strips, or ledgers rather than card grids, and empty states stay compact.
- Shared vocabulary components are reused, not reimplemented.
- Side by side with the old capture, the page does not read as the same panels holding different numbers.

These checks add to the contrast, containment, honesty, and accessibility checks in the working-interface language; they never replace them.
