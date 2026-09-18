using System.Dynamic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Monica.Core.Results;
using Monica.Core.Results.Abstractions;
using Monica.Framework.ChainTracing.Models;
using Monica.Modules;
using Monica.Tool.Extensions;

namespace Monica.Framework.ChainTracing.Services.Support;

/// <summary>
/// Attaches completed chain-trace metadata to a result envelope at the owning HTTP boundary. Shared by
/// the MVC result filter and the exception-response diagnostics hook so normal and exception paths expose
/// the same correlation members.
/// </summary>
public sealed class ChainResultMetadataAttacher(IOptions<ModuleResultEnvelopeOption> envelopeOptions)
{
    /// <summary>
    /// Marks the chain complete and attaches correlation metadata. The trace identifier is always public;
    /// the chain itself (SQL text, parameter values, isolated exception messages) is reserved for hosts
    /// that expose diagnostic details.
    /// </summary>
    /// <param name="chain">The active chain, if any.</param>
    /// <param name="httpContext">The owning request context, when available.</param>
    /// <param name="response">The response envelope to enrich.</param>
    public void Attach(ChainTraceContext? chain, HttpContext? httpContext, IResultEnvelope response)
    {
        if (chain is null) return;

        chain.MarkComplete();
        response.SetMetadata(ResultMetadataKeys.TraceId, ResultTraceId.Capture(httpContext));
        if (!envelopeOptions.Value.ExposeDiagnosticDetails || chain.Root is null) return;

        // The chain contains SQL text, parameter values, and isolated exception messages; only diagnostic hosts may expose it.
        response.Metadata ??= new ExpandoObject();
        response.Metadata.Append(ChainTraceContext.CHAIN_KEY, chain.Root);
        if (chain.IsolatedNodes is not null)
        {
            response.Metadata.Append($"{ChainTraceContext.CHAIN_KEY}_error", chain.IsolatedNodes.Select(p => new
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
