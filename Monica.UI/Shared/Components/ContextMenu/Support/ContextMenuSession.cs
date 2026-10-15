namespace Monica.UI.Shared.Components.ContextMenu.Support;

/// <summary>
/// Owns one cursor menu's opening identity, selected-action tail and serialized native transitions.
/// Native adapters report their actual OpenChanged events through <see cref="NativeOpenChangedAsync"/>.
/// </summary>
internal sealed class ContextMenuSession<TItem>(
    Func<ContextMenuPosition, Task> openNative,
    Func<Task> closeNative) : IAsyncDisposable
{
    private readonly object _sync = new();
    private ContextMenuOpening<TItem>? _opening;
    private ContextMenuOpening<TItem>? _renderedOpening;
    private NativeTarget? _nativeTarget;
    private IReadOnlyList<ContextMenuItem<TItem>>? _lastItems;
    private TItem? _lastContext;
    private ContextMenuPosition _position;
    private long _callerRevision;
    private long _openingSequence;
    private long _presentationSequence;
    private long? _dismissedOpening;
    private bool _visible;
    private bool _nativeOpen;
    private bool _closingForReconciliation;
    private bool _retired;
    private Task? _reconciliation;
    private Task? _retirement;

    public bool IsRetired
    {
        get { lock (_sync) return _retired; }
    }

    public ContextMenuOpening<TItem>? RenderedOpening
    {
        get
        {
            lock (_sync)
            {
                return !_retired && IsDesiredVisible() && _nativeOpen
                    && _renderedOpening?.Id == _opening?.Id
                    && _renderedOpening?.PresentationRevision == _opening?.PresentationRevision
                    ? _renderedOpening
                    : null;
            }
        }
    }

    public void SetParameters(
        bool visible,
        long openingRevision,
        ContextMenuPosition position,
        IReadOnlyList<ContextMenuItem<TItem>> items,
        TItem? context,
        Func<ContextMenuItem<TItem>, Task> itemSelected,
        Func<Task> closeRequested)
    {
        lock (_sync)
        {
            if (_retired) return;

            var contentChanged = !ReferenceEquals(items, _lastItems)
                || (_opening is not null && !HasSameItemSequence(items, _opening.Items))
                || !EqualityComparer<TItem?>.Default.Equals(context, _lastContext);
            var newOpening = visible && (_opening is null || !_visible
                || openingRevision != _callerRevision
                || (openingRevision == 0 && contentChanged));

            if (newOpening)
            {
                _opening = new ContextMenuOpening<TItem>(++_openingSequence, ++_presentationSequence,
                    items.ToArray(), context, itemSelected, closeRequested);
            }
            else if (_opening is not null)
            {
                _opening = _opening with
                {
                    PresentationRevision = contentChanged || position != _position
                        ? ++_presentationSequence : _opening.PresentationRevision,
                    Items = contentChanged ? items.ToArray() : _opening.Items,
                    Context = context,
                    ItemSelected = itemSelected,
                    CloseRequested = closeRequested
                };

                if (_renderedOpening?.Id == _opening.Id
                    && _renderedOpening.PresentationRevision == _opening.PresentationRevision)
                {
                    _renderedOpening = _opening;
                }
            }

            _visible = visible;
            _callerRevision = openingRevision;
            _position = position;
            _lastItems = items;
            _lastContext = context;
        }
    }

    private static bool HasSameItemSequence(IReadOnlyList<ContextMenuItem<TItem>> items,
        IReadOnlyList<ContextMenuItem<TItem>> snapshot)
    {
        if (items.Count != snapshot.Count) return false;
        for (var index = 0; index < items.Count; index++)
        {
            if (!ReferenceEquals(items[index], snapshot[index])) return false;
        }

        return true;
    }

    public Task NativeOpenChangedAsync(bool isOpen)
    {
        Func<Task>? closeRequested = null;
        lock (_sync)
        {
            if (_retired) return Task.CompletedTask;
            _nativeOpen = isOpen;
            if (!isOpen)
            {
                var nativeOpening = _renderedOpening;
                _nativeTarget = null;
                _renderedOpening = null;
                if (!_closingForReconciliation && _visible
                    && nativeOpening is not null && nativeOpening.Id == _opening?.Id)
                {
                    _dismissedOpening = nativeOpening.Id;
                    closeRequested = nativeOpening.CloseRequested;
                }
            }
        }

        // A native callback can run inside the current transition. It must not join that same task.
        return closeRequested?.Invoke() ?? Task.CompletedTask;
    }

    public Task SelectAsync(ContextMenuOpening<TItem> opening, ContextMenuItem<TItem> item)
    {
        Func<TItem?, Task>? action;
        lock (_sync)
        {
            if (_retired || !IsDesiredVisible() || opening.Id != _opening?.Id
                || opening.PresentationRevision != _opening?.PresentationRevision
                || item.Disabled || item.IsDivider || item.HasSubMenu)
            {
                return Task.CompletedTask;
            }

            action = item.OnClick;
        }

        return ExecuteSelectionAsync(opening, item, action);
    }

    private async Task ExecuteSelectionAsync(ContextMenuOpening<TItem> opening,
        ContextMenuItem<TItem> item, Func<TItem?, Task>? action)
    {
        // The caller owns the actual action and its resources. Observe it even after this opening retires.
        if (action is not null) await action(opening.Context);
        if (!IsCurrentOpening(opening.Id)) return;
        await opening.ItemSelected(item);
        if (!IsCurrentOpening(opening.Id)) return;
        await opening.CloseRequested();
    }

    public Task ReconcileAsync()
    {
        TaskCompletionSource completion;
        lock (_sync)
        {
            if (_retired) return Task.CompletedTask;
            if (_reconciliation is not null) return _reconciliation;
            if (GetNextOperation() == NativeOperation.None) return Task.CompletedTask;
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _reconciliation = completion.Task;
        }

        // Publish admission before invoking native code, which can re-enter through OpenChanged.
        _ = ExecuteReconciliationAsync(completion);
        return completion.Task;
    }

    private async Task ExecuteReconciliationAsync(TaskCompletionSource completion)
    {
        try
        {
            while (true)
            {
                NativeOperation operation;
                ContextMenuPosition position;
                lock (_sync)
                {
                    operation = _retired ? NativeOperation.None : GetNextOperation();
                    if (operation == NativeOperation.None)
                    {
                        _reconciliation = null;
                        break;
                    }

                    position = _position;
                    if (operation == NativeOperation.Open)
                    {
                        _renderedOpening = _opening;
                        _nativeTarget = new NativeTarget(_opening!.Id, _opening.PresentationRevision, position);
                    }
                    else
                    {
                        _closingForReconciliation = true;
                    }
                }

                if (operation == NativeOperation.Open)
                {
                    await openNative(position);
                }
                else
                {
                    try
                    {
                        await closeNative();
                        lock (_sync)
                        {
                            _nativeOpen = false;
                            _nativeTarget = null;
                            _renderedOpening = null;
                        }
                    }
                    finally
                    {
                        lock (_sync) _closingForReconciliation = false;
                    }
                }

                // Every incomplete native await returns here before choosing the latest desired target.
            }

            completion.TrySetResult();
        }
        catch (Exception exception)
        {
            lock (_sync) _reconciliation = null;
            completion.TrySetException(exception);
        }
    }

    private NativeOperation GetNextOperation()
    {
        if (!IsDesiredVisible())
            return _nativeTarget is not null || _nativeOpen ? NativeOperation.Close : NativeOperation.None;

        if (_nativeTarget is not null && _nativeOpen
            && _nativeTarget.OpeningId == _opening!.Id
            && _nativeTarget.PresentationRevision == _opening.PresentationRevision
            && _nativeTarget.Position == _position)
        {
            return NativeOperation.None;
        }

        return _nativeTarget is not null || _nativeOpen ? NativeOperation.Close : NativeOperation.Open;
    }

    private bool IsDesiredVisible() => _visible && _opening is not null && _dismissedOpening != _opening.Id;

    private bool IsCurrentOpening(long id)
    {
        lock (_sync) return !_retired && _opening?.Id == id;
    }

    public ValueTask DisposeAsync()
    {
        TaskCompletionSource completion;
        Task? admitted;
        lock (_sync)
        {
            if (_retirement is not null) return new ValueTask(_retirement);
            _retired = true;
            admitted = _reconciliation;
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _retirement = completion.Task;
        }

        _ = ExecuteRetirementAsync(admitted, completion);
        return new ValueTask(completion.Task);
    }

    private async Task ExecuteRetirementAsync(Task? admitted, TaskCompletionSource completion)
    {
        try
        {
            if (admitted is not null) await admitted;
            completion.TrySetResult();
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
        finally
        {
            // MudMenu remains renderer-owned. Retirement never starts DOM work or disposes that child.
            lock (_sync)
            {
                _nativeTarget = null;
                _renderedOpening = null;
            }
        }
    }

    private enum NativeOperation { None, Open, Close }
    private sealed record NativeTarget(long OpeningId, long PresentationRevision, ContextMenuPosition Position);
}

internal readonly record struct ContextMenuPosition(double PageX, double PageY);

internal sealed record ContextMenuOpening<TItem>(
    long Id,
    long PresentationRevision,
    IReadOnlyList<ContextMenuItem<TItem>> Items,
    TItem? Context,
    Func<ContextMenuItem<TItem>, Task> ItemSelected,
    Func<Task> CloseRequested);
