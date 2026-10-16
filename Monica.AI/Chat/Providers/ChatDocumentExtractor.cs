using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using Monica.AI.Chat.Abstractions;
using Monica.AI.Chat.Models;
using Monica.Modules;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Monica.AI.Chat.Providers;

internal sealed class ChatDocumentExtractor(IOptions<ModuleAIOption> options) : IChatDocumentExtractor
{
    private const string WORD_NAMESPACE = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly UTF8Encoding STRICT_UTF8 = new(false, true);

    public Task<string?> ExtractAsync(ChatAttachmentData attachment, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (attachment.ExtractedText is { } existing)
        {
            return Task.FromResult<string?>(existing);
        }

        var mediaType = attachment.Reference.MediaType;
        string? text = null;
        if (mediaType == "application/pdf")
        {
            using var document = PdfDocument.Open(attachment.Data);
            var builder = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                ct.ThrowIfCancellationRequested();
                builder.AppendLine(ContentOrderTextExtractor.GetText(page));
                ValidateLength(builder.Length);
            }

            text = builder.ToString();
        }
        else if (mediaType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
        {
            using var data = new MemoryStream(attachment.Data, writable: false);
            using var archive = new ZipArchive(data, ZipArchiveMode.Read);
            var entry = archive.GetEntry("word/document.xml")
                        ?? throw new InvalidDataException("The DOCX file has no document body.");
            if (entry.Length > options.Value.MaxExtractedDocumentCharacters * 8L)
            {
                throw new InvalidDataException("The expanded document exceeds the configured text limit.");
            }

            using var stream = entry.Open();
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = options.Value.MaxExtractedDocumentCharacters * 8L
            });
            var xml = XDocument.Load(reader);
            XNamespace word = WORD_NAMESPACE;
            var builder = new StringBuilder();
            foreach (var paragraph in xml.Descendants(word + "p"))
            {
                ct.ThrowIfCancellationRequested();
                foreach (var node in paragraph.Descendants())
                {
                    if (node.Name == word + "t") builder.Append(node.Value);
                    else if (node.Name == word + "tab") builder.Append('\t');
                    else if (node.Name == word + "br") builder.AppendLine();
                }

                builder.AppendLine();
                ValidateLength(builder.Length);
            }

            text = builder.ToString();
        }
        else if (mediaType.StartsWith("text/", StringComparison.Ordinal)
                 || mediaType is "application/json" or "application/xml" or "application/yaml")
        {
            text = STRICT_UTF8.GetString(attachment.Data).TrimStart('\uFEFF');
        }

        if (text is not null) ValidateLength(text.Length);
        return Task.FromResult(string.IsNullOrWhiteSpace(text) ? null : text);
    }

    private void ValidateLength(int length)
    {
        if (length > options.Value.MaxExtractedDocumentCharacters)
        {
            throw new InvalidDataException("The document exceeds the configured extracted-text limit.");
        }
    }
}
