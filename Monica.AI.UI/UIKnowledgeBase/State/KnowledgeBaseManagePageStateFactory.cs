using Microsoft.Extensions.DependencyInjection;

namespace Monica.AI.UI.UIKnowledgeBase.State;

/// <summary>Creates independent state for a routed knowledge-inventory visit.</summary>
/// <remarks>The page owns and asynchronously disposes the returned state. Facades and UI services are borrowed
/// from the current scope and remain container-owned. Dispatch must target the page's renderer.</remarks>
public sealed class KnowledgeBaseManagePageStateFactory(IServiceProvider services)
{
    /// <summary>Creates a page-owned state with renderer dispatch for notifications and native dialog closure.</summary>
    public KnowledgeBaseManagePageState Create(Func<Action, Task> dispatch)
        => ActivatorUtilities.CreateInstance<KnowledgeBaseManagePageState>(services, dispatch);
}
