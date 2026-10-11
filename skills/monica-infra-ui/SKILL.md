---
name: monica-infra-ui
description: Register and use Monica Blazor UI modules, the shell, Markdown viewer, themes, navigation, and localization in a host. Use monica-ui-development for building components and monica-ui-localization for authoring resources.
---

# Monica UI hosting

The shell hosts pages; feature UI modules add operational views, while Markdown provides searchable document groups.

| Task | Read |
| --- | --- |
| Compose the shell, add feature pages, configure navigation, or authorize operational pages | [Shell and access](references/shell-and-access.md) |
| Inspect host identity, process evidence, startup timing, or configured operational links | [System information](references/system-information.md) |
| Inspect registered DbContexts, provider sessions, or migration outcomes | [Repository diagnostics](../monica-infra-persistence/references/repository-diagnostics.md) |
| Publish searchable documents, use the viewer, or serve local images | [Markdown](references/markdown.md) |
| Probe connectivity from a host or transform escaped text and JSON | [Utilities workspaces](references/utilities.md) |
| Inspect registered object-mapping pairs and generated expressions | [Mapper workspace](references/mapper.md) |
| Compare text or embed a Diff viewer with source-faithful line evidence | [Text comparison](references/diff.md) |
| Set theme/language defaults or integrate custom page localization | [Theme and localization](references/theme-and-localization.md) |

For building components, use [UI development](../monica-ui-development/SKILL.md); for authoring translation resources, use [UI localization](../monica-ui-localization/SKILL.md); configure each feature's underlying service through its owning infrastructure skill.
