# Render icons and supply control glyphs

Use `Monica.UI.Icons.MoIcon` for a standalone SVG and `MoIcons` for the shared catalog. Both ship in `Monica.UI`; there is no icon-specific module registration, provider, JavaScript, icon font, or runtime download. A host using the shell still follows the normal [shell setup](shell-and-access.md) for its application lifecycle and styles.

```razor
@using Monica.UI.Icons

<MoIcon Icon="@MoIcons.Search" />
<MoIcon Icon="@MoIcons.Copy" Size="16px" />

<MudButton StartIcon="@MoIcons.Save">@L["Actions:Save"]</MudButton>
<MudIconButton Icon="@MoIcons.Close" aria-label="@L["Actions:Close"]" />
```

`L` in these examples is the component's module-owned localizer, with the shown action keys present in its resources. An icon inside a labeled control is decorative; the button or link owns its accessible name, keyboard behavior, focus, disabled state, click handling, hit area, and tooltip.

| MoIcon parameter | Contract |
| --- | --- |
| `Icon` | Trusted SVG body fragment; null, empty, or whitespace produces no output. |
| `Size` | Optional CSS length such as `16px` or `1em`. Omitted sizing uses `--mo-icon-size`, with a `20px` fallback. |
| `Label` | Optional localized accessible name for a meaningful standalone image. Blank values mean decorative. |
| `Class`, `Style` | Presentation extensions on the root SVG. Color inherits through `currentColor`. |
| `AdditionalAttributes` | Unmatched SVG attributes, for example `data-*` hooks. Component-owned class, style, view box, fill, focusability, role, and label/hidden semantics take priority. |

MoIcon uses a fixed `0 0 24 24` view box and `focusable="false"`. By default it emits `aria-hidden="true"`. A nonblank `Label` instead emits `role="img"` and an encoded `aria-label`. Use `Label` only when the graphic independently conveys information; localize it through the owning module's resources. Adding a click handler to an SVG does not provide button semantics.

`MoIcons.Search` and the other catalog properties return SVG body strings without an outer `<svg>`. The same value works in a MudBlazor `StartIcon`, `EndIcon`, `Icon`, or adornment slot that accepts SVG strings. Navigation registrations also accept these values directly:

```csharp
using Monica.UI.Icons;

registry.RegisterCategory(
    "example.tools",
    "Tools",
    icon: MoIcons.Sliders);
```

This literal example belongs in an existing module's `ConfigureNavigation(...)` callback; use localized registrations for application-facing labels as described in [shell and access](shell-and-access.md). Icon selection does not register a route or change access policy.

Outline fragments carry their paint settings in a shared group: `currentColor`, rounded caps and joins, and an initial stroke width of 1.5 SVG units. A surrounding stylesheet can set `--mo-icon-stroke-width` for a denser context; the variable also applies when a Mud component renders the fragment. It scales with the SVG: a 1.5-unit line at 16px is approximately 1 CSS pixel. Use the owner's semantic theme colors for status or disabled states.

The public catalog contains selected Lucide assets and Monica-authored glyphs, including the Shell drawings. Applications do not need npm or access to Lucide's servers. Replacing an explicit icon does not replace the internal defaults of every Mud component; use the specific control's supported icon parameters when that scope is being migrated. `MonicaIcon` remains the separately owned theme-aware brand mark.

Only pass reviewed developer assets to `Icon`. The component renders the fragment as markup; it does not sanitize uploaded SVG or arbitrary service responses. Custom application fragments must follow the same 24-unit body contract, including self-contained paint and no scripts, external references, or event handlers. For catalog maintenance, source imports, and visual review, follow [MoIcon authoring](../../monica-ui-development/references/moicon-authoring.md).
