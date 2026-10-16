#!/usr/bin/env python3
"""Initialize or update MudBlazor CSS variable JSON from MudBlazor source."""

from __future__ import annotations

import argparse
import json
import re
import sys
from datetime import datetime, timezone
from pathlib import Path

from skill_repository import find_repository_root

REPO_ROOT = find_repository_root(__file__)
OUTPUT_JSON = REPO_ROOT / ".tmp/monica-ui-development/mudblazor-css-variables.json" if REPO_ROOT else None
SOURCE_RELATIVE_FILE = Path("src/MudBlazor/Components/ThemeProvider/MudThemeProvider.razor.cs")
CONST_PATTERN = re.compile(r'private const string\s+(\w+)\s*=\s*"([^"]+)";')
PLACEHOLDER_PATTERN = re.compile(r"\{([A-Za-z_][A-Za-z0-9_]*)\}")
VAR_TEMPLATE_PATTERN = re.compile(
    r"--(?:\{[A-Za-z_][A-Za-z0-9_]*\}|[A-Za-z0-9-])+(?=\s*:)"
)
VAR_PATTERN = re.compile(r"^--mud-[a-z0-9-]+$")


def _strip_comment_only_lines(content: str) -> str:
    return "\n".join(
        line for line in content.splitlines() if not line.lstrip().startswith("//")
    )


def _replace_placeholders(template: str, constants: dict[str, str]) -> str:
    def replace(match: re.Match[str]) -> str:
        key = match.group(1)
        return constants.get(key, match.group(0))

    return PLACEHOLDER_PATTERN.sub(replace, template)


def extract_mud_variables(source_text: str) -> list[str]:
    cleaned = _strip_comment_only_lines(source_text)
    constants = dict(CONST_PATTERN.findall(cleaned))
    found: set[str] = set()

    for template in VAR_TEMPLATE_PATTERN.findall(cleaned):
        resolved = _replace_placeholders(template, constants).lower()
        match = VAR_PATTERN.match(resolved)
        if match:
            found.add(match.group(0))

    return sorted(found)


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Initialize or update MudBlazor CSS variable JSON from MudBlazor source."
    )
    parser.add_argument(
        "--source-root", type=Path, required=True,
        help="Read-only MudBlazor source root matching the resolved package version.",
    )
    parser.add_argument("--force", action="store_true", help="Rewrite JSON even when unchanged.")
    parser.add_argument(
        "--output-json", type=Path, default=OUTPUT_JSON,
        help="Catalog destination; defaults to the Monica repository cache when available.",
    )
    args = parser.parse_args()
    if args.output_json is None:
        parser.error("No Monica repository default; pass --output-json <catalog-path>.")
    output_json = args.output_json.expanduser().resolve()

    resolved_root = args.source_root.expanduser().resolve()
    source_file = resolved_root / SOURCE_RELATIVE_FILE
    try:
        source_text = source_file.read_text(encoding="utf-8")
    except OSError as exc:
        print(f"[ERROR] Cannot read MudBlazor theme source: {source_file} ({exc})", file=sys.stderr)
        return 1

    variables = extract_mud_variables(source_text)
    if not variables:
        print("[ERROR] No MudBlazor CSS variables were extracted from source.")
        return 1

    generated_at_utc = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    payload = {
        "source_root": str(resolved_root),
        "source_file": str(source_file),
        "generated_at_utc": generated_at_utc,
        "variable_count": len(variables),
        "variables": variables,
    }
    json_content = json.dumps(payload, indent=2) + "\n"

    mode = "initialized" if not output_json.exists() else "updated"
    output_json.parent.mkdir(parents=True, exist_ok=True)
    if args.force:
        output_json.write_text(json_content, encoding="utf-8")
    else:
        current_json = output_json.read_text(encoding="utf-8") if output_json.exists() else None
        if current_json != json_content:
            output_json.write_text(json_content, encoding="utf-8")

    print(f"[OK] MudBlazor CSS variable JSON {mode}.")
    print(f"Source root: {resolved_root}")
    print(f"Variable count: {len(variables)}")
    print(f"JSON: {output_json}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
