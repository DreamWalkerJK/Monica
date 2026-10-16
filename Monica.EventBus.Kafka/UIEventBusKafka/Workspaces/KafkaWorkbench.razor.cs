using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Monica.EventBus.Kafka.Models;
using Monica.EventBus.Kafka.Pages;
using Monica.EventBus.Kafka.UIEventBusKafka.Dialogs;
using Monica.EventBus.Kafka.UIEventBusKafka.State;
using Monica.EventBus.Kafka.UIEventBusKafka.Support;
using Monica.UI.Icons;
using MudBlazor;

namespace Monica.EventBus.Kafka.UIEventBusKafka.Workspaces;

/// <summary>Composes a routed Kafka task and owns its operations, dialogs, and sampler session.</summary>
public partial class KafkaWorkbench
{
    /// <summary>Gets or sets the task selected by the route wrapper.</summary>
    [Parameter] public KafkaWorkspace Workspace { get; set; }
    /// <summary>Gets or sets an exact portable cluster identity, including an unavailable identity.</summary>
    [Parameter] public string? ClusterId { get; set; }

    [Inject] private ILogger<KafkaWorkbench> Logger { get; set; } = null!;
    private readonly object _workSync = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly HashSet<Task> _operations = [];
    private readonly HashSet<IDialogReference> _dialogs = [];
    private KafkaWorkbenchSession _session = null!;
    private Task? _retirement;
    private bool _commandBusy;
    private bool _dialogInteraction;
    private volatile bool _retired;
    private bool _initializationStarted;
    private EventBusKafkaPageState State => _session.State;
    private bool InteractionBusy => !_dialogInteraction && IsWorking;
    private bool IsWorking => _commandBusy || State.IsBusy;

