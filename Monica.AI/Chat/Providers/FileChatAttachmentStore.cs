using Microsoft.Extensions.Options;
using Monica.AI.Chat.Abstractions;
using Monica.AI.Chat.Models;
using Monica.AI.Chat.Services;
using Monica.AI.Storage.Providers;
using Monica.Modules;

namespace Monica.AI.Chat.Providers;

internal sealed class FileChatAttachmentStore(
    AIFileStore files,
    IChatDocumentExtractor documentExtractor,
    IOptions<ModuleAIOption> options) : IChatAttachmentStore
{
    private static readonly IReadOnlyDictionary<string, string> MEDIA_TYPES = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg",
        [".webp"] = "image/webp", [".gif"] = "image/gif", [".pdf"] = "application/pdf",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".txt"] = "text/plain", [".md"] = "text/markdown", [".csv"] = "text/csv",
        [".json"] = "application/json", [".xml"] = "application/xml",
        [".yaml"] = "application/yaml", [".yml"] = "application/yaml", [".log"] = "text/plain"
    };

    public async Task<ChatAttachmentReference> SaveAsync(
        ChatHistoryPartition partition, string sessionId, string fileName, string mediaType, Stream content,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);
        var displayName = Path.GetFileName(fileName.Replace('\\', '/'));
        if (displayName.Length > 255 || !MEDIA_TYPES.TryGetValue(Path.GetExtension(displayName), out var canonicalType))
        {
            throw new NotSupportedException("Choose a PNG, JPEG, WEBP, GIF, PDF, DOCX, or UTF-8 text document.");
        }

        // The extension allowlist determines the persisted type; browser MIME values are only advisory.
        _ = mediaType;
        await using var buffer = new MemoryStream();
        var block = new byte[65536];
        int read;
        while ((read = await content.ReadAsync(block, ct)) != 0)
        {
            if (buffer.Length + read > options.Value.MaxChatAttachmentBytes)
            {
                throw new InvalidDataException("The attachment exceeds the configured size limit.");
            }

            await buffer.WriteAsync(block.AsMemory(0, read), ct);
        }

        if (buffer.Length == 0) throw new InvalidDataException("The attachment is empty.");
        var bytes = buffer.ToArray();
        ValidateSignature(canonicalType, bytes);
        var reference = new ChatAttachmentReference
        {
            Id = Guid.NewGuid().ToString("N"), FileName = displayName, MediaType = canonicalType,
            Size = bytes.LongLength,
            Kind = canonicalType.StartsWith("image/", StringComparison.Ordinal)
                ? ChatAttachmentKind.Image : ChatAttachmentKind.Document
        };
        var text = reference.Kind == ChatAttachmentKind.Document
            ? await documentExtractor.ExtractAsync(new ChatAttachmentData(reference, bytes), ct) : null;
        if (reference.Kind == ChatAttachmentKind.Document && text is null && canonicalType != "application/pdf")
        {
            throw new InvalidDataException("The document has no readable text. Native document input is available for PDF files.");
        }
        reference = reference with { HasExtractedText = text is not null };
        var payload = new ChatAttachmentData(reference, bytes, text);
        await files.WithLockAsync(ChatStoragePaths.Manifest(partition), async token =>
        {
            // Recheck at publication: another tab may archive or delete the conversation while a file is uploading.
            var catalog = await FileChatCatalog.ReadAsync(files, partition, token);
            catalog.RequireActiveSession(sessionId);
            await files.WriteAsync(AttachmentPath(partition, sessionId, reference.Id), payload, token);
            return true;
        }, ct);
        return reference;
    }

    public async Task<ChatAttachmentData> ReadAsync(ChatHistoryPartition partition, string sessionId,
        string attachmentId, CancellationToken ct = default)
    {
        var data = await files.ReadAsync<ChatAttachmentData>(AttachmentPath(partition, sessionId, attachmentId), ct)
                   ?? throw new FileNotFoundException("The attachment does not exist in this conversation.");
        if (data.Reference.Id != attachmentId || data.Reference.Size != data.Data.LongLength)
        {
            throw new InvalidDataException("The persisted attachment metadata is inconsistent.");
        }

        return data;
    }

    public async Task DeleteAsync(ChatHistoryPartition partition, string sessionId,
        string attachmentId, CancellationToken ct = default)
    {
        await files.WithLockAsync(ChatStoragePaths.Manifest(partition), async token =>
        {
            var catalog = await FileChatCatalog.ReadAsync(files, partition, token);
            var session = await catalog.LoadSessionAsync(files, partition, sessionId, token);
            if (session is not null && ChatSnapshotAttachments.Enumerate(session).Any(reference => reference.Id == attachmentId))
            {
                throw new InvalidOperationException("An attachment retained by a conversation cannot be deleted separately.");
            }

            token.ThrowIfCancellationRequested();
            File.Delete(files.GetPath(AttachmentPath(partition, sessionId, attachmentId)));
            return true;
        }, ct);
    }

    private static string AttachmentPath(ChatHistoryPartition partition, string sessionId, string attachmentId)
        => ChatStoragePaths.Attachment(partition, sessionId, attachmentId) + ".json";

    private static void ValidateSignature(string mediaType, byte[] bytes)
    {
        var valid = mediaType switch
        {
            "image/png" => bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            "image/jpeg" => bytes.AsSpan().StartsWith(new byte[] { 255, 216, 255 }),
            "image/gif" => bytes.AsSpan().StartsWith("GIF87a"u8) || bytes.AsSpan().StartsWith("GIF89a"u8),
            "image/webp" => bytes.Length >= 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            "application/pdf" => bytes.AsSpan().StartsWith("%PDF-"u8),
            _ => true
        };
        if (!valid) throw new InvalidDataException("The attachment content does not match its file type.");
    }
}
