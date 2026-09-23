---
name: monica-application-microservice
description: Design a Monica DDD microservice solution: subdomain service ownership, Platform dependency direction, published contracts, API/Domain/migration projects, and cross-service collaboration.
---

# Monica Application Microservice

Split services by business capability and data ownership. The intended solution chain is `{Subdomain}Service.API → {Subdomain}Service.Domain → Platform.Infrastructure → Platform.Protocol → Platform.BuildingBlocks`. Put project-common libraries in `Platform.BuildingBlocks`, solution wiring in `Platform.Infrastructure`, shared business language in `Platform.Protocol`, and service-only dependencies in the owning Domain project. A service's API, Domain, and migrations move together; AppHost or gateway projects remain composition adapters. See [solution layout](references/00-solution-layout.md) and [new subdomain](references/01-create-subdomain-and-service.md) when changing topology.

Published cross-service requests belong under `Platform.Protocol.PublishedLanguages.Domain{Subdomain}.Requests`. A published request with `[ApiEndpoint]` participates in generated HTTP/RPC clients; an unattributed type is shared language only. An attributed request beside a service handler is local HTTP and does not enter the RPC contract. The request owns route, verb, binding, and operation metadata; handlers own behavior. Configure `WebApiGenerationConfig` in each request-owning assembly: Protocol selects its RPC targets and the service API provides its local `DomainName`. Read [protocol and contracts](references/02-protocol-platform-and-service-contracts.md) before changing this boundary and use `$monica-infra-web` for consumer setup.

Keep persistence entities and repositories inside the owning service. Cross-service collaborators consume deliberate Protocol contracts rather than another service's implementation. Read [service boundaries](references/03-service-boundaries-and-collaboration.md) for coordination choices. Use `$monica-application-project-unit-development` for handlers and domain units, and `$monica-infra-persistence`, `$monica-infra-messaging`, or `$monica-infra-configuration` for those infrastructure capabilities.
