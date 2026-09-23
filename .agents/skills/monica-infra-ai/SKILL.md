---
name: monica-infra-ai
description: Configure and use Monica AI providers, chat, knowledge bases, RAG, runtime skills, and MCP in an application. Use when composing AI capabilities or diagnosing their provider and storage choices.
---

# Monica AI integration

Read [references/usage.md](references/usage.md) for the separate registration paths: chat and provider discovery, knowledge inventory, vector search, class or file skills, and MCP. Select the smallest capability graph that serves the application. Module dependencies add underlying services but do not choose a vector provider or an AI provider for you.

Use `$monica-infra-ui` when enabling the AI management pages; use `$monica-infra-hosting` for general host setup. For authoring business ProjectUnit behavior, use `$monica-application-project-unit-development`. For implementing a new Monica module rather than consuming these modules, use `$monica-development`.
