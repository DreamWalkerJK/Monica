using System.Text.Json.Serialization;

namespace Monica.Core.Results;

/// <summary>Describes a public failure using stable reason codes and safe correlation information.</summary>
public sealed record ResultError
{
    /// <summary>Creates a failure description. Field collections are copied and cannot be mutated by the caller.</summary>
    /// <exception cref="ArgumentException">The reason code or trace identifier is empty.</exception>
    [JsonConstructor]
    public ResultError(string code, string traceId, string? service = null, string? operation = null,
        IReadOnlyList<ResultFieldError>? fields = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);
        if (fields?.Any(field => field is null) is true)
            throw new ArgumentException("Field errors cannot contain null entries.", nameof(fields));
        Code = code;
        TraceId = traceId;
        Service = service;
        Operation = operation;
        Fields = fields is { Count: > 0 } ? Array.AsReadOnly(fields.ToArray()) : null;
    }

    /// <summary>Gets the stable reason code. Consumers must not parse display messages.</summary>
    public string Code { get; }
    /// <summary>Gets the nonempty identifier that correlates this failure with operational evidence.</summary>
    public string TraceId { get; }
    /// <summary>Gets the logical originating service or unavailable dependency, never its address.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Service { get; }
    /// <summary>Gets the logical operation name at the origin.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Operation { get; }
    /// <summary>Gets errors expressed in the originating request contract, when applicable.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ResultFieldError>? Fields { get; }
}
