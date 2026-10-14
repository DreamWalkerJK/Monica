using Monica.AI.KnowledgeBase.Models;
using Monica.AI.RAG.Models;
using MudBlazor;

namespace Monica.AI.UI.UIRAG.State;

public sealed partial class RAGManagePageState
{
    /// <summary>
    /// Check whether one queue row can be indexed now.
    /// </summary>
    public bool CanStartDocumentIndexing(DocumentQueueItem document)
        => SelectedKnowledgeBase is not null
           && HasEmbeddingBinding(SelectedKnowledgeBase)
           && document.Status is DocumentStatus.Pending or DocumentStatus.Error
           && !HasActiveQueueWork
           && !HasRagSupportActionInFlight;

    /// <summary>
    /// Toggles one queue row in the batch-indexing selection.
    /// </summary>
    public void ToggleDocumentSelection(DocumentQueueItem document, bool isSelected)
    {
        if (_session.IsRetired) return;
        if (document.Status is not (DocumentStatus.Pending or DocumentStatus.Error))
        {
            SelectedDocumentIds.Remove(document.Id);
            NotifyStateChanged();
            return;
        }

        if (isSelected)
        {
            SelectedDocumentIds.Add(document.Id);
        }
        else
        {
            SelectedDocumentIds.Remove(document.Id);
        }

        NotifyStateChanged();
    }

    /// <summary>
    /// Selects all pending or failed queue rows.
    /// </summary>
    public void SelectAllIndexableDocuments()
    {
        if (_session.IsRetired) return;
        SelectedDocumentIds.Clear();
        foreach (var document in DocumentQueue.Where(static item => item.Status is DocumentStatus.Pending or DocumentStatus.Error))
        {
            SelectedDocumentIds.Add(document.Id);
        }

        NotifyStateChanged();
    }

    /// <summary>
    /// Clears document selection.
    /// </summary>
    public void ClearDocumentSelection()
    {
        if (_session.IsRetired) return;
        SelectedDocumentIds.Clear();
        NotifyStateChanged();
    }

    private void RemoveUnavailableDocumentSelections()
    {
        var availableDocumentIds = DocumentQueue
            .Where(static document => document.Status is DocumentStatus.Pending or DocumentStatus.Error)
            .Select(static document => document.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        SelectedDocumentIds.RemoveWhere(id => !availableDocumentIds.Contains(id));
    }

    private void OnBatchIndexingProgress(string knowledgeBaseId, long generation)
    {
        if (_session.IsRetired || generation != _session.Generation
            || !string.Equals(SelectedKnowledgeBase?.Id, knowledgeBaseId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _session.StartBackground(() => _queuePollingState.RefreshFromProgressAsync(knowledgeBaseId));
    }

    private void EnsureQueuePolling()
    {
        if (_session.IsRetired) return;
        if (SelectedKnowledgeBase is null)
        {
            _queuePollingState.Stop();
            return;
        }

        _queuePollingState.EnsurePolling(SelectedKnowledgeBase.Id, () => HasActiveQueueWork);
    }

    private bool EnsureEmbeddingModelConfiguredForIndexing()
    {
        if (SelectedKnowledgeBase is null)
        {
            return false;
        }

        if (HasEmbeddingBinding(SelectedKnowledgeBase))
        {
            return true;
        }

        _snackbar.Add(_localizer["RAG:EmbeddingModel:BindingRequired"], Severity.Warning);
        return false;
    }

    private static bool IsCancellationMessage(string? message)
        => !string.IsNullOrWhiteSpace(message)
           && message.Contains("cancel", StringComparison.OrdinalIgnoreCase);
}
