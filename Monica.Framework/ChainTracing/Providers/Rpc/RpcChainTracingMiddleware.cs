using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Monica.Core.JsonSerialization.Abstractions;
using Monica.Framework.ChainTracing.Abstractions;
using Monica.Framework.ChainTracing.Extensions;
using Monica.WebApi.RpcClient.Models;

namespace Monica.Framework.ChainTracing.Providers.Rpc;

/// <summary>
/// Traces actor calls without buffering or enriching their public response bodies.
/// </summary>
internal sealed class RpcChainTracingMiddleware(
    IJsonSerializerOptionsProvider jsonSerializerOptionsProvider,
    IChainTracing tracing) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.GetEndpoint()?.DisplayName != "Dapr Actors Invoke")
        {
            await next(context);
            return;
        }

        context.Request.EnableBuffering();

        if (context.Request.Body.CanRead)
        {
            try
            {
                context.Request.Body.Position = 0;

                if (JsonSerializer.Deserialize<RpcRequest>(context.Request.Body, jsonSerializerOptionsProvider.SerializerOptions) is { Headers.Count: > 0 } request)
                {
                    foreach (var (key, value) in request.Headers.Where(p => p.Key.StartsWith("X-")))
                    {
                        if (!context.Request.Headers.ContainsKey(key))
                        {
                            context.Request.Headers.Append(key, value);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                context.Request.Body.Position = 0;
            }
        }

        using var scope = tracing.BeginScope("Actor invocation", "Actor");
        try
        {
            await next(context);
            scope.EndWithSuccess();
        }
        catch (Exception exception)
        {
            scope.EndWithException(exception);
            throw;
        }
    }
}
