# Health, logging, timing, and metrics

## Health probes

```csharp
builder.AddMonica(monica =>
{
    monica.AddHealthCheck(options =>
    {
        options.ReadinessPath = "/health";
        options.LivenessPath = "/alive";
    });
});
```

`AddHealthCheck()` registers the native ASP.NET Core health-check service and a sanitized `HealthCheckFacade`. On Web hosts it maps anonymous readiness (`/health`) and liveness (`/alive`) endpoints by default. Ready- and live-tagged checks are filtered separately; the built-in `monica.self` check carries both tags. Healthy and degraded responses are HTTP 200, unhealthy is HTTP 503, with a plain-text status body. Generic hosts retain the health service without endpoints. Paths must be distinct absolute application paths other than `/`, without query or fragment. Disable either endpoint via its `Enable*Endpoint` option if the host already owns that probe contract.

`AddHealthCheckUI()` supplies a dashboard for the health snapshot and declares its health-check, localization, and shell dependencies. It does not change readiness or liveness semantics. Use `$monica-infra-ui` for shell hosting and access policy.

## Logging and request timing

`AddLogging()` installs Monica's Serilog logging at builder composition time, clears existing logging providers, and adds the configured Serilog provider. Console and file sinks are enabled by default. Set `EnableFileSink = false` if the host does not want a local file. `AddRequestResponseLoggingMiddleware()` is an opt-in Web-only feature; it makes logging part of the ASP.NET Core pipeline. Decide deliberately whether request and response bodies should be logged before enabling it.

`AddExecutionTiming()` attaches an execution behavior to business-operation descriptors in the shared execution pipeline. Inline aggregation is the default and makes statistics immediately visible. `UseBackgroundBatchAggregation()` reduces hot-path work and declares the hosted-service dependency; its flush interval defaults to 250 ms. Web hosts can expose `/execution-timing/statistics` and `/execution-timing/running` when the common minimal-API switch is enabled. A generic host still collects timing without those routes. Use `ExecutionTimingFacade` from application code or the UI module for inspection; avoid treating a diagnostic route as the only access path.

## Metrics export versus local inspection

```csharp
builder.AddMonica(monica =>
{
    monica.AddOpenTelemetry()
        .UseOtlpExporter();
});
```

`AddOpenTelemetry()` wires metrics, the `Monica.*` meter pattern by default, and ASP.NET Core, HttpClient, and runtime instrumentation. It does not enable an exporter by default. Choose `UseOtlpExporter(endpoint?)` for an external collector; without an explicit endpoint the OpenTelemetry SDK uses its environment configuration. `UsePrometheusEndpoint(path: "/metrics")` adds a Web scraping endpoint. `UseConsoleExporter()` is useful locally. `UseInProcessCollector()` enables a bounded per-instance snapshot and, on Web hosts with the minimal-API switch, `/opentelemetry/snapshot`; this is not durable or cross-instance storage. `AddMeter(pattern)` subscribes additional names or wildcard patterns. The exporter and in-process collector serve different retention needs and can coexist.

The following compact composition is valid for a Web application:

```csharp
monica.AddHealthCheck();
monica.AddLogging(options => options.EnableFileSink = false);
monica.AddExecutionTiming().UseBackgroundBatchAggregation();
monica.AddOpenTelemetry().UsePrometheusEndpoint().UseInProcessCollector();
```

For module graph and option diagnostics, add `AddModuleSystem()` and consume `ModuleDiagnosticsFacade`. For hosted-service state and heartbeat history, add `AddHostedService()` and use its registry or dashboard. These report different state from health probes; add ready/live checks that reflect the dependencies your application actually needs.

## Source checks

Verify changing API details in `Monica.HealthCheck/Modules/ModuleHealthCheck.cs`, `Monica.Logging/Modules/ModuleLogging.cs`, `Monica.Profiling/Modules/ModuleExecutionTiming.cs`, and `Monica.OpenTelemetry/Modules/ModuleOpenTelemetry.cs`. The reference application `Program.cs` composes health probes and Prometheus metrics.
