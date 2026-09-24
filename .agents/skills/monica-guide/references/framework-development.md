# Developing the shared Guide engine

Use this reference when changing Guide implementation or product release contracts. Ordinary installation and workspace operations use the executable commands described in the skill.

- `Monica.Guide.Engine` owns the shared setup behavior for Monica agent products. `Monica.Guide.App` packages the per-platform wizard and CLI. Neither is a NuGet product.
- `KnownAgentProducts` owns each product's release contract. Consumers such as Monica.Workflow select their definition and reuse the engine; they do not maintain a second implementation of Guide behavior.
- `skills/monica-guide` teaches and routes to the engine. Do not put a local implementation of engine setup logic or scripts in this skill.
- `.monica/agent-skill-catalog.json` owns managed instruction templates, source repositories, and aliases. `scripts/build_monica_guide_bundle.py` projects them into release bundles; edit the canonical catalog rather than a generated bundle.
- Guide manages Monica framework skills and source lookup. Monica.Docs is an ordinary checkout with no Guide-specific profile, source binding, or instruction injection.
