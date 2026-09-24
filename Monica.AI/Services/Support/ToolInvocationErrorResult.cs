using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Monica.AI.Services.Support;

internal sealed record ToolInvocationErrorResult
{
    private static readonly JsonSerializerOptions JSON_OPTIONS = new() { PropertyNameCaseInsensitive = true };

    public string Status { get; init; } = "tool_error";

    public required string ToolName { get; init; }

    public required string CallId { get; init; }

    public required string ErrorType { get; init; }

    public required string Message { get; init; }

    public required string Instruction { get; init; }

    public IReadOnlyDictionary<string, object?>? Arguments { get; init; }

    internal static ToolInvocationErrorResult? FromResult(object? result)
    {
        if (result is ToolInvocationErrorResult error) return error;
        try
        {
            // AIFunction implementations can return the structured value directly or serialize it first.
            if (result is JsonElement { ValueKind: not JsonValueKind.String } element) return ReadObject(element);
            if (result is JsonDocument document) return ReadObject(document.RootElement);
            var text = result is JsonElement { ValueKind: JsonValueKind.String } json ? json.GetString() : result as string;
            if (string.IsNullOrWhiteSpace(text)) return null;
            using var parsed = JsonDocument.Parse(text);
            return ReadObject(parsed.RootElement);
        }
        catch (JsonException) { return null; }
    }

    private static ToolInvocationErrorResult? ReadObject(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object) return null;
        var status = value.EnumerateObject().FirstOrDefault(property => property.Name.Equals(nameof(Status), StringComparison.OrdinalIgnoreCase));
        return status.Value.ValueKind == JsonValueKind.String && status.Value.GetString() == "tool_error"
            ? value.Deserialize<ToolInvocationErrorResult>(JSON_OPTIONS) : null;
    }

    public static ToolInvocationErrorResult Create(FunctionCallContent functionCall, Exception exception)
    {
        return new ToolInvocationErrorResult
        {
            ToolName = functionCall.Name,
            CallId = functionCall.CallId ?? string.Empty,
            ErrorType = exception.GetType().Name,
            Message = exception.Message,
            Instruction =
                "The tool call failed before it completed. Inspect the error and retry with corrected arguments when possible. Do not repeat the exact same call unchanged.",
            Arguments = functionCall.Arguments is { Count: > 0 }
                ? new Dictionary<string, object?>(functionCall.Arguments)
                : null
        };
    }

    public static ToolInvocationErrorResult Create(
        string toolName,
        IDictionary<string, object?>? arguments,
        Exception exception)
    {
        return new ToolInvocationErrorResult
        {
            ToolName = toolName,
            CallId = string.Empty,
            ErrorType = exception.GetType().Name,
            Message = exception.Message,
            Instruction =
                "The script call failed before it completed. Inspect the script schema and retry with corrected arguments. Do not repeat the exact same call unchanged.",
            Arguments = arguments is { Count: > 0 }
                ? new Dictionary<string, object?>(arguments)
                : null
        };
    }
}
