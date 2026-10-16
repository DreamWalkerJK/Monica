# Author MoIcon assets and maintain the catalog

`Monica.UI/Icons/` owns the native `MoIcon` renderer, the generated `MoIcons` catalog, and reviewed SVG originals. Keep the public API in `Monica.UI.Icons` inside the existing `Monica.UI` project. Read the [consumer contract](../../monica-infra-ui/references/icons.md) for parameters, accessibility, control integration, and trusted-input requirements; those contracts also apply to framework-authored components.

## Source and generated ownership

| Repository path | Responsibility |
| --- | --- |
| `Monica.UI/Icons/MoIcon.razor` and `.razor.css` | Native SVG markup, parameters, and root presentation. |
| `Monica.UI/Icons/MoIcons.g.cs` | Checked-in generated public static string properties; never edit manually. |
| `Monica.UI/Icons/Assets/catalog.json` | Explicit public Monica names mapped to reviewed SVG sources. |
| `Monica.UI/Icons/Assets/lucide.lock.json` | Pinned package version, source, and verified integrity. |
| `Monica.UI/Icons/Assets/Lucide/icons/` and `LICENSE` | Full upstream SVG snapshot and original license notices. |
| `Monica.UI/Icons/Assets/Monica/` | Monica-authored SVG originals, including the Shell drawings. |
| `scripts/mo_icons.py` | Explicit import, offline generation, freshness checking, and gallery generation. |
| `.ui-design/moicon/index.html` | Generated local visual review page; not an application route. |

Keep source assets outside `wwwroot`. The published library carries the compiled catalog and component styles, with the upstream notices under `licenses/lucide/`; original SVGs, catalog/lock inputs, tooling, and gallery are not runtime package resources. Consumers require no npm installation, icon service, DI registration, or runtime asset request. The renderer uses native Blazor/SVG; this does not remove the other components' MudBlazor dependency from `Monica.UI`.

## Add or update a glyph

Run these commands from the Monica repository root; they are repository tools, not scripts installed with this skill:

```text
python scripts/mo_icons.py generate
python scripts/mo_icons.py check
python scripts/mo_icons.py gallery
```

1. Reuse a suitable checked-in Lucide source or add a reviewed Monica SVG original. Preserve imported originals instead of rewriting their geometry or paint in place.
2. Add the intended public name and source to `Assets/catalog.json`. Names describe the Monica-facing concept; upstream aliases do not automatically become public API. Avoid duplicate exports or semantic synonyms without an actual consumer need.
3. Generate `MoIcons.g.cs`, inspect the diff, and run `check`. Commit the source/catalog change and generated output together. Ordinary .NET builds compile the checked-in file and do not invoke an import.
4. Generate the gallery and inspect the affected glyphs at 16, 18, 20, and 24px in light and dark modes. Inspect 14px chevrons where used, actual control alignment, optical centering, and recognizable detail. Use real host browser smoke checks for migrated Blazor and Mud controls; the standalone gallery does not establish those integration behaviors.

To update the upstream snapshot, select a fixed official release explicitly:

```text
python scripts/mo_icons.py import-lucide --version <version>
python scripts/mo_icons.py generate
python scripts/mo_icons.py check
python scripts/mo_icons.py gallery
```

Only `import-lucide` accesses the network. It verifies package integrity before recording the local snapshot and lock information. Review upstream geometry, source removals, selected exports, and original license notices together; retain both ISC and relevant Feather-derived MIT notices from the imported license. Generation and checks use local inputs only.

## SVG and renderer contract

The generator accepts reviewed 24-unit SVG originals and emits body fragments with no outer `<svg>`. Generated public properties return literal strings, avoiding public `const` values copied into consumer assemblies. Do not add a runtime dictionary, provider, string-name resolver, or per-glyph request to render catalog values.

