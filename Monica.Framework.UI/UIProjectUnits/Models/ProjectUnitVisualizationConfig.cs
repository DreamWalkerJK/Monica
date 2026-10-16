using Monica.ProjectUnits.Models;
using Monica.UI.Icons;
using MudBlazor;

namespace Monica.Framework.UI.UIProjectUnits.Models;

public static class ProjectUnitVisualizationConfig
{
    public static Color GetUnitTypeMudColor(EProjectUnitType unitType)
    {
        return unitType switch
        {
            EProjectUnitType.ApplicationService => Color.Primary,
            EProjectUnitType.CrudApplicationService => Color.Primary,
            EProjectUnitType.DomainService => Color.Success,
            EProjectUnitType.Repository => Color.Warning,
            EProjectUnitType.DomainEvent => Color.Secondary,
            EProjectUnitType.DomainEventHandler => Color.Tertiary,
            EProjectUnitType.LocalEventHandler => Color.Info,
            EProjectUnitType.RecurringJob => Color.Info,
            EProjectUnitType.TriggeredJob => Color.Success,
            EProjectUnitType.HostedService => Color.Info,
            EProjectUnitType.HttpApi => Color.Error,
            EProjectUnitType.GrpcApi => Color.Secondary,
            EProjectUnitType.Entity => Color.Dark,
            EProjectUnitType.RequestDto => Color.Default,
            EProjectUnitType.Configuration => Color.Warning,
            EProjectUnitType.Seeder => Color.Success,
            EProjectUnitType.StateStore => Color.Error,
            EProjectUnitType.EventBus => Color.Info,
            EProjectUnitType.Actor => Color.Tertiary,
            _ => Color.Default
        };
    }

    public static bool IsComplexUnitType(EProjectUnitType unitType)
    {
        return unitType is EProjectUnitType.ApplicationService
            or EProjectUnitType.CrudApplicationService
            or EProjectUnitType.DomainService
            or EProjectUnitType.RecurringJob
            or EProjectUnitType.TriggeredJob
            or EProjectUnitType.HostedService
            or EProjectUnitType.HttpApi
            or EProjectUnitType.GrpcApi;
    }

    public static string GetUnitTypeColorRole(EProjectUnitType unitType)
    {
        return unitType switch
        {
            EProjectUnitType.ApplicationService => "primary",
            EProjectUnitType.CrudApplicationService => "primary",
            EProjectUnitType.DomainService => "success",
            EProjectUnitType.Repository => "warning",
            EProjectUnitType.DomainEvent => "secondary",
            EProjectUnitType.DomainEventHandler => "tertiary",
            EProjectUnitType.LocalEventHandler => "info",
            EProjectUnitType.RecurringJob => "info",
            EProjectUnitType.TriggeredJob => "success",
            EProjectUnitType.HostedService => "info",
            EProjectUnitType.HttpApi => "error",
            EProjectUnitType.GrpcApi => "secondary",
            EProjectUnitType.Entity => "dark",
            EProjectUnitType.RequestDto => "default",
            EProjectUnitType.Configuration => "warning",
            EProjectUnitType.Seeder => "success",
            EProjectUnitType.StateStore => "error",
            EProjectUnitType.EventBus => "info",
            EProjectUnitType.Actor => "tertiary",
            _ => "default"
        };
    }

    public static string GetUnitTypeIcon(EProjectUnitType unitType)
    {
        return unitType switch
        {
            EProjectUnitType.ApplicationService => MoIcons.Briefcase,
            EProjectUnitType.CrudApplicationService => MoIcons.NotebookEdit,
            EProjectUnitType.DomainService => MoIcons.Building,
            EProjectUnitType.Repository => MoIcons.Database,
            EProjectUnitType.DomainEvent => MoIcons.Calendar,
            EProjectUnitType.DomainEventHandler => MoIcons.CalendarCheck,
            EProjectUnitType.LocalEventHandler => MoIcons.CalendarDays,
            EProjectUnitType.RecurringJob => MoIcons.History,
            EProjectUnitType.TriggeredJob => MoIcons.Clock,
            EProjectUnitType.HostedService => MoIcons.HardHat,
            EProjectUnitType.HttpApi => MoIcons.Globe,
            EProjectUnitType.GrpcApi => MoIcons.Plug,
            EProjectUnitType.Entity => MoIcons.Table,
            EProjectUnitType.RequestDto => MoIcons.Braces,
            EProjectUnitType.Configuration => MoIcons.Settings,
            EProjectUnitType.Seeder => MoIcons.Sprout,
            EProjectUnitType.StateStore => MoIcons.Cpu,
            EProjectUnitType.EventBus => MoIcons.Network,
            EProjectUnitType.Actor => MoIcons.User,
            _ => MoIcons.Circle
        };
    }

    public static string? GetHighestAlertLevel(ProjectUnitSummary unit)
    {
        if (!unit.Alerts.Any())
        {
            return null;
        }

        if (unit.Alerts.Any(alert => alert.Level == EAlertLevel.Error))
        {
            return "error";
        }

        if (unit.Alerts.Any(alert => alert.Level == EAlertLevel.Warning))
        {
            return "warning";
        }

        return unit.Alerts.Any(alert => alert.Level == EAlertLevel.Info)
            ? "info"
            : null;
    }
}
