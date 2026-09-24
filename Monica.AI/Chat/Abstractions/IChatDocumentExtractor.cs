using Monica.AI.Chat.Models;

namespace Monica.AI.Chat.Abstractions;

/// <summary>Extracts bounded model-readable text from an uploaded document.</summary>
public interface IChatDocumentExtractor
{
    /// <summary>
    /// Returns extracted text, or null when the document has no extractable text. Malformed documents
    /// and documents exceeding the configured text limit throw; content is never silently truncated.
    /// </summary>
    Task<string?> ExtractAsync(ChatAttachmentData attachment, CancellationToken ct = default);
}
