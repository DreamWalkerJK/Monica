using System.Text.Json;
using Monica.Core.Results.Abstractions;

namespace Monica.Core.Results;

/// <summary>Applies the public result contract at HTTP and remote-call boundaries.</summary>
public static class ResultPresentationExtensions
{
    private static readonly string[] DIAGNOSTIC_KEYS =
        ["originResponse", "response", "request", "deserializationError", "exception", "detail", "chain", "chain_error"];

    /// <summary>
    /// Removes reserved technical metadata and fills missing failure presentation. Domain data and public
    /// application metadata are retained. An existing error must use <see cref="ResultError"/>; mixed producers
    /// are a local contract defect. Capture the trace once at the owning boundary and use it in diagnostics too.
    /// </summary>
    /// <param name="exposeReservedDiagnostics">
    /// Retains reserved diagnostic members in the response. Only trusted development hosts may enable this;
    /// remote-call boundaries always strip diagnostics.
    /// </param>
    public static T PrepareForPresentation<T>(this T result, JsonSerializerOptions json,
        IResultErrorMessageProvider messages, string traceId, string? service = null, string? operation = null,
        bool exposeReservedDiagnostics = false)
        where T : IResultEnvelope
    {
        if (result.Metadata is IDictionary<string, object?> metadata)
        {
            if (!exposeReservedDiagnostics)
                foreach (var key in metadata.Keys.Where(IsDiagnosticKey).ToArray()) metadata.Remove(key);
            if (metadata.ContainsKey("error") && !result.TryGetError(json, out _))
                throw new InvalidOperationException("The reserved metadata.error member must be a ResultError.");
            if (result.TryGetError(json, out var declaredError))
                result.SetError(declaredError); // Project JSON into the public model; discard undeclared technical members.
        }

        if (result.IsOk()) return result;
        if (!result.TryGetError(json, out var error))
        {
            error = new ResultError(result.Status switch
            {
                ResStatus.InternalError => ResultErrorCodes.UnexpectedError,
                ResStatus.ValidateError => ResultErrorCodes.ValidationFailed,
                ResStatus.AccessTokenExpired => "auth.access_token_expired",
                ResStatus.RefreshTokenExpired => "auth.refresh_token_expired",
                ResStatus.Unauthorized => "auth.unauthorized",
                ResStatus.Forbidden => "auth.forbidden",
                ResStatus.ErrorWarning => "operation.confirmation_required",
                _ => ResultErrorCodes.OperationFailed
            }, traceId, service, operation);
            result.SetError(error);
        }
        if (string.IsNullOrWhiteSpace(result.Message)) result.Message = messages.GetMessage(error);
        return result;
    }

    private static bool IsDiagnosticKey(string key) => DIAGNOSTIC_KEYS.Any(reserved =>
        key.Equals(reserved, StringComparison.OrdinalIgnoreCase) ||
        key.StartsWith(reserved, StringComparison.OrdinalIgnoreCase) &&
        key.AsSpan(reserved.Length).ContainsAnyExceptInRange('0', '9') == false);
}
