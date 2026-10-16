---
name: monica-infra-ui
description: Register and use Monica Blazor UI modules, the shell, icons, Markdown viewer, themes, navigation, and localization in a host. Use monica-ui-development for building components and monica-ui-localization for authoring resources.
---

# Monica UI hosting

| Task | Read |
| --- | --- |
| Compose the shell, add feature pages, configure navigation, or authorize operational pages | [Shell and access](references/shell-and-access.md) |
| Render local SVG icons or supply icons to controls and navigation | [MoIcon and icon catalog](references/icons.md) |
| Inspect host identity, process evidence, startup timing, or configured operational links | [System information](references/system-information.md) |
| Inspect registered DbContexts, provider sessions, or migration outcomes | [Repository diagnostics](../monica-infra-persistence/references/repository-diagnostics.md) |
| Publish searchable documents, use the viewer, or serve local images | [Markdown](references/markdown.md) |
| Probe connectivity from a host or transform escaped text and JSON | [Utilities workspaces](references/utilities.md) |
| Inspect registered object-mapping pairs and generated expressions | [Mapper workspace](references/mapper.md) |
| Inspect or maintain configured Git working copies and credential resolver metadata | [Git workspace](references/git-workspace.md) |
| Browse bounded host files, edit text, transfer files, or configure FileOps runtime policy | [FileOps workspace](references/fileops-workspace.md) |
| Execute host-local commands and inspect session, output, history and cancellation evidence | [Terminal workspace](references/terminal-workspace.md) |
| Inspect Kubernetes resources, execute Service-backed operations, or publish the host's NodePort addresses | [K8S operations](references/k8s-workspace.md) |
| Compare text or embed a Diff viewer with source-faithful line evidence | [Text comparison](references/diff.md) |
| Read and copy captured exception or diagnostic text inside an existing UI | [Embedded diagnostic text](references/stack-trace.md) |
| Theme the standalone Swagger reference or add application destinations to its toolbar | [Swagger workbench](references/swagger-ui.md) |
| Set theme/language defaults or integrate custom page localization | [Theme and localization](references/theme-and-localization.md) |
| Configure page timestamp formats and deployment timezone display | [Page time display](references/time-display.md) |
| Center loading or empty regions | [Content states](references/content-states.md) |

Use `$monica-ui-development` for components and `$monica-ui-localization` for translations; configure underlying services through their infrastructure skills.

For unavailable authoring skills, use `$monica-guide`.
