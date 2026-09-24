namespace Monica.AI.Chat.Models;

/// <summary>The supported attachment categories for the chat workbench.</summary>
public enum ChatAttachmentKind
{
    /// <summary>An image sent through the model's image-input capability.</summary>
    Image,
    /// <summary>A document sent as extracted text or supported native document input.</summary>
    Document
}

/// <summary>Identifies an immutable attachment owned by one isolated conversation.</summary>
/// <remarks>Identifiers are opaque. Read the store's canonical metadata before sending model input.</remarks>
public sealed record ChatAttachmentReference
{
    /// <summary>Opaque attachment identifier assigned by the store.</summary>
    public required string Id { get; init; }
    /// <summary>Original display filename, never used as a storage path.</summary>
    public required string FileName { get; init; }
    /// <summary>Validated MIME type used to construct model content.</summary>
    public required string MediaType { get; init; }
    /// <summary>Unencoded content length in bytes.</summary>
    public long Size { get; init; }
    /// <summary>Image or document input.</summary>
    public ChatAttachmentKind Kind { get; init; }
    /// <summary>Whether document text was extracted during upload.</summary>
    public bool HasExtractedText { get; init; }
}

/// <summary>Canonical attachment content returned to an authorized conversation caller.</summary>
/// <param name="Reference">Canonical metadata from the store.</param>
/// <param name="Data">Owned content bytes. Callers may retain them for the current operation.</param>
/// <param name="ExtractedText">Extracted document text, or null for images and non-text documents.</param>
public sealed record ChatAttachmentData(
    ChatAttachmentReference Reference,
    byte[] Data,
    string? ExtractedText = null);
