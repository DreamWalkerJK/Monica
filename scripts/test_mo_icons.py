#!/usr/bin/env python3
"""Behavior checks for MoIcon's offline generation and trusted-asset boundary."""

from __future__ import annotations

import base64
import contextlib
import hashlib
import io
import json
import tarfile
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import mo_icons


SVG = b'''<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24"
viewBox="0 0 24 24" class="upstream-icon" fill="none" stroke="currentColor"
stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
<path d="M4 12h16"/><circle cx="12" cy="12" r="3"/>
</svg>'''
VERSION = "1.2.3"


def archive_bytes(entries: dict[str, bytes]) -> bytes:
    output = io.BytesIO()
    with tarfile.open(fileobj=output, mode="w:gz") as archive:
        for name, data in entries.items():
            member = tarfile.TarInfo(name)
            member.size = len(data)
            archive.addfile(member, io.BytesIO(data))
    return output.getvalue()


def import_fixture(assets: Path, entries: dict[str, bytes] | None = None, corrupt: bool = False) -> None:
    package = archive_bytes(entries or {
        "package/LICENSE": b"ISC License\nThe MIT License (MIT)\nFixture notice\n",
        "package/icons/check.svg": SVG,
    })
    integrity = "sha512-" + base64.b64encode(hashlib.sha512(package).digest()).decode("ascii")
    metadata = {
        "name": "lucide-static", "version": VERSION,
        "dist": {"tarball": f"https://registry.npmjs.org/lucide-static/-/lucide-static-{VERSION}.tgz",
                 "integrity": integrity},
    }
    payload = package + b"corrupt" if corrupt else package
    with patch("urllib.request.urlopen", side_effect=[
        io.BytesIO(json.dumps(metadata).encode("utf-8")), io.BytesIO(payload),
    ]):
        mo_icons.import_lucide(assets, VERSION)


class IconPipelineTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary = tempfile.TemporaryDirectory(prefix="mo-icons-tests-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.assets = self.root / mo_icons.ASSETS_PATH
        import_fixture(self.assets)
        self.catalog = {name: "Lucide/icons/check.svg" for name in ("Search", "Save", "Copy", "Delete", "Refresh")}
        self.write_catalog(self.catalog)
        theme = self.root / "Monica.UI/Theming/Definitions/DefaultTheme.cs"
        theme.parent.mkdir(parents=True)
        theme.write_text('''class DefaultTheme {
    private static Palette CreateLightPalette() => new()
    {
        Primary = "CanvasText",
        Background = "Canvas"
    };
    private static Palette CreateDarkPalette() => new()
    {
        Primary = "CanvasText",
        Background = "Canvas"
    };
}''', encoding="utf-8")

    def write_catalog(self, catalog: dict) -> None:
        (self.assets / "catalog.json").write_text(json.dumps(catalog), encoding="utf-8")

    def run_command(self, command: str) -> int:
        with patch.object(mo_icons, "ROOT", self.root), contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
            return mo_icons.main([command])

    def test_import_records_verified_source_and_original_bytes(self) -> None:
        lock = mo_icons.verify_snapshot(self.assets)
        self.assertEqual(VERSION, lock["version"])
        self.assertEqual(1, lock["svg_count"])
        self.assertEqual(SVG, (self.assets / "Lucide/icons/check.svg").read_bytes())
        self.assertIn("The MIT License", (self.assets / "Lucide/LICENSE").read_text())

    def test_corrupt_download_leaves_previous_snapshot_and_lock_intact(self) -> None:
        previous = {path.relative_to(self.assets): path.read_bytes() for path in self.assets.rglob("*") if path.is_file()}
        with self.assertRaisesRegex(mo_icons.IconError, "integrity mismatch"):
            import_fixture(self.assets, corrupt=True)
        actual = {path.relative_to(self.assets): path.read_bytes() for path in self.assets.rglob("*") if path.is_file()}
        self.assertEqual(previous, actual)

    def test_import_rejects_archive_path_traversal_without_writing(self) -> None:
        with self.assertRaisesRegex(mo_icons.IconError, "archive entry"):
            import_fixture(self.assets, {
                "package/LICENSE": b"ISC License\nThe MIT License",
                "package/icons/../../outside.svg": SVG,
            })
        self.assertFalse((self.root / "outside.svg").exists())
        mo_icons.verify_snapshot(self.assets)

    def test_import_requires_a_pinned_release_before_network_access(self) -> None:
        with patch("urllib.request.urlopen", side_effect=AssertionError("network requested")):
            with self.assertRaisesRegex(mo_icons.IconError, "explicit Lucide release"):
                mo_icons.import_lucide(self.assets, "latest")

    def test_generation_is_offline_and_deterministic_across_input_order(self) -> None:
        with patch("urllib.request.urlopen", side_effect=AssertionError("network requested")):
            first = mo_icons.outputs(self.root)
            self.write_catalog(dict(reversed(list(self.catalog.items()))))
            second = mo_icons.outputs(self.root)
            self.assertEqual(first, second)
            self.assertEqual(0, self.run_command("generate"))
            self.assertEqual(0, self.run_command("check"))

    def test_check_rejects_missing_and_stale_generated_files(self) -> None:
        self.assertEqual(1, self.run_command("check"))
        self.assertEqual(0, self.run_command("generate"))
        catalog_path = self.root / mo_icons.CATALOG_PATH
        catalog_path.write_text(catalog_path.read_text() + "// stale\n", encoding="utf-8")
        self.assertEqual(1, self.run_command("check"))
        self.assertEqual(0, self.run_command("generate"))
        self.assertEqual(0, self.run_command("check"))

    def test_upstream_original_and_license_mutations_are_detected_offline(self) -> None:
        for relative in ("Lucide/icons/check.svg", "Lucide/LICENSE"):
            with self.subTest(relative=relative):
                path = self.assets / relative
                original = path.read_bytes()
                path.write_bytes(original + b"\n")
                with self.assertRaisesRegex(mo_icons.IconError, "snapshot differs"):
                    mo_icons.outputs(self.root)
                path.write_bytes(original)

    def test_explicit_public_aliases_are_supported(self) -> None:
        self.catalog["Confirm"] = "Lucide/icons/check.svg"
        self.write_catalog(self.catalog)
        catalog = mo_icons.load_catalog(self.assets)
        self.assertEqual(catalog["Confirm"][1], catalog["Save"][1])
        generated = mo_icons.render_catalog(catalog, VERSION)
        self.assertIn("public static string Confirm =>", generated)
        self.assertNotIn("const string", generated)

    def test_duplicate_export_names_cannot_silently_override_an_asset(self) -> None:
        (self.assets / "catalog.json").write_text('{"Check":"Monica/a.svg","Check":"Monica/b.svg"}', encoding="utf-8")
        with self.assertRaisesRegex(mo_icons.IconError, "duplicate key"):
            mo_icons.load_catalog(self.assets)

    def test_invalid_names_and_source_escapes_are_rejected(self) -> None:
        for name, source in (
            ("bad-name", "Lucide/icons/check.svg"), ("MoIcons", "Lucide/icons/check.svg"),
            ("GetType", "Lucide/icons/check.svg"), ("Check", "Monica/../../outside.svg"),
            ("Check", "/outside.svg"), ("Check", "C:\\outside.svg"),
            ("Check", "Lucide/icons/./check.svg"), ("Check", "Lucide/icons//check.svg"),
            ("Check", "unknown/check.svg"),
        ):
            with self.subTest(name=name, source=source):
                self.write_catalog({name: source})
                with self.assertRaises(mo_icons.IconError):
                    mo_icons.load_catalog(self.assets)

    def test_fragment_normalizes_paint_and_preserves_geometry(self) -> None:
        fragment = mo_icons.svg_fragment(SVG, "fixture.svg")
        self.assertNotIn("<svg", fragment)
        self.assertNotIn("upstream-icon", fragment)
        self.assertNotIn('stroke-width="2"', fragment)
        self.assertIn('style="stroke-width:var(--mo-icon-stroke-width,1.5)"', fragment)
        self.assertIn('<path d="M4 12h16"/>', fragment)
        self.assertIn('<circle cx="12" cy="12" r="3"/>', fragment)

    def test_rejects_active_external_and_nonmonochrome_svg(self) -> None:
        fragments = (
            '<script>alert(1)</script>', '<foreignObject/>', '<use href="other.svg#icon"/>',
            '<path d="M0 0" onclick="alert(1)"/>', '<path d="M0 0" id="icon"/>',
            '<path d="M0 0" fill="red"/>', '<path d="M0 0" stroke="url(#paint)"/>',
            '<path d="M0 0" style="fill:red"/>', '<path d="M0 0" stroke-width="2"/>',
            '<text>label</text>', '<path d="M0 0">unexpected text</path>',
            '<svg viewBox="0 0 24 24"><path d="M0 0"/></svg>',
        )
        for fragment in fragments:
            with self.subTest(fragment=fragment):
                data = f'<svg viewBox="0 0 24 24">{fragment}</svg>'.encode()
                with self.assertRaises(mo_icons.IconError):
                    mo_icons.svg_fragment(data, "unsupported.svg")

    def test_rejects_changed_coordinate_system_or_xml_entities(self) -> None:
        for data in (
            b'<svg viewBox="0 0 40 40"><path d="M0 0"/></svg>',
            b'<svg viewBox="0 0 24 24" width="48"><path d="M0 0"/></svg>',
            b'<!DOCTYPE svg [<!ENTITY icon "path">]><svg viewBox="0 0 24 24"><path d="M0 0"/></svg>',
            b'<svg viewBox="0 0 24 24" fill="#fff"><path d="M0 0"/></svg>',
        ):
            with self.subTest(data=data), self.assertRaises(mo_icons.IconError):
                mo_icons.svg_fragment(data, "unsupported.svg")


if __name__ == "__main__":
    unittest.main()
