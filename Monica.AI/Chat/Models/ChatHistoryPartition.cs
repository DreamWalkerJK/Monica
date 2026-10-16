using System.Security.Claims;
using System.Text.Json;

namespace Monica.AI.Chat.Models;

/// <summary>
/// Identifies the isolated storage partition used for chat history operations.
/// </summary>
/// <remarks>
/// Resolve the key from the host workspace and trusted authenticated tenant/user identity.
/// A client-supplied browser or session identifier is not an authentication boundary.
/// </remarks>
public sealed record ChatHistoryPartition
{
    /// <summary>Current serialized contract version.</summary>
    public const int CurrentVersion = 1;

    /// <summary>Creates a validated history partition.</summary>
    /// <param name="key">Provider-specific opaque partition key.</param>
    public ChatHistoryPartition(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Key = key;
    }

    /// <summary>Contract version used to create this value.</summary>
    public int Version { get; init; } = CurrentVersion;

    /// <summary>Provider-specific opaque partition key.</summary>
    public string Key { get; }

    /// <summary>
    /// Creates a workspace partition, additionally isolated by authenticated issuer, tenant, and user.
    /// An authenticated principal without a stable subject is rejected; anonymous standalone hosts
    /// share only their configured workspace. Callers must supply a trusted server principal.
    /// </summary>
    public static ChatHistoryPartition ForIdentity(string workspaceId, ClaimsPrincipal? user)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceId);
        if (user?.Identity?.IsAuthenticated != true)
        {
            return new ChatHistoryPartition(JsonSerializer.Serialize(new[] { workspaceId, "workspace" }));
        }

        var subject = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
        if (subject is null || string.IsNullOrWhiteSpace(subject.Value))
        {
            throw new InvalidOperationException("Authenticated chat users must have a stable subject identifier.");
        }

        var tenant = user.FindFirst("tid")?.Value ?? user.FindFirst("tenant_id")?.Value;
        return new ChatHistoryPartition(JsonSerializer.Serialize(
            new[] { workspaceId, "user", subject.Issuer, tenant, subject.Value }));
    }
}
