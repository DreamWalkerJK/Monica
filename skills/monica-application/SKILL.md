---
name: monica-application
description: Router skill for Monica application development. Use when designing or implementing Monica-based DDD applications, choosing between microservice and modular monolith architecture, or creating ProjectUnit-based application features.
---

# Monica Application

Use this skill as the entry point for products and application systems built on Monica. It routes solution-level architecture work and unit-level feature implementation to the correct granular skill.

## Routing

- Exact Monica framework source needed outside the active checkout: use `$monica-guide source resolve --repository monica --json` as a read-only locator.
- Microservice solution layout, service boundaries, `Platform.Protocol`, published language, migrations, or cross-service collaboration: use `$monica-application-microservice`.
- Modular monolith layout, bounded contexts under `Domains/`, AppHost composition, cross-domain collaboration, or persistence ownership: use `$monica-application-modular-monolith`.
- Feature implementation with `ApplicationService`, `RequestDto`, `DomainService`, `Entity`, `Repository`, events, configuration, recurring jobs, or triggered jobs: use `$monica-application-project-unit-development`.
- Sociable application tests, `MonicaTestApplicationFactory`, handler or repository scenarios, database isolation, and migration away from mock-heavy tests: use `$monica-application-unit-testing`.
- Consumer setup and use of Monica infrastructure: route to `$monica-infra-persistence`, `$monica-infra-messaging`, `$monica-infra-jobs`, `$monica-infra-configuration`, `$monica-infra-hosting`, `$monica-infra-observability`, `$monica-infra-ai`, `$monica-infra-web`, or `$monica-infra-ui` for the capability in use.

## Working Rule

Start with the architecture skill when the deployment style or subdomain placement is unclear. Use `$monica-application-project-unit-development` once the target project and boundary are known.
