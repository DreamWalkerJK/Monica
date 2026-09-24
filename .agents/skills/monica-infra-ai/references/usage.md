# AI runtime usage

## Chat and providers

`AddAI()` registers provider discovery, chat sessions, streaming, and the host-facing `ChatFacade`. Add an actual provider with `AddOpenAIProvider(...)`, `AddAnthropicProvider(...)`, `AddFakeProvider(...)`, or `AddProvider<TProvider>(...)`. The built-in OpenAI and Anthropic registrations create a disabled provider with configuration errors when their API key or initialization is invalid; they do not make a failed remote integration appear usable. Inspect `ProviderFacade` or `ChatFacade.GetProviders()` when diagnosing a provider that is visible but disabled. `ChatFacade.CreateSessionAsync(...)` returns `Res<ChatSession>`, and `SendMessageStreamingAsync(...)` emits `Res<ChatStreamEvent>`; check `IsFailed` before consuming data or stream events.

```csharp
builder.AddMonica(monica =>
{
    monica.AddAI()
        .AddOpenAIProvider(options =>
        {
            options.ApiKey = builder.Configuration["AI:ApiKey"] ?? "";
            options.SupportedModels = ["gpt-4o-mini"];
        });
});
```

The host owns secret loading and model choice. `gpt-4o-mini` is included in Monica's current reserved model catalog; a custom model name must first be registered with `AddModel(...)` or the provider reports it as missing. `AddAIEndpoints().MapAIEndpoints()` is optional and maps stateless provider discovery under `/ai/providers` by default on a Web host; it is not the chat streaming API. Chat history persists by default through `FileChatHistoryProvider` under `monica_data/ai` (`ModuleAIOption.StorageRootPath`, relative to the process working directory unless set to an absolute writable path). `UseChatHistoryProvider<TProvider,TPartitionResolver>()` replaces that default; custom providers must still isolate partitions by caller identity and secure their persisted agent state. Persistence is keyed by the trusted identity from `IChatUserIdentityAccessor`: the AI UI replaces the default HTTP accessor with a Blazor one that resolves the authenticated circuit principal, and hosts without authentication share one workspace partition — browser-generated IDs are never identities. `ChatHistoryFacade` is the host-facing history catalog: load, save, and delete sessions, pin or archive conversations, and clear archived sessions.

## Knowledge bases and RAG

`AddKnowledgeBase()` registers knowledge-base and document inventory facades, with file-backed document-index state and source-content stores by default. The defaults live under `monica_data/rag/` relative to the running application directory. Its dependency graph includes the skill system and Markdown services. Use the provider registration extensions only when those default stores do not suit the host.

`AddRAG()` adds chunking, indexing, vector search, and RAG facades. It requires a vector store choice: `UseVectorStoreInMemoryProvider()` for local development, `UseVectorStoreQdrantProvider(...)` for Qdrant, or `UseVectorStoreProvider<TVectorStore>()` for a custom store. Leaving that feature unsatisfied fails composition. Embedding model selection is per knowledge base, so configure an AI provider and matching embedding model for real indexing. `AddFakeEmbeddingsModel(...)` supports development and tests through the normal AI pipeline. `RAGSearchFacade.SearchAsync(...)` is the application-facing retrieval API. In-memory vectors are process-local and disappear on restart; document metadata/source files do not turn them into durable vectors.

```csharp
builder.AddMonica(monica =>
{
    monica.AddRAG()
        .UseVectorStoreInMemoryProvider()
        .AddFakeEmbeddingsModel();
});
```

## Runtime skills and MCP

`AddAISkillSystem()` discovers concrete Monica `Skill` subclasses in the configured type-discovery scope and exposes them to Agent Framework. `AddFileSkills(path)` discovers file skills from one skill directory or a package root, searching up to two levels deep. Without a script runner, scripts are discoverable but fail if invoked; `AddFileSkillsWithSubprocessRunner(path)` opts into local subprocess execution. `AddReadOnlyFileAccessRoot(name, path)` authorizes one explicit root for the built-in read-only file access skill; duplicate names fail during configuration. Restrict discovered assemblies and roots to intended capabilities.

`AddMcp()` discovers Monica `McpServer` classes and can host their configured HTTP or stdio transports; it also catalogs external MCP clients. HTTP server paths use `/mcp/{serverName}` by default, with stateless Streamable HTTP. `ConfigureMcpHttpEndpoint(...)` changes the base path, display URL, or statefulness. `RequireHttpAuthorization(policyName)` applies an existing ASP.NET Core authorization policy to hosted HTTP MCP endpoints; the host must provide authentication and authorization middleware. `AddMcpClient(name, description, endpoint, ...)` adds an external HTTP server to the agent capability catalog. MCP is not automatically an HTTP endpoint merely because `AddMcp()` is registered: a discovered server must select HTTP transport.

`ModuleMcpOption.ExposeToolErrorDetail` defaults to `true`, exposing exception type and message chain to clients on unhandled tool errors. Disable it when clients cross a less trusted boundary. External MCP profile state defaults to `monica_data/ai/external_mcp_clients.json`; capability enablement state defaults to `monica_data/ai/capabilities_state.json`.

For packaged management pages, `AddAIUI()` brings in chat, knowledge base, the skill system, MCP, localization, and the shell. `AddKnowledgeBaseUI()` provides knowledge inventory; `AddRAGUI()` adds indexing/search pages and requires an explicit RAG vector-store selection. These modules are UI entry points, not replacements for the provider or persistence choices above. See `$monica-infra-ui` for shell setup and operational page access.

## Source checks

The current contracts are in `Monica.AI/Modules/ModuleAI.cs`, `ModuleAIEndpoints.cs`, `ModuleKnowledgeBase.cs`, `ModuleRAG.cs`, `ModuleSkillSystem.cs`, and `ModuleMcp.cs`. Facade surfaces live under `Monica.AI/Facades`, `Monica.AI/KnowledgeBase/Facades`, and `Monica.AI/RAG/Facades`. For changes in Microsoft Agent Framework or MCP SDK behavior, verify the exact package version through primary documentation or dependency source.
