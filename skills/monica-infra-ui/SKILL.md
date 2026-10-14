---
name: monica-infra-ui
description: Register and use Monica Blazor UI modules, the shell, Markdown viewer, themes, navigation, and localization in a host. Use monica-ui-development for building components and monica-ui-localization for authoring resources.
---

# Monica UI hosting

The shell hosts pages; feature UI modules add operational views, while Markdown provides searchable document groups.

| Task | Read |
| --- | --- |
| Compose the shell, add feature pages, configure navigation, or authorize operational pages | [Shell and access](references/shell-and-access.md) |
| Publish searchable documents, use the viewer, or serve local images | [Markdown](references/markdown.md) |
| Set theme/language defaults or integrate custom page localization | [Theme and localization](references/theme-and-localization.md) |

For building components, use `$monica-ui-development`; for authoring translation resources, use `$monica-ui-localization`; configure each feature's underlying service through its owning infrastructure skill.

If a routed authoring skill is unavailable, use `$monica-guide` to resolve the verified Monica checkout (`source resolve --repository monica --json`) and read its canonical `skills/<name>/SKILL.md`.
