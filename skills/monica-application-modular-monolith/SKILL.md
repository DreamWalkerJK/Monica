---
name: monica-application-modular-monolith
description: Design a Monica DDD modular monolith with bounded contexts under Domains, AppHost composition, Platform dependency direction, published language, and persistence ownership.
---

# Monica Application Modular Monolith

Keep one deployment with business boundaries under `Domains/`, rather than global Application/Domain/Infrastructure buckets. Each bounded context uses one `Domains.{Subdomain}.csproj` containing its application units, domain model, and domain-owned infrastructure. The intended chain is `AppHost → Domains.{Subdomain} → Platform.Infrastructure → Platform.Protocol → Platform.BuildingBlocks`. Put project-common libraries in BuildingBlocks, solution wiring in Infrastructure, and subdomain-only dependencies in the owning project. AppHost remains composition-only. See [solution layout](references/00-solution-layout.md) and [new bounded context](references/01-create-subdomain-and-domain.md).

Cross-domain collaboration goes through deliberate `Platform.Protocol.PublishedLanguages` contracts, not another domain's internals. A request with `[ApiEndpoint]` in `Platform.Protocol.PublishedLanguages.Domain{Subdomain}.Requests` is generated for published HTTP/RPC; one local to a domain project is HTTP-only. Requests own endpoint metadata; handlers own behavior. Give Protocol the published RPC configuration and each domain assembly a `WebApiGenerationConfig` with its local `DomainName`. Read [protocol and collaboration](references/02-protocol-platform-and-internal-collaboration.md) before changing shared contracts, and `$monica-infra-web` for consumer setup.

Keep domain-owned handlers in `Application/HandlersCommand`, `HandlersQuery`, `HandlersEvent`, and `BackgroundWorkers`; repository implementations and context files belong in `Repository/`. Read [boundaries and persistence](references/03-boundaries-dependencies-and-persistence.md) when placing data and host dependencies. Use `$monica-application-project-unit-development` for individual units, and `$monica-infra-persistence`, `$monica-infra-messaging`, or `$monica-infra-configuration` for those capabilities.
