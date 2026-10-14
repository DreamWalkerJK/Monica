using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Monica.DevOps.Localization;
using Monica.DevOps.Terminal.Facades;
using Monica.DevOps.Terminal.Models;
using Monica.DevOps.Terminal.Services;
using Monica.DevOps.Terminal.Services.Support;
using Monica.Modules;
using Monica.Testing.Localization;

namespace Test.Monica.DevOps.Terminal.Support;

internal sealed class TerminalRuntimeFixture : IAsyncDisposable
{
    private readonly string _parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "Test.Monica.DevOps", "terminal"));
    public TerminalRuntimeFixture(ModuleTerminalOption? options = null, TimeProvider? timeProvider = null,
        ILogger<TerminalSessionService>? logger = null)
    {
        Root = Path.Combine(_parent, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
        var configured = Options.Create(options ?? new ModuleTerminalOption());
        Runner = new(Detector, configured, NullLogger<TerminalCommandRunner>.Instance, Factory, timeProvider);
        SessionService = new(Runner, Detector, configured, logger);
        Facade = new(SessionService, NullLogger<TerminalFacade>.Instance, new EchoStringLocalizer<TerminalResource>());
    }
    public string Root { get; }
    public ControlledTerminalProcessFactory Factory { get; } = new();
    public TerminalEnvironmentDetector Detector { get; } = new();
    public TerminalCommandRunner Runner { get; }
    public TerminalSessionService SessionService { get; }
    public TerminalFacade Facade { get; }
    public TerminalSessionSnapshot CreateSession(string ownerKey = "owner") => SessionService.CreateSession(ownerKey,
        new TerminalSessionCreateRequest { WorkingDirectory = Root });
    public async Task<TerminalSessionSnapshot> WaitForIdleAsync(string ownerKey, string sessionId, CancellationToken ct = default)
    {
        using var watchdog = CancellationTokenSource.CreateLinkedTokenSource(ct);
        watchdog.CancelAfter(TimeSpan.FromSeconds(10));
        while (true)
        {
            watchdog.Token.ThrowIfCancellationRequested();
            var session = SessionService.GetSession(ownerKey, sessionId);
            if (!session.IsRunning) return session;
            await Task.Yield();
        }
    }
    public async ValueTask DisposeAsync()
    {
        foreach (var process in Factory.Processes) process.Complete();
        try { await SessionService.DisposeAsync(); }
        finally
        {
            var relative = Path.GetRelativePath(_parent, Path.GetFullPath(Root));
            if (relative == "." || Path.IsPathRooted(relative) || relative.StartsWith("..", StringComparison.Ordinal))
                throw new InvalidOperationException("Terminal fixture escaped its owned root.");
            if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
        }
    }
}
