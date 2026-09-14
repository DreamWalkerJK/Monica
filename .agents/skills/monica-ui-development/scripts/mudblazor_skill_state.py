#!/usr/bin/env python3
"""Shared state and the local MudBlazor source path contract."""

from __future__ import annotations

import os
from pathlib import Path

SKILL_ROOT = Path(__file__).resolve().parents[1]
PROJECT_ROOT = SKILL_ROOT.parents[2]
STATE_DIR = PROJECT_ROOT / ".tmp" / "monica-ui-development"
VARIABLES_JSON_FILE = STATE_DIR / "mudblazor-css-variables.json"
MUDBLAZOR_SOURCE_PATH_ENV = "MUDBLAZOR_SOURCE_PATH"


def ensure_state_dir() -> Path:
    STATE_DIR.mkdir(parents=True, exist_ok=True)
    return STATE_DIR


def mudblazor_source_path() -> Path | None:
    """Return the configured local MudBlazor checkout root, or None when unset."""

    value = os.environ.get(MUDBLAZOR_SOURCE_PATH_ENV, "").strip()
    if not value:
        return None
    return Path(value).expanduser()
