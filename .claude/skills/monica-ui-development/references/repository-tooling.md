# MudBlazor source and repository tooling

These scripts ship with the skill. Run the selected script using its actual installed
path; `scripts/` below means the scripts directory beside this reference's skill.

## Inspect the resolved dependency source

Read the project's resolved MudBlazor package version from package metadata or
`obj/project.assets.json`. When `inspect-dependency-source` is available, follow
that skill to resolve the package and inspect its returned source path and commit
evidence. Otherwise use an ordinary local checkout verified against the exact
package version and published repository commit. A folder name or a source marker
alone does not establish its version. Keep dependency source read-only.

This skill owns Monica UI conventions and CSS validation; it does not manage a
second source catalog, require another skill to be installed, or configure a
machine-specific source environment variable.

## Default paths belong to the containing Monica checkout

Repository discovery searches upward from the script for the nearest `Monica.slnx`
file. Canonical `skills/`, `.agents/skills/`, and `.claude/skills/` scripts therefore
share the same repository defaults. The caller's working directory does not choose
a repository default. Detached or globally installed skills have no implicit
repository cache, font target, or resource discovery root; importing the scripts
and requesting help still work.

The default CSS catalog is
`<repository>/.tmp/monica-ui-development/mudblazor-css-variables.json`.
Generate it from the verified source path, then check style usage:

```text
python scripts/sync_mud_css_variables.py --source-root <exact-source-root>
python scripts/validate_mud_css_variables.py --root <style-root>
```

Outside a Monica checkout, or when a task owns another catalog destination, select
the same explicit file for generation and validation:

```text
python scripts/sync_mud_css_variables.py --source-root <exact-source-root> --output-json <task-cache>/mudblazor-css-variables.json
python scripts/validate_mud_css_variables.py --root <style-root> --variables-file <task-cache>/mudblazor-css-variables.json
```

Explicit file options take precedence over repository defaults. Relative explicit
file paths resolve from the caller's working directory. The generator creates only
the selected destination's parent directory, after source validation and successful
variable extraction. `--source-root` is required and selects the read-only input;
the generator extracts variables without establishing package provenance. If no
repository default is available and the relevant file option is omitted, command
execution reports the required flag. The validator's
`--root` selects scanned styles; it does not silently relocate the catalog.

The validator's `--fix` performs deterministic safe substitutions. Without it,
validation reads the selected catalog and styles and writes no cache.

The catalog covers variables emitted by `MudThemeProvider`; component-local
variables such as `--mud-drawer-width` have separate source owners. Review a
reported name against that component before treating it as invalid. Scan the
owning style root rather than archived browser evidence or bundled vendor CSS.

## Font subset defaults and explicit paths

Within a Monica checkout, `subset_ui_font.py` defaults to:

- Input: `.tmp/monica-ui-font-sources/zcool_qingke_huangyou.ttf`.
- Output: `Monica.UI/wwwroot/fonts/Komi-ZCOOL-QingKe-HuangYou.woff2`.
- Text: top-level `Monica*/Localization/**/*.json`, excluding `Monica.Testing`.

Explicit `--localization-dir` values replace repository resource discovery and can
be repeated. Extra text remains additive. Detached skills require explicit source
and target paths plus localization directories or an explicit text input:

```text
python scripts/subset_ui_font.py --source-font <source.ttf> --target-font <target.woff2> --localization-dir <resources>
python scripts/subset_ui_font.py --mode check --source-font <source.ttf> --target-font <target.woff2> --extra-text-file <required-text.txt>
```

Omitted implicit defaults produce actionable flag diagnostics only when executing
the command. Explicit targets outside a Monica checkout remain valid and are
reported by their absolute paths. See [offline asset requirements](offline-requirements.md#font-subsetting-workflow)
for glyph coverage and packaging constraints; keep source TTF/OTF files outside UI
runtime packages.
