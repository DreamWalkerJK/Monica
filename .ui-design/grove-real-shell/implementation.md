# Monica shell implementation and refinements

The selected Grove direction is implemented in Monica.UI, with the original Default theme preserved as the framework default. The checkout is running through the FIPS2022 Flight host at [System Information](http://localhost:5294/system-info) and [AI Chat](http://localhost:5294/ai-chat). No consumer source changes were needed.

## Appearance and themes

- Default retains its original light and dark palettes and Precision typography. Its palette values match the original implementation.
- Grove is a separate optional theme (`MonicaThemeKind.Grove = 11`), with warm stone surfaces, forest accents, local interface fonts, and editorial headings. Existing theme identifiers remain unchanged.
- Appearance provides all 12 themes, light/dark modes, and the Sidebar/Topbar setting. Changes are staged until Apply; Cancel preserves current preferences. Applying preferences preserves the routed page instance.
- Monica's SVG mark adapts to each theme. Preview marks use their own theme identity without changing the surrounding dialog.
- All built-in themes share a restrained elevation scale: menus use compact 12px blur shadows, and high elevation is bounded to 28px blur. Theme colors remain palette-owned.
- Material Design 3 no longer forces generic controls into large pills. Appearance owns its geometry: theme cards use 12px corners, layout choices 10px, and mode buttons 8px. Preview panels fill their card width consistently.
- Shared theme button defaults allow the shell's measured trigger geometry and typography to take precedence.

## Shell and navigation

- Desktop Sidebar consists of a 64px category rail and a 226px contextual pane. Selecting a category while collapsed opens a full-height, temporary 226px context beside the rail; the page retains its collapsed-layout width. Opening this panel does not persist an expanded sidebar.
- Expanded and temporary contextual navigation use the same authorized hierarchy, including nested disclosure and quiet-dot leaf destinations. The rail remains available for category switching. Escape, clickaway, and focus leaving the rail and context dismiss the temporary panel; route navigation preserves destination focus. Crossing the mobile breakpoint clears temporary desktop state, and returning to desktop does not reopen it.
- Built-in category and shell action icons use local lightweight SVG fragments supported by MudIcon. Category rail icons render at 20px with 1.5-unit rounded strokes in the 24-unit view box and inherit theme colors. Custom registered category and host action icons remain supported.
- Sidebar typography now uses a 22px/550 contextual identity, separate 11px metadata lines, a quiet identity divider, and 17px/500 category headings. Destination labels use 13px/400, with 500 for the active leaf; 42px quiet-dot rows have 4px gaps. Local Plex interface text remains intact, and the refinement does not replace Default's global Precision typography.
- Topbar uses one 56px row, with category icons, a brand divider, and categories that overflow into More. Category menus follow the prototype's larger heading, description, and simple destination rows. Nested registered groups remain expandable.
- Visible trigger dimensions, icons, typography, and gaps match the hidden width measurements. Narrow desktop navigation remains clear of shell actions.
- Both layouts use a temporary global navigation drawer at widths up to 929px. Dismissal restores trigger focus; navigation permits the destination heading to receive focus.
- Navigation checks page access before constructing the tree, prunes empty branches, and selects the longest matching destination. Module diagnostics retains its five existing nested workbench destinations.
- Host name, application ID/version, localization, optional actions, and operational page policies remain integrated.
- Shell preferences persist separately from theme preferences. Host defaults apply when the browser has no saved preference; desktop navigation still defaults to Sidebar.

## AI Chat containment

The conversation history drawer now belongs to the chat workbench, using a page-relative MudDrawerContainer and a non-fixed responsive drawer. It starts at the shell body's origin instead of overlapping the global sidebar. The mobile overlay covers the workbench while leaving the global shell header usable. Composer controls remain within the available conversation width.

## Verification

- Release solution build passed with zero warnings and zero errors.
- The standard test runner passed all 24 selected projects. Focused pure shell/catalog/state tests passed 44 cases; pure theme typography/display tests passed 7 cases. No UI rendering tests were run.
- AI Chat passed 18 combinations: expanded Sidebar, collapsed Sidebar, and Topbar at 1440px, 1024px, and 390px, with history open and closed. No overlap, document overflow, clipped composer controls, or fresh browser errors/warnings were observed.
- Topbar passed 15 focused checks, including 1796px through 390px widths, nested More navigation, keyboard dismissal/focus return, Default/Grove separation, and Material Design 3 geometry.
- Fresh and Fluent passed 20 additional Topbar combinations across light/dark modes and 1440px, 1280px, 1100px, 1024px, and 930px widths. Trigger widths, padding, weight, and letter spacing match their measurements; all remain inside the navigation area.
- All 12 themes passed in light and dark modes: 24 live combinations with adaptive icons, bounded shadows, a 56px header, and no horizontal overflow or fresh browser errors/warnings.
- Appearance verification passed Default, Material Design 3, and Grove in both modes: six cases with all 12 choices, correct isolated previews, zero preview/label width difference, visible actions, and no dialog overflow.
- The Sidebar refinement passed 20 visual combinations: Default and Grove, light and dark, expanded and collapsed at 1440px and 930px, plus four mobile cases at 390px. Measurements confirmed contextual typography, separate metadata lines, quiet-dot row geometry, palette-inherited SVG strokes, reachable rail controls, and zero document overflow or fresh browser errors.
- Temporary contextual navigation passed 11 live checks: full-height geometry, quiet destinations, Escape and outside dismissal, category switching and nested routes, unchanged persisted collapse, current-route dismissal, native Tab exit, mobile/desktop cleanup, and short-screen reachability. No fresh browser errors or warnings were observed. Opening focus waits for actual browser visibility; dismissals restore the category control, while a changed route retains destination focus.
- Final Topbar icon checks passed at 1440px and 930px: all shell SVG strokes inherit theme colors, trigger widths exactly match their hidden measurements, menus and nested More navigation remain usable, and the single 56px row has no overflow or fresh browser errors.
- The final chat capture confirms a workbench origin of x290/y56 and a 264px local history drawer at the same origin in expanded Sidebar mode. A final Topbar capture confirms a 56px row with zero horizontal overflow.
- Strict localization, MudBlazor CSS variables, scoped Monica.UI/Monica.AI.UI semantic tokens, canonical skill validation/projections/tests, knowledge impact, and `git diff --check` passed.
- The repository-wide semantic token validator reports 59 existing raw-color issues in the unchanged `Monica.Guide.App/Components/App.razor`. The UI projects changed here have zero issues.

The earlier implementation also passed responsive shell and loaded Configuration checks; the current checks above cover the requested corrections. Detailed logs, browser measurements, and bridge ownership records are retained under `.tmp/monica-ui-bridge-debug/grove-implementation/`, with the chat matrix under `.tmp/monica-ui-bridge-debug/grove-chat-containment/`.

## Current captures

- [Temporary full-height Grove sidebar](evidence/sidebar-temporary-grove.png)
- [Refined Grove Sidebar, light](evidence/sidebar-grove-refined.png)
- [Refined Grove Sidebar, dark](evidence/sidebar-grove-dark-refined.png)
- [Refined Sidebar with original Default colors](evidence/sidebar-default-refined.png)
- [Grove Topbar and category menu](evidence/grove-topbar-final.png)
- [Original Default colors](evidence/default-topbar-after.png)
- [AI Chat beside the global Sidebar](evidence/chat-sidebar-corrected.png)
- [Material Design 3 Appearance, light](evidence/material-correction-after-light.png)
- [Material Design 3 Appearance, dark](evidence/material-correction-after-dark.png)
- [Independent Grove theme choice](evidence/appearance-correction-grove-light-more.png)
- [All 12 light themes](evidence/correction-theme-overview-light.png)
- [All 12 dark themes](evidence/correction-theme-overview-dark.png)

The local preview host is left running for inspection. Canonical UI and AI guidance reflects the new contracts and is regenerated into both agent projections.
