using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;

namespace Monica.Framework.UI.UILogging.State;

/// <summary>
/// Owns one independent logging reader for the current Blazor circuit while navigation remains in the
/// reader/catalog workspace. Components borrow this owner and its reader; they do not dispose the reader.
/// </summary>
/// <remarks>
/// The DI scope owns this service. Reader/catalog route changes preserve the buffer, filter, source and
/// following work even when the shell temporarily removes their components during access checks. Leaving
/// those routes detaches and asynchronously retires the reader; returning starts a fresh reader. Circuit
/// disposal unsubscribes the borrowed NavigationManager and drains all current or retiring readers.
/// </remarks>
public sealed class LoggingWorkspaceSession : IAsyncDisposable
{
    private readonly LoggingService _logging;
    private readonly NavigationManager _navigation;
    private readonly ILogger<LoggingWorkspaceSession> _logger;
    private readonly object _sync = new();
    private readonly HashSet<Task> _retirements = [];
    private LogReadingSession? _reader;
    private bool _initialized;
    private bool _disposed;
    private Task? _disposeTask;
    private LogReaderViewState? _viewState;

    /// <summary>Creates a circuit-scoped owner; navigation and the host logging facade remain borrowed dependencies.</summary>
    public LoggingWorkspaceSession(LoggingService logging, NavigationManager navigation, ILogger<LoggingWorkspaceSession> logger)
    {
        _logging = logging;
        _navigation = navigation;
        _logger = logger;
        _navigation.LocationChanged += LocationChanged;
    }

    /// <summary>Whether the current workspace reader has successfully adopted its first requested source.</summary>
    public bool IsInitialized { get { lock (_sync) return _initialized; } }

    internal LogReaderViewState? ReaderViewState { get { lock (_sync) return _viewState; } }

    internal void SaveReaderViewState(LogReadingSession reader, LogReaderViewState state)
    {
        lock (_sync)
        {
            if (!_disposed && ReferenceEquals(reader, _reader)) _viewState = state;
        }
    }

    /// <summary>
    /// Returns the same independent reader throughout reader/catalog navigation. A new reader is created
    /// on first acquisition or after reentering the logging workspace; its initial state is uninitialized.
    /// </summary>
    /// <exception cref="InvalidOperationException">The current route is outside the logging workspace.</exception>
    /// <exception cref="ObjectDisposedException">This circuit-scoped owner has retired.</exception>
    public LogReadingSession GetReader()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!IsWorkspaceLocation(_navigation.Uri))
                throw new InvalidOperationException("The logging reader is available only within the logging reader and file catalog routes.");
            return _reader ??= _logging.CreateSession();
        }
    }

    /// <summary>
    /// Marks a successful source adoption for the current reader. Returns false when a late caller refers
    /// to a detached reader or a retired owner, preventing completion of old work from initializing a new reader.
    /// </summary>
    public bool MarkInitialized(LogReadingSession reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        lock (_sync)
        {
            if (_disposed || !ReferenceEquals(reader, _reader)) return false;
            _initialized = true;
            return true;
        }
    }

    private void LocationChanged(object? sender, LocationChangedEventArgs args)
    {
        lock (_sync)
        {
            if (!_disposed && !IsWorkspaceLocation(args.Location)) DetachReader();
        }
    }

    private bool IsWorkspaceLocation(string location)
    {
        string relative;
        try { relative = _navigation.ToBaseRelativePath(location); }
        catch (ArgumentException) { return false; }
        var delimiter = relative.IndexOfAny(['?', '#']);
        var path = (delimiter < 0 ? relative : relative[..delimiter]).TrimEnd('/');
        return path.Equals("logging-monitor", StringComparison.OrdinalIgnoreCase)
            || path.Equals("logging-monitor/files", StringComparison.OrdinalIgnoreCase);
    }

    private void DetachReader()
    {
        var reader = _reader;
        _reader = null;
        _initialized = false;
        _viewState = null;
        if (reader is null) return;
        var retirement = reader.DisposeAsync().AsTask();
        _retirements.Add(retirement);
        _ = ObserveRetirementAsync(retirement);
    }

    private async Task ObserveRetirementAsync(Task retirement)
    {
        try { await retirement.ConfigureAwait(false); }
        catch (Exception error) { _logger.LogError(error, "Failed to retire an owned logging workspace reader."); }
        finally { lock (_sync) _retirements.Remove(retirement); }
    }

    /// <summary>Unsubscribes navigation and drains current and previously admitted reader retirements once.</summary>
    public ValueTask DisposeAsync()
    {
        lock (_sync)
        {
            if (_disposeTask is not null) return new ValueTask(_disposeTask);
            _disposed = true;
            _navigation.LocationChanged -= LocationChanged;
            DetachReader();
            _disposeTask = DrainAsync(_retirements.ToArray());
            return new ValueTask(_disposeTask);
        }
    }

    private static async Task DrainAsync(Task[] retirements)
    {
        try { await Task.WhenAll(retirements).ConfigureAwait(false); }
        catch { /* Every owned retirement is observed and logged by ObserveRetirementAsync. */ }
    }
}
