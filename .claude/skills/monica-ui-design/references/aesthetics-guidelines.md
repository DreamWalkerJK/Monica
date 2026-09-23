# Monica UI design choices

Start from the page's job. Monitoring pages favor scannable status, dense rows, and clear severity; editing flows favor field grouping, validation, and safe actions; exploration pages can use more space for context and comparison. Establish an intentional type scale and responsive grid before adding decoration.

Choose distinct canvas, surface, border, text, focus, and semantic roles for light and dark modes. Use meaningful accents for status or navigation and reinforce color with labels or icons. For a Monica implementation, express colors through `--mud-palette-*` or approved `--mo-color-*` tokens. Avoid introducing raw colors into Razor, CSS, JavaScript, or visualization payloads.

Use one visual signature when it improves identity or comprehension. Repeated cards, gradients, glows, or motion can obscure operational data. Give interactive elements visible hover, focus, selected, and pressed states. Motion should clarify a state transition and honor `prefers-reduced-motion`.

Choose fonts by interface, display, and code roles, with fallbacks that cover the product's languages. Runtime Monica assets must work offline; use local WOFF2 files for implementation and for prototypes intended to model offline behavior. Validate at wide, intermediate, and narrow viewports and compare the prototype with the rendered implementation when a handoff is part of the task.
