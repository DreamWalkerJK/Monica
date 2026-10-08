# Grove B — Monica, with the real Flight host

Direction **B / Grove** is selected. This is the next design prototype, grounded in the running FIPS2022 Flight host and current Monica UI contracts.

Preview: `http://127.0.0.1:63140/.ui-design/grove-real-shell/index.html`.

Run the preview from the repository root with `python -m http.server 63140 --bind 127.0.0.1`. Local fonts are served from `Monica.UI/wwwroot/fonts`.

## Design scope

The stone canvas, forest identity surface, fine separators, local Plex interface type, and Playfair display type establish Grove as a default language. Chinese service names use the local Noto Serif SC fallback. Operational tables use compact interface text and measured spacing. Display typography is reserved for page identity; numbers and evidence remain readable.

The page uses the entire content width. The workspace owns vertical scrolling, with a sticky 56px header. Long tables and topology diagrams own their horizontal overflow. Dossier indexes and configuration definitions remain adjacent to their content at desktop widths and become bounded local navigation on narrow screens.

Both shell modes share the captured host's category and page ordering. The expanded sidebar has a 64px category rail and 226px contextual navigation. Collapsing retains the rail; choosing a category opens its pages in a temporary flyout. Mobile uses one drawer. The Topbar has **one 56px row** containing host identity, categories, and tools. Overflow moves complete category groups to More. Nested workbench sections expand inside the same menu.

Sidebar / Topbar and Light / Dark are preferences inside **Appearance**. Apply persists them and preserves the current route, filters, and configuration drafts. Browsing a category does not navigate until a destination is selected.

## Actual information architecture

The host's nine categories contain 40 registered destinations: AI, Knowledge & Retrieval, Documentation, Configuration, Infrastructure, Module, Task Scheduling, Monitor, and Debug. Labels, ordering, and routes were collected from the running shell, including Debug inside More. Projecting the five existing workbench sections creates 44 leaf links in this design.

The current registry contributes category → page entries. The prototype proposes projecting Module workbench's existing Overview, Performance, Modules, Dependencies, and Discovery routes into one expandable menu branch. This is a shell design proposal; it does not claim the registry already exposes hierarchical menu metadata.

The fully redesigned screen families are:

- **System dossier:** host-owned application name, AppId and version; readiness and tracked application startup; process uptime, working set, process ID, capture time; seven configured operational links; runtime, environment, bound endpoints, and executable artifact evidence.
- **Module workbench:** all five actual sections; a captured inventory of 94 active modules, 31 UI modules, 31 Web modules, and maximum dependency depth 5; real direct relationships and callback/startup costs; searchable catalog, details, dependency graph, local JSON export and baseline comparison.
- **Configuration State:** all 57 actual catalog definitions, grouped by publishing project; captured TowerDepArrOptions and DuplicateCheckOptions values; definition metadata, Values / JSON / Metadata views, local staged edits, validation, review, undo, import/export, and browser-local draft saving.

The remaining real destinations remain reachable and link to their current host pages. Configuration History, Versions, Debug, and Storage preserve their route and relevant layout structure while identifying records that were not included in this capture. No sample audit entries, health scores, trends, or invented configuration values are supplied.

## Evidence and interaction boundaries

Source service: `http://localhost:5294`, project `D:/Repositories/WorkTree2/FIPS2022/src/Services/Flight/FlightService.API`. The existing bridge helper started the host; no dedicated bridge project or production source change was needed. The source shell was switched to English through its Language control. Host-owned Chinese names remain unchanged.

The sanitized module export is [module-capture.json](evidence/module-capture.json). Original rendered screens are saved in `evidence/`. `data.js` is a normalized capture for this standalone prototype. System, module, and configuration observations were taken at separate instants and carry their respective capture times. Measurements remain frozen for visual comparison. A Refresh dialog explains how to inspect fresh data in the running host.

Module exports include exact direct edges, timing spans, discovery counters, and findings. Capability flags and full type names were captured from all 94 rows in the live Module catalog. The Overview screen explicitly recorded zero causal intervals, so its critical-path ribbon uses that captured empty state. Other missing evidence is marked Not recorded.

Configuration fields were captured only from the two nonsensitive Flight definitions. The remaining entries contain catalog metadata. Source chains and audit histories were not copied. Edits, import, validation, and Save preview draft are local interactions; they do not call the configuration store or reload the host. Restart is an explanatory prototype dialog and sends no request.

The header preserves the actual host identity, version, Appearance access, language access, and Mo User identity. The host supplies no search, notification, or account callbacks, so those controls are not added.

## Implementation handoff

This artifact is an HTML/CSS/JavaScript design study. The Monica Blazor shell and default theme have not been changed. After design acceptance, map the semantic roles to MudBlazor palette and typography roles, retain registry/localization/authorization ownership, and introduce hierarchy metadata only through an explicit navigation contract. Shell dimensions and placement belong to shell styles; reusable surface, field, table, status, and typography roles belong to the shared theme.

Verification is recorded separately in [verification.md](verification.md).
