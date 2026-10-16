using Monica.Core.Execution.Models;
using Monica.UI.Icons;
using MudBlazor;

namespace Monica.Framework.UI.UIExecutionPipeline.Support;

internal static class ExecutionPipelinePresentation
{
    public static Color GetStatusColor(ExecutionPipelinePlanStatus status) => status switch
    {
        ExecutionPipelinePlanStatus.Building => Color.Info,
        ExecutionPipelinePlanStatus.Ready => Color.Success,
        ExecutionPipelinePlanStatus.Faulted => Color.Error,
        _ => Color.Default
    };

    public static string GetStatusIcon(ExecutionPipelinePlanStatus status) => status switch
    {
        ExecutionPipelinePlanStatus.Building => MoIcons.Pending,
        ExecutionPipelinePlanStatus.Ready => MoIcons.Success,
        ExecutionPipelinePlanStatus.Faulted => MoIcons.Error,
        _ => MoIcons.Help
    };
}
