---
name: monica-infra-hosting
description: Compose a Monica host and use module diagnostics, conventional DI, hosted services, service discovery, and state stores. Use for application setup or runtime integration; use monica-development when authoring a module.
---

# Monica infrastructure hosting

Use this skill when an application consumes Monica modules or diagnoses host composition. Read [references/usage.md](references/usage.md) for the host lifecycle, registration choices, and failure behavior.

Start from the current application's host type. Register modules inside its single `AddMonica(...)` callback, then complete a Web host with `UseMonica()` and `MapMonica()` on the same application. Generic hosts have no web mapping. Prefer a module's public registration extensions over direct service registrations when the extension declares dependencies or features.

For module strategy design, type discovery rules, and hosted-service subclass implementation, use `$monica-development`. For database repositories and units of work, use `$monica-infra-persistence`. For telemetry/exporting, use `$monica-infra-observability`. The old `dynamic-proxy` documentation describes no current production `AddDynamicProxy` module; inspect the requested decoration scenario before suggesting an API.
