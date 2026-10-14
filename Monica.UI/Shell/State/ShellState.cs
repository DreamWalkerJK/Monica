using Microsoft.Extensions.Options;
using Monica.Modules;
using Monica.UI.Shell.Models;
using Monica.UI.Shell.Support;

namespace Monica.UI.Shell.State;

/// <summary>
/// Owns navigation preferences for one Blazor circuit. Browser preferences take precedence over host defaults.
/// </summary>
/// <remarks>
/// The shell calls <see cref="InitializeAsync"/> after its first interactive render. Browser storage is owned by
/// dependency injection; this state does not dispose it. Changes preserve the routed page instance.
/// </remarks>
public sealed class ShellState(IOptions<ModuleShellUIOption> options, IBrowserStorage storage)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ShellNavigationLayout _defaultLayout = Normalize(options.Value.DefaultNavigationLayout);
    private Task? _initialization;

    /// <summary>Raised when layout or sidebar visibility changes.</summary>
    public event Action? OnChanged;

    /// <summary>Gets the current desktop layout, initially the configured host default.</summary>
    public ShellNavigationLayout Layout { get; private set; } = Normalize(options.Value.DefaultNavigationLayout);

    /// <summary>Gets whether only the category rail remains visible on desktop.</summary>
    public bool SidebarCollapsed { get; private set; }

    /// <summary>
    /// Restores local browser preferences once. Call only after interactive rendering makes JS interop available.
    /// </summary>
    public Task InitializeAsync() => _initialization ??= RestoreAsync();

    /// <summary>Applies and persists a desktop layout. Mobile navigation remains a temporary drawer.</summary>
    public Task SetLayoutAsync(ShellNavigationLayout layout) => UpdateAsync(Normalize(layout), toggleCollapsed: false);

    /// <summary>Toggles and persists the desktop contextual sidebar without changing the current route.</summary>
    public Task ToggleSidebarAsync() => UpdateAsync(null, toggleCollapsed: true);

    private async Task RestoreAsync()
    {
        await _gate.WaitAsync();
        try
        {
            var saved = await storage.GetAsync<Preferences?>("shell:preferences", null);
            if (saved is null)
            {
                return;
            }

            Layout = Enum.IsDefined(saved.Layout) ? saved.Layout : _defaultLayout;
            SidebarCollapsed = saved.SidebarCollapsed;
            OnChanged?.Invoke();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task UpdateAsync(ShellNavigationLayout? requestedLayout, bool toggleCollapsed)
    {
        await InitializeAsync();
        await _gate.WaitAsync();
        try
        {
            var layout = requestedLayout ?? Layout;
            var collapsed = toggleCollapsed ? !SidebarCollapsed : SidebarCollapsed;
            if (Layout == layout && SidebarCollapsed == collapsed)
            {
                return;
            }

            Layout = layout;
            SidebarCollapsed = collapsed;
            OnChanged?.Invoke();
            await storage.SetAsync("shell:preferences", new Preferences(Layout, SidebarCollapsed));
        }
        finally
        {
            _gate.Release();
        }
    }

    private static ShellNavigationLayout Normalize(ShellNavigationLayout layout) =>
        Enum.IsDefined(layout) ? layout : ShellNavigationLayout.Sidebar;

    private sealed record Preferences(ShellNavigationLayout Layout, bool SidebarCollapsed);
}
