using System.Text.Json;
using Monica.DevOps.K8S.Models;
using Monica.DevOps.K8S.State;
using Monica.UI.Shell.Support;
using Test.Monica.DevOps.K8S.Support;
using Xunit;

namespace Test.Monica.DevOps.K8S.State;

public sealed class K8SViewContextTests
{
    [Fact]
    public async Task Persist_WhenWritesOverlap_ShouldStoreLatestContextWithOnlyPortableFields()
    {
        var fixture = new K8SStateFixture();
        var navigation = new K8SNavigationState { Namespace = "alpha", ResourceType = K8SResourceType.Deployment, HasContext = true };
        await using var visit = new K8SInventoryVisitController(fixture.Facade, navigation, () => Task.CompletedTask);
        await visit.InitializeAsync();
        var storage = new ControlledContextStorage { WriteGate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        visit.SetSearch("first");
        var first = visit.PersistBrowserAsync(storage);
        await storage.Entered.Task;
        visit.SetSearch("latest");
        var latest = visit.PersistBrowserAsync(storage);
        storage.WriteGate.SetResult();
        await Task.WhenAll(first, latest);

        using var json = JsonDocument.Parse(storage.Written!);
        Assert.Equal(new[] { "Namespace", "ResourceType", "Search" }, json.RootElement.EnumerateObject().Select(x => x.Name));
        Assert.Equal("latest", json.RootElement.GetProperty("Search").GetString());
        Assert.Equal(BrowserStorageType.Session, storage.WrittenType);
        Assert.Equal("k8s:view-context", storage.WrittenKey);
        Assert.False(visit.IsBusy);
    }

    [Fact]
    public async Task Dispose_WhenBrowserReadFinishesLate_ShouldDrainWithoutAdoptingReturnContext()
    {
        var fixture = new K8SStateFixture();
        var navigation = new K8SNavigationState();
        var visit = new K8SInventoryVisitController(fixture.Facade, navigation, () => Task.CompletedTask);
        await visit.InitializeAsync();
        var storage = new ControlledContextStorage { ReadGate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var restore = visit.RestoreBrowserAsync(storage);
        await storage.Entered.Task;
        var retirement = visit.DisposeAsync().AsTask();
        try { Assert.False(retirement.IsCompleted); }
        finally { storage.ReadGate.TrySetResult(new("beta", K8SResourceType.Deployment, "late-query")); }
        await Task.WhenAll(restore, retirement);

        Assert.NotEqual("beta", visit.Namespace);
        Assert.NotEqual("late-query", navigation.Search);
        Assert.Equal(0, storage.DisposeCount);
    }

    private sealed class ControlledContextStorage : IBrowserStorage
    {
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource<K8SViewContext>? ReadGate { get; init; }
        internal TaskCompletionSource? WriteGate { get; init; }
        internal string? Written { get; private set; }
        internal string? WrittenKey { get; private set; }
        internal BrowserStorageType WrittenType { get; private set; }
        internal int DisposeCount { get; private set; }
        public async Task<T> GetAsync<T>(string key, T defaultValue, BrowserStorageType storageType = BrowserStorageType.Local)
        {
            Entered.TrySetResult();
            return ReadGate is null ? defaultValue : (T)(object)await ReadGate.Task;
        }
        public async Task<BrowserStorageWriteResult> TrySetAsync<T>(string key, T value, BrowserStorageType storageType = BrowserStorageType.Local)
        {
            Entered.TrySetResult();
            if (WriteGate is not null) await WriteGate.Task;
            WrittenKey = key;
            WrittenType = storageType;
            Written = JsonSerializer.Serialize(value);
            return new(BrowserStorageWriteFailureKind.None);
        }
        public Task SetAsync<T>(string key, T value, BrowserStorageType storageType = BrowserStorageType.Local) => throw new NotSupportedException();
        public Task RemoveAsync(string key, BrowserStorageType storageType = BrowserStorageType.Local) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> GetKeysAsync(string category, BrowserStorageType storageType = BrowserStorageType.Local) => throw new NotSupportedException();
        public Task<int> ClearCategoryAsync(string category, BrowserStorageType storageType = BrowserStorageType.Local) => throw new NotSupportedException();
        public ValueTask DisposeAsync() { DisposeCount++; return ValueTask.CompletedTask; }
    }
}
