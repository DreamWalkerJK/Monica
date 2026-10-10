using Monica.JobScheduler.Models.Execution;

namespace Monica.JobScheduler.UI.UIJobScheduler.Executions.State;

/// <summary>
/// Describes the outcome of an operator cancellation request made from execution UI state.
/// </summary>
/// <param name="IsAuthorized">Whether the operator remained authorized when the mutation was attempted.</param>
/// <param name="Status">The durable cancellation outcome, when the store accepted the request.</param>
/// <param name="Error">The authorization-provider or facade error when no durable outcome was obtained.</param>
internal sealed record ExecutionCancellationUiResult(
    bool IsAuthorized,
    JobCancellationStatus? Status,
    string? Error);
