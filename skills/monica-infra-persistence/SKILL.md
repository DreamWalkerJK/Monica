---
name: monica-infra-persistence
description: Use when integrating Monica Repository, UnitOfWork, transactional domain events, outbox, or inbox in an application, or diagnosing their save and commit behavior.
---

# Monica persistence

Use this skill for application persistence backed by `Monica.Repository` and `ModuleUnitOfWork`. Read [the usage reference](references/usage.md) for registration, operation boundaries, durable events, and troubleshooting. It is sufficient for ordinary integration; open the linked implementation and tests only when a behavior is version-sensitive, a provider or context adapter is custom, or a failure contradicts the reference.

Choose the operation boundary before adding a context: ordinary write operations use a UnitOfWork context; independently managed stores use `DbContextProviderType.Default`. Stage aggregate changes through `IRepository<TEntity>` and let the outer operation commit. Treat a flush as a save inside the transaction, never as the commit. Route external delivery through outbox, and add inbox only to handlers that need durable deduplication.

When changing Monica's persistence implementation, also use `$monica-development` for module registration and result boundaries. For application ProjectUnit placement, use `$monica-application-project-unit-development`.
