# MudBlazor source and repository tooling

These scripts ship with the skill. Run the selected script using its actual installed
path; `scripts/` below means the scripts directory beside this reference's skill.

## Source lookup is independent of repository defaults

Set `MUDBLAZOR_SOURCE_PATH` to a local MudBlazor v9.0.0 checkout. Run
`python scripts/check_mudblazor_source.py --json` to verify the required source marker.
The source checker works in installed or copied skills outside a Monica checkout.
It does not create a cache or require `Monica.slnx`. An unset or unavailable source
produces a structured diagnostic and nonzero exit status; verify the checkout's
version when exact-source work is planned.

## Default paths belong to the containing Monica checkout

Repository discovery searches upward from the script for the nearest `Monica.slnx`
file. Canonical `skills/`, `.agents/skills/`, and `.claude/skills/` scripts therefore
share the same repository defaults. The caller's working directory does not choose
a repository default. Detached or globally installed skills have no implicit
repository cache, font target, or resource discovery root; importing the scripts
and requesting help still work.

The default CSS catalog is
`<repository>/.tmp/monica-ui-development/mudblazor-css-variables.json`.
Generate it from the configured source, then check style usage:

```text
python scripts/sync_mud_css_variables.py
python scripts/validate_mud_css_variables.py --root <style-root>
```

Outside a Monica checkout, or when a task owns another catalog destination, select
the same explicit file for generation and validation:

```text
python scripts/sync_mud_css_variables.py --output-json <task-cache>/mudblazor-css-variables.json
python scripts/validate_mud_css_variables.py --root <style-root> --variables-file <task-cache>/mudblazor-css-variables.json
```

Explicit file options take precedence over repository defaults. Relative explicit
file paths resolve from the caller's working directory. The generator creates only
the selected destination's parent directory, after source validation and successful
variable extraction. `MUDBLAZOR_SOURCE_PATH` selects the read-only input; it does
not own the output cache. If no repository default is available and the relevant
file option is omitted, command execution reports the required flag. The validator's
`--root` selects scanned styles; it does not silently relocate the catalog.

The validator's `--fix` performs deterministic safe substitutions. Without it,
validation reads the selected catalog and styles and writes no cache.

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
