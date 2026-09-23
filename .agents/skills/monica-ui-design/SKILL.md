---
name: monica-ui-design
description: Design or prototype a Monica UI before or alongside Blazor implementation. Use for layout, interaction flows, visual direction, or a browser-runnable mockup.
---

# Monica UI Design

Understand the UI's task, audience, information density, and key interactions. Inspect existing Monica shell and related pages when the design must fit an existing product. Choose a visual direction that makes the subject clear through hierarchy, type, spacing, and restrained use of color or effects. Read [design guidance](references/aesthetics-guidelines.md) when visual direction is uncertain, and `$monica-ui-development`'s Precision reference when handing a prototype into the default Monica theme.

Produce the artifact the user needs: a design note, wireframe, interactive prototype, or implementation-ready page specification. Use `.ui-design/{feature}/` for standalone prototype artifacts when the task has no other location. A small prototype may be one `index.html`; split CSS, JavaScript, and mock data only when it helps review. Make meaningful controls interactive and test responsive behavior in a browser. Keep assets local when the prototype is intended for offline or intranet use, and map color roles to MudBlazor palette or approved Monica tokens for an implementation handoff.

If the user also requested implementation, continue with `$monica-ui-development` in the same task. A design direction does not require a separate approval ceremony unless a consequential choice remains unresolved.
