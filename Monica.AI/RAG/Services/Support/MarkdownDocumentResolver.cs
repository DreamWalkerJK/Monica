using Monica.Markdown.Abstractions;
using Monica.Markdown.Models;

namespace Monica.AI.RAG.Services.Support;

/// <summary>
/// Resolves markdown documents within their recorded source group.
/// </summary>
internal sealed class MarkdownDocumentResolver(IMarkdownDocumentCatalog markdownService)
{
    /// <summary>
    /// Returns the matching document only from the recorded group. Missing group metadata or a missing path
    /// returns null; an unavailable group propagates the catalog failure rather than borrowing another source.
    /// </summary>
    public async Task<MarkdownDocument?> FindByPathAsync(
        string documentPath,
        string? sourceGroupKey,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(sourceGroupKey))
        {
            return null;
        }

        var documents = await markdownService.GetDocumentsAsync(sourceGroupKey);
        ct.ThrowIfCancellationRequested();
        return documents.FirstOrDefault(doc =>
            string.Equals(doc.RelativePath, documentPath, StringComparison.OrdinalIgnoreCase));
    }
}
