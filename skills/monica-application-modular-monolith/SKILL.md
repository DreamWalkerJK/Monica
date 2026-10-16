---
name: monica-application-modular-monolith
description: Apply the Monica modular-monolith specification when designing, implementing, refactoring, or reviewing domain ownership, host composition, project dependencies, contracts, or persistence. Load monica-application and the repository's project specification first.
---

# Monica Application Modular Monolith

Before using this profile, load `$monica-application` and the repository-designated project specification. The [shared specification](../monica-application/references/00-application-specification.md) defines mandatory rules, allowed options, and examples; the project specification records the adopted profile and actual topology. Compose within those contracts; use [specification evolution](../monica-application/references/01-specification-evolution.md) for a concrete architectural gap or rule change.

Keep one deployment with business boundaries under `Domains/`, rather than global Application/Domain/Infrastructure buckets. Each bounded context uses one `Domains.{Subdomain}.csproj` containing its application units, domain model, and domain-owned infrastructure. The intended chain is `AppHost → Domains.{Subdomain} → Platform.Infrastructure → Platform.Protocol → Platform.BuildingBlocks`. Put project-common libraries in BuildingBlocks, solution wiring in Infrastructure, and subdomain-only dependencies in the owning project. AppHost remains composition-only. See [solution layout](references/00-solution-layout.md) and [new bounded context](references/01-create-subdomain-and-domain.md).

Cross-domain collaboration goes through deliberate `Platform.Protocol.PublishedLanguages` contracts, not another domain's internals. A request with `[ApiEndpoint]` in `Platform.Protocol.PublishedLanguages.Domain{Subdomain}.Requests` is generated for published HTTP/RPC; one local to a domain project is HTTP-only. Requests own endpoint metadata; handlers own behavior. Give Protocol the published RPC configuration and each domain assembly a `WebApiGenerationConfig` with its local `DomainName`. Read [protocol and collaboration](references/02-protocol-platform-and-internal-collaboration.md) before changing shared contracts, and `$monica-infra-web` for consumer setup.

Keep domain-owned handlers in `Application/HandlersCommand`, `HandlersQuery`, `HandlersEvent`, and `BackgroundWorkers`; repository implementations and context files belong in `Repository/`. Read [boundaries and persistence](references/03-boundaries-dependencies-and-persistence.md) when placing data and host dependencies. Use `$monica-application-project-unit-development` for individual units, and `$monica-infra-persistence`, `$monica-infra-messaging`, or `$monica-infra-configuration` for those capabilities.
