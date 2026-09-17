# Service-call redesign: validation record

Date: 2026-09-15  
Monica baseline: `71a816f4`  
FIPS2022 baseline: `6d526e89`  
Design: [API and Service-Call Outcomes](service-call-outcomes.md)

## Scope delivered

The working-tree implementation covers the shared public error contract, strict input rejection, HTTP/OpenAPI projection, complete remote-call ownership, generated RPC, Dapr classification, both FIPS primary-node proxy paths, and the separate DataComparison caller.

The numeric migration completed on 2026-09-17: the synthetic 451/452/453/460 statuses are removed, producers emit standard statuses with typed reason codes, and off-whitelist numbers are contract defects at API boundaries.

## Automated verification

| Suite | Result |
|---|---:|
| Test.Monica.Core | 311 passed |
| Test.Monica.WebApi | 52 passed |
| Test.Monica.Dapr | 61 passed |
| Test.Monica.Framework | 48 passed |
| Test.Monica.Generators.AutoController | 62 passed |
| Test.Platform.Shared | 235 passed |
| Test.DataExchangeService.DataComparisonAdaptor | 29 passed |
| Test.FlightRouteService.API | 27 passed |
| Total .NET tests | **825 passed** |

All listed tests use the repository's normal `dotnet test <project> --no-restore` path. The remote and API tests use host-owned scenarios, deterministic HTTP doubles, and a controllable deadline; primary-node proxy tests replace the Dapr health boundary before startup.

Meaningful regressions covered include:

- Invalid JSON/date values, null bodies, supplied invalid query/form/header/route values, and unsupported content types stop action and forwarding.
- Optional values remain optional; the existing generator suite continues covering request binding and route overlays.
- Minimal API validation uses the typed envelope only for marked Monica endpoints; unrelated endpoints retain ProblemDetails.
- OpenAPI reflects the configured `code` alias, typed error schema, required reason/trace fields, and mapped failure responses.
- Valid 200/201 null-data responses and legacy status mappings are accepted.
- Application 500 and origin error correlation survive remote hops.
- Incorrect aliases, duplicate authoritative fields, status contradictions, malformed payloads, and legacy error objects are rejected.
- Compressed response limits, lengthless response limits, large configured limits, stalled body deadlines, caller cancellation, request/response disposal, and borrowed-client timeout preservation.
- Dedicated RPC registration overrides its provider timeout without changing unrelated clients.
- Registered provider classifiers run only for the selected transport and after valid-envelope recognition.
- Worker failures have a nonempty trace shared with diagnostics; throttling retains its distinct reason and validated Retry-After.
- Public output and default remote diagnostics exclude test secrets and reserved transport dumps.
- Primary-node failures never trigger local application execution.

## Builds and repository checks

- Monica solution: `dotnet build Monica.slnx --no-restore -m` succeeds with **0 warnings and 0 errors**.
- FIPS Aspire AppHost and its System/FlightRoute service references compile successfully.
- The final incremental Aspire build reports 0 warnings and 0 errors. Earlier FIPS test builds emitted existing nullable, XML-comment, and unused-parameter warnings in consumer code; this is not a clean zero-warning claim for the entire FIPS solution.
- `git diff --check` passes for both workspaces after normalizing generated line endings.
- Canonical skill validation, projection synchronization check, and all four skill-owned test suites pass.
- Source searches in both production trees find no remaining old reader/connector references or legacy `AppendMetadata("error", ...)` producers. The reserved member is written through `SetError`.

Detailed run logs are retained in `.pending/024-service-call-contract-redesign/` in the Monica working tree. The architectural design and this report are tracked documentation; the pending folder is local evidence.

## Live environment limit

The configured local Aspire dashboard is `http://localhost:15278`. No Aspire AppHost, System/FlightRoute API, or Dapr process was running during the check; no listener was present at the configured dashboard ports. The HTTP probe did not return within its two-second deadline.

Installed Dapr CLI/runtime report **1.16.1**; the inspected Dapr.Client package is **1.18.4**. Compilation and deterministic host tests are complete. A live Aspire/Dapr exchange was **not** verified.

Before rollout, run representative generated RPC and primary-node forwarding scenarios against the target environment, check its actual sidecar/gateway retry configuration, and confirm that each deployment unit contains only the typed `metadata.error` producers. The source audit cannot certify independently built plugins or running deployment artifacts.

No commit, push, deployment, database migration, or live business request was performed.

