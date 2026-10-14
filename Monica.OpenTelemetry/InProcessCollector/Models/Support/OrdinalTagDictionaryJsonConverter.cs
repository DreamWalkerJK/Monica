using System.Text.Json;
using System.Text.Json.Serialization;

namespace Monica.OpenTelemetry.InProcessCollector.Models.Support;

/// <summary>Preserves normalized tag keys independently of the host's dictionary-key naming policy.</summary>
internal sealed class OrdinalTagDictionaryJsonConverter : JsonConverter<IReadOnlyDictionary<string, string>>
{
    // Attribute-based activation requires a public parameterless constructor even for an internal converter.
    public OrdinalTagDictionaryJsonConverter() { }

    public override IReadOnlyDictionary<string, string>? Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);

    public override void Write(
        Utf8JsonWriter writer, IReadOnlyDictionary<string, string> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var tag in value)
        {
            writer.WritePropertyName(tag.Key);
            JsonSerializer.Serialize(writer, tag.Value, options);
        }

        writer.WriteEndObject();
    }
}
