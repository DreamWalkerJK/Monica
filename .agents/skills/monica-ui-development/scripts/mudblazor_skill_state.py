#!/usr/bin/env python3
"""Shared state and the local MudBlazor source path contract."""

from __future__ import annotations

import os
from pathlib import Path

from skill_repository import find_repository_root

SKILL_ROOT = Path(__file__).resolve().parents[1]
PROJECT_ROOT = find_repository_root(__file__)
STATE_DIR = PROJECT_ROOT / ".tmp" / "monica-ui-development" if PROJECT_ROOT else None
VARIABLES_JSON_FILE = STATE_DIR / "mudblazor-css-variables.json" if STATE_DIR else None
MUDBLAZOR_SOURCE_PATH_ENV = "MUDBLAZOR_SOURCE_PATH"


def ensure_state_dir() -> Path:
    """Create the repository cache, or require an explicit portable output."""

    if STATE_DIR is None:
        raise FileNotFoundError("No Monica repository cache; pass --output-json to the generator.")
    STATE_DIR.mkdir(parents=True, exist_ok=True)
    return STATE_DIR


def mudblazor_source_path() -> Path | None:
    """Return the configured local MudBlazor checkout root, or None when unset."""

    value = os.environ.get(MUDBLAZOR_SOURCE_PATH_ENV, "").strip()
    if not value:
        return None
    return Path(value).expanduser()
