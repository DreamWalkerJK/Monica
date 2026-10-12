using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Localization;
using Monica.DevOps.FileOps.Dialogs;
using Monica.DevOps.FileOps.Facades;
using Monica.DevOps.FileOps.Models;
using Monica.DevOps.FileOps.State;
using Monica.DevOps.FileOps.Support;
using Monica.DevOps.Localization;
using MudBlazor;

namespace Monica.DevOps.FileOps.Components;

/// <summary>Owns the runtime-policy form's cloned draft, revisions, native decisions and routed visit.</summary>
public partial class FileOpsPolicyEditor
{
    [Inject] private FileOpsFacade Facade { get; set; } = null!;
    [Inject] private IDialogService Dialogs { get; set; } = null!;
    [Inject] private IStringLocalizer<FileOpsResource> L { get; set; } = null!;
    private FileOpsWorkspaceController _controller = null!;
    private FileOpsDialogOwner _dialogs = null!;
    private FileOpsRuntimeConfig _draft = new();
    private string _roots = "", _readOnly = "", _extensions = "", _baseline = "";
    private long _revision;
    private bool _initialized, _pending, _retired, _newerEdits;
    private Task? _retirement;
    private readonly HashSet<Task> _users = [];
    private readonly object _sync = new();
    private bool Busy => _pending || _controller?.IsBusy == true;
    private bool Dirty => _initialized && Fingerprint() != _baseline;
    private bool Valid => _draft.MaxTextFileSizeBytes >= 4096 && _draft.MaxUploadFileSizeBytes >= 65536 &&
        _draft.MaxDownloadFileSizeBytes >= _draft.MaxUploadFileSizeBytes && _draft.MaxDirectoryEntries is >= 50 and <= 5000;

    protected override async Task OnInitializedAsync()
    {
        _dialogs = new(Dialogs, action => InvokeAsync(action));
        _controller = new(Facade, ChangedAsync, () => _dialogs.DisposeAsync().AsTask());
        await Track(async () =>
        {
            await _controller.InitializeAsync(browseWorkspace: false);
            if (!_retired && _controller.RuntimeConfig is { } policy) Accept(policy);
        });
    }
    private Task ChangedAsync() => Track(() => InvokeAsync(() => { if (!_retired) StateHasChanged(); }));
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
    private Task ApplyAsync()
    {
        if (_retired || Busy || !Dirty || !Valid) return Task.CompletedTask;
        _pending = true;
        var revision = _revision;
        var captured = Capture();
        return Track(async () =>
        {
            try
            {
                await _controller.SavePolicyAsync(captured);
                if (_retired) return;
                if (_controller.LastCommand is { Kind: FileOpsOperationKind.SavePolicy, Succeeded: true } &&
                    _controller.LastRead is { Kind: FileOpsOperationKind.LoadPolicy, Succeeded: true } &&
                    _controller.RuntimeConfig is { } accepted)
                {
                    if (_revision == revision) Accept(accepted);
                    else { _baseline = AcceptedFingerprint(accepted); _newerEdits = true; }
                }
            }
            finally { _pending = false; }
        });
    }
    private Task ReloadAsync()
    {
        if (_retired || Busy) return Task.CompletedTask;
        _pending = true;
        var revision = _revision;
        return Track(async () =>
        {
            try
            {
                if (Dirty && !await ConfirmDiscardAsync(revision)) return;
                if (_retired || _revision != revision) return;
                await _controller.InitializeAsync(browseWorkspace: false);
                if (!_retired && _controller.LastRead is { Kind: FileOpsOperationKind.LoadPolicy, Succeeded: true } && _controller.RuntimeConfig is { } accepted)
                {
                    if (_revision == revision) Accept(accepted);
                    else { _baseline = AcceptedFingerprint(accepted); _newerEdits = true; }
                }
            }
            finally { _pending = false; }
        });
    }
    private async Task<bool> ConfirmDiscardAsync(long revision)
    {
        var result = await _dialogs.ShowAsync<FileOpsDiscardDialog>(L["Discard:PolicyTitle"],
            new() { [nameof(FileOpsDiscardDialog.IsPolicy)] = true },
            new() { FullWidth = true, MaxWidth = MaxWidth.Small, CloseOnEscapeKey = true });
        return !_retired && revision == _revision && result is { Canceled: false, Data: true };
    }
    private async Task BeforeNavigationAsync(LocationChangingContext context)
    {
        if (_retired) return;
        if (!Dirty) return;
        if (Busy) { context.PreventNavigation(); return; }
        _pending = true;
        var revision = _revision;
        try { await Track(async () => { if (!await ConfirmDiscardAsync(revision)) context.PreventNavigation(); }); }
        finally { _pending = false; }
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
    /// <summary>Closes native decisions before draining policy/controller work and renderer callbacks.</summary>
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
            try
            {
                var dialogs = _dialogs?.DisposeAsync().AsTask() ?? Task.CompletedTask;
                var controller = _controller?.DisposeAsync().AsTask() ?? Task.CompletedTask;
                await Task.WhenAll(dialogs, controller, Task.WhenAll(users)); completion.TrySetResult();
            }
            catch (Exception exception) { completion.TrySetException(exception); }
        }
    }
}
