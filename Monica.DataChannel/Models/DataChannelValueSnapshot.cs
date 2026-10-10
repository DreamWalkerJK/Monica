using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Monica.Tool.Extensions;

namespace Monica.DataChannel.Models;

/// <summary>A complete serialized value capture that retains no caller-owned object references.</summary>
/// <param name="TypeName">The value's runtime CLR type, or null for an absent value.</param>
/// <param name="Json">Indented JSON, including literal null for an absent value; null on capture failure.</param>
/// <param name="CaptureError">The getter or serialization failure, or null when capture succeeded.</param>
/// <remarks>Caller-owned mutable values require their own synchronization for atomic serialization.</remarks>
public sealed record DataChannelValueSnapshot(string? TypeName, string? Json, string? CaptureError)
{
    private static readonly JsonSerializerOptions SERIALIZER_OPTIONS = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    internal static DataChannelValueSnapshot Capture(object? value)
    {
        var type = value?.GetType();
        try
        {
            return new(type?.GetCleanFullName(), JsonSerializer.Serialize(value, type ?? typeof(object), SERIALIZER_OPTIONS), null);
        }
        catch (Exception exception)
        {
            return new(type?.GetCleanFullName(), null, exception.Message);
        }
    }

    internal static DataChannelValueSnapshot Capture(Func<object?> readValue)
    {
        try { return Capture(readValue()); }
        catch (Exception exception) { return new(null, null, exception.Message); }
    }
}
