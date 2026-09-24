using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Monica.AI.Abstractions;
using Monica.AI.Chat.Models;
using Monica.AI.Chat.Services;

namespace Monica.AI.Services.Support;

/// <summary>
/// Emits synthetic streaming updates for function calls and results so the UI can observe tool activity.
/// </summary>
internal sealed class ToolInvocationTrackingAgentDecorator(
    ILogger<ToolInvocationTrackingAgentDecorator> logger,
    AgentResponseUpdateChannelContext updateChannelContext)
    : IAIChatAgentDecorator
{
    /// <inheritdoc />
    public void Configure(AIAgentBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        logger.LogInformation("Configuring tool invocation tracking middleware.");
        builder.Use(TrackInvocationAsync);
    }

    private async ValueTask<object?> TrackInvocationAsync(
        AIAgent _,
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var updateChannel = ResolveUpdateChannel(context.Options?.AdditionalProperties);
        var functionCall = CreateFunctionCallContent(context);
        var run = updateChannel?.RunContext;
        var step = run?.Session.StartStep(ChatExecutionStepKind.Tool, run.Turn?.Id, tool: new ChatToolExecution
        {
            CallId = functionCall.CallId,
            Name = functionCall.Name,
            Arguments = functionCall.Arguments is null ? null : ChatInspectionRedactor.Redact(
                System.Text.Json.JsonSerializer.SerializeToElement(functionCall.Arguments), run.RedactDiagnostic).GetRawText()
        });
        if (run is not null && step is not null) await run.PublishAsync(new ChatStepChangedEvent(step));

        logger.LogInformation(
            "Observed tool invocation '{ToolName}' (CallId: {CallId}). Channel available: {HasChannel}.",
            functionCall.Name,
            functionCall.CallId,
            updateChannel is not null);

        if (updateChannel is not null)
        {
            await updateChannel.PublishAsync(
                new AgentResponseUpdate(ChatRole.Assistant, [functionCall])
                {
                    CreatedAt = DateTimeOffset.UtcNow
                },
                cancellationToken);
        }

        try
        {
            var result = await next(context, cancellationToken).ConfigureAwait(false);
            var toolError = ToolInvocationErrorResult.FromResult(result);
            if (toolError is not null)
            {
                toolError = toolError with { Message = Redact(toolError.Message, run) };
                result = toolError;
            }

            if (run is not null && step is not null)
            {
                await run.RecordAsync(step with
                {
                    Status = toolError is null ? ChatExecutionStatus.Completed : ChatExecutionStatus.Failed,
                    CompletedAt = DateTimeOffset.UtcNow,
                    Error = toolError?.Message,
                    Tool = step.Tool! with { Result = run.Redact(ToolCallContentSerializer.SerializeResult(result)) }
                });
            }

            if (updateChannel is not null)
            {
                await updateChannel.PublishAsync(
                    CreateFunctionResultUpdate(functionCall.CallId, result, exception: null),
                    cancellationToken);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            if (run is not null && step is not null)
                await run.RecordAsync(step with { Status = ChatExecutionStatus.Cancelled, CompletedAt = DateTimeOffset.UtcNow });
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "Tool invocation '{ToolName}' failed with {ErrorType}: {Error}. It will be returned to the model as a tool error result.",
                functionCall.Name, ex.GetType().Name, Redact(ex.Message, run));

            var errorResult = ToolInvocationErrorResult.Create(functionCall, ex) with { Message = Redact(ex.Message, run) };
            if (run is not null && step is not null)
                await run.RecordAsync(step with
                {
                    Status = ChatExecutionStatus.Failed, CompletedAt = DateTimeOffset.UtcNow,
                    Error = run.Redact(ex.Message),
                    Tool = step.Tool! with { Result = run.Redact(ToolCallContentSerializer.SerializeResult(errorResult)) }
                });
            if (updateChannel is not null)
            {
                await updateChannel.PublishAsync(
                    CreateFunctionResultUpdate(functionCall.CallId, errorResult, new InvalidOperationException(errorResult.Message)),
                    cancellationToken);
            }

            return errorResult;
        }
    }

    private static string Redact(string value, ChatRunContext? run)
        => (run?.Redact(value) ?? ChatInspectionRedactor.Redact(value))!;

    private static AgentResponseUpdate CreateFunctionResultUpdate(
        string callId,
        object? result,
        Exception? exception)
    {
        var content = new FunctionResultContent(callId, result)
        {
            Exception = exception
        };

        return new AgentResponseUpdate(ChatRole.Tool, [content])
        {
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static FunctionCallContent CreateFunctionCallContent(FunctionInvocationContext context)
    {
        var original = context.CallContent;
        var callId = !string.IsNullOrWhiteSpace(original?.CallId)
            ? original.CallId
            : Guid.NewGuid().ToString("N");
        var name = !string.IsNullOrWhiteSpace(original?.Name)
            ? original.Name
            : context.Function?.Name ?? "unknown_tool";
        var arguments = original?.Arguments is { Count: > 0 }
            ? new Dictionary<string, object?>(original.Arguments)
            : new Dictionary<string, object?>(context.Arguments ?? []);

        return new FunctionCallContent(callId, name, arguments);
    }

    private AgentResponseUpdateChannel? ResolveUpdateChannel(
        AdditionalPropertiesDictionary? additionalProperties)
    {
        if (additionalProperties is not null
            && additionalProperties.TryGetValue<AgentResponseUpdateChannel>(out var updateChannel))
        {
            return updateChannel;
        }

        return updateChannelContext.Current;
    }
}
