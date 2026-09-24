---
name: monica-infra-configuration
description: Use when integrating Monica managed configuration, bootstrap input plans, file or EF Core stores, runtime reload and validation, or the configuration operator UI.
---

# Monica managed configuration

Use an immutable `MonicaConfigurationInputPlan` when startup code and the runtime module must read the same managed store and JSON sources. Read [the usage reference](references/usage.md) for the input-plan flow, annotated options, storage, runtime mutation and reload, and UI. The reference is sufficient for ordinary integration; inspect the linked implementation and tests for a custom store, cross-process reload, schema migration, or surprising precedence.

Treat bootstrap values and later runtime values as distinct snapshots. A bootstrap read does not publish metadata or write effective values. Runtime activation projects managed values into Microsoft configuration and binds annotated options. Choose validation behavior deliberately: the default reports invalid effective values without blocking startup or options resolution; `FailFast` enforces them. Configuration stored in an EF Core store needs host-owned migrations before startup loading.

For module implementation changes, also use `$monica-development`. For configuration UI component work, use `$monica-ui-development` and `$monica-ui-localization`.
