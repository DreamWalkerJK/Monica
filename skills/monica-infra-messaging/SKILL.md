---
name: monica-infra-messaging
description: Use when integrating Monica EventBus, RPC clients, DataChannel pipelines, or SignalR hubs and diagnosing their transport, delivery, and provider configuration.
---

# Monica messaging

Choose the communication boundary before the transport; database-coupled delivery also needs the persistence skill's [transactional events](../monica-infra-persistence/references/transactional-events.md).

| Task | Read |
| --- | --- |
| Notify typed handlers within one process or across processes | [EventBus](references/event-bus.md) |
| Call another application's published request contract and await a result | [RPC client](references/rpc.md) |
| Recover structured remote failures from Dapr actor calls | [Dapr diagnostics](references/dapr-diagnostics.md) |
| Send through a named bidirectional pipeline owned by one host | [DataChannel](references/data-channels.md) |
| Invoke typed methods on connected Web clients | [SignalR](references/signalr.md) |

For application contract placement, use [ProjectUnit development](../monica-application-project-unit-development/SKILL.md). For framework transport or module implementation, use `$monica-framework`, then `$monica-development`.

If a routed authoring skill is unavailable, use `$monica-guide` to resolve the verified Monica checkout (`source resolve --repository monica --json`) and read its canonical `skills/<name>/SKILL.md`.
