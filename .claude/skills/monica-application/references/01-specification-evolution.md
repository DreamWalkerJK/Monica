# Evolve an Application Specification

Use this reference when source facts drift, an existing rule is inadequate, or a task would introduce a new pattern. Approval of a feature authorizes implementation within the existing specification. It authorizes an architectural deviation only when the human's decision identifies that deviation and its scope.

## Classify the Change

| Change | Action |
| --- | --- |
| Verified factual correction, such as a moved file, an existing dependency, or a renamed domain within approved patterns | Correct the project specification autonomously with source/test evidence. Preserve the distinction between current facts and approved intent. |
| Routine implementation within an approved role, dependency direction, base family, or execution model | Implement and validate it. Update affected project facts or conventions when the change makes them stale. |
| A new role, layer, dependency direction, shared base family, execution model, or local deviation from a mandatory shared/project rule | Prepare a concrete gap and proposal, then obtain a human decision before adopting it. |
| Existing implementation outside the approved specification | Describe the known migration gap and its evidence. Do not silently rewrite a rule to bless the drift or spread it to new code. |

A new concrete subclass in an already adopted base family is routine implementation. Adding a reusable product base family is an architecture change even if its code is short. A fact correction must not erase the rule the source currently violates.

## Make the Decision Concrete

Show the unmet use case, the relevant current rule, the smallest proposed contract change, and the affected consumers and files. Explain why existing native configuration, extension points, inheritance, or composition do not meet the need. Include lifetime/ownership and validation where they matter. Keep this proportional: a local placement exception needs its precise boundary; a new execution model needs its execution and failure semantics.

The human chooses adoption, revision, or rejection. Record the approved scope, rationale, and any conditions beside the owning rule, with a link or reference to existing evidence. Reuse Git history, a PR/discussion, or the project's existing decision record. When a relevant governed Workflow Change or decision flow already applies, record the confirmed decision through that flow's established semantic operations. Otherwise retain the human authorization, rule rationale, and existing history without creating a Change solely for specification maintenance. An existing explicit approval remains valid within its agreed scope; do not request it again for the same pattern. Do not create a new governance engine, approval schema, or ledger.

## Update the Correct Owner

- Generalizable Monica application patterns belong in the relevant shared skill and its examples/tests.
- Application topology, conventions, specializations, and local deviations belong in the project specification skill.
- A project-only exception does not change the shared default. State exactly which rule it deviates from, which projects/types may use it, and what remains required in that scope.

Once approved, update the owning specification and affected implementation, routes, examples, and verification together. Keep the rationale next to the rule. Summarize only unresolved migration gaps as current gaps; durable change history belongs in existing history rather than accumulating session narratives in the skill.
