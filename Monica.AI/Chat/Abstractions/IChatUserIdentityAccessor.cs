using System.Security.Claims;

namespace Monica.AI.Chat.Abstractions;

/// <summary>Supplies the current trusted host identity for chat storage isolation.</summary>
/// <remarks>
/// Implementations are scoped to the caller and must not accept user identifiers from request bodies.
/// A null or anonymous principal selects the configured standalone workspace. Authenticated users
/// must have a stable subject claim. Blazor hosts should resolve identity from their circuit.
/// </remarks>
public interface IChatUserIdentityAccessor
{
    /// <summary>Gets the current principal, or null when the host runs without authentication.</summary>
    ValueTask<ClaimsPrincipal?> GetUserAsync(CancellationToken ct = default);
}
