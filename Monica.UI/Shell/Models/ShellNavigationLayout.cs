namespace Monica.UI.Shell.Models;

/// <summary>
/// Selects the shell's desktop navigation placement. Narrow screens use a temporary drawer in either layout.
/// </summary>
public enum ShellNavigationLayout
{
    /// <summary>A category rail and collapsible contextual sidebar. This is the default layout.</summary>
    Sidebar,

    /// <summary>A single-row top navigation bar with expandable category menus.</summary>
    Topbar
}
