using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Monica.Core.Extensions;
using Monica.Core.Results;
using Monica.DevOps.Localization;
using Monica.DevOps.Terminal.Models;
using Monica.DevOps.Terminal.Services;

namespace Monica.DevOps.Terminal.Facades;

/// <summary>
/// Public terminal entry point used by the DevOps terminal UI.
/// </summary>
public sealed class TerminalFacade(
    TerminalSessionService sessionService,
    ILogger<TerminalFacade> logger,
    IStringLocalizer<TerminalResource> localizer)
{
    /// <summary>
    /// Gets the local terminal environment.
    /// </summary>
    /// <returns>The detected environment information.</returns>
    public Task<Res<TerminalEnvironmentSnapshot>> GetEnvironmentAsync()
        => ExecuteAsync(
            () => Task.FromResult(sessionService.GetEnvironment()),
            localizer["Messages:LoadEnvironmentFailed"].Value);

    /// <summary>
    /// Gets active terminal sessions for the requested owner.
    /// </summary>
    /// <param name="ownerKey">Stable owner key that isolates browser or user terminal sessions.</param>
    /// <returns>The active sessions.</returns>
    public Task<Res<IReadOnlyList<TerminalSessionSnapshot>>> GetSessionsAsync(string ownerKey)
        => ExecuteAsync(
            () => Task.FromResult(sessionService.GetSessions(ownerKey)),
            localizer["Messages:LoadSessionsFailed"].Value);

    /// <summary>
    /// Creates a terminal session for the requested owner.
    /// </summary>
    /// <param name="ownerKey">Stable owner key that isolates browser or user terminal sessions.</param>
    /// <param name="request">Session creation request.</param>
    /// <returns>The created session.</returns>
    public Task<Res<TerminalSessionSnapshot>> CreateSessionAsync(string ownerKey, TerminalSessionCreateRequest? request = null)
        => ExecuteAsync(
            () => Task.FromResult(sessionService.CreateSession(ownerKey, request)),
            localizer["Messages:CreateSessionFailed"].Value);

    /// <summary>
    /// Closes an owner-scoped terminal session.
    /// </summary>
    /// <param name="ownerKey">Stable owner key that isolates browser or user terminal sessions.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <returns>Removal and cancellation-request receipt; this does not prove process drainage.</returns>
    public Task<Res> CloseSessionAsync(string ownerKey, string sessionId)
        => ExecuteAsync(
            () => sessionService.CloseSessionAsync(ownerKey, sessionId),
            localizer["Messages:SessionClosed"].Value,
            localizer["Messages:CloseSessionFailed"].Value);

    /// <summary>
    /// Cancels the current command in an owner-scoped terminal session.
    /// </summary>
    /// <param name="ownerKey">Stable owner key that isolates browser or user terminal sessions.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <returns>Operation result.</returns>
    /// <param name="expectedCommandId">Frozen active command identity; stale targets fail without canceling a newer run.</param>
    /// <remarks>Request completion does not prove command termination. Observe IsRunning in later snapshots.</remarks>
    public Task<Res<TerminalSessionSnapshot>> CancelCommandAsync(string ownerKey, string sessionId, string expectedCommandId)
        => ExecuteAsync(
            () => sessionService.CancelCommandAsync(ownerKey, sessionId, expectedCommandId),
            localizer["Messages:CancelFailed"].Value);

    /// <summary>Accepts host-owned execution independently of the calling UI visit.</summary>
    /// <remarks>Success means admitted, not successful process completion. Observe session snapshots by
    /// the receipt's CommandId. No visit token cancels the admitted command.</remarks>
    public Task<Res<TerminalCommandAcceptance>> StartCommandAsync(string ownerKey, string sessionId, TerminalCommandRequest request)
        => ExecuteAsync(
            () => Task.FromResult(sessionService.StartCommand(ownerKey, sessionId, request)),
            localizer["Messages:RunCommandFailed"].Value);

    /// <summary>
    /// Clears retained output for an owner-scoped terminal session.
    /// </summary>
    /// <param name="ownerKey">Stable owner key that isolates browser or user terminal sessions.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <returns>Operation result.</returns>
    public Task<Res> ClearOutputAsync(string ownerKey, string sessionId)
        => ExecuteAsync(
            () =>
            {
                sessionService.ClearOutput(ownerKey, sessionId);
                return Task.CompletedTask;
            },
            localizer["Messages:OutputCleared"].Value,
            localizer["Messages:ClearOutputFailed"].Value);

    /// <summary>
    /// Executes a command in an owner-scoped terminal session.
    /// </summary>
    /// <param name="ownerKey">Stable owner key that isolates browser or user terminal sessions.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="request">Command request.</param>
    /// <param name="outputCallback">Optional serial observer; actual async callbacks are joined and failures observed.</param>
    /// <param name="ct">Explicit execution cancellation; do not pass a route-lifetime token for continued commands.</param>
    /// <returns>The command result after actual process, output and callback drainage.</returns>
    public Task<Res<TerminalCommandResult>> ExecuteCommandAsync(
        string ownerKey,
        string sessionId,
        TerminalCommandRequest request,
        Func<TerminalOutputLine, CancellationToken, ValueTask>? outputCallback = null,
        CancellationToken ct = default)
        => ExecuteAsync(
            () => sessionService.ExecuteCommandAsync(ownerKey, sessionId, request, outputCallback, ct),
            localizer["Messages:RunCommandFailed"].Value);

    private async Task<Res<T>> ExecuteAsync<T>(
        Func<Task<T>> action,
        string failureMessage)
    {
        try
        {
            return Res.Ok(await action());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{FailureMessage}", failureMessage);
            return Res.Fail($"{failureMessage}: {ex.GetMessageRecursively()}", GetResultStatus(ex));
        }
    }

    private async Task<Res> ExecuteAsync(
        Func<Task> action,
        string successMessage,
        string failureMessage)
    {
        try
        {
            await action();
            return Res.Ok(successMessage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{FailureMessage}", failureMessage);
            return Res.Fail($"{failureMessage}: {ex.GetMessageRecursively()}", GetResultStatus(ex));
        }
    }

    private static ResStatus GetResultStatus(Exception exception)
    {
        return exception switch
        {
            ArgumentException => ResStatus.BadRequest,
            InvalidOperationException => ResStatus.BadRequest,
            KeyNotFoundException => ResStatus.BadRequest,
            DirectoryNotFoundException => ResStatus.BadRequest,
            UnauthorizedAccessException => ResStatus.Forbidden,
            _ => ResStatus.InternalError
        };
    }
}
