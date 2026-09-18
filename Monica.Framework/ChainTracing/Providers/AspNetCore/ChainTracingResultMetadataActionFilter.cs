using Microsoft.AspNetCore.Mvc.Filters;
using Monica.Core.Results.Abstractions;
using Monica.Framework.ChainTracing.Abstractions;
using Monica.Framework.ChainTracing.Services.Support;

namespace Monica.Framework.ChainTracing.Providers.AspNetCore;

/// <summary>
/// Completes controller tracing and attaches correlation metadata to result envelopes through the shared
/// <see cref="ChainResultMetadataAttacher" />: the public trace identifier always, the call chain
/// (including recorded SQL commands) on hosts that expose reserved diagnostics.
/// </summary>
public class ChainTracingResultMetadataActionFilter(
    IChainTracing chainTracing,
    ChainResultMetadataAttacher attacher) : IActionFilter
{
    /// <summary>
    /// Runs before the action executes.
    /// </summary>
    /// <param name="context">The action execution context.</param>
    public void OnActionExecuting(ActionExecutingContext context) { }

    /// <summary>
    /// Runs after the action executes.
    /// </summary>
    /// <param name="context">The action executed context.</param>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (chainTracing.GetCurrentChain() is not { } chain ||
            ChainTracingResultHelper.ExtractResult(context.Result) is not IResultEnvelope serviceResponse)
        {
            return;
        }

        attacher.Attach(chain, context.HttpContext, serviceResponse);
    }
}
