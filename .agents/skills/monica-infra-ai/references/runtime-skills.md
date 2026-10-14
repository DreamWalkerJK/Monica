# Skills available to chat agents

Register `AddAISkillSystem()` when chat agents should discover Monica `Skill<TSelf>` classes or file-based Agent Framework skills. The module brings in `AddAI()` and XML documentation infrastructure. A class skill remains inert until its concrete type is in the host's type-discovery scope and its required modules are loaded. Keep that scope limited to assemblies whose capabilities the host intends to expose; the skill definition's `IsEnabled` and `RequiredModules` can further constrain startup discovery.

For file skills, pass one skill directory containing `SKILL.md` or a package root to `AddFileSkills(path)`; discovery searches up to two levels deep. Skill instructions, resources, and scripts remain inspectable without a runner, but script invocation fails until an appropriate runner is registered. `AddFileSkillsWithSubprocessRunner(path)` deliberately enables local script execution, so use it only for trusted roots. The built-in read-only file access skill requires an explicit `AddReadOnlyFileAccessRoot(name, path)`; paths may be absolute or relative to the running application directory, and duplicate root names fail configuration.

```csharp
builder.AddMonica(monica =>
{
    monica.AddAISkillSystem()
        .AddFileSkills("agent-skills")
        .AddReadOnlyFileAccessRoot("handbook", "docs/handbook");
});
```

This registration permits discovery and read-only file access; it does not run file-skill scripts. When capabilities appear missing, check the module graph, type-discovery scope or file-root depth, author `IsEnabled`, required modules, and runtime capability switches. Runtime switches cannot register an undiscovered class, grant a new filesystem root, repair an inaccessible source, or enable an unregistered script runner.

The packaged `AddAIUI()` includes the skill system and opens the Skills catalog directly at `/ai/capabilities`, alongside providers and models at `/ai-providers` and MCP at `/ai/capabilities/mcp`. The Skills catalog lets operators inspect real instructions, resources, tool definitions, registered file roots, and source diagnostics without executing a script or tool. Search and source/availability filters change the view only. A source with no script runner can still supply usable instructions and resources; inspect the reported runner and source issues rather than treating it as an empty skill.

`AgentCapabilityFacade.GetManagementInfoAsync()` returns registered skill and MCP entries plus catalog settings. `SetCatalogEnabledAsync(...)` persists the independent Skills or MCP catalog gate; `SetEntryEnabledAsync(...)` persists the named entry gate. Disabling a catalog preserves individual entry choices. Use each returned management result as authoritative state after a mutation, and retain the last successful view with its error when refresh fails. Refreshing this view does not promise a new connectivity test of every MCP endpoint; [MCP](mcp.md) owns those checks.

`AgentCapabilityEntryInfo.IsEntryEnabled` is the persisted entry choice. `IsEnabled` is effective availability after `IsBuiltInEnabled`, `IsCatalogEnabled`, `IsEntryEnabled`, and `DisabledReason` have been evaluated. Required-module, author-disable, source, and discovery failures can keep an entry unavailable even when its runtime switch is on. Show the actual disabled reason alongside these separate facts; turning on an entry does not establish healthy discovery or repair a missing prerequisite. These settings are host-wide agent exposure preferences, not controls that stop an MCP server process.

The default file capability store writes `capabilities.json` beneath `ModuleAIOption.StorageRootPath`, giving `monica_data/ai/capabilities.json` with the default root. This file is independent of conversation history partitions. Set an absolute writable storage root when the process working directory varies.

A `Skill<TSelf>` can declare `McpServerDefinition` so its annotated tools can also be exposed through MCP. `AgentCapabilityFacade.SetSkillMcpServerEnabledAsync(...)` persists that separate exposure choice only for a skill with server metadata. Endpoint exposure may require host restart; it is independent of the skill's in-process runtime enablement and of inclusion in the MCP agent-tool catalog. A skill-backed MCP entry may be deliberately excluded from Monica-agent tools to avoid exposing the same local tools twice. See [MCP](mcp.md) for endpoint materialization, transport, and authorization. The packaged UI still needs a chat provider for chat turns.

Source and checks: `Monica.Core/Skills/Skill.cs`, `Monica.AI/Modules/ModuleSkillSystem.cs`, `Monica.AI/Facades/AgentCapabilityFacade.cs`, `Monica.AI/AgentCapabilities/Models/AgentCapabilityEntryInfo.cs`, and `Monica.AI/AgentCapabilities/Services/FileAgentCapabilityStateStore.cs` with `tests/Test.Monica.AI/AgentCapabilities/Services/FileAgentCapabilityStateStoreTests.cs`. Verify Microsoft Agent Framework file-skill behavior against the exact package version when changing discovery or runner assumptions.
