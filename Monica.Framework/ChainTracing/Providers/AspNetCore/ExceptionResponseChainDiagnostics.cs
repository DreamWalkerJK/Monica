using Microsoft.AspNetCore.Http;
using Monica.Core.ExceptionHandling.Abstractions;
using Monica.Core.Results.Abstractions;
using Monica.Framework.ChainTracing.Abstractions;
using Monica.Framework.ChainTracing.Models;
using Monica.Framework.ChainTracing.Services.Support;

namespace Monica.Framework.ChainTracing.Providers.AspNetCore;

/// <summary>
/// Publishes the request chain onto exception-built responses so they carry the same correlation members
/// (trace identifier and, on diagnostic hosts, the call chain) as normal responses. The exception handler
/// runs after the pipeline unwinds, where downstream AsyncLocal mutations are no longer visible, so
/// request-boundary scopes publish the chain through <see cref="HttpContext" /> items as well.
/// </summary>
public sealed class ExceptionResponseChainDiagnostics(
    IChainTracing chainTracing,
    ChainResultMetadataAttacher attacher) : IExceptionResponseDiagnostics
{
    /// <summary>
    /// Attaches the recovered chain to an exception response before it is written.
    /// </summary>
    /// <param name="httpContext">The failing request context, when available.</param>
    /// <param name="response">The response envelope about to be written.</param>
    public void Attach(HttpContext? httpContext, IResultEnvelope response)
    {
        var chain = httpContext?.Items.TryGetValue(ChainTraceContext.HTTP_ITEM_KEY, out var published) == true
            ? published as ChainTraceContext
            : chainTracing.GetCurrentChain();
        attacher.Attach(chain, httpContext, response);
    }
}
