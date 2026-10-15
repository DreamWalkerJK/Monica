using Microsoft.Extensions.Options;
using Monica.JobScheduler.Facades;
using Monica.JobScheduler.UI.UIJobScheduler.Shared;
using Monica.JobScheduler.UI.UIJobScheduler.Support;
using Monica.Modules;

namespace Monica.JobScheduler.UI.UIJobScheduler.Executions.State;

/// <summary>
/// Creates component-owned execution ledger and detail states from circuit-scoped scheduler dependencies.
/// </summary>
internal sealed class JobExecutionsStateFactory(
    JobSchedulerFacade facade,
    IJobSchedulerUiAccess access,
    IOptions<ModuleJobSchedulerUIOption> options,
    TimeProvider timeProvider,
    SchedulerTimePresentation timePresentation)
{
    /// <summary>
    /// Creates a fresh state for one execution-ledger page instance.
    /// </summary>
    public JobExecutionsPageState CreatePageState()
    {
        return new JobExecutionsPageState(
            facade,
            access,
            options.Value.DefaultPageSize,
            timeProvider,
            timePresentation);
    }

    /// <summary>
    /// Creates a fresh state bound to one exact execution-detail identity.
    /// </summary>
    /// <remarks>The caller owns and drains this state. Identity changes require a new state; identifiers are not trimmed.</remarks>
    /// <exception cref="ArgumentException">The identifier is blank.</exception>
    public ExecutionDetailState CreateDetailState(string instanceId)
    {
        return new ExecutionDetailState(facade, access, timeProvider, instanceId);
    }
}
