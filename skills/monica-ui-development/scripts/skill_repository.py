#!/usr/bin/env python3
"""Discover Monica source roots without assigning a portable command's owner."""

from __future__ import annotations

from pathlib import Path


def find_repository_root(script_path: str | Path) -> Path | None:
    """Return the nearest Monica.slnx ancestor, or None for an installed skill.

    Discovery performs no writes and does not inspect the caller's working
    directory. An absent repository is valid for portable commands; consumers
    must choose their output owner separately before creating directories.
    """

    for ancestor in Path(script_path).resolve().parents:
        if (ancestor / "Monica.slnx").is_file():
            return ancestor
    return None