Keep monochrome paint self-contained in the generated wrapping `<g>`: no fill, `currentColor` stroke, rounded joins/caps, and `style="stroke-width:var(--mo-icon-stroke-width,1.5)"`. Lucide geometry is adapted to this shared paint contract during generation; do not claim its upstream 2-unit baseline is identical to Monica's initial 1.5-unit baseline. At small display sizes, simplify or choose clearer geometry when a thinner outline loses meaning.

Raw `MarkupString` descendants do not acquire Blazor isolation attributes. Put root size, baseline alignment, and display in `MoIcon.razor.css`, and put stroke/fill defaults on the generated group so the same fragment paints correctly inside `MoIcon` and Mud controls. When replacing a `MudIcon`, inspect existing `.mud-icon-root` selectors and update their owning styles to reach the actual rendered root. A child `MoIcon` root still needs a parent HTML wrapper and `::deep` when styled from the parent's isolated CSS.

Reject unsupported elements/attributes, unexpected coordinate systems, IDs, external references, executable content, embedded HTML, and event handlers before emitting markup. Generation validates trusted source assets; it is not an upload sanitizer. Keep label encoding and the fixed view box, focusability, and accessibility semantics owned by the component even when callers supply additional attributes.

Shell and module icons use this shared catalog; do not recreate `ShellIcons`, duplicate glyph markup in a component, or introduce new Material catalog references in Monica-owned production code. `Shared/Components/Branding/MonicaIcon.razor` remains a distinct theme-aware brand drawing with its own view box.

## Migrate a component or module

Use native `MoIcon` for a Monica-owned standalone glyph. Keep existing interactive controls such as `MudIconButton`, `MudButton`, `MudMenu`, and input adornments, supplying `MoIcons` fragments to their public icon parameters. This retains the control's keyboard handling, accessible name, disabled state, and hit area. The [consumer contract](../../monica-infra-ui/references/icons.md#configure-mud-control-slots) lists supported slots and the exact upstream limitations.

Choose the catalog entry by meaning. Existing Material names and their filled/outlined variants are migration clues, not the new API vocabulary: copy uses `Copy`, downward expansion uses `ChevronDown`, and completed status uses `Success`. Preserve meaningful distinctions such as checked, unchecked, mixed, play, pause, stop, success, and error. State labels and semantic theme colors still belong to their existing owners.

When replacing direct `MudIcon` markup:

1. Preserve conditions, loops, localized labels, tooltip ownership, classes, and data hooks. Convert `Color` to inherited owner color or an existing semantic palette variable; `MoIcon` has no MudBlazor `Color` parameter.
2. Preserve the actual CSS size and alignment. `MoIcon` uses `1em` dimensions and a CSS font size controlled by `--mo-icon-size`; existing owner classes can still set font size. Use `Size` for an explicit length. Do not let a generic replacement size override a deliberately larger hero or smaller status glyph.
3. Update affected `.mud-icon-root` or size-class selectors to `.mo-icon` at the owner. Keep `::deep` where parent CSS isolation must reach a child component root. Keep selectors for Mud-rendered interactive icon slots when those roots still exist.
4. Cover explicit icon values in C#, navigation registrations, presentation helpers, and control-specific parameters as well as Razor tags. Check empty/loading/error states, dynamic severity, and selected/unselected branches.
5. Run browser smoke checks across representative module pages, both theme modes, compact and expanded navigation, menus, clear buttons, selection states, and inherited disabled color.

Treat coverage of Monica-owned icon sources and coverage of private MudBlazor markup as separate evidence. MudBlazor 9.11 contains private, hardcoded glyphs that its enclosing components do not expose through public icon parameters. Keep those limitations explicit; a source scan with no Material references in Monica does not prove every rendered vendor glyph has changed. Do not fork the dependency, use reflection, or patch rendered SVG paths to make a broader coverage claim.

Use the focused generator checks in `scripts/test_mo_icons.py` for deterministic output, invalid input, duplicate names, stale generation, and source-contract behavior. Follow the normal repository gate and skill validators for implementation/guidance changes. UI rendering tests require an explicit user request; use browser smoke checks for this component and its actual consumers.
