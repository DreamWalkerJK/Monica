using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Monica.Core.Results;

/// <summary>Creates nonempty correlation identifiers even without an activity listener or HTTP request.</summary>
public static class ResultTraceId
{
    /// <summary>Uses the current trace, a stable HTTP-request fallback, or a newly generated worker identifier.</summary>
    /// <remarks>Capture once per worker operation and use the returned value in both its result and log event.</remarks>
    public static string Capture(HttpContext? httpContext = null)
    {
        if (Activity.Current is { TraceId: var traceId } && traceId != default)
        {
            return traceId.ToHexString();
        }

        if (httpContext is not null)
        {
            // The same host request must receive the same fallback across independently invoked adapters.
            return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(httpContext.TraceIdentifier)))[..32];
        }

        return ActivityTraceId.CreateRandom().ToHexString();
    }
}
