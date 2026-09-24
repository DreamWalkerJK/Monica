#!/usr/bin/env python3
"""Require canonical skill knowledge to track catalog-owned infrastructure changes."""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path, PurePosixPath
from typing import Sequence


REPOSITORY_ROOT = Path(__file__).resolve().parents[1]
CATALOG_PATH = Path(".monica/agent-skill-catalog.json")


@dataclass(frozen=True)
class KnowledgeOwner:
    name: str
    skill_path: str
    source_paths: tuple[str, ...]


@dataclass(frozen=True)
class KnowledgeImpact:
    source_changes: tuple[str, ...]
    knowledge_changes: tuple[str, ...]


def repository_path(value: str, root: Path) -> str:
    """Normalize one changed file path and reject paths outside the repository."""
    candidate = Path(value)
    if candidate.is_absolute():
        try:
            candidate = candidate.resolve().relative_to(root.resolve())
        except ValueError as exception:
            raise ValueError(f"Path is outside the repository: {value}") from exception
    parts = PurePosixPath(str(candidate).replace("\\", "/")).parts
    if not parts or any(part == ".." for part in parts):
        raise ValueError(f"Invalid repository path: {value}")
    return "/".join(part for part in parts if part != ".")


def load_owners(root: Path) -> tuple[KnowledgeOwner, ...]:
    catalog = json.loads((root / CATALOG_PATH).read_text(encoding="utf-8"))
    return tuple(
        KnowledgeOwner(
            name,
            repository_path(entry["path"], root),
            tuple(repository_path(path, root) for path in entry["publication"]["sourcePaths"]),
        )
        for name, entry in sorted(catalog["skills"].items())
        if entry.get("publication", {}).get("sourcePaths")
    )


def changed_paths_from_git(root: Path, base: str) -> tuple[str, ...]:
    """Include committed, staged, unstaged, and untracked changes since base."""
    if not base or base.startswith("-"):
        raise ValueError("--base must name a Git revision")

    def git(*arguments: str) -> set[str]:
        result = subprocess.run(
            ["git", "-C", str(root), *arguments],
            capture_output=True,
            check=True,
        )
        return {
            repository_path(item.decode("utf-8", "surrogateescape"), root)
            for item in result.stdout.split(b"\0")
            if item
        }

    tracked = git("diff", "--no-ext-diff", "--no-renames", "--name-only", "-z", base, "--")
    untracked = git("ls-files", "--others", "--exclude-standard", "-z")
    return tuple(sorted(tracked | untracked))


def _is_within(path: str, prefix: str) -> bool:
    return path == prefix or path.startswith(f"{prefix}/")


def _is_knowledge(path: str, skill_path: str) -> bool:
    if not _is_within(path, skill_path):
        return False
    relative = path[len(skill_path) + 1 :]
    return relative == "SKILL.md" or relative.startswith("references/")


def assess_impacts(paths: Sequence[str], owners: Sequence[KnowledgeOwner]) -> dict[str, KnowledgeImpact]:
    """Assign each source path to its longest catalog prefix; ties have shared owners."""
    source_changes: dict[str, set[str]] = {owner.name: set() for owner in owners}
    knowledge_changes: dict[str, set[str]] = {owner.name: set() for owner in owners}

    for path in paths:
        matches = [
            (len(prefix), owner.name)
            for owner in owners
            for prefix in owner.source_paths
            if _is_within(path, prefix)
        ]
        if matches:
            longest = max(length for length, _ in matches)
            for length, owner_name in matches:
                if length == longest:
                    source_changes[owner_name].add(path)

        for owner in owners:
            if _is_knowledge(path, owner.skill_path):
                knowledge_changes[owner.name].add(path)

    return {
        owner.name: KnowledgeImpact(
            tuple(sorted(source_changes[owner.name])),
            tuple(sorted(knowledge_changes[owner.name])),
        )
        for owner in owners
        if source_changes[owner.name]
    }


def main(argv: Sequence[str] | None = None, *, root: Path = REPOSITORY_ROOT) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    inputs = parser.add_mutually_exclusive_group(required=True)
    inputs.add_argument(
        "--base",
        metavar="REVISION",
        help="compare Git revision to the current tree, including untracked files",
    )
    inputs.add_argument(
        "--paths",
        nargs="+",
        metavar="PATH",
        help="check an explicit list of repository-relative changed files",
    )
    parser.add_argument(
        "--no-impact",
        metavar="REASON",
        help="explain why affected source changes require no knowledge update",
    )
    args = parser.parse_args(argv)

    if args.no_impact is not None and not args.no_impact.strip():
        parser.error("--no-impact requires a nonempty reason")

    try:
        owners = load_owners(root)
        paths = (
            changed_paths_from_git(root, args.base)
            if args.base is not None
            else tuple(sorted({repository_path(path, root) for path in args.paths}))
        )
    except (OSError, ValueError, KeyError, json.JSONDecodeError, subprocess.CalledProcessError) as exception:
        print(f"[error] Cannot check knowledge impact: {exception}", file=sys.stderr)
        return 2

    impacts = assess_impacts(paths, owners)
    if not impacts:
        print("[ok] No catalog-owned infrastructure source paths changed.")
        return 0

    missing = []
    for name, impact in sorted(impacts.items()):
        updated = bool(impact.knowledge_changes)
        print(f"[{ 'ok' if updated else 'missing' }] {name}: {len(impact.source_changes)} source file(s); "
              f"canonical knowledge {'updated' if updated else 'unchanged'}")
        for path in impact.source_changes:
            print(f"  source: {path}")
        for path in impact.knowledge_changes:
            print(f"  knowledge: {path}")
        if not updated:
            missing.append(name)

    if missing and args.no_impact is None:
        print(f"[error] Update canonical knowledge for {', '.join(missing)}, or pass "
              "--no-impact REASON explaining why no update is needed.", file=sys.stderr)
        return 1
    if missing:
        print(f"[no-impact] {args.no_impact.strip()}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
