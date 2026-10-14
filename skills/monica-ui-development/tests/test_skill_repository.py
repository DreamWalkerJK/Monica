#!/usr/bin/env python3
"""Regression contracts for repository defaults and portable skill commands."""

from __future__ import annotations

import json
import os
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPTS_DIRECTORY = Path(__file__).resolve().parents[1] / "scripts"
LOCATIONS = ("skills", ".agents/skills", ".claude/skills")


class SkillRepositoryTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary_directory.cleanup)
        self.sandbox = Path(self.temporary_directory.name).resolve()
        self.repository = self.sandbox / "repository"
        self.repository.mkdir()
        (self.repository / "Monica.slnx").write_text("<Solution />", encoding="utf-8")
        (self.sandbox / "Monica.slnx").write_text("<Solution />", encoding="utf-8")
        self.caller = self.sandbox / "decoy-caller"
        self.caller.mkdir()
        (self.caller / "Monica.slnx").write_text("<Solution />", encoding="utf-8")
        self.environment = os.environ.copy()
        self.environment["PYTHONDONTWRITEBYTECODE"] = "1"
        self.environment.pop("MUDBLAZOR_SOURCE_PATH", None)

    def scripts_at(self, location: str, *, detached: bool = False) -> Path:
        root = self.sandbox / "custom-installed-target" if detached else self.repository
        scripts = root / location / "monica-ui-development/scripts"
        shutil.copytree(SCRIPTS_DIRECTORY, scripts)
        return scripts

    def python(self, scripts: Path, code: str) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [sys.executable, "-B", "-c", code], cwd=self.caller,
            env={**self.environment, "PYTHONPATH": str(scripts)},
            capture_output=True, text=True, check=False,
        )

    def cli(self, scripts: Path, filename: str, *arguments: str) -> subprocess.CompletedProcess[str]:
        return self.python(scripts, f"""
import runpy
import sys
sys.argv = {[str(scripts / filename), *arguments]!r}
runpy.run_path(sys.argv[0], run_name='__main__')
""")

    def configure_source(self) -> None:
        source = self.sandbox / "mudblazor-source"
        marker = source / "src/MudBlazor/Components/ThemeProvider/MudThemeProvider.razor.cs"
        marker.parent.mkdir(parents=True)
        marker.write_text('var css = "--mud-palette-primary: value;";', encoding="utf-8")
        self.environment["MUDBLAZOR_SOURCE_PATH"] = str(source)

    def styles(self) -> Path:
        root = self.sandbox / "consumer/styles"
        root.mkdir(parents=True)
        (root / "component.css").write_text("color: var(--mud-palette-primary);", encoding="utf-8")
        return root

    def detach(self) -> Path:
        (self.sandbox / "Monica.slnx").unlink()
        scripts = self.scripts_at("arbitrary-target", detached=True)
        markers = [
            ancestor / "Monica.slnx" for ancestor in (scripts, *scripts.parents)
            if (ancestor / "Monica.slnx").is_file()
        ]
        if markers:
            self.skipTest(
                "The OS temporary directory cannot provide detached scripts; "
                f"real ancestor solution markers remain: {markers}"
            )
        return scripts

    def test_repository_defaults_are_shared_across_all_locations(self) -> None:
        resource = self.repository / "Monica.UI/Localization/Pages/demo.json"
        resource.parent.mkdir(parents=True)
        resource.write_text('{"title": "\\u2603"}', encoding="utf-8")
        excluded = self.repository / "Monica.Testing/Localization"
        excluded.mkdir(parents=True)
        for location in LOCATIONS:
            with self.subTest(location=location):
                scripts = self.scripts_at(location)
                result = self.python(scripts, """
import json
import mudblazor_skill_state as state
import subset_ui_font as font
print(json.dumps([str(state.PROJECT_ROOT), str(state.VARIABLES_JSON_FILE), str(font.DEFAULT_SOURCE_FONT), str(font.DEFAULT_TARGET_FONT), [str(path) for path in font.discover_default_localization_dirs()]]))
""")
                self.assertEqual(0, result.returncode, result.stderr)
                root, cache, source, target, directories = json.loads(result.stdout)
                self.assertEqual(str(self.repository), root)
                self.assertEqual(str(self.repository / ".tmp/monica-ui-development/mudblazor-css-variables.json"), cache)
                self.assertEqual(str(self.repository / ".tmp/monica-ui-font-sources/zcool_qingke_huangyou.ttf"), source)
                self.assertEqual(str(self.repository / "Monica.UI/wwwroot/fonts/Komi-ZCOOL-QingKe-HuangYou.woff2"), target)
                self.assertEqual([str(resource.parent.parent)], directories)
        self.assertEqual([], list(self.sandbox.rglob(".tmp")))

    def test_generator_and_validator_reuse_one_repository_catalog(self) -> None:
        self.configure_source()
        styles = self.styles()
        cache = self.repository / ".tmp/monica-ui-development/mudblazor-css-variables.json"
        for location in LOCATIONS:
            with self.subTest(location=location):
                scripts = self.scripts_at(location)
                generated = self.cli(scripts, "sync_mud_css_variables.py")
                self.assertEqual(0, generated.returncode, generated.stdout + generated.stderr)
                self.assertTrue(cache.is_file())
                self.assertIn(str(cache), generated.stdout)
                validated = self.cli(scripts, "validate_mud_css_variables.py", "--root", str(styles), "--json")
                self.assertEqual(0, validated.returncode, validated.stdout + validated.stderr)
                self.assertEqual("PASSED", json.loads(validated.stdout)["summary"]["status"])
        self.assertEqual([cache.parent.parent], list(self.sandbox.rglob(".tmp")))

    def test_detached_imports_help_and_source_checker_remain_available(self) -> None:
        scripts = self.detach()
        result = self.python(scripts, """
import json
import mudblazor_skill_state as state
import subset_ui_font as font
import sync_mud_css_variables
import validate_mud_css_variables
print(json.dumps([state.PROJECT_ROOT, state.STATE_DIR, state.VARIABLES_JSON_FILE, font.REPO_ROOT]))
""")
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual([None] * 4, json.loads(result.stdout))
        for filename in ("sync_mud_css_variables.py", "validate_mud_css_variables.py", "subset_ui_font.py"):
            with self.subTest(filename=filename):
                help_result = self.cli(scripts, filename, "--help")
                self.assertEqual(0, help_result.returncode, help_result.stderr)
        unset = self.cli(scripts, "check_mudblazor_source.py", "--json")
        self.assertEqual(1, unset.returncode, unset.stderr)
        self.assertEqual("mudblazor_source_path_unset", json.loads(unset.stdout)["error_code"])
        self.configure_source()
        available = self.cli(scripts, "check_mudblazor_source.py", "--json")
        self.assertEqual(0, available.returncode, available.stderr)
        self.assertTrue(json.loads(available.stdout)["ok"])
        self.assertEqual([], list(self.sandbox.rglob(".tmp")))

    def test_detached_missing_defaults_are_execution_time_flag_diagnostics(self) -> None:
        scripts = self.detach()
        for filename, arguments, flag in (
            ("sync_mud_css_variables.py", (), "--output-json"),
            ("validate_mud_css_variables.py", (), "--variables-file"),
            ("subset_ui_font.py", (), "--source-font"),
            ("subset_ui_font.py", ("--source-font", "input.ttf"), "--target-font"),
            ("subset_ui_font.py", ("--source-font", "input.ttf", "--target-font", "output.woff2"), "--localization-dir"),
        ):
            with self.subTest(filename=filename, arguments=arguments):
                result = self.cli(scripts, filename, *arguments)
                self.assertEqual(2, result.returncode)
                self.assertIn(flag, result.stderr)
                self.assertNotIn("Traceback", result.stderr)
        self.assertEqual([], list(self.sandbox.rglob(".tmp")))

    def test_explicit_catalog_destination_wins_and_failed_source_creates_nothing(self) -> None:
        styles = self.styles()
        for detached in (False, True):
            with self.subTest(detached=detached):
                scripts = self.detach() if detached else self.scripts_at("skills")
                destination = self.sandbox / f"task-{detached}/catalog/mud.json"
                self.environment.pop("MUDBLAZOR_SOURCE_PATH", None)
                failed = self.cli(scripts, "sync_mud_css_variables.py", "--output-json", str(destination))
                self.assertEqual(1, failed.returncode, failed.stderr)
                self.assertFalse(destination.parent.exists())
                self.configure_source() if not detached else None
                if detached:
                    self.environment["MUDBLAZOR_SOURCE_PATH"] = str(self.sandbox / "mudblazor-source")
                generated = self.cli(scripts, "sync_mud_css_variables.py", "--output-json", str(destination))
                self.assertEqual(0, generated.returncode, generated.stdout + generated.stderr)
                self.assertEqual(["--mud-palette-primary"], json.loads(destination.read_text(encoding="utf-8"))["variables"])
                validated = self.cli(scripts, "validate_mud_css_variables.py", "--root", str(styles), "--variables-file", str(destination), "--json")
                self.assertEqual(0, validated.returncode, validated.stdout + validated.stderr)
                self.assertEqual("PASSED", json.loads(validated.stdout)["summary"]["status"])
        self.assertEqual([], list(self.sandbox.rglob(".tmp")))

    def test_explicit_font_paths_and_reporting_work_with_and_without_repository(self) -> None:
        # Stub conversion, but execute both command flows and their path reports.
        resource = self.sandbox / "explicit-resources"
        resource.mkdir()
        (resource / "demo.json").write_text('{"title": "\\u2603"}', encoding="utf-8")
        default_resource = self.repository / "Monica.UI/Localization"
        default_resource.mkdir(parents=True)
        (default_resource / "unselected.json").write_text('{"title": "\\u2665"}', encoding="utf-8")
        source = self.sandbox / "external-fonts/input.ttf"
        source.parent.mkdir()
        source.write_bytes(b"fixture input, not a real font")
        for detached in (False, True):
            with self.subTest(detached=detached):
                scripts = self.detach() if detached else self.scripts_at("skills")
                target = self.sandbox / f"external-output-{detached}/target.woff2"
                arguments = ["--source-font", str(source), "--target-font", str(target), "--localization-dir", str(resource)]
                result = self.python(scripts, f"""
import sys
import subset_ui_font as font
def fake_subset(source, target, text):
    assert '\\u2603' in text
    assert '\\u2665' not in text
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(b'fixture output')
font.run_subset = fake_subset
sys.argv = ['subset_ui_font.py', *{arguments!r}]
assert font.main() == 0
sys.argv = ['subset_ui_font.py', '--mode', 'check', *{arguments!r}]
assert font.main() == 0
""")
                self.assertEqual(0, result.returncode, result.stdout + result.stderr)
                self.assertIn(f"Generated {target}", result.stdout)
                self.assertIn(f"Font subset is current: {target}", result.stdout)
        self.assertEqual([], list(self.sandbox.rglob(".tmp")))


if __name__ == "__main__":
    unittest.main()
