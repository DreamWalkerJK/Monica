using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;
using Monica.Framework.UI.UIObservableInstance.Models;

namespace Monica.Framework.UI.UIObservableInstance.Support;

/// <summary>
/// Circuit-owned snapshot and filters shared across the four registry routes. Leaving the workspace
/// releases captured evidence and resets filters; returning starts a new read. There is no polling.
/// </summary>
public sealed class ObservableWorkspaceState : IDisposable
{
    private readonly NavigationManager _navigation;
    private bool _disposed;

    /// <summary>Creates the scoped owner and borrows the navigation dependency.</summary>
    public ObservableWorkspaceState(NavigationManager navigation)
    {
        _navigation = navigation;
        _navigation.LocationChanged += LocationChanged;
    }

    internal ObservableInstanceCapture? Capture { get; set; }
    internal string Search { get; set; } = string.Empty;
    internal string Status { get; set; } = string.Empty;
    internal HashSet<LogLevel> Levels { get; } = [];
    internal bool CriticalErrorOnly { get; set; }
    internal string Group { get; set; } = string.Empty;
    internal string Type { get; set; } = string.Empty;
    internal string From { get; set; } = string.Empty;
    internal string To { get; set; } = string.Empty;
    internal bool ExceptionsOnly { get; set; }

    internal void ResetFilters()
    {
        Search = Status = Group = Type = From = To = string.Empty;
        Levels.Clear();
        CriticalErrorOnly = false;
        ExceptionsOnly = false;
    }

    private void LocationChanged(object? sender, LocationChangedEventArgs args)
    {
        string path;
        try { path = _navigation.ToBaseRelativePath(args.Location).Split('?', '#')[0].TrimEnd('/'); }
        catch (ArgumentException) { path = string.Empty; }
        if (new[] { "observable-instance-monitor", "observable-instance-monitor/instances", "observable-instance-monitor/groups", "observable-instance-monitor/exceptions" }
            .Contains(path, StringComparer.OrdinalIgnoreCase)) return;
        Capture = null;
        ResetFilters();
    }

    /// <summary>Releases captured evidence and unsubscribes the borrowed navigation once.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _navigation.LocationChanged -= LocationChanged;
        Capture = null;
    }
}
