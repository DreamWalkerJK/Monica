using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Monica.Modules;
using Monica.UI.Shell.Models;
using Monica.UI.Shell.State;
using Monica.UI.Shell.Support;
using Xunit;

namespace Test.Monica.UI.Shell.State;

public sealed class ShellStateTests
{
    [Theory]
    [InlineData(ShellNavigationLayout.Sidebar)]
    [InlineData(ShellNavigationLayout.Topbar)]
    public void Constructor_WhenBrowserPreferencesAreNotRestored_ShouldUseHostDefaultWithoutStorageAccess(
        ShellNavigationLayout hostDefault)
    {
        var storage = new RecordingBrowserStorage();
        var state = CreateState(storage, hostDefault);

        state.Layout.Should().Be(hostDefault);
        state.SidebarCollapsed.Should().BeFalse();
        storage.Reads.Should().BeEmpty();
        storage.Writes.Should().BeEmpty();
    }

    [Theory]
    [InlineData(ShellNavigationLayout.Sidebar, ShellNavigationLayout.Topbar)]
    [InlineData(ShellNavigationLayout.Topbar, ShellNavigationLayout.Sidebar)]
    public async Task InitializeAsync_WhenBrowserPreferencesExist_ShouldOverrideHostDefaultAndRestoreCollapse(
        ShellNavigationLayout hostDefault,
        ShellNavigationLayout savedLayout)
    {
        var storage = new RecordingBrowserStorage();
        storage.Values["shell:preferences"] = JsonSerializer.Serialize(new { layout = savedLayout, sidebarCollapsed = true });
        var state = CreateState(storage, hostDefault);

        await state.InitializeAsync();

        state.Layout.Should().Be(savedLayout);
        state.SidebarCollapsed.Should().BeTrue();
        storage.Reads.Should().Equal("shell:preferences");
        storage.Writes.Should().BeEmpty();
    }

    [Theory]
    [InlineData(ShellNavigationLayout.Sidebar)]
    [InlineData(ShellNavigationLayout.Topbar)]
    public async Task InitializeAsync_WhenStoredLayoutIsInvalid_ShouldFallBackToHostDefault(
        ShellNavigationLayout hostDefault)
    {
        var storage = new RecordingBrowserStorage();
        storage.Values["shell:preferences"] = "{\"layout\":999,\"sidebarCollapsed\":true}";
        var state = CreateState(storage, hostDefault);

        await state.InitializeAsync();

        state.Layout.Should().Be(hostDefault);
        state.SidebarCollapsed.Should().BeTrue();
        storage.Writes.Should().BeEmpty();
    }

    [Fact]
    public async Task InitializeAsync_WhenCalledAgain_ShouldKeepCurrentPreferencesAndReadStorageOnlyOnce()
    {
        var storage = new RecordingBrowserStorage();
        storage.Values["shell:preferences"] = "{\"layout\":1,\"sidebarCollapsed\":true}";
        var state = CreateState(storage);
        await state.InitializeAsync();
        await state.SetLayoutAsync(ShellNavigationLayout.Sidebar);
        storage.Values["shell:preferences"] = "{\"layout\":1,\"sidebarCollapsed\":false}";

        await state.InitializeAsync();

        state.Layout.Should().Be(ShellNavigationLayout.Sidebar);
        state.SidebarCollapsed.Should().BeTrue();
        storage.Reads.Should().Equal("shell:preferences");
    }

    [Fact]
    public async Task PreferenceChanges_WhenApplied_ShouldPersistLayoutAndCollapseWithoutChangingTheme()
    {
        var storage = new RecordingBrowserStorage();
        const string savedTheme = "{\"themeKind\":2,\"isDarkMode\":true}";
        storage.Values["theme:data"] = savedTheme;
        var state = CreateState(storage);
        var changes = 0;
        state.OnChanged += () => changes++;
        await state.InitializeAsync();

        await state.SetLayoutAsync(ShellNavigationLayout.Topbar);
        await state.ToggleSidebarAsync();

        state.Layout.Should().Be(ShellNavigationLayout.Topbar);
        state.SidebarCollapsed.Should().BeTrue();
        changes.Should().Be(2);
        storage.Writes.Should().HaveCount(2);
        storage.Writes.Should().OnlyContain(write => write.Key == "shell:preferences" && write.StorageType == BrowserStorageType.Local);
        using var saved = JsonDocument.Parse(storage.Values["shell:preferences"]);
        saved.RootElement.GetProperty("layout").GetInt32().Should().Be((int)ShellNavigationLayout.Topbar);
        saved.RootElement.GetProperty("sidebarCollapsed").GetBoolean().Should().BeTrue();
        storage.Values["theme:data"].Should().Be(savedTheme);
    }

