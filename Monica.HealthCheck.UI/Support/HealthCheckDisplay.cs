using Monica.HealthCheck.Models;
using Monica.UI.Icons;
using MudBlazor;

namespace Monica.HealthCheck.UI.Support;

/// <summary>
/// Maps health states to the shared MudBlazor semantic presentation contract.
/// </summary>
public static class HealthCheckDisplay
{
    /// <summary>Gets the semantic MudBlazor color for a health state.</summary>
    public static Color Color(HealthCheckState state) => state switch
    {
        HealthCheckState.Healthy => MudBlazor.Color.Success,
        HealthCheckState.Degraded => MudBlazor.Color.Warning,
        HealthCheckState.Unhealthy => MudBlazor.Color.Error,
        _ => MudBlazor.Color.Default
    };

    /// <summary>Gets the semantic MudBlazor alert severity for a health state.</summary>
    public static Severity Severity(HealthCheckState state) => state switch
    {
        HealthCheckState.Healthy => MudBlazor.Severity.Success,
        HealthCheckState.Degraded => MudBlazor.Severity.Warning,
        HealthCheckState.Unhealthy => MudBlazor.Severity.Error,
        _ => MudBlazor.Severity.Normal
    };

    /// <summary>Gets the shared Monica icon for a health state.</summary>
    public static string Icon(HealthCheckState state) => state switch
    {
        HealthCheckState.Healthy => MoIcons.Success,
        HealthCheckState.Degraded => MoIcons.Warning,
        HealthCheckState.Unhealthy => MoIcons.Error,
        _ => MoIcons.Help
    };

    /// <summary>Gets the theme color declaration for a standalone health status icon.</summary>
    internal static string IconStyle(HealthCheckState state) => state switch
    {
        HealthCheckState.Healthy => "color:var(--mud-palette-success);",
        HealthCheckState.Degraded => "color:var(--mud-palette-warning);",
        HealthCheckState.Unhealthy => "color:var(--mud-palette-error);",
        _ => "color:var(--mud-palette-text-secondary);"
    };
}
