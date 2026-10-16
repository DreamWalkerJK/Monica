using Monica.Configuration.Models;
using Monica.Configuration.UI.Dialogs;
using Monica.Configuration.UI.State;
using Monica.Configuration.UI.Support;
using MudBlazor;

namespace Monica.Configuration.UI.Pages;

/// <summary>Reviews independent runtime provider, storage-capability, and latest store-operation evidence.</summary>
/// <remarks>
/// Observations are local accepted reads, not a single transaction or continuous health check. The page owns
/// tokenless reads and native file-dialog results. Custom owners must await teardown; services remain borrowed.
/// </remarks>
public partial class ConfigurationStoragePage
{
    /// <summary>Gets the Configuration Storage route.</summary>
    public const string PAGE_URL = ConfigurationUiRoutes.STORAGE_ROUTE;
    private readonly ConfigurationOperationLifetime _operations = new();
    private ConfigurationStoragePageState? _state;
    private bool _fileDialogOpen;
    private ConfigurationStorageOverview? _overview => _state?.Overview.Value;
    private IReadOnlyList<ConfigurationStoreState> _states => _state?.StoreStates.Value ?? [];
    private IReadOnlyList<ConfigurationSourceInventory> _sourceInventories => _state?.SourceInventories.Value ?? [];
    private bool _loading => _state?.Loading ?? true;
    private bool _hasLoadedOverview => _state?.Overview.HasLoaded ?? false;
    private bool _hasLoadedStoreStates => _state?.StoreStates.HasLoaded ?? false;
    private bool _hasLoadedSourceInventories => _state?.SourceInventories.HasLoaded ?? false;
    private bool _hasAcceptedEvidence => _state?.HasAcceptedEvidence ?? false;
    private string? _overviewLoadError => _state?.Overview.Error;
    private string? _storeStateLoadError => _state?.StoreStates.Error;
    private string? _sourceInventoryLoadError => _state?.SourceInventories.Error;
    private DateTimeOffset? _overviewObservedAtUtc => _state?.Overview.ObservedAtUtc;
    private DateTimeOffset? _storeStatesObservedAtUtc => _state?.StoreStates.ObservedAtUtc;
    private DateTimeOffset? _sourceInventoriesObservedAtUtc => _state?.SourceInventories.ObservedAtUtc;

    private IReadOnlyList<StoreDossier> StoreDossiers => _overview is not { } overview ? []
        : new[]
        {
            (Label: L["Storage:Roles:EffectiveValues"].Value, Store: overview.EffectiveValueStore),
            (Label: L["Storage:Roles:History"].Value, Store: overview.HistoryStore),
            (Label: L["Storage:Roles:Metadata"].Value, Store: overview.MetadataStore)
        }
        .GroupBy(static role => role.Store.StoreKey, StringComparer.OrdinalIgnoreCase)
        .Select(static group => new StoreDossier(group.First().Store, group.Select(static role => role.Label).ToArray()))
        .ToArray();

