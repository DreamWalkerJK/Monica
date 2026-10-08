---
name: monica-ui-design
description: Design or prototype a Monica UI before or alongside Blazor implementation. Use for layout, interaction flows, visual direction, or a browser-runnable mockup.
---

# Monica UI Design

Understand the UI's task, audience, information density, and key interactions. Inspect real data, actions, and runtime states before choosing the composition. For operational page redesigns, read the shared [working-interface language](../monica-ui-development/references/monica-workbench-design-language.md); use [design guidance](references/aesthetics-guidelines.md) for prototype decisions and `$monica-ui-development`'s Precision Default or optional Grove contract for theme handoff. Keep theme identity explicit: a page redesign or new optional visual direction does not replace the preserved Default palette.

Choose an intentional visual character for each page. Expressive color, typography, depth, and feature composition are valid when they clarify the task; an accepted restrained page does not establish a universal minimal style.

Produce the artifact the user needs: a design note, wireframe, interactive prototype, or implementation-ready page specification. Use `.ui-design/{feature}/` for standalone prototype artifacts when the task has no other location. A small prototype may be one `index.html`; split CSS, JavaScript, and mock data only when it helps review. Make meaningful controls interactive and test responsive behavior in a browser. Keep assets local when the prototype is intended for offline or intranet use, and map color roles to MudBlazor palette or approved Monica tokens for an implementation handoff.

If the user also requested implementation, continue with `$monica-ui-development` in the same task. A design direction does not require a separate approval ceremony unless a consequential choice remains unresolved.
