---
name: monica-ui-audit
description: Audit Monica Blazor UI for async disposal, JS interop ownership, MudBlazor semantics, responsive containment, theme roles, prototype fidelity, and user-visible type formatting. Use for requested reviews or a concrete UI risk.
---

# Monica UI Audit

Audit the component or area named by the task. If a failure identifies a component, inspect its `.razor`, code-behind, isolated CSS, injected state owner, DI lifetime, and any owned JavaScript module. For a repository-wide audit, scan production UI owners and follow disposal, timers, subscriptions, and background work beyond the Razor file. Read [audit categories](references/audit-categories.md) for detailed P0–P5 signals and fixes; load only the categories relevant to the task. `$monica-ui-development` owns implementation guidance and its Precision reference applies to global theme or prototype comparisons.

Prioritize observed failures: P0 resource and async lifetime races; P1 custom interactive elements replacing MudBlazor primitives; P2 sizing, overflow, and scroll ownership; P3 theme tokens and visual hierarchy; P4 private component classes coupled to global theme CSS; P5 duplicate display utilities and raw CLR type names. Cite the exact file/line and evidence for each actionable finding. A review request reports findings without edits; a fix request repairs them in the requested scope.

For P0, trace each incomplete await and resource producer through disposal. Confirm that late imports are observed and disposed, callbacks stop before their `DotNetObjectReference` is released, in-flight JS users cannot invoke a disposed reference, and teardown is idempotent. Use a deterministic interleaving test when the race is real and the repository's test policy permits it. For P2/P3, inspect runtime DOM, computed styles, compiled isolated selectors, and the smallest element that should own scrolling. Verify the affected layout at relevant wide and narrow viewports, in light and dark modes when theme behavior changes.

Keep Monica color usage on MudBlazor palette or approved `--mo-color-*` tokens. Place page layout in component CSS and shared MudBlazor visuals in theme CSS. For ordinary user-facing CLR type names, normalize at the producing boundary with `GetCleanFullName()`; show assembly-qualified identity only in views explicitly about assembly provenance. Summarize the fix and category-appropriate build/browser verification when changes are requested.
