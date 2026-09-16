namespace Monica.Core.Results;

/// <summary>Defines framework failure reasons independently of numeric result statuses.</summary>
public static class ResultErrorCodes
{
    /// <summary>A supplied value could not bind to the declared request contract.</summary>
    public const string InvalidRequest = "request.invalid";
    /// <summary>A bound request failed declarative validation.</summary>
    public const string ValidationFailed = "validation.failed";
    /// <summary>An application failure has no more specific declared reason.</summary>
    public const string OperationFailed = "operation.failed";
    /// <summary>An unexpected local defect was handled at the host boundary.</summary>
    public const string UnexpectedError = "internal.unexpected";
    /// <summary>A dependency could not be reached or reported unavailability.</summary>
    public const string DependencyUnavailable = "dependency.unavailable";
    /// <summary>A dependency rejected the call because it was rate limited.</summary>
    public const string DependencyRateLimited = "dependency.rate_limited";
    /// <summary>The complete call deadline or a transport timeout expired.</summary>
    public const string DependencyTimeout = "dependency.timeout";
    /// <summary>The dependency response violated the configured envelope or body contract.</summary>
    public const string DependencyInvalidResponse = "dependency.invalid_response";
    /// <summary>A dependency returned an unrecognized client error response.</summary>
    public const string DependencyRejected = "dependency.rejected";
    /// <summary>A dependency returned an unrecognized server error response.</summary>
    public const string DependencyFailed = "dependency.failed";
}
