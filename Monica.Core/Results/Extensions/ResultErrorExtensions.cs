using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Monica.Core.Results.Abstractions;

namespace Monica.Core.Results;

/// <summary>Accesses the single typed public error stored in result metadata.</summary>
public static class ResultErrorExtensions
{
    /// <summary>Replaces the reserved error member rather than creating numbered duplicate errors.</summary>
    public static T SetError<T>(this T result, ResultError error) where T : IResultEnvelope
    {
        ArgumentNullException.ThrowIfNull(error);
        return result.SetMetadata("error", error);
    }

    /// <summary>Reads an in-process or deserialized error using the host's canonical serializer options.</summary>
    /// <returns>False when the member is absent or is not the typed public error contract.</returns>
    public static bool TryGetError(this IResultEnvelope result, JsonSerializerOptions options,
        [NotNullWhen(true)] out ResultError? error)
    {
        error = null;
        if (result.Metadata is not IDictionary<string, object?> metadata ||
            !metadata.TryGetValue("error", out var value)) return false;
        if (value is ResultError typed)
        {
            error = typed;
            return true;
        }

        if (value is not JsonElement { ValueKind: JsonValueKind.Object } element) return false;
        try
        {
            error = element.Deserialize<ResultError>(options);
            return error is not null;
        }
        catch (JsonException) { return false; }
        catch (ArgumentException) { return false; }
    }
}
