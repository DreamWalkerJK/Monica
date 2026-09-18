using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Monica.Framework.ChainTracing.Abstractions;
using Monica.Framework.ChainTracing.Models;
using Monica.Tool.Extensions;

namespace Monica.Framework.ChainTracing.Providers.EntityFrameworkCore;

/// <summary>
/// Records EF Core command execution inside the current chain-tracing context. All command kinds
/// (reader, non-query, scalar) and all terminal paths (executed, failed, canceled) are paired so the
/// command-to-trace map never leaks entries.
/// </summary>
/// <param name="chainTracing">The chain tracing service.</param>
/// <param name="logger">The logger.</param>
public class ChainTracingDbCommandInterceptor(
    IChainTracing chainTracing,
    ILogger<ChainTracingDbCommandInterceptor> logger) : DbCommandInterceptor
{
    private readonly ConcurrentDictionary<object, string> _commandTraceMap = new();

    private string StartCommandTrace(DbCommand command)
    {
        try
        {
            var extraInfo = new
            {
                command.CommandTimeout,
                ParameterCount = command.Parameters.Count,
                Parameters = command.Parameters.Cast<DbParameter>()
                    .Take(10)
                    .Select(p => new
                    {
                        Name = p.ParameterName,
                        DbType = p.DbType.ToString(),
                        Value = p.Value?.ToString()?.LimitMaxLength(100, "...")
                    })
                    .ToArray()
            };

            var traceId = chainTracing.BeginTrace(
                command.CommandText.LimitMaxLength(1000, "..."),
                null,
                extraInfo,
                EChainTracingType.Database);

            _commandTraceMap[command] = traceId;
            return traceId;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to record the start of a database command trace.");
            return string.Empty;
        }
    }

    private void FinishCommandTrace(string traceId, CommandEventData eventData, object? result = null, bool isCanceled = false)
    {
        if (string.IsNullOrEmpty(traceId))
        {
            return;
        }

        try
        {
            string resultDescription;
            var success = true;
            Exception? exception = null;

            if (eventData is CommandEndEventData endEvent)
            {
                resultDescription = "Success";

                if (eventData is CommandErrorEventData errorEvent)
                {
                    success = false;
                    exception = errorEvent.Exception;
                    resultDescription = "Error";
                }
                else if (isCanceled)
                {
                    success = false;
                    resultDescription = "Canceled";
                }
                else if (result is int affectedRows)
                {
                    resultDescription += $"[AffectedRows:{affectedRows}]";
                }

                resultDescription += $"[{endEvent.Duration.TotalMilliseconds:0.##}ms]";
            }
            else
            {
                resultDescription = "Unknown";
            }

            chainTracing.EndTrace(traceId, resultDescription, success, exception);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to record the end of a database command trace.");
        }
    }

    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        StartCommandTrace(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        if (_commandTraceMap.TryRemove(command, out var traceId))
        {
            FinishCommandTrace(traceId, eventData, result);
        }

        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        StartCommandTrace(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (_commandTraceMap.TryRemove(command, out var traceId))
        {
            FinishCommandTrace(traceId, eventData, result);
        }

        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        StartCommandTrace(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        if (_commandTraceMap.TryRemove(command, out var traceId))
        {
            FinishCommandTrace(traceId, eventData, result);
        }

        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        StartCommandTrace(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        if (_commandTraceMap.TryRemove(command, out var traceId))
        {
            FinishCommandTrace(traceId, eventData, result);
        }

        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        StartCommandTrace(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        if (_commandTraceMap.TryRemove(command, out var traceId))
        {
            FinishCommandTrace(traceId, eventData, result);
        }

        return base.ScalarExecuted(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
    {
        StartCommandTrace(command);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        if (_commandTraceMap.TryRemove(command, out var traceId))
        {
            FinishCommandTrace(traceId, eventData, result);
        }

        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        if (_commandTraceMap.TryRemove(command, out var traceId))
        {
            FinishCommandTrace(traceId, eventData);
        }

        base.CommandFailed(command, eventData);
    }

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        if (_commandTraceMap.TryRemove(command, out var traceId))
        {
            FinishCommandTrace(traceId, eventData);
        }

        return base.CommandFailedAsync(command, eventData, cancellationToken);
    }

    public override void CommandCanceled(DbCommand command, CommandEndEventData eventData)
    {
        if (_commandTraceMap.TryRemove(command, out var traceId))
        {
            FinishCommandTrace(traceId, eventData, isCanceled: true);
        }

        base.CommandCanceled(command, eventData);
    }

    public override Task CommandCanceledAsync(DbCommand command, CommandEndEventData eventData, CancellationToken cancellationToken = default)
    {
        if (_commandTraceMap.TryRemove(command, out var traceId))
        {
            FinishCommandTrace(traceId, eventData, isCanceled: true);
        }

        return base.CommandCanceledAsync(command, eventData, cancellationToken);
    }
}
