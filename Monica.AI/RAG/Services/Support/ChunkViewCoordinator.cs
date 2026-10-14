using Monica.AI.KnowledgeBase.Models;
using Monica.AI.RAG.Models;
using Monica.Markdown.Abstractions;

namespace Monica.AI.RAG.Services.Support;

/// <summary>
/// Coordinates chunk views from persisted source or the document's recorded Markdown group.
/// </summary>
internal sealed class ChunkViewCoordinator(
    RAGChunkingService chunkingService,
    RAGIndexStateCoordinator indexStates,
    IMarkdownDocumentCatalog markdownService,
    MarkdownDocumentResolver markdownDocumentResolver)
{
    public async Task<DocumentChunkView> GetDocumentChunksAsync(
        string kbId,
        string documentId,
        CancellationToken ct = default)
    {
        var indexedView = await chunkingService.GetDocumentChunkViewAsync(kbId, documentId, ct);
        if (indexedView is not null)
        {
            return indexedView;
        }

        var state = await indexStates.GetDocumentStateRequiredAsync(kbId, documentId, ct);
        // Only recorded Markdown inventory may fall back to the catalog. Uploaded and unknown sources
        // must never inherit same-path content or provenance from another document.
        var markdownDocument = string.Equals(state.SourceKind, KnowledgeDocumentSourceKinds.Markdown, StringComparison.OrdinalIgnoreCase)
            ? await markdownDocumentResolver.FindByPathAsync(documentId, state.SourceGroupKey, ct)
            : null;
        if (markdownDocument is null)
        {
            throw new FileNotFoundException(
                $"Document '{documentId}' has no available source content for chunk preview.");
        }

        var originalText = await markdownService.GetDocumentContentAsync(markdownDocument);
        var previewView = await chunkingService.BuildPreviewAsync(
            kbId,
            markdownDocument.RelativePath,
            markdownDocument.Title,
            originalText,
            sourceKind: state.SourceKind,
            sourceGroupKey: state.SourceGroupKey,
            ct: ct);

        return previewView;
    }
}
