# Developing the shared Guide engine

Use this reference when changing Guide implementation or product release contracts. Ordinary installation and workspace operations use the executable commands described in the skill.

- `Monica.Guide.Engine` owns the shared setup behavior for Monica agent products. `Monica.Guide.App` packages the per-platform wizard and CLI. Neither is a NuGet product.
- `KnownAgentProducts` owns each product's release contract. Consumers such as Monica.Workflow select their definition and reuse the engine; they do not maintain a second implementation of Guide behavior.
- `skills/monica-guide` teaches and routes to the engine. Do not put a local implementation of engine setup logic or scripts in this skill.
- `.monica/agent-skill-catalog.json` owns managed instruction templates, source repositories, and aliases. `scripts/build_monica_guide_bundle.py` projects them into release bundles; edit the canonical catalog rather than a generated bundle.
- The bundle also projects canonical profile `required`, `recommended`, and capability-conditioned roots into `profileSelections`. Guide expands selected roots through required dependency edges; skill-level recommendations do not recursively widen the installation. Workspace configure, release refresh, inspection, and setup preview counts use the same selector and the workspace's confirmed capabilities. Products with unconditional profiles, such as Workflow, can retain per-entry profile membership.
- Managed instructions live in `AGENTS.md`. Initialization and workspace configure remove only the current product's well-formed legacy span from `CLAUDE.md`, preserving user and sibling-product content and the minimal `@AGENTS.md` import when Claude is selected globally or through `.claude/skills`. Malformed or duplicate product markers block the entire plan before apply.
- Guide manages Monica framework skills and source lookup. Monica.Docs is an ordinary checkout with no Guide-specific profile, source binding, or instruction injection.
