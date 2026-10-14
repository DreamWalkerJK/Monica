# Grove real-host prototype verification

Verified on 5 October 2026, Asia/Shanghai, with separate Playwright Chromium sessions for the source host, prototype, configuration review, and navigation review.

## Source evidence

- Started the existing FIPS2022 FlightService.API with Monica's bridge helper at `http://localhost:5294`; its System dossier readiness probe passed.
- Inspected the source shell's category menus and Debug overflow after selecting English through the actual Language control: nine categories, 40 registered destinations.
- Exported the host's sanitized module diagnostics: revision 2363, 94 active modules, 185 direct edges, 787 trace spans. Captured all 94 live catalog rows to obtain full type names and capability flags: 31 UI and 31 Web modules.
- Captured System dossier identity, startup/readiness, runtime, environment, endpoint, artifact, and seven configured link values. Captured the actual Overview empty critical-path state.
- Captured the 57-definition configuration catalog and eight nonsensitive fields from TowerDepArrOptions and DuplicateCheckOptions. Uncaptured keys, values, source chains, and audit records remain unknown.
- Original screenshots and sanitized observations are in `evidence/`.

## Browser checks

- Desktop matrix: System dossier, Module catalog, and Configuration State × Sidebar / Topbar × Light / Dark at 1440×1000. All 12 cases rendered with document width 1440; Topbar measured exactly 56px and remained one row.
- Responsive matrix: the same three screens in both layouts at 1024, 768, and 390px widths. All 18 cases had no page-level horizontal overflow. Final mobile captures at 390×844 were repeated after the compact picker, header identity, and startup layout refinements.
- All 44 projected leaf routes rendered a nonempty heading and stayed within the viewport width. All five module sections rendered; the full dependency view contained 94 modules / 185 edges, and the selected Repository neighborhood contained ten modules / 13 edges.
- Sidebar measured 290px expanded and 64px collapsed. Category browsing preserved the active route; collapsed flyout and nested five-section Module menu navigated correctly.
- Topbar overflow retained complete categories and nested sections. At 1024px the active Module category was represented under More. ArrowDown opened its menu; Escape restored focus.
- Mobile drawer navigation and focus restoration passed at 390×844 and 390×400. Independent category scrolling kept Debug reachable at the shorter height.
- Module search reduced the table to ModuleShellUI; its detail dialog showed the recorded dependency and 28 dependents. Empty search, Reset, page 2 / 4, JSON export, and comparison against the original export passed. Identical baseline comparison returned zero duration delta and zero added / removed modules.
- Configuration string and array drafts survived layout and theme changes. Invalid JSON arrays set aria-invalid and prevented saving; corrected values could be reviewed and saved locally. The compact mobile picker exposed all 57 definitions and selected the correct fields.
- Undo all removed the saved browser draft; reload retained zero staged changes and the captured TAK value. Closing Review restored focus to the fresh Review button after the editor rerender.
- Metadata-only MatchOptions displayed Not recorded for its canonical key and linked to the plain source State route. No inferred key was passed to the running host.
- DuplicateCheck TimeSpan preview validation rejected `abc` and accepted `1.03:04:05`; the UI explicitly limits these checks to local draft formats and does not claim host validation.
- No host requests occurred during the configuration edit, review/save, undo, or Appearance flows in the configuration review session. Restart is represented locally and was never called on the source host.
- Final prototype console: zero errors and zero warnings. Local font and asset requests succeeded.

## Scope

`node --check` passed for data.js, navigation.js, modules.js, configuration.js, and app.js. Browser checks were used for this standalone design artifact. No production C# or Blazor files were changed, and no UI rendering tests were added or run.

The portable observations remain immutable. Refresh opens an explanation and the original host destination; it does not manufacture new measurements. Screen families outside the three detailed studies retain real routes and explicitly identify uncaptured content.
