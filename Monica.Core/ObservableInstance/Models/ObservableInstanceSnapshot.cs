using System.Text.Json;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Monica.Tool.Extensions;

namespace Monica.Core.ObservableInstance.Models;

/// <summary>A detached value capture that never retains the caller's state object.</summary>
/// <param name="TypeName">The runtime type's full name, or null for an absent value.</param>
/// <param name="Text">The complete value text, without preview truncation.</param>
/// <param name="CaptureError">The serialization failure, if the value could not be represented.</param>
public sealed record ObservableValueSnapshot(string? TypeName, string? Text, string? CaptureError)
{
    internal static ObservableValueSnapshot Capture(object? value)
    {
        if (value is null) return new(null, null, null);
        var type = value.GetType();
        try
        {
            return new(type.GetCleanFullName(),
                value is string text ? text : type.IsEnum ? value.ToString() :
                    JsonSerializer.Serialize(value, type, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }), null);
        }
        catch (Exception exception)
        {
            return new(type.GetCleanFullName(), null, exception.Message);
        }
    }
}

/// <summary>A detached exception capture, including inner exceptions and stack evidence.</summary>
/// <param name="TypeName">The exception runtime type.</param>
/// <param name="Message">The complete exception message.</param>
/// <param name="Details">The complete exception representation.</param>
/// <param name="CaptureError">The failure when a custom exception cannot render its evidence.</param>
public sealed record ObservableExceptionSnapshot(string TypeName, string Message, string Details, string? CaptureError)
{
    internal static ObservableExceptionSnapshot? Capture(Exception? value)
    {
        if (value is null) return null;
        var type = value.GetType().GetCleanFullName();
        try { return new(type, value.Message, value.ToString(), null); }
        catch (Exception exception) { return new(type, string.Empty, string.Empty, exception.Message); }
    }
}

/// <summary>A retained record frozen with its instance capture.</summary>
/// <param name="Sequence">The monotonic sequence within this registration.</param>
/// <param name="Timestamp">The UTC record timestamp.</param>
/// <param name="PreviousState">The captured prior value.</param>
/// <param name="CurrentState">The captured resulting value.</param>
/// <param name="Message">The complete record message.</param>
/// <param name="Exception">Detached exception evidence, if present.</param>
/// <param name="LogLevel">The level recorded at the time of this entry.</param>
public sealed record ObservableStateEntrySnapshot(long Sequence, DateTime Timestamp,
    ObservableValueSnapshot PreviousState, ObservableValueSnapshot CurrentState, string Message,
    ObservableExceptionSnapshot? Exception, LogLevel? LogLevel);

/// <summary>Identifies one registration, including when the same instance ID is reused.</summary>
/// <param name="InstanceId">The registry key.</param>
/// <param name="RegistrationId">The immutable registration generation.</param>
public sealed record ObservableInstanceReference(string InstanceId, string RegistrationId);

/// <summary>
/// A detached read of one tracker. Metadata, references, counters, mappings, and retained entries
/// are captured under one tracker lock; caller-owned values are serialized after releasing it.
/// Concurrent mutation of caller-owned values is not an atomic part of this contract.
/// </summary>
public sealed record ObservableInstanceSnapshot
{
    /// <summary>Gets the captured registration identity.</summary>
    public required ObservableInstanceReference Identity { get; init; }
    /// <summary>Gets the display name.</summary>
    public required string InstanceName { get; init; }
    /// <summary>Gets the registered type.</summary>
    public Type? InstanceType { get; init; }
    /// <summary>Gets the normalized type display name captured at the producing boundary.</summary>
    public string? InstanceTypeName { get; init; }
    /// <summary>Gets the normalized short type display name.</summary>
    public string? InstanceTypeShortName { get; init; }
    /// <summary>Gets the optional keyed-service identity.</summary>
    public string? InstanceKey { get; init; }
    /// <summary>Gets the exact group ID; null and empty both indicate ungrouped.</summary>
    public string? GroupId { get; init; }
    /// <summary>Gets the full detached current value.</summary>
    public required ObservableValueSnapshot CurrentState { get; init; }
    /// <summary>Gets the current mapped level; this is independent of retained exceptions.</summary>
    public LogLevel? CurrentLogLevel { get; init; }
    /// <summary>Gets the UTC registration time.</summary>
    public DateTime RegisteredAt { get; init; }
    /// <summary>Gets the UTC time of the last recorded entry, including message-only records.</summary>
    public DateTime LastRecordedAt { get; init; }
    /// <summary>Gets the UTC time of the locked tracker read.</summary>
    public DateTime CapturedAt { get; init; }
    /// <summary>Gets the lifetime count of recorded entries, preserved after history clear.</summary>
    public int TotalRecords { get; init; }
    /// <summary>Gets the lifetime exception count, preserved after history clear.</summary>
    public int TotalExceptions { get; init; }
    /// <summary>Gets the configured retention capacity.</summary>
    public int MaxHistorySize { get; init; }
    /// <summary>Gets all retained records in ascending sequence order.</summary>
    public required IReadOnlyList<ObservableStateEntrySnapshot> History { get; init; }
}
