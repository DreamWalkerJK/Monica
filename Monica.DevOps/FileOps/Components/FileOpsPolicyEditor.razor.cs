using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Monica.DevOps.FileOps.Facades;
using Monica.DevOps.FileOps.Models;
using Monica.DevOps.FileOps.Pages;
using Monica.DevOps.FileOps.State;
using Monica.DevOps.Localization;
using Monica.UI.Shell.Support;
using MudBlazor;

namespace Monica.DevOps.FileOps.Components;

/// <summary>Owns a native policy dialog's detached form, revisions and reads, borrowing the Files visit.</summary>
public partial class FileOpsPolicyEditor
{
    [Inject] private FileOpsFacade Facade { get; set; } = null!;
    [Inject] private IStringLocalizer<FileOpsResource> L { get; set; } = null!;
    [Inject] private PageAccessEvaluator PageAccess { get; set; } = null!;
    [CascadingParameter] private IMudDialogInstance Dialog { get; set; } = null!;
    /// <summary>The Files visit's controller; this form never disposes or reinitializes it.</summary>
    [Parameter, EditorRequired] public FileOpsWorkspaceController Controller { get; set; } = null!;
    /// <summary>Admits a captured policy through the existing Files controller and returns detached evidence.</summary>
    [Parameter, EditorRequired] public Func<FileOpsRuntimeConfig, Task<FileOpsPolicyApplyReceipt>> Apply { get; set; } = null!;
    /// <summary>Uses the visit's inner native decision owner for an unapplied policy draft.</summary>
    [Parameter, EditorRequired] public Func<Task<bool>> ConfirmDiscard { get; set; } = null!;
    /// <summary>Registers this form with the visit for coordinated route departure and teardown.</summary>
    [Parameter, EditorRequired] public Action<FileOpsPolicyEditor> Attach { get; set; } = null!;
    private FileOpsRuntimeConfig _draft = new();
    private string _roots = "", _readOnly = "", _extensions = "", _baseline = "";
    private long _revision;
    private bool _initialized, _pending, _reloadActive, _dismissPending, _retired, _newerEdits, _readbackFailed;
    private bool _accessDenied;
    private FileOpsOutcome? _command, _read;
    private MudButton? _closeButton;
    private Task? _retirement;
    private readonly CancellationTokenSource _reads = new();
    private readonly HashSet<Task> _users = [];
    private readonly object _sync = new();
    private bool Busy => _pending || _dismissPending || Controller.IsBusy;
    private bool Dirty => _initialized && Fingerprint() != _baseline;
    private bool Valid => _draft.MaxTextFileSizeBytes >= 4096 && _draft.MaxUploadFileSizeBytes >= 65536 &&
        _draft.MaxDownloadFileSizeBytes >= _draft.MaxUploadFileSizeBytes && _draft.MaxDirectoryEntries is >= 50 and <= 5000;

