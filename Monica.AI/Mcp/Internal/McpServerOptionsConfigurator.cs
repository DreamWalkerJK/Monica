using System.Text;
using Microsoft.Extensions.Options;
using Monica.AI.AgentCapabilities.Abstractions;
using Monica.AI.AgentCapabilities.Models;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Monica.AI.Mcp.Services;
using Monica.Modules;

namespace Monica.AI.Mcp.Internal;

/// <summary>
/// Configures the shared Monica <see cref="McpServerOptions"/>: stdio server registration, skill-gated tool
/// visibility, and — unless <see cref="ModuleMcpOption.ExposeToolErrorDetail"/> is disabled — failure reasons
/// carried to tool callers.
/// </summary>
internal sealed class McpServerOptionsConfigurator(
    MonicaMcpCatalog catalog,
    IAgentCapabilityStateStore stateStore,
    IOptions<ModuleMcpOption> mcpOptions) : IConfigureOptions<McpServerOptions>
{
    private const int MaxErrorDetailLength = 2000;

    private readonly Lazy<AgentCapabilityState> _state = new(() =>
        stateStore.LoadAsync().GetAwaiter().GetResult());

    public void Configure(McpServerOptions options)
    {
        catalog.TryConfigureStdioServerOptions(options, _state.Value);
        options.Filters.Request.ListToolsFilters.Add(FilterListTools);
        options.Filters.Request.CallToolFilters.Add(FilterCallTool);
        ConfigureToolErrorDetail(options, mcpOptions.Value);
    }

    internal static void ConfigureToolErrorDetail(McpServerOptions options, ModuleMcpOption moduleOption)
    {
        if (moduleOption.ExposeToolErrorDetail)
        {
            options.Filters.Request.CallToolFilters.Add(WrapToolErrorDetail);
        }
    }

    /// <summary>
    /// Wraps a CallTool pipeline so unhandled tool failures reach the caller as protocol errors carrying their
    /// reason. The SDK collapses everything except McpException messages into a generic invocation error, which
    /// hides argument-binding and unexpected-fault causes from the calling agent.
    /// </summary>
    internal static McpRequestHandler<CallToolRequestParams, CallToolResult> WrapToolErrorDetail(
        McpRequestHandler<CallToolRequestParams, CallToolResult> next)
    {
        return async (context, cancellationToken) =>
        {
            try
            {
                return await next(context, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is not McpProtocolException)
            {
                throw new McpProtocolException(ComposeErrorDetail(exception), McpErrorCode.InternalError);
            }
        };
    }

    internal static string ComposeErrorDetail(Exception exception)
    {
        var detail = new StringBuilder(exception.GetType().Name).Append(": ").Append(exception.Message);
        for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (string.IsNullOrEmpty(inner.Message))
            {
                continue;
            }

            detail.Append(" -> ").Append(inner.Message);
            if (detail.Length >= MaxErrorDetailLength)
            {
                break;
            }
        }

        return detail.Length <= MaxErrorDetailLength
            ? detail.ToString()
            : detail.ToString(0, MaxErrorDetailLength) + "…";
    }

    private McpRequestHandler<ListToolsRequestParams, ListToolsResult> FilterListTools(
        McpRequestHandler<ListToolsRequestParams, ListToolsResult> next)
    {
        return async (context, cancellationToken) =>
        {
            var result = await next(context, cancellationToken);

            for (var i = result.Tools.Count - 1; i >= 0; i--)
            {
                var tool = result.Tools[i];
                if (tool.Name is null || !IsToolStartupEnabled(context, tool.Name))
                {
                    result.Tools.RemoveAt(i);
                }
            }

            return result;
        };
    }

    private McpRequestHandler<CallToolRequestParams, CallToolResult> FilterCallTool(
        McpRequestHandler<CallToolRequestParams, CallToolResult> next)
    {
        return async (context, cancellationToken) =>
        {
            if (!IsToolStartupEnabled(context, context.Params.Name))
            {
                throw new McpProtocolException(
                    $"Tool '{context.Params.Name}' is not available on this MCP server.",
                    McpErrorCode.InvalidParams);
            }

            return await next(context, cancellationToken);
        };
    }

    private bool IsToolStartupEnabled(McpServerTool tool)
    {
        var metadata = tool.Metadata.OfType<McpToolMetadata>().FirstOrDefault();
        return metadata?.SkillName is null
               || _state.Value.IsSkillMcpServerEnabled(
                   metadata.SkillName,
                   metadata.SkillMcpEnabledByDefault);
    }

    private bool IsToolStartupEnabled<TParams>(
        RequestContext<TParams> context,
        string toolName)
    {
        if (context.Server.ServerOptions.ToolCollection?.TryGetPrimitive(toolName, out var primitive) != true)
        {
            return true;
        }

        var metadata = primitive!.Metadata.OfType<McpToolMetadata>().FirstOrDefault();
        return metadata?.SkillName is null
               || _state.Value.IsSkillMcpServerEnabled(
                   metadata.SkillName,
                   metadata.SkillMcpEnabledByDefault);
    }
}
