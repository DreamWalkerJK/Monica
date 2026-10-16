---
name: monica-application
description: Apply the shared Monica application specification and the repository's project specification before designing, implementing, refactoring, or reviewing a Monica application, its boundaries, or ProjectUnits.
---

# Monica Application

Use this skill before application design, implementation, refactoring, or review, including when entering through a granular application skill. Read the [shared specification](references/00-application-specification.md), then the repository-designated `<project>-application-spec` skill and the references relevant to the affected boundary. That project skill owns the application's actual architecture and adopted conventions.

Compose existing ProjectUnits and documented infrastructure capabilities first. A new business flow or a combination without an exact example is ordinary implementation when each unit preserves its approved responsibility and contracts. Read [specification evolution](references/01-specification-evolution.md) when that composition exposes a concrete rule conflict or an uncovered architectural contract. Use the [project specification contract and template](references/02-project-specification-skill.md) when adopting or maintaining a repository specification. A missing project specification does not authorize inventing its architecture; inspect the source and prepare a concrete adoption proposal while continuing work within already approved patterns.

## Routing

- Exact Monica framework source needed outside the active checkout: use `$monica-guide source resolve --repository monica --json` as a read-only locator.
- Microservice solution layout, service boundaries, `Platform.Protocol`, published language, migrations, or cross-service collaboration: use `$monica-application-microservice`.
- Modular monolith layout, bounded contexts under `Domains/`, AppHost composition, cross-domain collaboration, or persistence ownership: use `$monica-application-modular-monolith`.
- Feature implementation or review of handlers, entities, repositories, events, jobs, seeders, host services, CRUD adapters, controllers, Facades, and other support types: use `$monica-application-project-unit-development`.
- Sociable application tests, `MonicaTestApplicationFactory`, handler or repository scenarios, database isolation, and migration away from mock-heavy tests: use `$monica-application-unit-testing`.
- Consumer setup and use of Monica infrastructure: route to `$monica-infra-persistence`, `$monica-infra-messaging`, `$monica-infra-jobs`, `$monica-infra-configuration`, `$monica-infra-hosting`, `$monica-infra-observability`, `$monica-infra-ai`, `$monica-infra-web`, or `$monica-infra-ui` for the capability in use.

## Apply the Specification

Use the architecture profile adopted by the project specification; do not choose a new deployment model for an ordinary feature. Load the matching architecture and unit references before changing their contracts. Reconcile affected project-specification facts and approved rules with the implementation in the same change. Record the rationale beside a rule; use Git history and existing project decision records for its evolution.
