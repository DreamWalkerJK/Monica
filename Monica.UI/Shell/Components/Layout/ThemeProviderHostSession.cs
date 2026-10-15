using Microsoft.JSInterop;
using Monica.UI.Shell.State;
using Monica.UI.Shell.Support;
using Monica.UI.Theming;

namespace Monica.UI.Shell.Components.Layout;

/// <summary>
/// Owns one theme host's startup, preference changes, renderer callbacks and JavaScript module.
/// The theme state, browser storage and runtime are borrowed; retirement drains their admitted work.
/// </summary>
internal sealed class ThemeProviderHostSession : IAsyncDisposable
{
    private const string THEME_MODULE_PATH = "./_content/Monica.UI/js/theme-helper.js";

    private readonly ThemeState _theme;
    private readonly IBrowserStorage _storage;
    private readonly Func<Task> _render;
    private readonly JsModuleSession _module;
    private readonly ComponentRenderTracker _renders = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _sync = new();
    private readonly List<Task> _themeChanges = [];
    private Task? _initialization;
    private Task? _disposal;
    private ThemePreference? _restoringPreference;
    private long _changeRevision;
    private volatile bool _disposed;

    internal ThemeProviderHostSession(
        ThemeState theme,
        IBrowserStorage storage,
        IJSRuntime runtime,
        Func<Task> render)
    {
        _theme = theme;
        _storage = storage;
        _render = render;
        _module = new JsModuleSession(runtime, THEME_MODULE_PATH);
        _theme.OnThemeChanged += OnThemeChanged;
    }

    /// <summary>Restores browser preferences once, after the host's first interactive render.</summary>
    internal Task InitializeAsync()
    {
        TaskCompletionSource completion;
        lock (_sync)
        {
            if (_disposed) return Task.CompletedTask;
            if (_initialization is not null) return _initialization;
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _initialization = completion.Task;
        }

        _ = CompleteAsync(completion, InitializeCoreAsync);
        return completion.Task;
    }

    private void OnThemeChanged()
    {
        TaskCompletionSource completion;
        Task initialization;
        lock (_sync)
        {
            // Ignore only the saved preference's own synchronous notification.
            if (_disposed || _restoringPreference == CapturePreference()) return;
            _changeRevision++;
            if (_initialization is not { } started) return;
            initialization = started;
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _themeChanges.Add(completion.Task);
        }

        _ = CompleteAsync(completion, () => ApplyThemeChangeAsync(initialization));
    }

    private async Task InitializeCoreAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_disposed) return;
            var saved = await _storage.GetAsync<ThemePreference?>(ThemePreference.StorageKey, null);
            if (_disposed) return;

            var restored = saved is not null && TryRestoreTheme(saved);
            await SynchronizeThemeAsync(persist: !restored);
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool TryRestoreTheme(ThemePreference saved)
    {
        var preference = saved.Normalize();
        lock (_sync)
        {
            // Any explicit change since subscription wins, even if it returns to the host default.
            if (_disposed || _changeRevision != 0) return false;
            _restoringPreference = preference;
        }

        try
        {
            if (_disposed) return false;
            _theme.SetTheme(preference.ThemeKind, preference.IsDarkMode);
            return true;
        }
        finally
        {
            lock (_sync) _restoringPreference = null;
        }
    }

    private async Task ApplyThemeChangeAsync(Task initialization)
    {
        await initialization;
        await _gate.WaitAsync();
        try
        {
            await SynchronizeThemeAsync(persist: true);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SynchronizeThemeAsync(bool persist)
    {
        if (_disposed) return;
        var preference = CapturePreference();
        if (persist)
        {
            await _storage.SetAsync(ThemePreference.StorageKey, preference);
            if (_disposed) return;
        }

        try
        {
            var mode = preference.IsDarkMode ? "dark" : "light";
            await _module.InvokeVoidAsync("setDocumentTheme", $"{preference.ThemeKind.ToCssToken()}-{mode}");
        }
        catch (JSDisconnectedException)
        {
            // The browser circuit no longer has a document to synchronize.
            return;
        }
        catch (ObjectDisposedException) when (_disposed)
        {
            return;
        }
        if (!_disposed) _renders.Queue(_render);
    }

    private ThemePreference CapturePreference() => new(_theme.CurrentThemeKind, _theme.IsDarkMode);

    /// <summary>Stops admission and shares one completion for initialization, updates, rendering and module release.</summary>
    public ValueTask DisposeAsync()
    {
        TaskCompletionSource completion;
        Task[] operations;
        lock (_sync)
        {
            if (_disposal is not null) return new ValueTask(_disposal);
            _disposed = true;
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _disposal = completion.Task;
            operations = _initialization is { } initialization
                ? [initialization, .. _themeChanges]
                : [];
        }

        _theme.OnThemeChanged -= OnThemeChanged;
        _ = CompleteAsync(completion, () => DisposeCoreAsync(operations));
        return new ValueTask(completion.Task);
    }

    private async Task DisposeCoreAsync(Task[] operations)
    {
        // Seal JS immediately so a pending import cannot issue a document call after retirement.
        var moduleDisposal = _module.DisposeAsync().AsTask();
        try
        {
            await Task.WhenAll(operations.Append(moduleDisposal));
        }
        finally
        {
            await _renders.CompleteAsync();
        }
    }

    private static async Task CompleteAsync(TaskCompletionSource completion, Func<Task> operation)
    {
        try
        {
            await operation();
            completion.TrySetResult();
        }
        catch (OperationCanceledException exception)
        {
            completion.TrySetCanceled(exception.CancellationToken);
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
    }
}
