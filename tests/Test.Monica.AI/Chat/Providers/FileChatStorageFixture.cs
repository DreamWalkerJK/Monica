using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Monica.AI.Chat.Models;
using Monica.AI.Chat.Providers;
using Monica.AI.Models;
using Monica.AI.Storage.Providers;
using Monica.Modules;

namespace Test.Monica.AI.Chat.Providers;

internal sealed class FileChatStorageFixture : IDisposable
{
    private readonly string _parent = Path.Combine(Path.GetTempPath(), "Test.Monica.AI", "chat-storage");

    public FileChatStorageFixture()
    {
        Options = Microsoft.Extensions.Options.Options.Create(new ModuleAIOption
        {
            StorageRootPath = Path.Combine(_parent, Guid.NewGuid().ToString("N"))
        });
        Files = new AIFileStore(Options);
        Attachments = new FileChatAttachmentStore(Files, new ChatDocumentExtractor(Options), Options);
        History = new FileChatHistoryProvider(Files, Attachments, NullLogger<FileChatHistoryProvider>.Instance);
    }

    public IOptions<ModuleAIOption> Options { get; }
    public AIFileStore Files { get; }
    public FileChatAttachmentStore Attachments { get; }
    public FileChatHistoryProvider History { get; }
    public ChatHistoryPartition Partition { get; } = new("workspace/user-one");

    public FileChatHistoryProvider ReopenHistory() => new(new AIFileStore(Options), Attachments,
        NullLogger<FileChatHistoryProvider>.Instance);

    public static ChatSessionSnapshot Snapshot(string id = "session-one", ChatAttachmentReference? attachment = null)
    {
        var created = new DateTimeOffset(2026, 8, 1, 1, 0, 0, TimeSpan.Zero);
        return new ChatSessionSnapshot
        {
            SessionId = id, Title = "A durable conversation", CreatedAt = created, UpdatedAt = created,
            Settings = new ChatSessionSettings("provider-one", "model-one"),
            Turns =
            [
                new ChatTurnSnapshot
                {
                    Id = "turn-one", StartedAt = created, CompletedAt = created, Status = ChatExecutionStatus.Completed,
                    UserMessage = new ChatMessageSnapshot
                    {
                        Id = "message-one", Role = AIChatRole.User, Kind = AIChatMessageKind.Message,
                        CreatedAt = created,
                        Parts = attachment is null ? [ChatContentPart.FromText("Explain the document")]
                            : [ChatContentPart.FromText("Explain the document"), ChatContentPart.FromAttachment(attachment)]
                    }
                }
            ]
        };
    }

    public void Dispose()
    {
        var target = Path.GetFullPath(Options.Value.StorageRootPath);
        var relative = Path.GetRelativePath(_parent, target);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative) || relative == ".")
            throw new InvalidOperationException("The test directory escaped its owned temporary root.");
        if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
    }
}
