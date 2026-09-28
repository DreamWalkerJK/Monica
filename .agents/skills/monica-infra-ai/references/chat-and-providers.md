# Chat, providers, and history

Register `AddAI()` for code-driven chat, then add a real provider with `AddOpenAIProvider(...)`, `AddAnthropicProvider(...)`, or `AddProvider<TProvider>(...)`. `AddFakeProvider(...)` supports development. `AddAIUI()` adds chat and management pages plus knowledge-base inventory, skill, MCP, localization, and shell dependencies, but does not choose provider credentials, models, or deployment storage and identity. Its pages are an application interface; code callers use `ChatFacade` and the other facades directly. Register modules inside one `builder.AddMonica(...)` callback; a Web host completes Monica with `app.UseMonica()` and `app.MapMonica()`.

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

The host owns secret loading and model choice. `gpt-4o-mini` is in the reserved model catalog; register a custom model with `AddModel(...)` before naming it in `SupportedModels`, or define provider-scoped model metadata when providers use the same name differently. Give providers stable IDs if persisted sessions must continue to resolve them. Code-defined provider settings can be overridden by persisted host settings. Invalid OpenAI or Anthropic keys/initialization yield visible but disabled providers with configuration errors, so inspect `ProviderFacade` or `ChatFacade.GetProviders()` before starting a session. Invalid provider or model configuration found at startup also disables the affected provider with its reasons shown on the provider management page (the default `Disable` mode); set `ModuleAIOption.ConfigurationValidationMode = Throw` to fail startup instead. Runtime settings edits always validate strictly in both modes.

The provider management page edits host-wide model metadata. The model editor accepts exact token counts and K/M shorthand (`256K` = 262,144; `1M` = 1,048,576), shows the exact value, and stores integer limits. Empty chat context uses the 256K planning fallback; it is not verified capacity. Code callers can use `TokenQuantity.TryParse` for the same input contract. See `tests/Test.Monica.AI/Configuration/TokenQuantityTests.cs` for fractions, overflow, and lossless formatting.

Capability checkboxes preserve three states: supported, unsupported, and unknown. Native document support is separate from text extraction. Select only endpoint-supported reasoning levels; Auto omits a default effort override. Advanced mappings retain custom level IDs, provider values, and Anthropic budgets. Fetch metadata fills empty fields without replacing explicit choices. Discovery reads enriched endpoint metadata and uses built-in templates only for trusted official endpoints; a model ID on a custom proxy does not prove capacity or capability. Monica does not synchronize ZCode's recommendation catalog.

Keep the Microsoft.Extensions.AI OpenAI adapter compatible with the OpenAI SDK: adapter 10.10.1 targets OpenAI 2.14.0. Adapter 10.10.0 references a Responses MCP approval type renamed in SDK 2.14.0 and can fail with `TypeLoadException`; upgrading only the SDK is insufficient. The production provider construction regression test covers both Chat and Responses API surfaces.

Resolve scoped `ChatFacade` from the caller's DI scope. Create a session, then consume the ordered `Res<ChatStreamEvent>` stream. Text arrives as `ChatTextDeltaEvent`; tool, reasoning, context, and completion events have separate types. A `ChatCompletedEvent` with `AwaitingApproval` means the run has paused: present the request and call `ContinueApprovalAsync(...)` with the decision, or `CancelAsync(...)`. `EditMessageAsync`, `RetryMessageAsync`, `UpdateSettings`, and `UpdateRuntimeContext` apply to subsequent execution, not a turn already in flight. Dispose the session when its owner is done.

```csharp
var created = await chat.CreateSessionAsync(ct: cancellationToken);
if (created.IsFailed(out var createError, out var session))
    throw new InvalidOperationException(createError.Message);

await using (session)
{
    await foreach (var next in chat.SendMessageStreamingAsync(session, "Summarize the order", cancellationToken))
    {
        if (next.IsFailed(out var streamError, out var update))
            throw new InvalidOperationException(streamError.Message);
        if (update is ChatTextDeltaEvent delta)
            Console.Write(delta.Text);
    }
}
```

Direct facade consumers own history writes. `ChatHistoryFacade.GetCatalogAsync()` gives the current catalog revision; pass it to `SaveSessionAsync(session, expectedRevision)` after a turn and inspect the returned `ChatHistoryWriteResult.IsPersisted`, `IsConflict`, and `FailureReason`. A stale revision applies no mutation and should trigger a catalog/session refresh before retry. `LoadSessionAsync` restores a transcript lazily; `SetPinnedAsync`, `SetArchivedAsync`, and `DeleteArchivedSessionsAsync` manage the catalog. The packaged AI UI manages this lifecycle for its own sessions.

For an image or document input, persist an active session first, then use `ChatAttachmentFacade.UploadAsync(sessionId, fileName, mediaType, stream)` in the same trusted partition. Send its returned `ChatAttachmentReference` as `ChatContentPart.FromAttachment(...)` inside a `ChatUserInput`; the plain-string overload is for text only. The caller owns the upload stream. `ModuleAIOption.MaxChatAttachmentBytes` defaults to 20 MiB and `MaxExtractedDocumentCharacters` to 200,000; an overlong document is rejected rather than truncated. `UseChatAttachmentStore<TStore>()` replaces the file attachment store when host storage requires it.

The default `FileChatHistoryProvider` stores history under `ModuleAIOption.StorageRootPath`, `monica_data/ai` relative to the process working directory. Set an absolute writable path when working directories vary. `UseChatHistoryProvider<TProvider,TPartitionResolver>()` replaces it; custom providers must isolate partitions and protect persisted agent state. `IChatUserIdentityAccessor` supplies trusted identity: the AI UI uses its authenticated Blazor circuit principal, while unauthenticated hosts share one workspace partition. Browser-generated IDs are not identities. `ModuleAIOption.WorkspaceId` defaults to `default` and separates host workspaces using the same storage.

`AddAIEndpoints().MapAIEndpoints()` optionally exposes provider discovery at `/ai/providers` on a Web host; it is not a chat streaming API. Source and checks: `Monica.AI/Modules/ModuleAI.cs`, `ModuleAIEndpoints.cs`, `Monica.AI/Facades/ChatFacade.cs`, `Monica.AI/Chat/Facades/ChatHistoryFacade.cs` and `ChatAttachmentFacade.cs`, `tests/Test.Monica.AI/Modules/ModuleAIChatHistoryTests.cs`, and `Monica.AI.UI/UIChat/State/ChatSessionWorkspace.cs`. Verify exact Microsoft Agent Framework behavior against the package version when changing the integration.
