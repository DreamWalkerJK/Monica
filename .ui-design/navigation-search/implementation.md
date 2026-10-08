# Registered navigation search and navigation refinements

The shell now opens a command palette from its search control or Ctrl/Cmd+K. It searches the current host's authorized navigation pages by localized title, description, category ancestry, and route. The FIPS flight-service bridge exposes 44 destinations in the verified Development session; other hosts and users receive their own registered, authorized set.

Every whitespace-separated query token must match. Exact and prefix title matches rank first, with culture-aware case and accent matching. Routes match with or without a leading slash. Page summaries take precedence over category summaries, and nested results retain their category path. Hidden navigation pages and parameterized route templates are excluded. Authentication changes invalidate the displayed snapshot, and opening a result evaluates access again.

The input retains focus while arrows select and scroll results; Enter opens the selected destination. Escape and outside dismissal close the palette and return focus to its invoking context. IME composition stays native. Repeated shortcuts share one opening operation, and the shortcut leaves another open dialog undisturbed. The shell owns its observer, callback references, modal result task, and cancellation lifetime. `EnableNavBarSearch` defaults to true; setting it to false removes the control and shortcut. A configured `NavBarSearchAction` replaces the built-in palette for both entry points.

Topbar categories and More open on hover with a fine mouse pointer, without moving focus. Moving across the trigger-to-popup gap retains the menu; changing categories closes its neighbor. Click, touch, ArrowDown, Escape, native Tab, and nested disclosures remain available. All ten built-in categories have meaningful English and Chinese descriptions; nine categories are present in this bridge. Module authors can contribute page and category descriptions through the same resource marker as their labels.

The collapsed desktop rail displays its temporary context panel immediately at x64, without slide, transform, or animation. It preserves the page width and the stored collapsed preference. The mobile drawer retains its responsive behavior.

## Captures

| Surface | Capture |
| --- | --- |
| Grove light palette | [Desktop](captures/search-grove-light.png) |
| Grove dark palette | [Desktop](captures/search-grove-dark.png), [mobile](captures/search-grove-dark-mobile.png) |
| Default palette | [Light](captures/search-default-light.png), [dark](captures/search-default-dark.png) |
| Material Design 3 palette | [Light](captures/search-material-design-3-light.png), [dark mobile](captures/search-material-design-3-dark-mobile.png) |
| Chinese query | [Dependency Injection](captures/search-chinese-query.png) |
| Hover category overview | [English](captures/hover-ai-english.png), [Chinese](captures/hover-ai-chinese.png) |
| Nested More menu | [Chinese](captures/hover-more-nested-chinese.png) |
| Instant contextual sidebar | [Collapsed rail](captures/context-sidebar-instant.png) |

## Verification

- `dotnet build Monica.slnx -m -c Release`: zero warnings and errors.
- Standard repository runner: all 24 non-UI test projects passed.
- Focused pure UI support, state, and theme tests: 98 passed. No UI rendering tests were run.
- Search browser interactions: 20 passed, including typing, title relevance, route queries, empty results, IME, arrows, Enter, Escape, backdrop, Ctrl/Cmd+K, repeated opening, and Chinese localization. Zero new browser warnings or errors.
- Search visual matrix: 12 cases passed across Default, Grove, and Material Design 3, light/dark, at 1440×1000 and 390×844. Input, close control, footer, and final result remain reachable; no viewport overflow. Long results scroll inside the palette.
- Navigation browser checks: 16 passed, including hover focus, gap travel, sibling switching, touch fallback, keyboard controls, nested More, responsive dismissal, and instant sidebar geometry. Zero browser warnings or errors.
- Strict localization: zero integrity, missing-key, unused-key, navigation-key, or language-sync issues. MudBlazor CSS variables, changed-source semantic tokens, canonical skill validation/projections, and all four skill-owned test suites passed.

The FIPS bridge was used without consumer source changes and remains available at `http://localhost:5294/job-scheduler/statistics`. Machine-specific logs and raw browser reports remain under `.tmp/monica-ui-bridge-debug/grove-implementation/`. The Monica solution gate has zero warnings; the consumer bridge retains its existing consumer compilation and external-infrastructure diagnostics. The full theme-token scan also retains 59 pre-existing literals in the unchanged Monica.Guide.App; changed UI sources have zero token issues.

## Knowledge impact

The canonical shell-and-access reference documents description metadata, search defaults and overrides, authorization boundaries, matching, keyboard controls, hover behavior, and instant contextual navigation. The localization guide and scanner now recognize description keys in navigation registrations, with focused behavioral fixtures. Generated skill projections are synchronized.

Observability module changes only publish localized navigation descriptions. Logging and runtime metrics setup, authorization, telemetry, and data behavior are unchanged; their observability guidance has no additional impact.
