using Microsoft.Extensions.DependencyInjection;
using Monica.AI.KnowledgeBase.Facades;

namespace Monica.AI.UI.UIRAG.State;

/// <summary>Creates independent state and polling ownership for a routed RAG management visit.</summary>
/// <remarks>The page owns and asynchronously disposes the returned state. Scoped facades and UI services are
/// borrowed and remain container-owned. The factory creates a private poller for each page.</remarks>
public sealed class RAGManagePageStateFactory(IServiceProvider services)
{
    /// <summary>Creates page-owned state; dispatch must target the page's renderer.</summary>
    public RAGManagePageState Create(Func<Action, Task> dispatch)
        => ActivatorUtilities.CreateInstance<RAGManagePageState>(services,
            new RAGQueuePollingState(services.GetRequiredService<KnowledgeDocumentFacade>()), dispatch);
}
