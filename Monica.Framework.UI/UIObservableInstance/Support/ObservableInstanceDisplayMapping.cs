using Microsoft.Extensions.Logging;
using Monica.Framework.UI.UIObservableInstance.Models;
using Monica.UI.Icons;
using MudBlazor;

namespace Monica.Framework.UI.UIObservableInstance.Support;

/// <summary>
/// Provides shared UI display metadata for observable instance health and log states.
/// </summary>
public static class ObservableInstanceDisplayMapping
{
    internal static string IconStyle(Color color) => color switch
    {
        Color.Info => "color:var(--mud-palette-info)",
        Color.Warning => "color:var(--mud-palette-warning)",
        Color.Error => "color:var(--mud-palette-error)",
        Color.Success => "color:var(--mud-palette-success)",
        _ => "color:var(--mud-palette-text-secondary)"
    };

    /// <summary>
    /// Gets the MudBlazor color used to represent a log level.
    /// </summary>
    public static Color GetLogLevelColor(LogLevel? level) =>
        level switch
        {
            LogLevel.Trace => Color.Default,
            LogLevel.Debug => Color.Default,
            LogLevel.Information => Color.Info,
            LogLevel.Warning => Color.Warning,
            LogLevel.Error => Color.Error,
            LogLevel.Critical => Color.Error,
            _ => Color.Default
        };

    /// <summary>
    /// Gets the icon used to represent a log level.
    /// </summary>
    public static string GetLogLevelIcon(LogLevel? level) =>
        level switch
        {
            LogLevel.Trace => MoIcons.Code,
            LogLevel.Debug => MoIcons.Bug,
            LogLevel.Information => MoIcons.Info,
            LogLevel.Warning => MoIcons.Warning,
            LogLevel.Error => MoIcons.Error,
            LogLevel.Critical => MoIcons.Error,
            _ => MoIcons.Help
        };

    /// <summary>
    /// Gets the MudBlazor color used to represent health.
    /// </summary>
    public static Color GetHealthStateColor(HealthState state) =>
        state switch
        {
            HealthState.Healthy => Color.Success,
            HealthState.Unhealthy => Color.Error,
            _ => Color.Default
        };

    /// <summary>
    /// Gets the icon used to represent health.
    /// </summary>
    public static string GetHealthStateIcon(HealthState state) =>
        state switch
        {
            HealthState.Healthy => MoIcons.Success,
            HealthState.Unhealthy => MoIcons.Warning,
            _ => MoIcons.Help
        };
}
