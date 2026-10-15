using Monica.Configuration.Models;
using Monica.Configuration.UI.State;
using Monica.Core.Results;

namespace Test.Monica.Configuration.UI.State;

public sealed class ConfigurationStoragePageStateTests
{
    [Fact]
    public async Task Load_WhenOneReadFails_ShouldAcceptOtherEmptyAndPopulatedReadsIndependently()
    {
        await using var state = new ConfigurationStoragePageState(
            () => Task.FromException<Res<ConfigurationStorageOverview>>(new InvalidOperationException("overview unavailable")),
            () => Task.FromResult(States()),
            () => Task.FromResult(Inventories(Inventory("source"))), "unavailable");

        await ObserveAsync(state.LoadAsync());

        Assert.False(state.Loading);
        Assert.False(state.Overview.HasLoaded);
        Assert.Equal("overview unavailable", state.Overview.Error);
        Assert.Null(state.Overview.ObservedAtUtc);
        Assert.True(state.StoreStates.HasLoaded);
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<ConfigurationStoreState>>(state.StoreStates.Value));
        Assert.NotNull(state.StoreStates.ObservedAtUtc);
        Assert.True(state.SourceInventories.HasLoaded);
        Assert.Equal("source", Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<ConfigurationSourceInventory>>(
            state.SourceInventories.Value)).Source.SourceKey);
        Assert.True(state.HasAcceptedEvidence);
    }

    [Fact]
    public async Task Refresh_ShouldRetainAcceptedEvidenceUntilEachNewestOutcomeIsAccepted()
    {
        var pendingOverview = NewCompletion<Res<ConfigurationStorageOverview>>();
        var pendingStates = NewCompletion<Res<IReadOnlyList<ConfigurationStoreState>>>();
        var pendingInventories = NewCompletion<Res<IReadOnlyList<ConfigurationSourceInventory>>>();
        var overviewCalls = 0;
        var stateCalls = 0;
        var inventoryCalls = 0;
        var clock = new FixedTimeProvider(new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        await using var state = new ConfigurationStoragePageState(
            () => ++overviewCalls == 1 ? Task.FromResult(Res.Ok(Overview("original"))) : pendingOverview.Task,
            () => ++stateCalls == 1 ? Task.FromResult(States(new ConfigurationStoreState { StoreKey = "original" })) : pendingStates.Task,
            () => ++inventoryCalls == 1 ? Task.FromResult(Inventories(Inventory("original"))) : pendingInventories.Task,
            "unavailable", clock);
        await ObserveAsync(state.LoadAsync());
        var overview = state.Overview.Value;
        var states = state.StoreStates.Value;
        var inventories = state.SourceInventories.Value;
        var observed = state.Overview.ObservedAtUtc;
        var refresh = state.LoadAsync();

        Assert.True(state.Loading);
        Assert.Same(overview, state.Overview.Value);
        Assert.Same(states, state.StoreStates.Value);
        Assert.Same(inventories, state.SourceInventories.Value);
        Assert.Equal(observed, state.Overview.ObservedAtUtc);
        clock.UtcNow = clock.UtcNow.AddMinutes(1);
        pendingOverview.SetResult(Res.Fail("new overview failed"));
        pendingStates.SetResult(States());
        pendingInventories.SetResult(Inventories(Inventory("new")));
        await ObserveAsync(refresh);

        Assert.False(state.Overview.HasLoaded);
        Assert.Null(state.Overview.Value);
        Assert.Null(state.Overview.ObservedAtUtc);
        Assert.Equal("new overview failed", state.Overview.Error);
        Assert.True(state.StoreStates.HasLoaded);
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<ConfigurationStoreState>>(state.StoreStates.Value));
        Assert.Equal(clock.UtcNow, state.StoreStates.ObservedAtUtc);
        Assert.Equal("new", Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<ConfigurationSourceInventory>>(
            state.SourceInventories.Value)).Source.SourceKey);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Refresh_WhenOlderReadCompletesLate_ShouldNotReplaceNewerEvidenceOrError(bool olderFails)
    {
        var pending = NewCompletion<Res<ConfigurationStorageOverview>>();
        var calls = 0;
        await using var state = new ConfigurationStoragePageState(
            () => ++calls == 1 ? pending.Task : Task.FromResult(Res.Ok(Overview("new"))),
            () => Task.FromResult(States()), () => Task.FromResult(Inventories()), "unavailable");
        var older = state.LoadAsync();
        await ObserveAsync(state.LoadAsync());
        pending.SetResult(olderFails ? Res.Fail("old failure") : Res.Ok(Overview("old")));
        await ObserveAsync(older);

        Assert.Equal("new", Assert.IsType<ConfigurationStorageOverview>(state.Overview.Value).EffectiveValueStore.StoreKey);
        Assert.Null(state.Overview.Error);
        Assert.True(state.Overview.HasLoaded);
    }

    [Fact]
    public async Task Refresh_WhenProducerReentersSynchronously_ShouldReserveEveryReadBeforeCallingAnyProducer()
    {
        ConfigurationStoragePageState? state = null;
        Task? nestedRefresh = null;
        var overviewCalls = 0;
        var stateCalls = 0;
        var inventoryCalls = 0;
        state = new ConfigurationStoragePageState(() =>
        {
            if (++overviewCalls == 1)
            {
                nestedRefresh = state!.LoadAsync();
                return Task.FromResult(Res.Ok(Overview("old")));
            }
            return Task.FromResult(Res.Ok(Overview("new")));
        }, () => Task.FromResult(States(new ConfigurationStoreState { StoreKey = ++stateCalls == 1 ? "new" : "old" })),
            () => Task.FromResult(Inventories(Inventory(++inventoryCalls == 1 ? "new" : "old"))), "unavailable");
        await using var owner = state;

        await ObserveAsync(state.LoadAsync());
        await ObserveAsync(Assert.IsAssignableFrom<Task>(nestedRefresh));

        Assert.Equal("new", Assert.IsType<ConfigurationStorageOverview>(state.Overview.Value).EffectiveValueStore.StoreKey);
        Assert.Equal("new", Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<ConfigurationStoreState>>(
            state.StoreStates.Value)).StoreKey);
        Assert.Equal("new", Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<ConfigurationSourceInventory>>(
            state.SourceInventories.Value)).Source.SourceKey);
    }

    [Fact]
    public async Task Load_ShouldCopyMutableProviderCollectionsBeforeAcceptingThem()
    {
        var states = new List<ConfigurationStoreState> { new() { StoreKey = "accepted" } };
        var items = new List<ConfigurationSourceInventoryItem>
        {
            new() { ConfigurationPath = "Example:Value", RelativeConfigurationPath = "Value", DisplayValue = "accepted" }
        };
        var inventories = new List<ConfigurationSourceInventory> { Inventory("accepted") with { Items = items } };
        await using var state = new ConfigurationStoragePageState(() => Task.FromResult(Res.Ok(Overview("accepted"))),
            () => Task.FromResult(Res.Ok<IReadOnlyList<ConfigurationStoreState>>(states)),
            () => Task.FromResult(Res.Ok<IReadOnlyList<ConfigurationSourceInventory>>(inventories)), "unavailable");
        await ObserveAsync(state.LoadAsync());

        states.Clear();
        items.Clear();
        inventories.Clear();

        Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<ConfigurationStoreState>>(state.StoreStates.Value));
        Assert.Single(Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<ConfigurationSourceInventory>>(
            state.SourceInventories.Value)).Items);
    }

    [Fact]
    public async Task Dispose_ShouldJoinActualTokenlessReadsAndShareCompletionWithoutAdoptingLateResults()
    {
        var overview = NewCompletion<Res<ConfigurationStorageOverview>>();
        var states = NewCompletion<Res<IReadOnlyList<ConfigurationStoreState>>>();
        var inventories = NewCompletion<Res<IReadOnlyList<ConfigurationSourceInventory>>>();
        var calls = 0;
        var state = new ConfigurationStoragePageState(() => { calls++; return overview.Task; },
            () => states.Task, () => inventories.Task, "unavailable");
        var load = state.LoadAsync();
        var first = state.DisposeAsync().AsTask();
        var repeated = state.DisposeAsync().AsTask();
        Assert.Same(first, repeated);
        Assert.False(first.IsCompleted);
        overview.SetResult(Res.Ok(Overview("late")));
        states.SetResult(Res.Fail("late failure"));
        Assert.False(first.IsCompleted);
        inventories.SetResult(Inventories(Inventory("late")));
        await ObserveAsync(load);
        await ObserveAsync(first);

        Assert.False(state.HasAcceptedEvidence);
        Assert.Null(state.Overview.Value);
        Assert.Null(state.StoreStates.Error);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ObserveAsync(state.LoadAsync()));
        Assert.Equal(1, calls);
    }

    private static ConfigurationStorageOverview Overview(string key)
    {
        var descriptor = new ConfigurationStoreDescriptor { StoreKey = key, DisplayName = key };
        return new() { EffectiveValueStore = descriptor, HistoryStore = descriptor, MetadataStore = descriptor };
    }

    private static ConfigurationSourceInventory Inventory(string key) => new()
    {
        Source = new() { SourceKey = key, DisplayName = key, ProviderType = "ReviewProvider" }
    };
    private static Res<IReadOnlyList<ConfigurationStoreState>> States(params ConfigurationStoreState[] values) => Res.Ok<IReadOnlyList<ConfigurationStoreState>>(values);
    private static Res<IReadOnlyList<ConfigurationSourceInventory>> Inventories(params ConfigurationSourceInventory[] values) => Res.Ok<IReadOnlyList<ConfigurationSourceInventory>>(values);
    private static TaskCompletionSource<T> NewCompletion<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static Task ObserveAsync(Task task) => task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        internal DateTimeOffset UtcNow { get; set; } = utcNow;
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
