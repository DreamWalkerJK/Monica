using Microsoft.Extensions.Localization;
using Monica.ServiceDiscovery.Localization;
using Monica.ServiceDiscovery.Models;
using Monica.UI.Icons;
using MudBlazor;

namespace Monica.ServiceDiscovery.UIServiceDiscovery.Support;

/// <summary>
/// Provides display metadata for <see cref="ServiceStatus"/>.
/// </summary>
public static class ServiceStatusExtensions
{
    private static readonly Dictionary<ServiceStatus, StatusDisplayInfo> Map = new()
    {
        [ServiceStatus.Running] = new(Color.Success, "Shared:ServiceStatuses:Running", MoIcons.Success),
        [ServiceStatus.Updating] = new(Color.Info, "Shared:ServiceStatuses:Updating", MoIcons.History),
        [ServiceStatus.Offline] = new(Color.Default, "Shared:ServiceStatuses:Offline", MoIcons.CloudOff),
        [ServiceStatus.Error] = new(Color.Error, "Shared:ServiceStatuses:Error", MoIcons.Error),
        [ServiceStatus.Unhealthy] = new(Color.Warning, "Shared:ServiceStatuses:Unhealthy", MoIcons.ShieldPlus),
    };

    /// <summary>
    /// Default display metadata for unmapped status values.
    /// </summary>
    private static readonly StatusDisplayInfo DefaultInfo =
        new(Color.Default, "Shared:ServiceStatuses:Unknown", MoIcons.Help);

    /// <summary>
    /// Gets the display metadata for the status.
    /// </summary>
    public static StatusDisplayInfo GetDisplayInfo(this ServiceStatus status)
        => Map.TryGetValue(status, out var info) ? info : DefaultInfo;

    /// <summary>
    /// Gets the MudBlazor color.
    /// </summary>
    public static Color GetColor(this ServiceStatus status) => status.GetDisplayInfo().Color;

    /// <summary>
    /// Gets the localized display text.
    /// </summary>
    public static string GetText(this ServiceStatus status, IStringLocalizer<ServiceDiscoveryResource> localizer)
        => localizer[status.GetDisplayInfo().TextKey];

    /// <summary>
    /// Gets the icon.
    /// </summary>
    public static string GetIcon(this ServiceStatus status) => status.GetDisplayInfo().Icon;

    /// <summary>
    /// Gets all statuses for UI filters.
    /// </summary>
    public static IEnumerable<ServiceStatus> GetAllStatuses() => Enum.GetValues<ServiceStatus>();
}
