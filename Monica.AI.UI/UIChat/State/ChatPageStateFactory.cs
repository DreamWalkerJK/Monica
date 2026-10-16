using Microsoft.Extensions.DependencyInjection;

namespace Monica.AI.UI.UIChat.State;

/// <summary>Creates component-owned workbench state using the current circuit's services.</summary>
public sealed class ChatPageStateFactory(IServiceProvider services)
{
    /// <summary>The caller owns and must asynchronously dispose the returned state.</summary>
    public ChatPageState Create() => ActivatorUtilities.CreateInstance<ChatPageState>(services);
}
