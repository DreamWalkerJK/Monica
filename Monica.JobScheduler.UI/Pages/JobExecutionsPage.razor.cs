using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Monica.JobScheduler.UI.Localization;

namespace Monica.JobScheduler.UI.Pages;

/// <summary>Routes an execution ledger whose filters, native dialogs, and lifetime belong to its query identity.</summary>
public partial class JobExecutionsPage
{
    /// <summary>Gets the route for the execution ledger.</summary>
    public const string PAGE_URL = "/job-scheduler/executions";

    [Inject]
    private IStringLocalizer<JobSchedulerResource> L { get; set; } = null!;

    /// <summary>
    /// Gets or sets the optional comma-separated execution-state filter. Query changes retire the previous owner.
    /// </summary>
    [SupplyParameterFromQuery(Name = "states")]
    public string? InitialStates { get; set; }

    /// <summary>
    /// Gets or sets the exact, opaque execution identifier to search and inspect initially.
    /// The ledger retains its identifier substring-search semantics; the inspector loads this unnormalized ID.
    /// </summary>
    [SupplyParameterFromQuery(Name = "instanceId")]
    public string? InitialInstanceId { get; set; }
}
