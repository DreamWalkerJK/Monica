using System.Dynamic;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Monica.Core.Results;
using Monica.Core.Results.Abstractions;
using Monica.Framework.ChainTracing.Abstractions;
using Monica.Framework.ChainTracing.Models;
using Monica.Framework.ChainTracing.Services.Support;
using Monica.Modules;
using Monica.Tool.Extensions;

namespace Monica.Framework.ChainTracing.Providers.AspNetCore;

/// <summary>
/// Completes controller tracing and attaches a public correlation identifier to result envelopes. Hosts that
/// expose reserved diagnostics additionally receive the call chain (including recorded SQL commands).
/// </summary>
public class ChainTracingResultMetadataActionFilter(
    IChainTracing chainTracing,
    IOptions<ModuleResultEnvelopeOption> envelopeOptions) : IActionFilter
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

        chain.MarkComplete();
        serviceResponse.SetMetadata("traceId", ResultTraceId.Capture(context.HttpContext));
        if (!envelopeOptions.Value.ExposeDiagnosticDetails) return;

        // The chain contains SQL text, parameter values, and isolated exception messages; only diagnostic hosts may expose it.
        serviceResponse.Metadata ??= new ExpandoObject();
        serviceResponse.Metadata.Append(ChainTraceContext.CHAIN_KEY, chain.Root);
        if (chain.IsolatedNodes is not null)
        {
            serviceResponse.Metadata.Append($"{ChainTraceContext.CHAIN_KEY}_error", chain.IsolatedNodes.Select(p => new
            {
                p.Operation,
                p.Handler,
                p.Duration,
                p.Type,
                p.ExceptionMessage,
                p.StartTime,
                p.EndTime,
                p.TraceId,
            }));
        }
    }
}
