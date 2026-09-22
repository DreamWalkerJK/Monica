using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Monica.AI.Chat.Services;

/// <summary>Removes credential-shaped values from the durable diagnostic surface.</summary>
internal static partial class ChatInspectionRedactor
{
    private static readonly HashSet<string> SECRET_KEYS = new(StringComparer.OrdinalIgnoreCase)
    {
        "apikey", "authorization", "password", "secret", "clientsecret", "accesstoken", "refreshtoken", "credential", "credentials"
    };

    internal static string? Redact(string? value)
        => value is null ? null : SecretAssignment().Replace(Bearer().Replace(value, "Bearer [REDACTED]"), "$1[REDACTED]");

    internal static JsonElement Redact(JsonElement value, Func<string, string>? redactSecret = null)
    {
        var node = JsonNode.Parse(value.GetRawText());
        Visit(node, redactSecret);
        return JsonSerializer.SerializeToElement(node);
    }

    private static void Visit(JsonNode? node, Func<string, string>? redactSecret)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj.ToArray())
            {
                var key = property.Key.Replace("_", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
                if (SECRET_KEYS.Contains(key)) obj[property.Key] = "[REDACTED]";
                else if (property.Value is JsonValue value && value.TryGetValue<string>(out var text)) obj[property.Key] = Redact(redactSecret?.Invoke(text) ?? text);
                else Visit(property.Value, redactSecret);
            }
        }
        else if (node is JsonArray array)
        {
            for (var index = 0; index < array.Count; index++)
            {
                if (array[index] is JsonValue value && value.TryGetValue<string>(out var text)) array[index] = Redact(redactSecret?.Invoke(text) ?? text);
                else Visit(array[index], redactSecret);
            }
        }
    }

    [GeneratedRegex(@"\bBearer\s+[A-Za-z0-9._~+/=-]+", RegexOptions.IgnoreCase)]
    private static partial Regex Bearer();
    [GeneratedRegex(@"((?:api[_-]?key|password|client[_-]?secret|access[_-]?token|authorization)\s*[:=]\s*[""']?)[^\s,""'}]+", RegexOptions.IgnoreCase)]
    private static partial Regex SecretAssignment();
}
