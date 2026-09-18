using Monica.Core.Results.Abstractions;

namespace Monica.Core.Results.Services;

internal sealed class DefaultResultErrorMessageProvider : IResultErrorMessageProvider
{
    public string GetMessage(ResultError error)
    {
        var target = error.Service is { Length: > 0 } name ? $"The {name} service" : "The service";
        return error.Code switch
        {
            ResultErrorCodes.InvalidRequest or ResultErrorCodes.ValidationFailed =>
                error.Fields?.FirstOrDefault() is { Path.Length: > 0 } field
                    ? $"Check the request field '{field.Path}': {field.Message}"
                    : "Check the request and correct the invalid fields.",
            ResultErrorCodes.DependencyRateLimited => $"{target} is receiving too many requests. Please wait before trying again.",
            ResultErrorCodes.DependencyUnavailable => $"{target} is unavailable. The operation's outcome could not be confirmed.",
            ResultErrorCodes.DependencyTimeout => $"{target} did not respond in time. The operation's outcome could not be confirmed.",
            ResultErrorCodes.DependencyInvalidResponse => $"{target} returned an invalid response. Contact support with the trace identifier.",
            ResultErrorCodes.DependencyRejected => $"{target} rejected the service call. Contact support with the trace identifier.",
            ResultErrorCodes.DependencyFailed => $"{target} could not complete the service call.",
            ResultErrorCodes.UnexpectedError => "An unexpected server error occurred.",
            _ => "The operation could not be completed."
        };
    }
}
