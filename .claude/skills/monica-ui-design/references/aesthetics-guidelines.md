# Monica prototype decisions

Use the shared [working-interface language](../../monica-ui-development/references/monica-workbench-design-language.md) for operational page hierarchy, surfaces, typography, runtime truth, and responsive acceptance. Use [Precision Default](../../monica-ui-development/references/monica-precision-design-language.md) or [Grove](../../monica-ui-development/references/monica-grove-design-language.md) for the chosen theme. Do not maintain another palette or page template here.

Before drawing, inspect the real content and identify the first decision, its supporting evidence, and the next action. Record the page's task and intentional composition in the design artifact. If several directions are requested, vary information hierarchy and interaction meaningfully rather than supplying the same dashboard in several accent colors.

Use representative names, descriptions, status combinations, long values, and empty states. Mark fixture data explicitly in a standalone prototype; never present it as live host evidence. Model only actions and data that the implementation can supply, and record unresolved assumptions instead of hiding them behind decorative controls.

Make the prototype's primary interactions usable: search and filters change results, selection exposes relevant details, and close/dismiss paths work. Demonstrate the relevant wide, intermediate, and phone reflow. A prototype may approximate a primitive's paint, but its handoff must identify the actual MudBlazor control and any keyboard or focus behavior to verify in implementation.

Map every color to a palette or approved token role and every font to the selected theme's local interface, display, or diagnostic role. Keep offline prototypes self-contained where required. During handoff, compare the browser implementation against the prototype's hierarchy, readable evidence, spacing, and surface roles before judging colors alone. Keep before/after captures with the page-specific design report.
