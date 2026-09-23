---
name: monica-infra-observability
description: Configure and consume Monica health probes, logging, execution timing, metrics exporters, and runtime diagnostics in an application. Use monica-opentelemetry for authoring new module instruments.
---

# Monica observability in an application

Read [references/usage.md](references/usage.md) when choosing probes, logs, timing, and metrics. These are separate signals: register only the modules needed for the operational question. Check whether a diagnostic endpoint should be enabled on a Web host and whether an exporter or an in-process view needs retention beyond one process.

For implementing new `System.Diagnostics.Metrics` instruments in Monica modules, use `$monica-opentelemetry`. For module and hosted-service internals use `$monica-development`. UI dashboards are routed through `$monica-infra-ui`.
