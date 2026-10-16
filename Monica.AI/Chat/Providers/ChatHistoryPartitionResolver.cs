using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Monica.AI.Chat.Abstractions;
using Monica.AI.Chat.Models;
using Monica.Modules;

namespace Monica.AI.Chat.Providers;

internal sealed class ChatHistoryPartitionResolver(
    IOptions<ModuleAIOption> options,
    IChatUserIdentityAccessor identityAccessor) : IChatHistoryPartitionResolver
{
    public async ValueTask<ChatHistoryPartition> ResolveAsync(CancellationToken ct = default)
        => ChatHistoryPartition.ForIdentity(options.Value.WorkspaceId, await identityAccessor.GetUserAsync(ct));
}

internal sealed class HttpChatUserIdentityAccessor(IHttpContextAccessor contextAccessor) : IChatUserIdentityAccessor
{
    public ValueTask<ClaimsPrincipal?> GetUserAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return ValueTask.FromResult(contextAccessor.HttpContext?.User);
    }
}
