using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Monica.AI.Chat.Abstractions;

namespace Monica.AI.UI.UIChat.Support;

/// <summary>
/// Resolves the authenticated circuit principal for conversation isolation. Hosts without authentication
/// intentionally share the configured workspace partition; browser-generated IDs are never identities.
/// </summary>
public sealed class BlazorChatUserIdentityAccessor(IServiceProvider services) : IChatUserIdentityAccessor
{
    /// <inheritdoc />
    public async ValueTask<ClaimsPrincipal?> GetUserAsync(CancellationToken ct = default)
    {
        if (services.GetService<AuthenticationStateProvider>() is { } authentication)
            return (await authentication.GetAuthenticationStateAsync().WaitAsync(ct)).User;
        return services.GetService<IHttpContextAccessor>()?.HttpContext?.User;
    }
}
