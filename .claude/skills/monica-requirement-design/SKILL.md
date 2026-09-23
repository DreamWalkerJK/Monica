---
name: monica-requirement-design
description: Clarify and design a Monica framework capability or module when scope, public contracts, dependencies, or placement need decisions before implementation.
---

# Monica Requirement and Design

Turn the user's idea into a concrete problem statement, users and use cases, behavioral boundaries, and acceptance evidence. Ask only about decisions that cannot be inferred from the repository or task. Keep unresolved assumptions visible; do not require a fixed interview sequence or a separate requirements phase when the user has already supplied enough detail.

Inspect neighboring modules and relevant source before choosing a new module or extending one. Use `$monica-architecture` for layer and public-boundary design, `$monica-development` for module composition and runtime constraints, `$monica-ui-development` for implementation-dependent UI choices, and the appropriate `$monica-infra-*` skill for existing consumer capability contracts.

Capture a durable design artifact when the task calls for one or the decisions are substantial. Place it where the repository's current design work lives; an existing `.pending/{number}-{name}/` folder can be reused, but do not create one merely to satisfy this skill. Record the chosen approach, public contracts, dependency direction, main tradeoffs, risks that change implementation, and verification criteria. Write only the sections needed for the decision. A diagram or small code sketch is useful when it resolves a boundary ambiguity.

When the design is ready to implement, continue into the authorized implementation task. Treat feedback as an edit to the design rather than a mandatory new phase or session.
