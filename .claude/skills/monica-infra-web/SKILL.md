---
name: monica-infra-web
description: Compose Monica Web API, generated controllers, AutoModel, Swagger, authentication, authorization, CORS, and ProjectUnit HTTP integration. Use for host-facing API setup and route or execution-pipeline diagnosis.
---

# Monica Web integration

Read [references/usage.md](references/usage.md) to choose the Web API bundle or individual modules and to understand the route, authentication, and execution boundaries. Complete Web composition through `UseMonica()` then `MapMonica()` after building the application.

Use `$monica-application-project-unit-development` to author ProjectUnit request endpoints and application services. Use `$monica-infra-hosting` for general graph setup, `$monica-infra-observability` for probes and metrics, and `$monica-development` when implementing a new Web module.
