using Microsoft.AspNetCore.Http;
using Monica.Core.Results.Abstractions;

namespace Monica.Core.ExceptionHandling.Abstractions;

/// <summary>
/// Attaches boundary diagnostics to a response built from an unhandled exception. The exception handler
/// invokes registered implementations after failure presentation is complete, so unhandled-exception
/// responses expose the same correlation members (for example trace id and call chain) as normal ones.
/// Exception handling keeps working when no implementation is registered.
/// </summary>
public interface IExceptionResponseDiagnostics
{
    /// <summary>
    /// Attaches diagnostics to an exception response before it is written.
    /// </summary>
    /// <param name="httpContext">The failing request context, when available.</param>
    /// <param name="response">The response envelope about to be written; must not be replaced.</param>
    void Attach(HttpContext? httpContext, IResultEnvelope response);
}
