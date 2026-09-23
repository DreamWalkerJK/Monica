---
name: monica-ui-bridge-debug
description: Run a bridge ASP.NET Core host to inspect and verify Monica Blazor UI when the framework has no standalone entry point. Use for live DOM, CSS, and browser diagnosis tied to a real host.
---

# Monica UI Bridge Debug

Use a runnable bridge host for Monica UI inspection and refinement. Apply `$monica-ui-development` to implementation, `$monica-ui-localization` to user-facing text, and `$playwright-cli` for browser evidence. The bridge helper lives at `scripts/bridge_service.py`; resolve it relative to this skill directory. Read [bridge service script](references/bridge-service-script.md) for arguments, readiness, cleanup, Windows/WSL handling, and artifact names.

Find the bridge project and target route from the task or repository. Use a user-supplied URL when given; otherwise inspect `Properties/launchSettings.json`, an existing project-owned listener, or choose an unused local port. Place logs, readiness files, and screenshots in a task-specific directory under `.tmp/monica-ui-bridge-debug/` unless the user supplied one. The helper may clean up only its recorded process or a process clearly owned by that bridge project; an unrelated listener on the chosen port is a conflict.

Run the helper's `run` command, then `wait-ready`, then open the target route. For layout or visibility bugs, inspect the live DOM, computed styles, loaded rules, and emitted Blazor `*.bundle.scp.css` before editing. Confirm the visual effect with a screenshot after the cause is understood. Do not infer MudBlazor internal selectors from memory when the runtime DOM can show them. Use the exact MudBlazor source only when the component's API or behavior is uncertain, following `$monica-ui-development`.

After a fix, restart or rebuild the bridge as needed and verify the same route. Keep the successful bridge available for user inspection unless the task requires cleanup. Parallel agent work is optional only when the user or governing instructions authorize delegation; if used, give one worker ownership of the bridge process to avoid competing restarts.
