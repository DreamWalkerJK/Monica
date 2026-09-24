---
name: monica-infra-jobs
description: Use when adding Monica JobScheduler recurring or triggered jobs, choosing its store and scope, or configuring startup seeders and their readiness behavior.
---

# Monica jobs and seeders

Choose JobScheduler for recurring or on-demand work with execution history and a queue; choose Seeder for finite dependency-aware startup work. Read [the usage reference](references/usage.md) for registration, examples, retry/lease behavior, and troubleshooting. It is sufficient for normal host integration. Open the linked source and tests when implementing a custom store, changing execution policy, or diagnosing a result that conflicts with the reference.

For JobScheduler, always select a store and stable scheduler scope. Use the in-memory store only for standalone development or deterministic tests; select an EF Core store for durable execution across replicas. For Seeder, make the work idempotent and declare dependencies when order matters. Seeder readiness is distinct from process startup: seeders run after `ApplicationStarted`.

Use `$monica-development` as well when changing module registration or hosted-service implementation. For operational UI changes, use `$monica-ui-development`.
