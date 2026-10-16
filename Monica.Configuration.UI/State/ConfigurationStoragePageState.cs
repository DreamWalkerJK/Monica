using Monica.Configuration.Facades;
using Monica.Configuration.Models;
using Monica.Configuration.UI.Support;
using Monica.Core.Results;

namespace Monica.Configuration.UI.State;

/// <summary>Owns independently accepted storage, operation-state, and provider-inventory reads for one page.</summary>
/// <remarks>
/// These reads are separate observations, not a transaction or health probe. Failures invalidate only their own
/// evidence; pending refreshes retain it. Services are borrowed. Await disposal to drain tokenless producers.
/// </remarks>
internal sealed class ConfigurationStoragePageState : IAsyncDisposable
{
    private readonly ConfigurationOperationLifetime _lifetime;
    private readonly Func<Task<Res<ConfigurationStorageOverview>>> _overview;
    private readonly Func<Task<Res<IReadOnlyList<ConfigurationStoreState>>>> _storeStates;
    private readonly Func<Task<Res<IReadOnlyList<ConfigurationSourceInventory>>>> _inventories;

    internal ConfigurationStoragePageState(ConfigurationFacade facade, string unavailableText,
        ConfigurationOperationLifetime lifetime)
        : this(facade.GetStorageOverviewAsync, facade.GetStoreStatesAsync,
            facade.GetConfigurationSourceInventoriesAsync, unavailableText, lifetime: lifetime) { }

    internal ConfigurationStoragePageState(Func<Task<Res<ConfigurationStorageOverview>>> overview,
        Func<Task<Res<IReadOnlyList<ConfigurationStoreState>>>> storeStates,
        Func<Task<Res<IReadOnlyList<ConfigurationSourceInventory>>>> inventories, string unavailableText,
        TimeProvider? timeProvider = null, ConfigurationOperationLifetime? lifetime = null)
    {
        _lifetime = lifetime ?? new();
        _overview = overview;
        _storeStates = storeStates;
        _inventories = inventories;
        Overview = new(_lifetime, unavailableText, timeProvider);
        StoreStates = new(_lifetime, unavailableText, timeProvider);
        SourceInventories = new(_lifetime, unavailableText, timeProvider);
    }

    internal ConfigurationReadState<ConfigurationStorageOverview> Overview { get; }
    internal ConfigurationReadState<IReadOnlyList<ConfigurationStoreState>> StoreStates { get; }
    internal ConfigurationReadState<IReadOnlyList<ConfigurationSourceInventory>> SourceInventories { get; }
    internal bool Loading => Overview.Loading || StoreStates.Loading || SourceInventories.Loading;
    internal bool HasAcceptedEvidence => Overview.HasLoaded || StoreStates.HasLoaded || SourceInventories.HasLoaded;

    /// <summary>Attempts each read independently; an unavailable read does not prevent the others completing.</summary>
    internal Task LoadAsync() => _lifetime.RunAsync(async _ =>
    {
        long overview = 0, states = 0, inventories = 0;
        // Reserve all three generations atomically before any facade's synchronous prefix can re-enter refresh.
        if (!_lifetime.TryApply(() =>
            {
                overview = Overview.BeginRead();
                states = StoreStates.BeginRead();
                inventories = SourceInventories.BeginRead();
            })) return;
        await Task.WhenAll(
            Overview.ReadReservedAsync(overview, _overview),
            StoreStates.ReadReservedAsync(states, _storeStates, values => Array.AsReadOnly(values.ToArray())),
            SourceInventories.ReadReservedAsync(inventories, _inventories, values => Array.AsReadOnly(values
                .Select(inventory => inventory with { Items = Array.AsReadOnly(inventory.Items.ToArray()) }).ToArray())));
    });

    public ValueTask DisposeAsync() => _lifetime.RetireAsync();
}
