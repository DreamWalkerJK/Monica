#!/usr/bin/env python3
"""Check that a local MudBlazor v9 source checkout is available through MUDBLAZOR_SOURCE_PATH."""

from __future__ import annotations

import argparse
import json
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from mudblazor_skill_state import MUDBLAZOR_SOURCE_PATH_ENV, mudblazor_source_path

MUDBLAZOR_VERSION = "9.0.0"
MUDBLAZOR_REPOSITORY = "https://github.com/MudBlazor/MudBlazor"
REQUIRED_RELATIVE_FILE = Path(
    "src/MudBlazor/Components/ThemeProvider/MudThemeProvider.razor.cs"
)
MAX_DIAGNOSTIC_TEXT_LENGTH = 2_000


@dataclass(frozen=True)
class MudBlazorResolution:
    """Validated result of one local MudBlazor source lookup."""

    source_root: Path | None
    marker_path: Path | None
    error_code: str | None
    error_message: str | None

    @property
    def is_available(self) -> bool:
        return self.error_code is None and self.source_root is not None and self.marker_path is not None

    def to_dict(self) -> dict[str, Any]:
        return {
            "ok": self.is_available,
            "version": MUDBLAZOR_VERSION,
            "environment_variable": MUDBLAZOR_SOURCE_PATH_ENV,
            "source_path": str(self.source_root) if self.source_root else None,
            "marker_relative_path": REQUIRED_RELATIVE_FILE.as_posix(),
            "marker_path": str(self.marker_path) if self.marker_path else None,
            "error_code": self.error_code,
            "error_message": self.error_message,
            "setup_hint": setup_hint(),
        }


def setup_hint() -> str:
    return (
        f"git clone {MUDBLAZOR_REPOSITORY} <mudblazor-source-root> && "
        f"git -C <mudblazor-source-root> checkout v{MUDBLAZOR_VERSION}, "
        f"then set {MUDBLAZOR_SOURCE_PATH_ENV} to <mudblazor-source-root>."
    )


def _result(
    *,
    source_root: Path | None = None,
    marker_path: Path | None = None,
    error_code: str | None = None,
    error_message: str | None = None,
) -> MudBlazorResolution:
    return MudBlazorResolution(
        source_root=source_root,
        marker_path=marker_path,
        error_code=error_code,
        error_message=error_message,
    )


def resolve_mudblazor_source() -> MudBlazorResolution:
    """Resolve the configured MudBlazor checkout and verify its source marker."""

    configured = mudblazor_source_path()
    if configured is None:
        return _result(
            error_code="mudblazor_source_path_unset",
            error_message=(
                f"The {MUDBLAZOR_SOURCE_PATH_ENV} environment variable is not set to a "
                f"local MudBlazor v{MUDBLAZOR_VERSION} checkout."
            ),
        )

    try:
        source_root = configured.resolve(strict=True)
    except OSError as exc:
        return _result(
            error_code="mudblazor_source_path_unavailable",
            error_message=f"The configured MudBlazor source path is unavailable: {configured} ({exc})",
        )

    if not source_root.is_dir():
        return _result(
            source_root=source_root,
            error_code="mudblazor_source_path_not_directory",
            error_message=f"The configured MudBlazor source path is not a directory: {source_root}",
        )

    marker_path = source_root / REQUIRED_RELATIVE_FILE
    if not marker_path.is_file():
        return _result(
            source_root=source_root,
            error_code="mudblazor_marker_missing",
            error_message=f"The configured tree does not contain the required marker: {marker_path}",
        )

    return _result(source_root=source_root, marker_path=marker_path)


def print_failure_details(resolution: MudBlazorResolution) -> None:
    if resolution.error_code:
        print(f"Error code:    {resolution.error_code}")
    if resolution.error_message:
        print(f"Reason:        {resolution.error_message}")
    if resolution.source_root:
        print(f"Configured:    {resolution.source_root}")

    print()
    print("Source-dependent work must stop here.")
    print("Provide a local MudBlazor v" + MUDBLAZOR_VERSION + " checkout, then rerun this check.")
    print("Setup hint:")
    print(f"  {setup_hint()}")


def main() -> int:
    parser = argparse.ArgumentParser(
        description=(
            f"Check if local MudBlazor v{MUDBLAZOR_VERSION} source is available "
            f"through {MUDBLAZOR_SOURCE_PATH_ENV}."
        )
    )
    parser.add_argument("--json", action="store_true", help="Output machine-readable JSON.")
    args = parser.parse_args()

    resolution = resolve_mudblazor_source()
    if args.json:
        print(json.dumps(resolution.to_dict(), indent=2))
    elif resolution.is_available:
        print(f"[OK] MudBlazor v{MUDBLAZOR_VERSION} source is available.")
        print(f"Source root:    {resolution.source_root}")
        print(f"Marker file:    {REQUIRED_RELATIVE_FILE.as_posix()}")
        print(
            f"Verify the checkout stays on v{MUDBLAZOR_VERSION} "
            "(git -C <source-root> describe --tags) when exact-parity work is planned."
        )
    else:
        print(
            f"[ERROR] MudBlazor source is not available through {MUDBLAZOR_SOURCE_PATH_ENV}."
        )
        print_failure_details(resolution)

    return 0 if resolution.is_available else 1


if __name__ == "__main__":
    sys.exit(main())
