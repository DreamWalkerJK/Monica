using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Monica.JobScheduler.Models.Execution;
using Monica.JobScheduler.UI.Localization;
using Monica.JobScheduler.UI.UIJobScheduler.Support;
using Monica.UI.Icons;
using MudBlazor;

namespace Monica.JobScheduler.UI.UIJobScheduler.Components;

/// <summary>Edits execution search, recorded-time range, and semantic state filters without owning their query state.</summary>
/// <typeparam name="TRange">The owner's enum describing recorded-time presets. Its values and labels are supplied by the owner.</typeparam>
/// <remarks>Callbacks are reserved before invocation and drained during shared retirement. No state counts are inferred from paged records.</remarks>
public partial class ExecutionFilterBar<TRange> : IAsyncDisposable where TRange : struct, Enum
{
    [Inject] private IStringLocalizer<JobSchedulerResource> L { get; set; } = null!;

    /// <summary>Gets or sets identifier search text. The query owner defines normalization and matching.</summary>
    [Parameter] public string? SearchText { get; set; }
    /// <summary>Receives the complete edited search value.</summary>
    [Parameter] public EventCallback<string?> SearchTextChanged { get; set; }
    /// <summary>Gets or sets the selected recorded-time preset.</summary>
    [Parameter] public TRange TimeRange { get; set; }
    /// <summary>Receives a selected recorded-time preset.</summary>
    [Parameter] public EventCallback<TRange> TimeRangeChanged { get; set; }
    /// <summary>Gets or sets the owner's ordered supported presets.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<TRange> TimeRanges { get; set; } = [];
    /// <summary>Gets or sets the localized display formatter for the owner's presets.</summary>
    [Parameter, EditorRequired] public Func<TRange, string> TimeRangeLabel { get; set; } = null!;
    /// <summary>Gets or sets whether the selected preset exposes scheduler-calendar date boundaries.</summary>
    [Parameter] public bool ShowCustomDates { get; set; }
    /// <summary>Gets or sets the optional inclusive starting scheduler-calendar date.</summary>
    [Parameter] public DateTime? CustomStartDate { get; set; }
    /// <summary>Receives the edited starting date. UTC conversion remains owned by the query state.</summary>
    [Parameter] public EventCallback<DateTime?> CustomStartDateChanged { get; set; }
    /// <summary>Gets or sets the optional inclusive ending scheduler-calendar date.</summary>
    [Parameter] public DateTime? CustomEndDate { get; set; }
    /// <summary>Receives the edited ending date. UTC conversion remains owned by the query state.</summary>
    [Parameter] public EventCallback<DateTime?> CustomEndDateChanged { get; set; }
    /// <summary>Gets or sets selected durable states. An empty collection includes every state.</summary>
    [Parameter] public IReadOnlyCollection<JobExecutionState> SelectedStates { get; set; } = [];
    /// <summary>Receives a copied state selection. Selecting the Any state control emits an empty collection.</summary>
    [Parameter] public EventCallback<IReadOnlyCollection<JobExecutionState>> SelectedStatesChanged { get; set; }
    /// <summary>Gets or sets the owner's loading state, which disables filter edits and actions.</summary>
    [Parameter] public bool IsLoading { get; set; }
    /// <summary>Gets or sets whether the owner rejected the custom date range. Invalid ranges cannot be applied.</summary>
    [Parameter] public bool HasInvalidCustomRange { get; set; }
    /// <summary>Requests applying the current draft filters, including from the native search field's Enter key.</summary>
    [Parameter] public EventCallback ApplyRequested { get; set; }
    /// <summary>Requests restoring the owner's default filters.</summary>
    [Parameter] public EventCallback ResetRequested { get; set; }

    private readonly SchedulerOperationLifetime _operations = new();

    private Task ChangeSearchTextAsync(string? value) => OwnCallbackAsync(() => SearchTextChanged.InvokeAsync(value));
    private Task ChangeTimeRangeAsync(TRange value) => OwnCallbackAsync(() => TimeRangeChanged.InvokeAsync(value));
    private Task ChangeStartDateAsync(DateTime? value) => OwnCallbackAsync(() => CustomStartDateChanged.InvokeAsync(value));
    private Task ChangeEndDateAsync(DateTime? value) => OwnCallbackAsync(() => CustomEndDateChanged.InvokeAsync(value));
    private Task ClearStatesAsync() => OwnCallbackAsync(() => SelectedStatesChanged.InvokeAsync(Array.Empty<JobExecutionState>()));

    private Task ToggleStateAsync(JobExecutionState state)
    {
        var selection = SelectedStates.ToHashSet();
        if (!selection.Add(state)) selection.Remove(state);
        return OwnCallbackAsync(() => SelectedStatesChanged.InvokeAsync(selection.ToArray()));
    }

    private Task ApplyAsync() => IsLoading || HasInvalidCustomRange
        ? Task.CompletedTask
        : OwnCallbackAsync(() => ApplyRequested.InvokeAsync());

    private Task ResetAsync() => IsLoading ? Task.CompletedTask : OwnCallbackAsync(() => ResetRequested.InvokeAsync());

    private Task HandleSearchKeyDownAsync(KeyboardEventArgs args) => args.Key == "Enter" ? ApplyAsync() : Task.CompletedTask;

    private async Task OwnCallbackAsync(Func<Task> callback)
    {
        if (_operations.IsRetired) return;
        try { await _operations.RunAsync(_ => callback()); }
        catch (OperationCanceledException) when (_operations.IsRetired) { }
    }

    private static string GetStateIcon(JobExecutionState state) => state switch
    {
        JobExecutionState.Queued => MoIcons.Clock,
        JobExecutionState.Running => MoIcons.PlayCircle,
        JobExecutionState.Succeeded => MoIcons.Success,
        JobExecutionState.Failed => MoIcons.Error,
        JobExecutionState.Cancelled => MoIcons.Error,
        JobExecutionState.Skipped => MoIcons.SkipNext,
        _ => MoIcons.Help
    };

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _operations.RetireAsync(() => { });
}