    protected override async Task OnInitializedAsync()
    {
        _state = new(Facade, L["Common:Errors:LoadFailed"], _operations);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_operations.IsRetired || _state is null) return;
        try { await _state.LoadAsync(); }
        catch (OperationCanceledException) when (_operations.IsRetired) { }
    }

    private async Task ViewFileAsync(ConfigurationSourceDescriptor source)
    {
        var sourceKey = source.SourceKey;
        var title = L["Sources:File:Title", source.DisplayName].Value;
        var admitted = false;
        if (!_operations.TryApply(() =>
            {
                if (_fileDialogOpen) return;
                _fileDialogOpen = admitted = true;
            }) || !admitted) return;
        try
        {
            var parameters = new DialogParameters<ConfigurationSourceFileDialog>();
            parameters.Add(dialog => dialog.SourceKey, sourceKey);
            await _operations.ShowDialogAsync(() => DialogService.ShowAsync<ConfigurationSourceFileDialog>(title, parameters,
                new DialogOptions { MaxWidth = MaxWidth.ExtraLarge, FullWidth = true, CloseButton = true, CloseOnEscapeKey = true }));
        }
        catch (OperationCanceledException) when (_operations.IsRetired) { }
        catch (Exception exception) when (!_operations.IsRetired)
        {
            Snackbar.Add($"{L["Common:Errors:LoadFailed"]}: {exception.Message}", Severity.Error);
        }
        finally { _operations.TryApply(() => _fileDialogOpen = false); }
    }

    /// <summary>Closes owned file dialogs and joins actual reads and native results before releasing the page.</summary>
    public ValueTask DisposeAsync() => _operations.RetireAsync();

    private string SourcePath(ConfigurationSourceDescriptor source)
    {
        return source.PhysicalPath ?? source.SourcePath ?? source.ProviderType;
    }

    private string ReloadText(ConfigurationSourceDescriptor source)
    {
        return source.ReloadOnChange is null
            ? L["Sources:Reload:Unknown"]
            : source.ReloadOnChange.Value ? L["Sources:Reload:Enabled"] : L["Sources:Reload:Disabled"];
    }

    private IReadOnlyList<SourceInventoryGroup> SourceInventoryGroups(ConfigurationSourceInventory inventory)
    {
        return inventory.Items
            .GroupBy(SourceInventoryGroupKey)
            .Select(group =>
            {
                var items = group
                    .OrderBy(static item => item.RelativeConfigurationPath, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                return new SourceInventoryGroup(
                    GroupTitle(items[0]),
                    GroupSubtitle(items[0]),
                    items,
                    items.Count(static item => item.IsEffective),
                    items.Count(static item => !item.IsManagedByMonica));
            })
            .OrderBy(group => group.UnmanagedCount > 0 ? 1 : 0)
            .ThenBy(group => group.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private string SourceInventoryGroupKey(ConfigurationSourceInventoryItem item)
    {
        return item.IsManagedByMonica
            ? item.DefinitionKey ?? item.DefinitionDisplayName ?? item.ConfigurationPath
            : L["Sources:Inventory:UnmanagedDefinition"].Value;
    }

    private string GroupTitle(ConfigurationSourceInventoryItem item)
    {
        return item.IsManagedByMonica
            ? item.DefinitionDisplayName ?? L["Sources:Inventory:UnknownDefinition"]
            : L["Sources:Inventory:UnmanagedDefinition"];
    }

    private string GroupSubtitle(ConfigurationSourceInventoryItem item)
    {
        if (!item.IsManagedByMonica)
        {
            return L["Sources:Inventory:RuntimeKey"];
        }

        return item.DefinitionKey ?? string.Empty;
    }

    private string FieldLabel(ConfigurationSourceInventoryItem item)
    {
        return string.IsNullOrWhiteSpace(item.NodeLabel)
            ? item.RelativeConfigurationPath
            : item.NodeLabel!;
    }

    private string DisplayValue(ConfigurationSourceInventoryItem item)
    {
        return item.IsSensitive ? L["State:Value:Sensitive"] : item.DisplayValue ?? L["Value:States:Empty"];
    }

    private ConfigurationSourceDescriptor? OverridingSourceFor(
        ConfigurationSourceInventoryItem item,
        ConfigurationSourceDescriptor currentSource)
    {
        if (item.IsEffective)
        {
            return null;
        }

        return _sourceInventories
            .OrderByDescending(static inventory => inventory.Source.PriorityIndex)
            .FirstOrDefault(inventory =>
                !string.Equals(inventory.Source.SourceKey, currentSource.SourceKey, StringComparison.Ordinal)
                && inventory.Items.Any(candidate => candidate.IsEffective && HasSameConfigurationPath(candidate, item)))
            ?.Source;
    }

    private string OverriddenTooltip(ConfigurationSourceDescriptor? source)
    {
        return source is null
            ? L["Sources:Inventory:OverriddenUnknownTooltip"]
            : L["Sources:Inventory:OverriddenTooltip", source.DisplayName, source.PriorityIndex, SourcePath(source)];
    }

    private static bool HasSameConfigurationPath(
        ConfigurationSourceInventoryItem left,
        ConfigurationSourceInventoryItem right)
    {
        return string.Equals(left.ConfigurationPath, right.ConfigurationPath, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record SourceInventoryGroup(
        string Title,
        string Subtitle,
        IReadOnlyList<ConfigurationSourceInventoryItem> Items,
        int EffectiveCount,
        int UnmanagedCount);

    private sealed record StoreDossier(ConfigurationStoreDescriptor Store, IReadOnlyList<string> Roles);
}
