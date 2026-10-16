using System.Security.Cryptography;
using System.Text;
using Monica.AI.Chat.Models;

namespace Monica.AI.Chat.Providers;

internal static class ChatStoragePaths
{
    public static string Partition(ChatHistoryPartition partition)
        => Path.Combine("conversations", Hash(partition.Key));

    public static string Manifest(ChatHistoryPartition partition)
        => Path.Combine(Partition(partition), "catalog.json");

    public static string Session(ChatHistoryPartition partition, string sessionId)
        => Path.Combine(Partition(partition), "sessions", Hash(sessionId));

    public static string Attachment(ChatHistoryPartition partition, string sessionId, string attachmentId)
        => Path.Combine(Session(partition, sessionId), "attachments", Hash(attachmentId));

    private static string Hash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
