# FIPS repository redesign: independent review prompt

The user subsequently authorized the same agent to implement both repositories. This prompt supersedes the original implementation handoff.

---

Review the completed repository redesign in `D:\Code\MoLibrary` and `D:\Code\FIPS2022`, both on `codex/repository-redesign`. Read both repositories' instructions and `docs/migrations/repository-redesign.md` in Monica, plus `docs/repository-redesign.md` in FIPS.

Review operation ownership, tracked graph updates, save policies, sharding transaction participation, outbox subscriptions, per-source ordering, and consumer idempotency. Pay particular attention to independent current/history transfers, the SQL flight journal, manual-repair checkpoints and cache compare-and-swap behavior. Do not infer distributed atomicity from separate local commits.

Check the generated `RepositoryPersistenceRedesign` migrations and the schema review. No real database migration or deployment has been performed. Running `database update`, changing production data, or deployment requires separate authorization. Keep credential-bearing configuration and logs private.

Use the production-composed tests and failure-injection cases. Run one dotnet process at a time, with the .NET 10 SDK and bounded MSBuild parallelism. Distinguish SQLite/actual ShardingCore package evidence from production GaussDB runtime validation.

Report concrete defects with paths, triggering scenarios and practical consequences. Existing synchronous business RPCs and the departure/arrival processing queue retain their application-specific delivery semantics; the new repository outbox does not make arbitrary external calls transactional. Propose any further business-process redesign separately rather than silently changing response contracts.

Preserve unrelated pre-existing FIPS edits in the four Workflow skill files, `.monica/guide.json`, and `CLAUDE.md`. Do not restore the old repository wrappers, ambient context switching, or test-only save loops.
