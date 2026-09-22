using AwesomeAssertions;
using Microsoft.JSInterop;
using Monica.AI.UI.UIChat.Support;
using Monica.UI.Shell.Support;

namespace Test.Monica.AI.UI.UIChat.State;

public sealed class ChatReadingWidthLifetimeTests
{
    [Fact]
    public async Task DisposeDuringImport_ShouldReleaseLateModuleWithoutInstallingCallbacks()
    {
        var imported = new TaskCompletionSource<IJSObjectReference>(TaskCreationOptions.RunContinuationsAsynchronously);
        var module = new Reference();
        var session = new ChatReadingWidthSession(new Runtime(imported.Task), new Storage());
        var initialize = session.InitializeAsync(default);
        var dispose = session.DisposeAsync().AsTask();
        imported.SetResult(module);
        await Task.WhenAll(initialize, dispose);
        await session.DisposeAsync();

        module.DisposeCount.Should().Be(1);
        module.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task FaultedControllerCreation_ShouldStillReleaseImportedModule()
    {
        var module = new Reference { FailCreation = true };
        var session = new ChatReadingWidthSession(new Runtime(Task.FromResult<IJSObjectReference>(module)), new Storage());
        var initialize = () => session.InitializeAsync(default);
        await initialize.Should().ThrowAsync<InvalidOperationException>();
        var dispose = () => session.DisposeAsync().AsTask();
        await dispose.Should().ThrowAsync<InvalidOperationException>();

        module.DisposeCount.Should().Be(1);
    }

    [Fact]
    public async Task DisposeDuringPersistence_ShouldDrainWriteAndRejectLaterCallbacks()
    {
        var controller = new Reference();
        var module = new Reference { Child = controller };
        var storage = new Storage { Write = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously) };
        var session = new ChatReadingWidthSession(new Runtime(Task.FromResult<IJSObjectReference>(module)), storage);
        await session.InitializeAsync(default);
        var save = session.CommitWidthAsync(1120);
        var dispose = session.DisposeAsync().AsTask();
        dispose.IsCompleted.Should().BeFalse();
        await session.CommitWidthAsync(800);
        storage.Values.Should().Equal(1120);
        storage.Write.SetResult();
        await Task.WhenAll(save, dispose);
        controller.Invocations.Should().Equal("shutdown");
        controller.DisposeCount.Should().Be(1);
    }

    private sealed class Runtime(Task<IJSObjectReference> import) : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => ReadAsync<TValue>();
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => ReadAsync<TValue>();
        private async ValueTask<TValue> ReadAsync<TValue>() => (TValue)(object)await import;
    }

    private sealed class Reference : IJSObjectReference
    {
        public Reference? Child { get; init; }
        public bool FailCreation { get; init; }
        public int DisposeCount { get; private set; }
        public List<string> Invocations { get; } = [];
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeCore<TValue>(identifier);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => InvokeCore<TValue>(identifier);
        private ValueTask<TValue> InvokeCore<TValue>(string identifier)
        {
            if (DisposeCount > 0) throw new InvalidOperationException("Invoked a disposed reference.");
            Invocations.Add(identifier);
            if (FailCreation) throw new InvalidOperationException("Controller initialization failed.");
            return ValueTask.FromResult(Child is not null ? (TValue)(object)Child : default!);
        }
        public ValueTask DisposeAsync() { DisposeCount++; return ValueTask.CompletedTask; }
    }

    private sealed class Storage : IBrowserStorage
    {
        public TaskCompletionSource? Write { get; init; }
        public List<int> Values { get; } = [];
        public Task<T> GetAsync<T>(string key, T defaultValue, BrowserStorageType storageType = BrowserStorageType.Local) => Task.FromResult(defaultValue);
        public Task SetAsync<T>(string key, T value, BrowserStorageType storageType = BrowserStorageType.Local)
        {
            Values.Add((int)(object)value!);
            return Write?.Task ?? Task.CompletedTask;
        }
        public Task<BrowserStorageWriteResult> TrySetAsync<T>(string key, T value, BrowserStorageType storageType = BrowserStorageType.Local) => throw new NotSupportedException();
        public Task RemoveAsync(string key, BrowserStorageType storageType = BrowserStorageType.Local) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> GetKeysAsync(string category, BrowserStorageType storageType = BrowserStorageType.Local) => throw new NotSupportedException();
        public Task<int> ClearCategoryAsync(string category, BrowserStorageType storageType = BrowserStorageType.Local) => throw new NotSupportedException();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