    [Fact]
    public async Task SetLayoutAsync_WhenLayoutIsUnchanged_ShouldAvoidWritingAndNotifying()
    {
        var storage = new RecordingBrowserStorage();
        var state = CreateState(storage);
        var changes = 0;
        state.OnChanged += () => changes++;

        await state.SetLayoutAsync(ShellNavigationLayout.Sidebar);

        state.Layout.Should().Be(ShellNavigationLayout.Sidebar);
        changes.Should().Be(0);
        storage.Writes.Should().BeEmpty();
    }

    [Fact]
    public async Task ToggleSidebarAsync_WhenFirstWriteIsStillPending_ShouldPreserveBothRequestedToggles()
    {
        var releaseFirstWrite = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var storage = new RecordingBrowserStorage { FirstWriteWait = releaseFirstWrite.Task };
        var state = CreateState(storage);
        await state.InitializeAsync();

        var firstToggle = state.ToggleSidebarAsync();
        state.SidebarCollapsed.Should().BeTrue();
        var secondToggle = state.ToggleSidebarAsync();
        releaseFirstWrite.SetResult();
        await Task.WhenAll(firstToggle, secondToggle).WaitAsync(TestContext.Current.CancellationToken);

        state.SidebarCollapsed.Should().BeFalse();
        storage.Writes.Should().HaveCount(2);
        using var saved = JsonDocument.Parse(storage.Values["shell:preferences"]);
        saved.RootElement.GetProperty("sidebarCollapsed").GetBoolean().Should().BeFalse();
    }

    private static ShellState CreateState(
        IBrowserStorage storage,
        ShellNavigationLayout hostDefault = ShellNavigationLayout.Sidebar)
        => new(Options.Create(new ModuleShellUIOption { DefaultNavigationLayout = hostDefault }), storage);

    private sealed class RecordingBrowserStorage : IBrowserStorage
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public Dictionary<string, string> Values { get; } = new(StringComparer.Ordinal);
        public List<string> Reads { get; } = [];
        public List<(string Key, BrowserStorageType StorageType)> Writes { get; } = [];
        public Task? FirstWriteWait { get; init; }

        public Task<T> GetAsync<T>(string key, T defaultValue, BrowserStorageType storageType = BrowserStorageType.Local)
        {
            Reads.Add(key);
            return Task.FromResult(Values.TryGetValue(key, out var json)
                ? JsonSerializer.Deserialize<T>(json, JsonOptions) ?? defaultValue
                : defaultValue);
        }

        public async Task SetAsync<T>(string key, T value, BrowserStorageType storageType = BrowserStorageType.Local)
        {
            Values[key] = JsonSerializer.Serialize(value, JsonOptions);
            Writes.Add((key, storageType));
            if (Writes.Count == 1 && FirstWriteWait is not null)
            {
                await FirstWriteWait;
            }
        }

        public async Task<BrowserStorageWriteResult> TrySetAsync<T>(string key, T value, BrowserStorageType storageType = BrowserStorageType.Local)
        {
            await SetAsync(key, value, storageType);
            return new BrowserStorageWriteResult(BrowserStorageWriteFailureKind.None);
        }

        public Task RemoveAsync(string key, BrowserStorageType storageType = BrowserStorageType.Local) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> GetKeysAsync(string category, BrowserStorageType storageType = BrowserStorageType.Local) => throw new NotSupportedException();
        public Task<int> ClearCategoryAsync(string category, BrowserStorageType storageType = BrowserStorageType.Local) => throw new NotSupportedException();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
