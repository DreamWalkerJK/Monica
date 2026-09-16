using Monica.Core.Results;
using Microsoft.AspNetCore.Mvc.Filters;
using Monica.Core.Results.Abstractions;
using Monica.Framework.ChainTracing.Abstractions;
using Monica.Framework.ChainTracing.Services.Support;

namespace Monica.Framework.ChainTracing.Providers.AspNetCore;

/// <summary>
/// Completes controller tracing and attaches a public correlation identifier to result envelopes.
/// </summary>
public class ChainTracingResultMetadataActionFilter(IChainTracing chainTracing) : IActionFilter
{
    /// <summary>
    /// Runs before the action executes.
    /// </summary>
    /// <param name="context">The action execution context.</param>
    public void OnActionExecuting(ActionExecutingContext context) { }

    /// <summary>
    /// Runs after the action executes.
    /// </summary>
    /// <param name="context">The action execution context.</param>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (chainTracing.GetCurrentChain() is not { } chain ||
            ChainTracingResultHelper.ExtractResult(context.Result) is not IResultEnvelope serviceResponse)
        {
            return;
        }

        chain.MarkComplete();
        serviceResponse.SetMetadata("traceId", ResultTraceId.Capture(context.HttpContext));
    }
}
