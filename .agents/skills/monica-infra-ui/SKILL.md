---
name: monica-infra-ui
description: Register and use Monica Blazor UI modules, the shell, Markdown viewer, themes, navigation, and localization in a host. Use monica-ui-development for building components and monica-ui-localization for authoring resources.
---

# Monica UI hosting

Read [references/usage.md](references/usage.md) for the shell, feature UI registrations, document groups, and access behavior. Add UI modules inside `AddMonica(...)`; complete the Web app with `UseMonica()` and `MapMonica()` so static assets, antiforgery, Razor components, and module endpoints are mapped.

For creating or editing Razor components and styles, use `$monica-ui-development`; for localized strings and resource validation, use `$monica-ui-localization`; for page structure, use `$monica-architecture`. `$monica-infra-ai` covers AI service configuration behind AI pages.