    private static readonly (KafkaWorkspace Workspace, string Icon)[] NavigationItems =
    [
        (KafkaWorkspace.Overview, MoIcons.Dashboard),
        (KafkaWorkspace.Clusters, MoIcons.Server),
        (KafkaWorkspace.Topics, MoIcons.Folder),
        (KafkaWorkspace.ConsumerGroups, MoIcons.Users),
        (KafkaWorkspace.Performance, MoIcons.HeartPulse)
    ];

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _session = SessionFactory.Create();
        _session.PerformancePolling.StateChanged += OnSamplingChanged;
    }

    /// <inheritdoc />
    protected override Task OnParametersSetAsync()
    {
        // Route wrappers key this owner by exact context, so a query transition retires its work.
        if (_initializationStarted) return Task.CompletedTask;
        _initializationStarted = true;
        var requested = ClusterId;
        return TrackAsync(() => State.InitializeAsync(requested, _lifetime.Token));
    }

    private string WorkspaceUrl(KafkaWorkspace workspace)
    {
        var path = workspace switch
        {
            KafkaWorkspace.Clusters => UIEventBusKafkaClustersPage.PAGE_URL,
            KafkaWorkspace.Topics => UIEventBusKafkaTopicsPage.PAGE_URL,
            KafkaWorkspace.ConsumerGroups => UIEventBusKafkaConsumerGroupsPage.PAGE_URL,
            KafkaWorkspace.Performance => UIEventBusKafkaPerformancePage.PAGE_URL,
            _ => UIEventBusKafkaPage.PAGE_URL
        };
        var id = State.SelectedClusterId ?? ClusterId;
        return id is null ? path : $"{path}?cluster={Uri.EscapeDataString(id)}";
    }

    private Task RefreshAsync() => CommandAsync(async () =>
    {
        var previous = State.SelectedClusterId;
        await State.RefreshAsync(_lifetime.Token);
        NotifyOutcome(string.IsNullOrWhiteSpace(State.ErrorMessage), "Page:Messages:RefreshSucceeded");
        if (!_retired && !string.Equals(previous, State.SelectedClusterId, StringComparison.Ordinal))
            Navigation.NavigateTo(WorkspaceUrl(Workspace));
    });

    private Task SelectClusterAsync(KafkaClusterSummary cluster) => SelectContextAsync(cluster.Config.ClusterId);
    private Task SelectContextAsync(string? clusterId) => CommandAsync(async () =>
    {
        if (clusterId is null) return;
        await State.SelectClusterAsync(clusterId, _lifetime.Token);
        if (!_retired) Navigation.NavigateTo(WorkspaceUrl(Workspace));
    });

    private Task TestClusterAsync(KafkaClusterSummary cluster)
    {
        var id = cluster.Config.ClusterId;
        return CommandAsync(async () =>
        {
            var success = await State.TestClusterAsync(id, _lifetime.Token);
            NotifyOutcome(success, "Clusters:Messages:TestSucceeded");
            if (success && !_retired) Navigation.NavigateTo(WorkspaceUrl(Workspace));
        });
    }

    private Task OpenCreateClusterAsync() => EditClusterAsync(null);
    private Task OpenEditClusterAsync(KafkaClusterSummary cluster) => EditClusterAsync(cluster.Config.Clone());
    private Task EditClusterAsync(KafkaClusterConfig? cluster) => DialogCommandAsync(async () =>
    {
        var result = await OpenOwnedAsync<CreateKafkaClusterDialog>(
            L[cluster is null ? "Clusters:Dialog:CreateTitle" : "Clusters:Dialog:EditTitle"],
            new DialogParameters<CreateKafkaClusterDialog> { { dialog => dialog.Cluster, cluster } }, KafkaDialogPresets.Standard);
        if (!_retired && result is { Canceled: false, Data: KafkaClusterConfig saved })
        {
            var previous = State.SelectedClusterId;
            var success = await State.UpsertClusterAsync(saved, _lifetime.Token);
            NotifyOutcome(success, "Clusters:Messages:Saved");
            if (success && !_retired && !string.Equals(previous, State.SelectedClusterId, StringComparison.Ordinal))
                Navigation.NavigateTo(WorkspaceUrl(Workspace));
        }
    });

    private Task DeleteClusterAsync(KafkaClusterSummary cluster)
    {
        var config = cluster.Config.Clone();
        return DialogCommandAsync(async () =>
        {
            if (await ConfirmAsync(L["Clusters:Confirm:DeleteTitle"], L["Clusters:Confirm:DeleteMessage", config.DisplayName], config.ClusterId, L["Common:Actions:Delete"]))
            {
                var success = await State.DeleteClusterAsync(config.ClusterId, _lifetime.Token);
                NotifyOutcome(success, "Clusters:Messages:Deleted");
                if (success && !_retired) Navigation.NavigateTo(WorkspaceUrl(Workspace));
            }
        });
    }

    private Task OpenCreateTopicAsync()
    {
        var id = State.SelectedClusterId;
        return DialogCommandAsync(async () =>
        {
            if (id is null) return;
            var result = await OpenOwnedAsync<CreateKafkaTopicDialog>(L["Topics:Dialog:CreateTitle"],
                new DialogParameters<CreateKafkaTopicDialog> { { dialog => dialog.ClusterId, id } }, KafkaDialogPresets.Standard);
            if (!_retired && result is { Canceled: false, Data: KafkaTopicCreateRequest request })
                NotifyOutcome(await State.CreateTopicAsync(request, _lifetime.Token), "Topics:Messages:Created");
        });
    }

    private Task DeleteTopicAsync(KafkaTopicSummary topic) => ConfirmTopicAsync(topic, false);
    private Task ClearMessagesAsync(KafkaTopicSummary topic) => ConfirmTopicAsync(topic, true);
    private Task ConfirmTopicAsync(KafkaTopicSummary topic, bool clear)
    {
        var id = State.SelectedClusterId;
        var name = topic.TopicName;
        return DialogCommandAsync(async () =>
        {
            if (id is null) return;
            var titleKey = clear ? "Topics:Confirm:ClearMessagesTitle" : "Topics:Confirm:DeleteTitle";
            var messageKey = clear ? "Topics:Confirm:ClearMessagesMessage" : "Topics:Confirm:DeleteMessage";
            var actionKey = clear ? "Topics:Actions:ClearMessages" : "Common:Actions:Delete";
            if (!await ConfirmAsync(L[titleKey], L[messageKey, name], $"{id}\n{name}", L[actionKey])) return;
            var success = clear
                ? await State.ClearTopicMessagesAsync(id, name, _lifetime.Token)
                : await State.DeleteTopicAsync(id, name, _lifetime.Token);
            NotifyOutcome(success, clear ? "Topics:Messages:Cleared" : "Topics:Messages:Deleted");
        });
    }

    private Task OpenPartitionsAsync(KafkaTopicSummary topic)
    {
        var id = State.SelectedClusterId;
        var name = topic.TopicName;
        return DialogCommandAsync(async () =>
        {
            if (id is null) return;
            var result = await OpenOwnedAsync<KafkaPartitionDialog>(L["Topics:Dialog:PartitionsTitle"],
                new DialogParameters<KafkaPartitionDialog> { { dialog => dialog.Topic, topic }, { dialog => dialog.ClusterId, id } }, KafkaDialogPresets.Standard);
            if (!_retired && result is { Canceled: false, Data: int count })
                NotifyOutcome(await State.IncreasePartitionsAsync(id, name, count, _lifetime.Token), "Topics:Messages:PartitionsUpdated");
        });
    }

    private Task OpenRetentionAsync(KafkaTopicSummary topic)
    {
        var id = State.SelectedClusterId;
        var name = topic.TopicName;
        return DialogCommandAsync(async () =>
        {
            if (id is null) return;
            var result = await OpenOwnedAsync<KafkaRetentionDialog>(L["Topics:Dialog:RetentionTitle"],
                new DialogParameters<KafkaRetentionDialog> { { dialog => dialog.Topic, topic }, { dialog => dialog.ClusterId, id } }, KafkaDialogPresets.Standard);
            if (!_retired && result is { Canceled: false, Data: long retention })
                NotifyOutcome(await State.UpdateRetentionAsync(id, name, retention, _lifetime.Token), "Topics:Messages:RetentionUpdated");
        });
    }

    private Task OpenMessagesAsync(KafkaTopicSummary topic)
    {
        var id = State.SelectedClusterId;
        var name = topic.TopicName;
        return DialogCommandAsync(async () =>
        {
            if (id is null) return;
            var batch = await State.ReadTopicMessagesAsync(id, name, 20, _lifetime.Token);
            if (batch is null) { ShowError(); return; }
            await OpenOwnedAsync<KafkaMessagesDialog>(L["Topics:Dialog:MessagesTitle", name],
                new DialogParameters<KafkaMessagesDialog> { { dialog => dialog.Batch, batch }, { dialog => dialog.ClusterId, id } }, KafkaDialogPresets.Message);
        });
    }

    private Task OpenBacklogAsync(KafkaTopicSummary topic)
    {
        var id = State.SelectedClusterId;
        var name = topic.TopicName;
        return DialogCommandAsync(async () =>
        {
            if (id is null) return;
            var snapshot = await State.GetTopicBacklogAsync(id, name, _lifetime.Token);
            if (snapshot is null) { ShowError(); return; }
            await OpenOwnedAsync<KafkaTopicBacklogDialog>(L["Topics:Dialog:BacklogTitle", name],
                new DialogParameters<KafkaTopicBacklogDialog> { { dialog => dialog.Snapshot, snapshot }, { dialog => dialog.ClusterId, id } }, KafkaDialogPresets.Message);
        });
    }

    private Task OpenTopicGroupsAsync(KafkaTopicSummary topic)
    {
        var id = State.SelectedClusterId;
        var name = topic.TopicName;
        return DialogCommandAsync(async () =>
        {
            if (id is null) return;
            await OpenOwnedAsync<KafkaTopicConsumerGroupsDialog>(L["Topics:ConsumerGroupsDialog:Title", name],
                new DialogParameters<KafkaTopicConsumerGroupsDialog> { { dialog => dialog.ClusterId, id }, { dialog => dialog.TopicName, name } }, KafkaDialogPresets.Detail);
        });
    }

    private Task OpenGroupAsync(KafkaConsumerGroupSummary group) => OpenGroupMetricsAsync(group.GroupId, null);
    private Task OpenPerformanceGroupAsync(KafkaConsumerGroupTopicMetrics metric) => OpenGroupMetricsAsync(metric.GroupId, metric.TopicName);
    private Task OpenGroupMetricsAsync(string groupId, string? topicName)
    {
        var id = State.SelectedClusterId;
        return DialogCommandAsync(async () =>
        {
            if (id is null) return;
            await OpenOwnedAsync<KafkaConsumerGroupMetricsDialog>(L[topicName is null ? "ConsumerGroups:MetricsDialog:Title" : "ConsumerGroups:MetricsDialog:TopicTitle"],
                new DialogParameters<KafkaConsumerGroupMetricsDialog>
                {
                    { dialog => dialog.ClusterId, id }, { dialog => dialog.GroupId, groupId }, { dialog => dialog.TopicName, topicName }
                }, KafkaDialogPresets.Detail);
        });
    }

    private Task CapturePerformanceAsync() => CommandAsync(async () =>
        NotifyOutcome(await State.CapturePerformanceAsync(_lifetime.Token), "Performance:Messages:Captured"));

    private async Task<bool> ConfirmAsync(string title, string message, string identity, string action)
    {
        var result = await OpenOwnedAsync<MudMessageBox>(title, new DialogParameters<MudMessageBox>
        {
            { dialog => dialog.TitleContent, ConfirmationHeading(title) },
            { dialog => dialog.MessageContent, ConfirmationBody(message, identity) },
            { dialog => dialog.YesText, action }, { dialog => dialog.CancelText, L["Common:Actions:Cancel"].Value }
        }, KafkaDialogPresets.Standard);
        return !_retired && result is { Canceled: false, Data: true };
    }

    private async Task<DialogResult?> OpenOwnedAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDialog>(
        string title, DialogParameters parameters, DialogOptions options) where TDialog : IComponent
    {
        if (_retired) return null;
        var dialog = await DialogService.ShowAsync<TDialog>(title, parameters, options);
        bool closeImmediately;
        lock (_workSync)
        {
            closeImmediately = _retired;
            if (!closeImmediately) _dialogs.Add(dialog);
        }
        Exception? closeFailure = null;
        if (closeImmediately)
        {
            try { dialog.Close(DialogResult.Cancel()); }
            catch (Exception exception) { closeFailure = exception; }
        }
        // A late producer still owns its actual native result, even when requesting closure fails.
        try
        {
            DialogResult? result;
            try { result = await dialog.Result; }
            catch (Exception exception) when (closeFailure is not null)
            {
                throw new AggregateException(closeFailure, exception);
            }
            if (closeFailure is not null) throw new AggregateException(closeFailure);
            return result;
        }
        finally { lock (_workSync) _dialogs.Remove(dialog); }
    }

    private void NotifyOutcome(bool success, string successKey)
    {
        if (_retired) return;
        if (success) Snackbar.Add(L[successKey], Severity.Success);
        else ShowError();
    }

    private void ShowError()
    {
        if (!_retired) Snackbar.Add(State.ErrorMessage ?? L["Page:Messages:OperationFailed"].Value, Severity.Error);
    }

    private Task DialogCommandAsync(Func<Task> action) => CommandAsync(action, dialogInteraction: true);

    private Task CommandAsync(Func<Task> action, bool dialogInteraction = false)
    {
        if (_retired || _commandBusy || State.IsBusy || State.IsLoading) return Task.CompletedTask;
        // Keep native openers focusable through fetch, dialog closure, and the resulting command.
        _dialogInteraction = dialogInteraction;
        _commandBusy = true;
        return TrackAsync(async () =>
        {
            try { await action(); }
            finally { _dialogInteraction = false; if (!_retired) _commandBusy = false; }
        });
    }

    private void OnSamplingChanged() => _ = TrackAsync(() => InvokeAsync(() => { if (!_retired) StateHasChanged(); }));

    private Task TrackAsync(Func<Task> action)
    {
        TaskCompletionSource completion;
        lock (_workSync)
        {
            if (_retired) return Task.CompletedTask;
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _operations.Add(completion.Task);
        }
        // Sampler notifications acquire this gate; never call the sampler while holding it.
        _ = CompleteTrackedAsync(action, completion);
        return completion.Task;
    }

    private async Task ExecuteAsync(Func<Task> action)
    {
        try { await action(); }
        catch (OperationCanceledException) when (_retired || _lifetime.IsCancellationRequested) { }
        catch (Exception exception)
        {
            Logger.LogError(exception, "The Kafka workspace operation failed.");
            if (_retired) throw;
            Snackbar.Add(L["Page:Messages:OperationFailed"], Severity.Error);
        }
    }

    private async Task CompleteTrackedAsync(Func<Task> action, TaskCompletionSource completion)
    {
        try { await ExecuteAsync(action); completion.TrySetResult(); }
        catch (Exception exception) { completion.TrySetException(exception); }
        finally { lock (_workSync) _operations.Remove(completion.Task); }
    }

    /// <summary>Stops admission, cancels provider work, closes and drains every produced dialog, then retires the session.</summary>
    /// <remarks>Repeated calls share one completion; retirement reports observed cleanup failures after all admitted work drains.</remarks>
    public ValueTask DisposeAsync()
    {
        TaskCompletionSource completion;
        lock (_workSync)
        {
            if (_retirement is not null) return new ValueTask(_retirement);
            _retired = true;
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _retirement = completion.Task;
        }
        _ = CompleteRetirementAsync(completion);
        return new ValueTask(completion.Task);
    }

    private async Task CompleteRetirementAsync(TaskCompletionSource completion)
    {
        try { await RetireAsync(); completion.TrySetResult(); }
        catch (Exception exception) { completion.TrySetException(exception); }
    }

    private async Task RetireAsync()
    {
        _session.PerformancePolling.StateChanged -= OnSamplingChanged;
        var failures = new List<Exception>();
        await ObserveRetirementAsync(_lifetime.CancelAsync());
        IDialogReference[] dialogs;
        Task[] operations;
        lock (_workSync) { dialogs = _dialogs.ToArray(); operations = _operations.ToArray(); }
        foreach (var dialog in dialogs)
        {
            try { dialog.Close(DialogResult.Cancel()); }
            catch (Exception exception) { failures.Add(exception); }
        }
        foreach (var operation in operations) await ObserveRetirementAsync(operation);
        try { await ObserveRetirementAsync(_session.DisposeAsync().AsTask()); }
        finally { _lifetime.Dispose(); }
        if (failures.Count > 0) throw new AggregateException("Kafka workspace retirement failed.", failures);

        async Task ObserveRetirementAsync(Task task)
        {
            try { await task; }
            catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
            catch (Exception exception) { failures.Add(exception); }
        }
    }
}
