from __future__ import annotations

import importlib.util
import json
import subprocess
import sys
import tempfile
import unittest
from contextlib import redirect_stderr, redirect_stdout
from io import StringIO
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = REPOSITORY_ROOT / "scripts" / "check_knowledge_impact.py"
SPEC = importlib.util.spec_from_file_location("check_knowledge_impact", SCRIPT_PATH)
assert SPEC and SPEC.loader
checker = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = checker
SPEC.loader.exec_module(checker)


class KnowledgeImpactTests(unittest.TestCase):
    def test_specific_source_path_wins_and_equal_prefixes_share_ownership(self) -> None:
        owners = (
            checker.KnowledgeOwner("hosting", "skills/hosting", ("Monica.Framework",)),
            checker.KnowledgeOwner("jobs", "skills/jobs", ("Monica.Framework/Seeder",)),
            checker.KnowledgeOwner("seeding", "skills/seeding", ("Monica.Framework/Seeder",)),
        )
        impacts = checker.assess_impacts(
            ["Monica.Framework/Seeder/SeedRunner.cs", "Monica.Framework/Other.cs"], owners
        )

        self.assertEqual(("Monica.Framework/Other.cs",), impacts["hosting"].source_changes)
        self.assertEqual(
            ("Monica.Framework/Seeder/SeedRunner.cs",), impacts["jobs"].source_changes
        )
        self.assertEqual(impacts["jobs"].source_changes, impacts["seeding"].source_changes)

    def test_knowledge_requires_entrypoint_or_reference_in_matching_owner(self) -> None:
        owners = (
            checker.KnowledgeOwner("persistence", "skills/persistence", ("Monica.Repository",)),
        )
        source = "Monica.Repository/Modules/ModuleRepository.cs"
        metadata = "skills/persistence/agents/openai.yaml"
        other = "skills/hosting/SKILL.md"

        impact = checker.assess_impacts([source, metadata, other], owners)["persistence"]
        self.assertEqual((), impact.knowledge_changes)

        impact = checker.assess_impacts(
            [source, "skills/persistence/references/usage.md"], owners
        )["persistence"]
        self.assertEqual(("skills/persistence/references/usage.md",), impact.knowledge_changes)

    def test_cli_requires_update_or_explicit_reason(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            root = Path(temporary_directory)
            catalog_path = root / checker.CATALOG_PATH
            catalog_path.parent.mkdir(parents=True)
            catalog_path.write_text(
                json.dumps(
                    {
                        "skills": {
                            "monica-infra-persistence": {
                                "path": "skills/monica-infra-persistence",
                                "publication": {"sourcePaths": ["Monica.Repository"]},
                            }
                        }
                    }
                ),
                encoding="utf-8",
            )
            source = "Monica.Repository/Persistence/Save.cs"
            with redirect_stdout(StringIO()), redirect_stderr(StringIO()) as errors:
                self.assertEqual(1, checker.main(["--paths", source], root=root))
            self.assertIn("monica-infra-persistence", errors.getvalue())

            output = StringIO()
            with redirect_stdout(output), redirect_stderr(StringIO()):
                self.assertEqual(
                    0,
                    checker.main(
                        ["--paths", source, "--no-impact", "Internal refactor; public save semantics unchanged."],
                        root=root,
                    ),
                )
            self.assertIn("[no-impact] Internal refactor", output.getvalue())

            with redirect_stdout(StringIO()), redirect_stderr(StringIO()):
                self.assertEqual(
                    0,
                    checker.main(
                        ["--paths", source, "skills/monica-infra-persistence/SKILL.md"], root=root
                    ),
                )

    def test_base_includes_tracked_worktree_and_untracked_files(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            root = Path(temporary_directory)

            def git(*arguments: str) -> None:
                subprocess.run(
                    ["git", "-C", str(root), *arguments],
                    check=True,
                    capture_output=True,
                )

            git("init", "-q")
            git("config", "user.name", "Guide Test")
            git("config", "user.email", "guide@example.test")
            (root / "Monica.Repository").mkdir()
            tracked = root / "Monica.Repository" / "Context.cs"
            tracked.write_text("old\n", encoding="utf-8")
            git("add", ".")
            git("commit", "-qm", "initial")

            tracked.write_text("new\n", encoding="utf-8")
            untracked = root / "Monica.Repository" / "New.cs"
            untracked.write_text("new\n", encoding="utf-8")

            self.assertEqual(
                ("Monica.Repository/Context.cs", "Monica.Repository/New.cs"),
                checker.changed_paths_from_git(root, "HEAD"),
            )


if __name__ == "__main__":
    unittest.main()
