using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Monica.DevOps.Git.State;
using Monica.DevOps.Localization;
using MudBlazor;

namespace Monica.DevOps.Git.Components;

/// <summary>Presents frozen, pre-redacted command targets; never executes a Git producer.</summary>
public partial class GitOperationConfirmDialog
{
    [Inject] private IStringLocalizer<GitResource> L { get; set; } = null!;
    [CascadingParameter] private IMudDialogInstance Dialog { get; set; } = null!;
    /// <summary>Gets or sets the reserved action; the default is one working-copy synchronization.</summary>
    [Parameter] public GitCommandKind Kind { get; set; }
    /// <summary>Gets or sets the frozen encoded-safe target ledger captured before dialog admission.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<GitOperationConfirmTarget> Targets { get; set; } = [];
    private string Title => L[Kind switch { GitCommandKind.SyncAll => "Workspace:Confirm:SyncAllTitle", GitCommandKind.DeleteLocal => "Workspace:Confirm:DeleteTitle", _ => "Workspace:Confirm:SyncTitle" }];
    private string ConfirmText => Kind == GitCommandKind.SyncAll ? L["Workspace:Confirm:SyncAllAction", Targets.Count] : L[Kind == GitCommandKind.DeleteLocal ? "Workspace:Confirm:DeleteAction" : "Workspace:Confirm:SyncAction"];
    private string Branch(string? value) => string.IsNullOrWhiteSpace(value) ? L["Workspace:Repositories:AutomaticBranch"] : value;
}

/// <summary>Frozen presentation metadata whose strings are redacted before native dialog publication.</summary>
public sealed record GitOperationConfirmTarget(string Id, string Path, string? ConfiguredBranch, string? ResolvedBranch, bool IsDirty, bool IsAvailable, string Remote);
