using Monica.Core.Results;

namespace Monica.AI.UI.UIChat.State;

public sealed partial class ChatPageState
{
    private async Task LoadCapabilityCandidatesAsync()
    {
        var result = await capabilityFacade.GetReferenceCandidatesAsync();
        if (_disposed) return;
        if (result.IsFailed(out var error, out var candidates))
        {
            SetError(error.Message ?? localizer["Error:Generic"]);
            return;
        }
        CapabilityCandidates = candidates.Where(candidate => candidate.IsEnabled).ToArray();
    }

    private async Task LoadKnowledgeBasesAsync()
    {
        if (!_options.ShowKnowledgeBaseSelector) return;
        var result = await knowledgeBaseFacade.GetAllAsync();
        if (_disposed) return;
        if (result.IsFailed(out var error, out var knowledgeBases))
        {
            SetError(error.Message ?? localizer["Error:Generic"]);
            return;
        }
        KnowledgeBases = knowledgeBases;
    }

    /// <summary>Updates knowledge retrieval tools for the next request.</summary>
    public void SetSelectedKnowledgeBases(List<string> ids)
    {
        SelectedKnowledgeBaseIds = ids;
        if (CurrentSession is { } session)
            _ = chatFacade.UpdateRuntimeContext(session, BuildRuntimeContext(ids));
        NotifyStateChanged();
    }
}
