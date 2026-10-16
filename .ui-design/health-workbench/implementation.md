# Health workbench page pilot

Implemented at [the live health page](http://localhost:5294/health-checks?revision=health-workbench-v1) through the FlightService bridge host. This is a focused page redesign to evaluate the visual direction before applying it to other Monica pages.

The page uses one prominent readiness statement beside the checks that need attention. Small inline counts replace four competing KPI cards. The check registry combines search, status tabs, and tags; names, descriptions, and registration tags form a readable hierarchy within each row. Thin dividers, restrained severity accents, and surfaces without shadows keep the operational evidence clear. The details drawer follows the same typography and spacing.

Default retains its palette. Grove and the other themes use their existing palette and typography roles. Tablets retain a four-column table; phones show stacked entries with real cell spacing. No external fonts or image assets were added.

Readiness still considers only ready-tagged registrations. All-check counts and attention evidence remain independent of the visible filters. Missing readiness coverage is stated explicitly. Automatic and manual refresh, authorization, sanitized evidence, stale-data warnings, and detail selection reconciliation are preserved. Details contain keyboard focus and restore it when closed with Escape, the close button, or the backdrop. The page introduces no stored history, availability percentage, or simulated data.

## Previews

- [Default light](captures/default-light.png)
- [Grove light](captures/grove-light.png)
- [Default dark](captures/default-dark.png)
- [Grove dark](captures/grove-dark.png)
- [Phone registry](captures/mobile-registry.png)
- [Previous page](captures/before.png)

## Verification

- Release solution build: zero warnings and errors.
- Standard test runner: all 24 projects passed.
- Browser behavior: 18 checks passed, including combined filters, empty-state clearing, details, keyboard focus, refresh, polling, and navigation away and back.
- Visual matrix: 36 cases passed across 12 themes in light and dark, plus phone, tablet, and expanded Sidebar layouts. No horizontal overflow or fresh browser errors. Four additional mobile registry captures verify spacing after scrolling.
- Status-filter scrollbar follow-up: removed the negative tab margin that extended children one pixel below their scroll container. Eighteen focused checks passed in Default, Grove, and Material Design 3, in English and Chinese, at phone, tablet, and desktop widths. The filter group has no vertical overflow, and status selection remains usable.
- Strict localization, MudBlazor variables, changed UI theme tokens, canonical skill validators, skill projections, skill tests, and knowledge impact checks passed.

The real bridge snapshot includes an unhealthy Dapr sidecar. The page reports that host evidence rather than masking it for the preview.

Replay scripts, computed metrics, raw reports, and service logs are in `.tmp/monica-ui-bridge-debug/health-redesign/`.
