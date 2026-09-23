---
name: monica-application-project-unit-development
description: Create or refactor Monica application ProjectUnits: requests, handlers, domain services, entities, repositories, events, options, and jobs. Use with the chosen microservice or modular-monolith architecture skill.
---

# Monica Application ProjectUnit Development

## Overview

Use this skill for unit-level application development in Monica-based DDD projects. It defines the common ProjectUnit language shared by both `monica-application-microservice` and `monica-application-modular-monolith`.

## Workflow

Choose the owning subdomain and project with `$monica-application-microservice` or `$monica-application-modular-monolith`. Use [unit overview](references/00-project-unit-overview.md) and [composition map](references/02-project-unit-composition-map.md) when the feature's units are unclear; load only the matching unit example below. Keep rich entity behavior in the domain and orchestration in handlers.

## Ground Rules

- Use Monica-native base classes and interfaces only. Do not introduce `Our*` wrappers or FIPS-specific conventions.
- Follow the naming, placement, and boundary rules in [01-project-unit-naming-and-boundaries.md](references/01-project-unit-naming-and-boundaries.md). These rules are aligned with the current `Monica.ProjectUnits` discovery behavior.
- Keep persistence concerns in repositories and persistence classes, not in request handlers. Use `$monica-infra-persistence` for the current operation, transaction, and provider contracts.
- Keep repository implementations in the owning subdomain or service infrastructure boundary. Do not move a repository or adapter to `Platform` just because it uses an external library; only project-common reusable infrastructure belongs in `Platform`.
- Keep contracts stable: requests, DTOs, and events are not persistence entities.
- Add explicit `[ProjectUnitMetadata]` and one or more `[ProjectUnitRequirement]` annotations to every discovered unit. Do not rely on metadata inherited from a base class.
- Treat missing metadata, description, ownership, and requirement references as four independent catalog debts. Do not collapse them into one readiness score.
- If a handler returns `Res<string>`, use `Res.Ok<string>(value)` instead of `Res.Ok(value)` to avoid the non-generic string overload.
- Make HTTP contracts request-owned. Put `[ApiEndpoint]` on the request type and keep route, verb, binding, and optional operation name off the `ApplicationService` handler.
- A request is published for generated RPC clients only when it is attributed source in the exact namespace `*.PublishedLanguages.Domain{DomainName}.Requests`. Attributed requests outside that boundary remain local HTTP endpoints.
- Put `[assembly: WebApiGenerationConfig(...)]` in each assembly that owns attributed requests. Protocol assemblies select the required `RpcClientTargets`; local-only assemblies normally use `None`.
- Do not add MVC `[Http*]`, `[Route]`, or `[From*]` attributes to generated `ApplicationService` endpoints. The request contract is the single endpoint authority.
- Use `ApplicationService` only for HTTP-exposed mediator handlers. If a request is intentionally internal and must have no HTTP endpoint, implement `IRequestHandler<TRequest, TResult>` as a normal transient dependency instead of omitting `[ApiEndpoint]` from an `ApplicationService`.
- For capability setup and runtime behavior, use `$monica-infra-configuration`, `$monica-infra-jobs`, `$monica-infra-messaging`, and `$monica-infra-web` as applicable. This skill owns the application unit's shape and placement.

## Reference Navigation

- Unit catalog and responsibilities: [00-project-unit-overview.md](references/00-project-unit-overview.md)
- Naming, placement, and boundary rules: [01-project-unit-naming-and-boundaries.md](references/01-project-unit-naming-and-boundaries.md)
- Unit selection by feature shape: [02-project-unit-composition-map.md](references/02-project-unit-composition-map.md)
- Metadata, ownership, and requirement traceability: [03-project-unit-context-metadata.md](references/03-project-unit-context-metadata.md)
- `ApplicationService` templates: [10-application-service-template.md](references/10-application-service-template.md)
- `DomainService` templates: [11-domain-service-template.md](references/11-domain-service-template.md)
- Entity, request, and event templates: [12-entity-request-event-template.md](references/12-entity-request-event-template.md)
- Repository and DbContext templates: [13-repository-and-persistence-template.md](references/13-repository-and-persistence-template.md)
- Event handler templates: [14-event-handler-template.md](references/14-event-handler-template.md)
- Job templates: [15-job-template.md](references/15-job-template.md)
- Configuration templates: [16-configuration-template.md](references/16-configuration-template.md)

## Scope Notes

- This skill is architecture-agnostic. It explains unit design, not solution topology.
- Use `monica-application-microservice` to decide service splits, published-language layout, and migration project boundaries.
- Use `monica-application-modular-monolith` to decide module splits, host composition, and cross-module contracts.
