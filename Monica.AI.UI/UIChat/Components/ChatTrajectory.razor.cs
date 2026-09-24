using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using Monica.AI.Chat.Models;
using Monica.AI.Models;
using Monica.AI.UI.Localization;
using Monica.AI.UI.UIChat.Support;

namespace Monica.AI.UI.UIChat.Components;

public partial class ChatTrajectory
{
    [Inject] private IStringLocalizer<AIResource> L { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    /// <summary>The original conversation and execution ledger to inspect.</summary>
    [Parameter, EditorRequired] public required ChatSession Session { get; set; }
    /// <summary>Optional execution step selected from the chat view.</summary>
    [Parameter] public ChatExecutionStep? SelectedStep { get; set; }

    private readonly TrajectoryViewport _viewport = new();
    private readonly HashSet<string> _folded = [];
    private ChatTrajectoryProjection _projection = ChatTrajectoryProjection.Create([], [], null, DateTimeOffset.UtcNow);
    private List<TrajectoryLedgerRow> _rows = [];
    private IReadOnlyList<TrajectoryTimelineMark> _marks = [];
    private string _query = string.Empty;
    private string? _selectedId, _lastSessionId, _lastSelectedId;
    private double? _dragStart;
    private ElementReference _root, _ledgerRoot, _timelineRoot;
    private ChatTrajectoryInteropSession? _interop;
    private bool _disposed, _suppressClick;
    private int? _revealRow;
    private DateTimeOffset _now;
    private TrajectoryRecord? Selected => _projection.Records.FirstOrDefault(record => record.Id == _selectedId);

    protected override void OnParametersSet()
    {
        _now = DateTimeOffset.UtcNow;
        _projection = ChatTrajectoryProjection.Create(Session, _now);
        if (_lastSessionId != Session.SessionId)
        {
            _lastSessionId = Session.SessionId; _selectedId = null; _folded.Clear(); _viewport.Reset(); _query = string.Empty;
        }
        _viewport.UpdateExtent(_projection.ExtentSeconds);
        if (SelectedStep?.Id != _lastSelectedId)
        {
            _lastSelectedId = SelectedStep?.Id;
            _selectedId = SelectedStep is { } step ? $"step:{step.Id}" : null;
            if (Selected is { } selected) { if (selected.GroupId is { } group) _folded.Remove(group); FocusRecord(selected); }
        }
        RebuildRows();
        RebuildTimeline();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed) return;
        if (_interop is null)
        {
            _interop = new ChatTrajectoryInteropSession(JSRuntime);
            await _interop.InitializeAsync(_root, _ledgerRoot, _timelineRoot);
            if (_disposed) return;
        }
        if (_revealRow is { } row)
        {
            _revealRow = null;
            await _interop.RevealAsync(row);
        }
    }

    private void Search(string value) { _query = value ?? string.Empty; RebuildRows(); }
    private void ToggleTurn(string id) { if (!_folded.Add(id)) _folded.Remove(id); RebuildRows(); }
    private void ToggleAll()
    {
        if (_folded.Count > 0) _folded.Clear();
        else foreach (var group in _projection.Groups) _folded.Add(group.Id);
        RebuildRows();
    }
    private void RebuildRows() => _rows = _projection.Rows(_query, _folded);
    private void RebuildTimeline() => _marks = TrajectoryTimelineProjection.Create(_projection, _viewport, _now);
    private void ChangeViewport(Action change) { change(); RebuildTimeline(); }
    private void ClearSelection() => _selectedId = null;

    private Task SelectRecordAsync(TrajectoryRecord record)
    {
        _selectedId = record.Id;
        FocusRecord(record);
        RebuildTimeline();
        return Task.CompletedTask;
    }

    private async Task SelectMarkAsync(TrajectoryTimelineMark mark)
    {
        if (_suppressClick) { _suppressClick = false; return; }
        if (mark.Records.Count > 1)
            ChangeViewport(() => _viewport.Focus(Math.Max(0, mark.Start - .05), Math.Max(mark.Start + .1, mark.End + .05)));
        var record = mark.First;
        if (record.GroupId is { } group) _folded.Remove(group);
        _query = string.Empty;
        RebuildRows();
        await SelectRecordAsync(record);
        _revealRow = _rows.FindIndex(row => row.Record?.Id == record.Id);
    }

    private Task SelectMarkWithKeyboardAsync(KeyboardEventArgs args, TrajectoryTimelineMark mark)
        => args.Key is "Enter" or " " ? SelectMarkAsync(mark) : Task.CompletedTask;

    private void FocusRecord(TrajectoryRecord record)
    {
        if (_projection.Origin is not { } origin || record.StartedAt is not { } time) return;
        var start = (time - origin).TotalSeconds;
        var end = ((ChatTrajectoryProjection.End(record, _now) ?? time) - origin).TotalSeconds;
        if (start < _viewport.Start || end > _viewport.End) _viewport.Focus(Math.Max(0, start - .5), end + .5);
    }

    private async Task FollowTailAsync()
    {
        _folded.Clear(); _query = string.Empty; RebuildRows();
        ChangeViewport(_viewport.Reset);
        if (_interop is not null) await _interop.FollowAsync();
    }

    private string TimelineLabel(TrajectoryTimelineMark mark) => mark.Records.Count > 1
        ? L["Workbench:TimelineCluster", mark.Records.Count].Value
        : $"{L[mark.First.RoleKey]} · {mark.First.Preview} · {ChatExecutionPresentation.Duration(mark.First.Step?.Duration)}";
    private double X(double seconds) => Math.Clamp((seconds - _viewport.Start) / _viewport.Width, 0, 1) * 1000;
    private double SpanWidth(double start, double end) => Math.Max(2, X(end) - X(start));
    private void BeginInterval(PointerEventArgs args) { _dragStart = args.ClientX; _suppressClick = false; }
    private async Task EndIntervalAsync(PointerEventArgs args)
    {
        var from = _dragStart;
        _dragStart = null;
        if (from is not { } start || Math.Abs(args.ClientX - start) < 12 || _interop is null) return;
        _suppressClick = true;
        var interval = await _interop.IntervalAsync(start, args.ClientX);
        if (_disposed || interval is not { Length: 2 }) return;
        var offset = _viewport.Start;
        var width = _viewport.Width;
        ChangeViewport(() => _viewport.Focus(offset + interval[0] * width, offset + interval[1] * width));
    }
    private void ZoomWheel(WheelEventArgs args) => ChangeViewport(() => _viewport.Zoom(args.DeltaY > 0 ? 1.2 : .8));

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_interop is { } interop) { _interop = null; await interop.DisposeAsync(); }
    }
}
