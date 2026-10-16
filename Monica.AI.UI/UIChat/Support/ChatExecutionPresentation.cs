using Microsoft.Extensions.Localization;
using Monica.AI.Chat.Models;
using Monica.AI.UI.Localization;
using MudBlazor;

namespace Monica.AI.UI.UIChat.Support;

/// <summary>Formats the same recorded execution facts for transcript, ledger, and inspectors.</summary>
internal static class ChatExecutionPresentation
{
    internal static string Title(ChatExecutionStep step, IStringLocalizer<AIResource> localizer) => step.Kind switch
    {
        ChatExecutionStepKind.ModelRequest => step.Request?.ModelName ?? localizer["Workbench:ModelRequest"],
        ChatExecutionStepKind.Tool => step.Tool?.Name ?? localizer["Workbench:Tool"],
        _ => localizer["Workbench:Compaction"]
    };

    internal static string Status(ChatExecutionStatus status, IStringLocalizer<AIResource> localizer) => status switch
    {
        ChatExecutionStatus.Running => localizer["Workbench:Status:Running"],
        ChatExecutionStatus.Completed => localizer["Workbench:Status:Completed"],
        ChatExecutionStatus.Failed => localizer["Workbench:Status:Failed"],
        ChatExecutionStatus.Cancelled => localizer["Workbench:Status:Cancelled"],
        ChatExecutionStatus.AwaitingApproval => localizer["Workbench:Status:AwaitingApproval"],
        _ => localizer["Workbench:Status:Interrupted"]
    };

    internal static Color StatusColor(ChatExecutionStatus status) => status switch
    {
        ChatExecutionStatus.Completed => Color.Success,
        ChatExecutionStatus.Failed => Color.Error,
        ChatExecutionStatus.Cancelled or ChatExecutionStatus.Interrupted => Color.Warning,
        _ => Color.Info
    };

    internal static string Icon(ChatExecutionStepKind kind) => kind switch
    {
        ChatExecutionStepKind.Tool => Icons.Material.Outlined.Build,
        ChatExecutionStepKind.Compaction => Icons.Material.Outlined.Compress,
        _ => Icons.Material.Outlined.AutoAwesome
    };

    internal static string Duration(TimeSpan? duration) => duration is { } value
        ? value.TotalSeconds < 1 ? $"{value.TotalMilliseconds:0} ms" : $"{value.TotalSeconds:0.00} s"
        : "—";

    internal static string Tokens(int? count) => count is { } value ? $"{value:N0}" : "—";

    internal static string SearchText(ChatExecutionStep step) => string.Join(' ',
        step.Request?.ModelName, step.Request?.ProviderId, step.Tool?.Name,
        step.Tool?.Arguments, step.Tool?.Result, step.Error,
        string.Join(' ', step.Request?.Output.Select(part => part.Text) ?? []),
        step.Compaction?.Summary);
}
