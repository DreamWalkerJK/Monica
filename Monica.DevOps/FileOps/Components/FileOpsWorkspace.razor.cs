using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using Monica.DevOps.FileOps.Dialogs;
using Monica.DevOps.FileOps.Facades;
using Monica.DevOps.FileOps.Models;
using Monica.DevOps.FileOps.Pages;
using Monica.DevOps.FileOps.State;
using Monica.DevOps.FileOps.Support;
using Monica.DevOps.Localization;
using Monica.UI.Shell.Support;
using MudBlazor;

namespace Monica.DevOps.FileOps.Components;

/// <summary>Owns a single Files visit and its native dialogs, transfer, sizing and rendered reveal users.</summary>
public partial class FileOpsWorkspace
{
    [Inject] private FileOpsFacade Facade { get; set; } = null!;
    [Inject] private FileOpsNavigationState ReturnContext { get; set; } = null!;
    [Inject] private IDialogService Dialogs { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private IStringLocalizer<FileOpsResource> L { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private PageAccessEvaluator PageAccess { get; set; } = null!;
    /// <summary>Opens the same native policy dialog once after the compatibility query becomes interactive.</summary>
    [Parameter] public bool OpenPolicy { get; set; }
    private FileOpsWorkspaceController _controller = null!;
    private FileOpsDialogOwner _dialogs = null!;
    private FileOpsDialogOwner _policyDialogs = null!;
    private FileOpsPolicyEditor? _policyEditor;
    private bool _policyOpen, _policyWriting, _policyRequestConsumed;
    private bool _policyAccessDenied;
    private readonly CancellationTokenSource _policyAccess = new();
    private FileOpsTransferInteropSession _transfer = null!;
    private readonly object _sync = new();
    private readonly HashSet<Task> _users = [];
    private readonly string _visit = Guid.NewGuid().ToString("N");
    private Task? _retirement, _nativeRetirement, _sizingStop;
    private Task<IJSObjectReference>? _moduleImport, _sizingStart;
    private DotNetObjectReference<FileOpsWorkspace>? _callback;
    private bool _retired, _sizingAdmitted, _split, _tableMode, _catalogOpen = true;
    private bool _createActive;
    private long _focusVersion;
    private FocusIntent? _focusIntent;
    private string? _uploadName;
    private FileOpsOutcome? _observedCommand;
    private string? _commandTarget, _pendingCommandTarget;
    private ElementReference _root;
    private bool HasInspector => _controller?.InspectedEntry is not null || _controller?.InspectedFile is not null || _controller?.InspectionAttempt is not null;
    private string SelectedPath => _controller?.InspectedFile?.FullPath ?? _controller?.InspectedEntry?.FullPath ?? _controller?.InspectionAttempt?.FullPath ?? "";
    private string DirectoryName => _controller?.Browse?.Breadcrumbs.LastOrDefault()?.Name ?? "";
    private FileOpsOutcome? DirectoryCommand => _controller?.LastCommand?.Kind is FileOpsOperationKind.SaveText or FileOpsOperationKind.SavePolicy ? null : _controller?.LastCommand;
    private static DialogOptions DecisionOptions => new() { FullWidth = true, MaxWidth = MaxWidth.Small, CloseOnEscapeKey = true };

    protected override async Task OnInitializedAsync()
    {
        _dialogs = new(Dialogs, action => InvokeAsync(action));
        _policyDialogs = new(Dialogs, action => InvokeAsync(action));
        _transfer = new(JS);
        _controller = new(Facade, ChangedAsync, RetireNativeAsync);
        await Track(async () =>
        {
            await _controller.InitializeAsync(rememberedDirectory: ReturnContext.DirectoryPath);
            if (_retired) return;
            _controller.SetSort(ReturnContext.SortField, ReturnContext.SortDescending);
            var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            var remembered = ReturnContext.CheckedPaths.ToHashSet(comparer);
            foreach (var entry in _controller.SortedEntries)
                if (remembered.Contains(entry.FullPath)) _controller.ToggleEntrySelection(entry, true);
            if (ReturnContext.InspectedPath is { } path)
            {
                var entry = _controller.SortedEntries.FirstOrDefault(value => comparer.Equals(value.FullPath, path));
                if (entry is not null) await _controller.OpenEntryAsync(entry);
                else await _controller.OpenTextPathAsync(path);
            }
            if (!_retired) { _catalogOpen = !HasInspector; Remember(); }
        });
    }

    private Task ChangedAsync() => Track(() => InvokeAsync(() =>
    {
        if (_retired) return;
        if (!ReferenceEquals(_observedCommand, _controller.LastCommand))
        { _observedCommand = _controller.LastCommand; _commandTarget = _pendingCommandTarget; }
        StateHasChanged();
    }));
    private void Remember()
    {
        if (_retired) return;
        ReturnContext.DirectoryPath = _controller.Browse?.CurrentPath;
        ReturnContext.SortField = _controller.SortField;
        ReturnContext.SortDescending = _controller.SortDescending;
        ReturnContext.CheckedPaths = [.. _controller.SelectedPaths];
        ReturnContext.InspectedPath = _controller.IsDirty ? null : SelectedPath.Length > 0 ? SelectedPath : null;
    }
    private void CancelFocus() { _focusVersion++; _focusIntent = null; }
    private void InventoryChanged() { CancelFocus(); Remember(); }
    private void SelectionCleared()
    {
        if (_retired) return;
        CancelFocus(); Remember();
        _focusIntent = new(_focusVersion, "selection-clear", SelectedPath, _controller.Browse?.CurrentPath);
    }
    private void DraftChanged(string value) { _controller.SetDraft(value); Remember(); }
    private void ToggleCatalog()
    {
        if (_retired) return;
        CancelFocus(); _catalogOpen = !_catalogOpen;
        if (_catalogOpen) _focusIntent = new(_focusVersion, "inventory", SelectedPath);
    }
    private Task NavigateAsync(string path) => Track(async () =>
    {
        if (_retired || _controller.IsBusy) return;
        CancelFocus(); await _controller.NavigateAsync(path, ConfirmDiscardAsync);
        if (!_retired) { if (!HasInspector) _catalogOpen = true; Remember(); }
    });
    private Task UpAsync() => _controller.Browse?.ParentPath is { } parent ? NavigateAsync(parent) : Task.CompletedTask;
    private Task RefreshAsync() => Track(async () => { CancelFocus(); await _controller.RefreshAsync(); if (!_retired) Remember(); });
    private Task OpenAsync(FileOpsEntrySummary entry) => Track(async () =>
    {
        if (_retired || _controller.IsBusy) return;
        CancelFocus();
        var disposition = await _controller.OpenEntryAsync(entry, ConfirmDiscardAsync);
        if (_retired || disposition != FileOpsOperationDisposition.Completed) return;
        if (entry.IsDirectory) _catalogOpen = true;
        else if (Same(SelectedPath, entry.FullPath)) { _catalogOpen = false; _focusIntent = new(_focusVersion, "inspector", SelectedPath); }
        Remember();
    });
    private Task SaveAsync() => CommandAsync(_controller.SaveAsync, SelectedPath);
    private Task ReloadAsync() => Track(async () => { CancelFocus(); await _controller.ReloadAsync(ConfirmDiscardAsync); if (!_retired) Remember(); });
    private Task CloseAsync() => Track(async () =>
    {
        if (_retired || _controller.IsBusy) return;
        CancelFocus();
        var version = _focusVersion;
        var disposition = await _controller.CloseInspectorAsync(ConfirmDiscardAsync);
        if (_retired) return;
        if (disposition == FileOpsOperationDisposition.Completed && !HasInspector)
        {
            _catalogOpen = true;
            if (version == _focusVersion) _focusIntent = new(version, "inventory-return", "");
        }
        Remember();
    });
    private Task CreateAsync() => Track(async () =>
    {
        if (_retired || _createActive || _controller.IsBusy || _controller.Browse is not { CanCreateDirectory: true } browse) return;
        // Reserve the exact native opener before controller admission publishes Busy.
        // MudBlazor owns restoration; this button must remain focusable through its dialog result.
        _createActive = true;
        try { await CommandAsync(() => _controller.CreateDirectoryAsync(CollectDirectoryAsync), browse.CurrentPath); }
        finally
        {
            _createActive = false;
            if (!_retired) StateHasChanged();
        }
    });
    private Task DeleteAsync(FileOpsEntrySummary entry) { CancelFocus(); return CommandAsync(() => _controller.DeleteEntryAsync(entry, ConfirmDeleteAsync), entry.FullPath); }
    private Task DeleteSelectionAsync() { CancelFocus(); return CommandAsync(() => _controller.DeleteSelectedAsync(ConfirmDeleteAsync), string.Join(Environment.NewLine, _controller.SelectedPaths)); }
    private Task UploadAsync(IBrowserFile file) => CommandAsync(async () =>
    {
        _uploadName = file.Name;
        try
        {
            return await _controller.UploadAsync(file.Name, file.Size, (maximum, token) => Task.FromResult<Stream>(file.OpenReadStream(maximum, token)), ConfirmDiscardAsync);
        }
        finally { _uploadName = null; }
    }, Path.Combine(_controller.Browse?.CurrentPath ?? "", file.Name.Trim()));
    private Task DownloadAsync(string path) => CommandAsync(() => _controller.DownloadAsync(path, TransferAsync), path);
    private Task DownloadSelectionAsync() => CommandAsync(() => _controller.DownloadSelectedAsync("files.zip", TransferAsync), string.Join(Environment.NewLine, _controller.SelectedPaths));
    private Task CommandAsync(Func<Task<FileOpsOperationDisposition>> action, string? target) => Track(async () =>
    {
        if (_retired || _controller.IsBusy) return;
        _pendingCommandTarget = target;
        try { await action(); if (!_retired) Remember(); }
        finally { _pendingCommandTarget = null; }
    });
    private Task<bool> TransferAsync(FileOpsDownloadDescriptor descriptor, CancellationToken token) => _retired ? Task.FromResult(false) : _transfer.DownloadAsync(descriptor);

    private async Task<string?> CollectDirectoryAsync(FileOpsCreateContext context, CancellationToken token)
    {
        var result = await _dialogs.ShowAsync<FileOpsCreateDirectoryDialog>(L["CreateDirectoryDialog:Title"],
            new() { [nameof(FileOpsCreateDirectoryDialog.ParentPath)] = context.ParentPath }, DecisionOptions);
        if (!_retired && result is { Canceled: false, Data: string name })
        { _pendingCommandTarget = Path.Combine(context.ParentPath, name); return name; }
        return null;
    }
    private async Task<bool> ConfirmDiscardAsync(FileOpsDiscardRequest request, CancellationToken token)
    {
        var result = await _dialogs.ShowAsync<FileOpsDiscardDialog>(L["Discard:Title"],
            new() { [nameof(FileOpsDiscardDialog.Request)] = request }, DecisionOptions);
        return !_retired && result is { Canceled: false, Data: true };
    }
    private async Task<bool?> ConfirmDeleteAsync(FileOpsDeleteConfirmation context, CancellationToken token)
    {
        _pendingCommandTarget = string.Join(Environment.NewLine, context.Entries.Select(entry => entry.FullPath));
        var result = await _dialogs.ShowAsync<FileOpsBatchDeleteConfirmDialog>(L["BatchDeleteDialog:Title"],
            new() { [nameof(FileOpsBatchDeleteConfirmDialog.Context)] = context,
                [nameof(FileOpsBatchDeleteConfirmDialog.OriginPath)] = _controller.Browse?.CurrentPath ?? "" },
            new() { FullWidth = true, MaxWidth = context.Entries.Count > 1 ? MaxWidth.Medium : MaxWidth.Small, CloseOnEscapeKey = true });
        return !_retired && result is { Canceled: false, Data: bool recursive } ? recursive : null;
    }
    private async Task BeforeNavigationAsync(LocationChangingContext context)
    {
        if (_retired) return;
        // Policy-query housekeeping is the same Files visit, not draft abandonment.
        if (new Uri(context.TargetLocation).AbsolutePath == new Uri(Navigation.Uri).AbsolutePath) return;
        if (_controller.IsBusy || _policyWriting || _policyOpen && _policyEditor is null) { context.PreventNavigation(); return; }
        await Track(async () =>
        {
            var form = _policyEditor;
            long? consent = null;
            var leave = false;
            try
            {
                consent = form is null ? null : await form.PrepareLeaveAsync();
                if (form is not null && consent is null) { context.PreventNavigation(); return; }
                var fileRevision = _controller.DraftRevision;
                var filePath = SelectedPath;
                var disposition = await _controller.ConfirmLeaveAsync(ConfirmDiscardAsync);
                leave = !_retired && disposition == FileOpsOperationDisposition.Completed &&
                    fileRevision == _controller.DraftRevision && Same(filePath, SelectedPath) &&
                    (form is null || consent is { } revision && form.IsLeaveCurrent(revision));
                if (!leave) context.PreventNavigation(); else Remember();
            }
            catch { context.PreventNavigation(); throw; }
            finally { if (form is not null && consent is { } revision) form.FinishLeave(revision, leave); }
        });
    }

    private Task OpenPolicyAsync() => Track(async () =>
    {
        if (_retired || _policyOpen || _controller.IsBusy) return;
        _policyOpen = true; CancelFocus(); StateHasChanged();
        try
        {
            if (!await AuthorizePolicyAsync()) return;
            if (_retired) return;
            await _policyDialogs.ShowAsync<FileOpsPolicyEditor>(L["Policy:Title"], new()
            {
                [nameof(FileOpsPolicyEditor.Controller)] = _controller,
                [nameof(FileOpsPolicyEditor.Apply)] = (Func<FileOpsRuntimeConfig, Task<FileOpsPolicyApplyReceipt>>)ApplyPolicyAsync,
                [nameof(FileOpsPolicyEditor.ConfirmDiscard)] = (Func<Task<bool>>)ConfirmPolicyDiscardAsync,
                [nameof(FileOpsPolicyEditor.Attach)] = (Action<FileOpsPolicyEditor>)(form => _policyEditor = form)
            }, new() { FullWidth = true, MaxWidth = MaxWidth.Large, CloseButton = false,
                CloseOnEscapeKey = false, CloseOnNavigation = false, BackdropClick = true });
        }
        finally
        {
            try { if (_policyEditor is { } form) await form.DisposeAsync(); }
            finally { _policyEditor = null; _policyOpen = false; if (!_retired) StateHasChanged(); }
        }
    });
    private async Task<bool> ConfirmPolicyDiscardAsync()
    {
        if (_retired) return false;
        var result = await _dialogs.ShowAsync<FileOpsDiscardDialog>(L["Discard:PolicyTitle"],
            new() { [nameof(FileOpsDiscardDialog.IsPolicy)] = true }, DecisionOptions);
        return !_retired && result is { Canceled: false, Data: true };
    }
    private async Task<FileOpsPolicyApplyReceipt> ApplyPolicyAsync(FileOpsRuntimeConfig draft)
    {
        if (_retired || !_policyOpen || _policyWriting || _controller.IsBusy)
            return new(FileOpsOperationDisposition.Rejected, null, null, null);
        _policyWriting = true;
        var captured = draft.Clone();
        var previous = _controller.LastCommand;
        try
        {
            if (!await AuthorizePolicyAsync())
                return new(FileOpsOperationDisposition.Rejected, null, null, null, AccessDenied: !_retired);
            if (_retired) return new(FileOpsOperationDisposition.Retired, null, null, null);
            var disposition = await _controller.SavePolicyAsync(captured, ConfirmDiscardAsync);
            var command = _controller.LastCommand;
            var read = _controller.LastRead;
            var current = !ReferenceEquals(previous, command) && command?.Kind == FileOpsOperationKind.SavePolicy;
            // SavePolicy reads policy before browse/text reconciliation. A failed LoadPolicy is
            // unconfirmed; a later browse/text failure is separate evidence after confirmed readback.
            var confirmed = current && command!.Succeeded && read is not null &&
                (read.Kind == FileOpsOperationKind.LoadPolicy && read.Succeeded || read.Kind is FileOpsOperationKind.Browse or FileOpsOperationKind.ReadText);
            if (!_retired) Remember();
            return new(disposition, current ? command : null, current ? read : null,
                confirmed ? _controller.RuntimeConfig : null);
        }
        finally { _policyWriting = false; }
    }
    private async Task<bool> AuthorizePolicyAsync()
    {
        bool allowed;
        try { allowed = await PageAccess.IsAuthorizedAsync(UIFileOpsPolicyPage.PAGE_URL, _policyAccess.Token); }
        catch { allowed = false; }
        if (_retired) return false;
        _policyAccessDenied = !allowed;
        StateHasChanged();
        return allowed;
    }

    /// <summary>Receives actual owner dimensions; browser viewport width is not an interaction breakpoint.</summary>
    [JSInvokable]
    public Task OwnerSizeChanged(double workWidth, double usableHeight, double inventoryWidth, string? focusedPath, long focusedVersion, bool rowFocusLost) => Track(() => InvokeAsync(async () =>
    {
        if (_retired) return;
        var restoreRow = HasInspector && focusedVersion == _focusVersion && Same(focusedPath, SelectedPath);
        var split = workWidth >= 1080 && usableHeight >= 520;
        if (split != _split)
        {
            CancelFocus();
            _split = split;
            if (HasInspector && !split) _catalogOpen = false;
        }
        var table = inventoryWidth >= 800;
        if (table == _tableMode && split == _lastRenderedSplit)
        {
            // Repair a removed native control without another render which would replace it again.
            if (restoreRow && rowFocusLost && _moduleImport is not null)
            {
                try
                {
                    var module = await _moduleImport;
                    if (!_retired && focusedVersion == _focusVersion && Same(focusedPath, SelectedPath))
                        await module.InvokeVoidAsync("restoreTrackedFocus", _root, _visit, focusedVersion, SelectedPath);
                }
                catch (JSDisconnectedException) { }
            }
            return;
        }
        if (restoreRow) _focusIntent ??= new(_focusVersion, split ? "row" : "inspector-transition", SelectedPath);
        _tableMode = table; _lastRenderedSplit = split;
        StateHasChanged();
    }));
    private bool _lastRenderedSplit;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_retired || _controller is null) return;
        var openPolicy = OpenPolicy && !_policyRequestConsumed && !_controller.IsBusy;
        var initialize = !_sizingAdmitted;
        var intent = _focusIntent;
        if (!initialize && intent is null)
        {
            if (openPolicy) await AdmitPolicyRequestAsync();
            return;
        }
        _sizingAdmitted = true; _focusIntent = null;
        await Track(async () =>
        {
            _moduleImport ??= JS.InvokeAsync<IJSObjectReference>("import", "./_content/Monica.DevOps/FileOps/Components/FileOpsWorkspace.razor.js").AsTask();
            try
            {
                var module = await _moduleImport;
                if (_retired) return;
                if (initialize)
                {
                    _callback = DotNetObjectReference.Create(this);
                    var started = new TaskCompletionSource<IJSObjectReference>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _sizingStart = started.Task;
                    try { started.TrySetResult(await module.InvokeAsync<IJSObjectReference>("createWorkspaceSizer", _root, _visit, _callback)); }
                    catch (Exception exception) { started.TrySetException(exception); }
                    await started.Task;
                    if (_retired) { await StopSizingAsync(); return; }
                }
                if (!_retired && intent is not null && intent.Version == _focusVersion && Same(intent.Path, SelectedPath))
                    await module.InvokeVoidAsync("reveal", _root, _visit, intent.Version, intent.Kind, intent.Path, intent.DirectoryPath);
            }
            catch (JSDisconnectedException) { }
        });
        if (openPolicy && !_retired && !_controller.IsBusy)
            await AdmitPolicyRequestAsync();
    }

