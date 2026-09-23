---
name: monica-infra-messaging
description: Use when integrating Monica EventBus, RPC clients, DataChannel pipelines, or SignalR hubs and diagnosing their transport, delivery, and provider configuration.
---

# Monica messaging

First identify the communication shape: EventBus publishes typed local or distributed events; RPC calls a request-owned remote contract; DataChannel builds host-owned bidirectional pipelines; SignalR sends to connected web clients. Read [the usage reference](references/usage.md) for the selected shape's registration, example, and failure semantics. The reference covers ordinary usage; inspect the linked source and tests for custom transports, version-specific options, or behavior that differs from the guide.

Keep transport selection explicit. A local EventBus does not provide cross-process delivery. A distributed EventBus needs a provider; a no-op provider is only for tests or hosts that intentionally do not send externally. Do not equate successful outbox staging with transport delivery. DataChannel and SignalR need a web host. The RPC client's gRPC selection is reserved and currently fails validation.

For module implementation work, also use `$monica-development`. For a request-owned RPC contract, use `$monica-application-project-unit-development` to place the application boundary.