    protected override async Task OnInitializedAsync()
    {
        Attach(this);
        _pending = true;
        try { await Track(() => ReadAsync(_revision)); }
        finally { _pending = false; }
    }
    private void Edit(Action action) { if (_retired) return; action(); _revision++; }
    private void SetRoots(string value) => Edit(() => _roots = value);
    private void SetReadOnly(string value) => Edit(() => _readOnly = value);
    private void SetExtensions(string value) => Edit(() => _extensions = value);
    private void SetStartup(string value) => Edit(() => _draft.StartupPath = value);
    private void SetWrite(bool value) => Edit(() => _draft.AllowWriteOperations = value);
    private void SetDelete(bool value) => Edit(() => _draft.AllowDeleteOperations = value);
    private void SetHidden(bool value) => Edit(() => _draft.AllowHiddenEntries = value);
    private void SetTextLimit(long value) => Edit(() => _draft.MaxTextFileSizeBytes = value);
    private void SetUploadLimit(long value) => Edit(() => _draft.MaxUploadFileSizeBytes = value);
    private void SetDownloadLimit(long value) => Edit(() => _draft.MaxDownloadFileSizeBytes = value);
    private void SetEntryLimit(int value) => Edit(() => _draft.MaxDirectoryEntries = value);
    private FileOpsRuntimeConfig Capture()
    {
        var config = _draft.Clone();
        config.AllowedRoots = FileOpsRuntimeConfig.ParseMultiValue(_roots);
        config.ReadOnlyPaths = FileOpsRuntimeConfig.ParseMultiValue(_readOnly);
        config.EditableExtensions = FileOpsRuntimeConfig.ParseMultiValue(_extensions);
        return config;
    }
    private string Fingerprint() => DraftFingerprint(_draft, _roots, _readOnly, _extensions);
    private static string AcceptedFingerprint(FileOpsRuntimeConfig config) => DraftFingerprint(config,
        FileOpsRuntimeConfig.ToMultilineText(config.AllowedRoots),
        FileOpsRuntimeConfig.ToMultilineText(config.ReadOnlyPaths),
        FileOpsRuntimeConfig.ToMultilineText(config.EditableExtensions));
    private static string DraftFingerprint(FileOpsRuntimeConfig config, string roots, string readOnly, string extensions) =>
        JsonSerializer.Serialize(new { Roots = roots, ReadOnly = readOnly, Extensions = extensions,
            config.StartupPath, config.AllowWriteOperations, config.AllowDeleteOperations, config.AllowHiddenEntries,
            config.MaxTextFileSizeBytes, config.MaxUploadFileSizeBytes, config.MaxDownloadFileSizeBytes, config.MaxDirectoryEntries });
    private void Accept(FileOpsRuntimeConfig policy)
    {
        _draft = policy.Clone();
        _roots = FileOpsRuntimeConfig.ToMultilineText(policy.AllowedRoots);
        _readOnly = FileOpsRuntimeConfig.ToMultilineText(policy.ReadOnlyPaths);
        _extensions = FileOpsRuntimeConfig.ToMultilineText(policy.EditableExtensions);
        _baseline = Fingerprint(); _revision++; _initialized = true; _newerEdits = false;
    }
    private void Adopt(FileOpsRuntimeConfig policy, long capturedRevision)
    {
        if (_revision == capturedRevision) Accept(policy);
        else { _baseline = AcceptedFingerprint(policy); _newerEdits = true; }
    }
    private Task ApplyAsync()
    {
        if (_retired || Busy || _accessDenied || !Dirty || !Valid) return Task.CompletedTask;
        _pending = true;
        var revision = _revision;
        var captured = Capture();
        return Track(async () =>
        {
            try
            {
                if (!await AuthorizeAsync() || _retired) return;
                var receipt = await Apply(captured);
                if (_retired) return;
                if (receipt.AccessDenied) { _accessDenied = true; return; }
                // Refusal cannot reuse an earlier successful command as acceptance of this draft.
                if (receipt.Disposition != FileOpsOperationDisposition.Completed || receipt.Command is null) return;
                _command = receipt.Command; _read = receipt.Read;
                _readbackFailed = receipt.Command.Succeeded && receipt.AcceptedPolicy is null;
                if (receipt.AcceptedPolicy is { } accepted) Adopt(accepted, revision);
            }
            finally { _pending = false; }
        });
    }
    private Task ReloadAsync()
    {
        if (_retired || Busy) return Task.CompletedTask;
        _pending = true; _reloadActive = true;
        var revision = _revision;
        return Track(async () =>
        {
            try
            {
                if (!await AuthorizeAsync() || _retired) return;
                if (Dirty && !await ConfirmDiscard()) return;
                if (_retired || _revision != revision) return;
                await ReadAsync(revision);
            }
            finally { _pending = false; _reloadActive = false; }
        });
    }
    private async Task ReadAsync(long revision)
    {
        if (!await AuthorizeAsync() || _retired) return;
        var result = await Facade.GetRuntimeConfigAsync(_reads.Token);
        if (_retired) return;
        _read = FileOpsOutcome.From(FileOpsOperationKind.LoadPolicy, result, result.Data is not null);
        if (_read.Succeeded && result.Data is { } accepted)
        { Adopt(accepted, revision); _readbackFailed = false; }
    }
    private async Task<bool> AuthorizeAsync()
    {
        bool allowed;
        try { allowed = await PageAccess.IsAuthorizedAsync(UIFileOpsPolicyPage.PAGE_URL, _reads.Token); }
        catch { allowed = false; }
        if (_retired) return false;
        _accessDenied = !allowed;
        return allowed;
    }
    private Task KeyUpAsync(KeyboardEventArgs args) => args.Key == "Escape" ? RequestDismissAsync() : Task.CompletedTask;
    private Task RequestDismissAsync() => Track(async () =>
    {
        var consent = await PrepareLeaveAsync();
        if (consent is not null) FinishLeave(consent.Value, leave: true);
    });
    /// <summary>Reserves dismissal and obtains consent without clearing either the policy or file draft.</summary>
    public async Task<long?> PrepareLeaveAsync()
    {
        if (_retired || Busy) return null;
        _dismissPending = true;
        StateHasChanged();
        var revision = _revision;
        try
        {
            if (Dirty)
            {
                // Backdrop and title-bar Close are outside the native focus trap. Reserve a
                // connected inner opener so a refused nested decision restores modal focus.
                if (_closeButton is null) { _dismissPending = false; return null; }
                await _closeButton.FocusAsync();
                if (_retired || revision != _revision || !await ConfirmDiscard())
                { _dismissPending = false; if (!_retired) StateHasChanged(); return null; }
            }
            if (_retired || revision != _revision || _pending || Controller.IsBusy)
            { _dismissPending = false; if (!_retired) StateHasChanged(); return null; }
            return revision;
        }
        catch { _dismissPending = false; throw; }
    }
    /// <summary>Rechecks captured policy consent after the Files draft's separate leave decision.</summary>
    public bool IsLeaveCurrent(long revision) => !_retired && _dismissPending && !_pending && !Controller.IsBusy && revision == _revision;
    /// <summary>Releases refused leave or closes the still-current native form after both decisions agree.</summary>
    public void FinishLeave(long revision, bool leave)
    {
        if (leave && IsLeaveCurrent(revision)) Dialog.Cancel();
        _dismissPending = false;
        if (!_retired) StateHasChanged();
    }
    private Task Track(Func<Task> action)
    {
        TaskCompletionSource completion;
        lock (_sync)
        {
            if (_retired) return Task.CompletedTask;
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously); _users.Add(completion.Task);
        }
        _ = RunAsync();
        return completion.Task;
        async Task RunAsync()
        {
            try { await action(); completion.TrySetResult(); }
            catch (Exception exception) { completion.TrySetException(exception); }
            finally { lock (_sync) _users.Remove(completion.Task); }
        }
    }
    /// <summary>Retires only form-owned reads and users; the Files visit owns its controller and native leases.</summary>
    public ValueTask DisposeAsync()
    {
        TaskCompletionSource completion;
        Task[] users;
        lock (_sync)
        {
            if (_retirement is not null) return new(_retirement);
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously); _retirement = completion.Task;
            _retired = true; users = _users.ToArray();
        }
        _ = DrainAsync();
        return new(_retirement);
        async Task DrainAsync()
        {
            Exception? failure = null;
            try { await _reads.CancelAsync(); } catch (Exception exception) { failure = exception; }
            try { await Task.WhenAll(users); } catch (Exception exception) { failure = failure is null ? exception : new AggregateException(failure, exception); }
            _reads.Dispose();
            if (failure is null) completion.TrySetResult(); else completion.TrySetException(failure);
        }
    }
}

/// <summary>UI-only detached receipt of this Files visit's admitted policy command and subsequent evidence.</summary>
/// <param name="Disposition">Admission/confirmation disposition, independent of backend success.</param>
/// <param name="Command">The new SavePolicy outcome, or null when this request never wrote.</param>
/// <param name="Read">The admitted scope's last evidence read; browse/text failure does not undo confirmed policy.</param>
/// <param name="AcceptedPolicy">Confirmed normalized policy, or null when persistence/readback did not succeed.</param>
/// <param name="AccessDenied">The existing policy route's host-owned authority denied mutation admission.</param>
public sealed record FileOpsPolicyApplyReceipt(FileOpsOperationDisposition Disposition, FileOpsOutcome? Command,
    FileOpsOutcome? Read, FileOpsRuntimeConfig? AcceptedPolicy, bool AccessDenied = false);
