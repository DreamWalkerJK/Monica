using Monica.AI.Chat.Models;

namespace Monica.AI.Chat.Services;

/// <summary>Conservative text-only estimate used when a provider has not reported the next request's usage.</summary>
internal static class ChatContextEstimator
{
    internal static int Estimate(IEnumerable<ChatContextMessage> messages, string? instructions = null,
        IEnumerable<ChatToolSchema>? tools = null)
    {
        long count = EstimateText(instructions);
        foreach (var message in messages)
        {
            count += 4;
            foreach (var part in message.Parts)
            {
                count += EstimateText(part.Text);
                if (part.Data is { } data) count += EstimateText(data.GetRawText());
            }
        }
        if (tools is not null)
            foreach (var tool in tools) count += EstimateText(tool.Name) + EstimateText(tool.Description) + EstimateText(tool.Parameters?.GetRawText());
        return (int)Math.Min(count, int.MaxValue);
    }

    private static int EstimateText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        // ASCII averages around four characters per token; non-ASCII receives a conservative one-rune allowance.
        var ascii = 0;
        var nonAscii = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            if (rune.IsAscii) ascii++;
            else nonAscii++;
        }
        return (ascii + 3) / 4 + nonAscii;
    }
}
