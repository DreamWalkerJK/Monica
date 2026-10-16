using Microsoft.Extensions.Logging;
using Monica.AI.AgentCapabilities.Abstractions;
using Monica.AI.AgentCapabilities.Models;
using Monica.AI.Storage.Providers;

namespace Monica.AI.AgentCapabilities.Services;

/// <summary>
/// File-backed runtime state store for agent capability enablement.
/// </summary>
internal sealed class FileAgentCapabilityStateStore(
    AIFileStore files,
    ILogger<FileAgentCapabilityStateStore> logger) : IAgentCapabilityStateStore
{
    private const string STATE_PATH = "capabilities.json";

    /// <inheritdoc />
    public Task<AgentCapabilityState> LoadAsync(CancellationToken ct = default)
        => files.WithLockAsync(STATE_PATH, ReadAsync, ct);

    /// <inheritdoc />
    public Task<AgentCapabilityState> UpdateAsync(
        Func<AgentCapabilityState, bool> update,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        return files.WithLockAsync(STATE_PATH, async token =>
        {
            var state = await ReadAsync(token);
            if (!update(state))
            {
                return Normalize(state);
            }

            state.Revision = Math.Max(1, state.Revision) + 1;
            state = Normalize(state);
            await files.WriteAsync(STATE_PATH, state, token);
            logger.LogDebug("Saved agent capability state at revision {Revision}.", state.Revision);
            return state;
        }, ct);
    }

    private async Task<AgentCapabilityState> ReadAsync(CancellationToken ct)
    {
        // Corrupt enablement state must surface as a failure, never silently re-enable previously disabled tools.
        return Normalize(await files.ReadAsync<AgentCapabilityState>(STATE_PATH, ct) ?? new AgentCapabilityState());
    }

    private static AgentCapabilityState Normalize(AgentCapabilityState state)
    {
        state.Revision = Math.Max(1, state.Revision);
        state.SkillEntries = NormalizeEntries(state.SkillEntries);
        state.McpEntries = NormalizeEntries(state.McpEntries);
        state.SkillMcpServers = NormalizeEntries(state.SkillMcpServers);
        return state;
    }

    private static Dictionary<string, bool> NormalizeEntries(Dictionary<string, bool>? entries)
    {
        return new Dictionary<string, bool>(
            (entries ?? [])
            .Where(static entry => !string.IsNullOrWhiteSpace(entry.Key))
            .Select(static entry => new KeyValuePair<string, bool>(entry.Key.Trim(), entry.Value)),
            StringComparer.OrdinalIgnoreCase);
    }
}