    private Task AdmitPolicyRequestAsync() => Track(async () =>
    {
        if (_retired || _policyRequestConsumed || _controller.IsBusy || _moduleImport is null) return;
        _policyRequestConsumed = true;
        try
        {
            var module = await _moduleImport;
            if (_retired) return;
            // Give the native focus trap a real connected opener for automatic compatibility entry.
            await module.InvokeVoidAsync("focusPolicyOpener", _root, _visit, _focusVersion);
            if (!_retired) await OpenPolicyAsync();
        }
        catch (JSDisconnectedException) { }
    });

    private Task Track(Func<Task> action)
    {
        TaskCompletionSource completion;
        lock (_sync)
        {
            if (_retired) return Task.CompletedTask;
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _users.Add(completion.Task);
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
    private Task StopSizingAsync()
    {
        if (_sizingStop is not null) return _sizingStop;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _sizingStop = completion.Task;
        _ = StopAsync();
        return completion.Task;
        async Task StopAsync()
        {
            Exception? failure = null;
            try
            {
                if (_sizingStart is not null)
                {
                    var observer = await _sizingStart;
                    try { await observer.InvokeVoidAsync("dispose"); }
                    finally { await observer.DisposeAsync(); }
                }
            }
            catch (JSDisconnectedException) { }
            catch (Exception exception) { failure = exception; }
            try { _callback?.Dispose(); }
            catch (Exception exception) { failure = failure is null ? exception : new AggregateException(failure, exception); }
            finally { _callback = null; }
            if (failure is null) completion.TrySetResult(); else completion.TrySetException(failure);
        }
    }
    private Task RetireNativeAsync() => _nativeRetirement ??= Task.WhenAll(
        _policyAccess.CancelAsync(),
        _policyDialogs?.DisposeAsync().AsTask() ?? Task.CompletedTask,
        _dialogs?.DisposeAsync().AsTask() ?? Task.CompletedTask,
        _policyEditor?.DisposeAsync().AsTask() ?? Task.CompletedTask,
        _transfer?.DisposeAsync().AsTask() ?? Task.CompletedTask,
        StopSizingAsync());
    /// <summary>Stops native owners before joining controller work and all actual import/render/focus users.</summary>
    public ValueTask DisposeAsync()
    {
        Task[] users;
        TaskCompletionSource completion;
        lock (_sync)
        {
            if (_retirement is not null) return new(_retirement);
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously); _retirement = completion.Task;
            _retired = true; CancelFocus(); users = _users.ToArray();
        }
        _ = RetireAsync();
        return new(_retirement);
        async Task RetireAsync()
        {
            Exception? failure = null;
            try
            {
                var native = RetireNativeAsync();
                var controller = _controller?.DisposeAsync().AsTask() ?? Task.CompletedTask;
                await Task.WhenAll(native, controller, Task.WhenAll(users));
            }
            catch (Exception exception) { failure = exception; }
            try
            {
                if (_moduleImport is not null) { try { await (await _moduleImport).DisposeAsync(); } catch (JSDisconnectedException) { } }
            }
            catch (Exception exception) { failure = failure is null ? exception : new AggregateException(failure, exception); }
            _policyAccess.Dispose();
            if (failure is null) completion.TrySetResult(); else completion.TrySetException(failure);
        }
    }
    private static bool Same(string? left, string? right) => string.Equals(left, right, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    private sealed record FocusIntent(long Version, string Kind, string Path, string? DirectoryPath = null);
}
