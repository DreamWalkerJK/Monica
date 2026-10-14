using System.Diagnostics;

namespace Monica.DevOps.Terminal.Services.Support;

internal interface ITerminalProcessFactory
{
    ITerminalProcess Create(ProcessStartInfo startInfo);
}

internal interface ITerminalProcess : IDisposable
{
    event Action<string, string?>? OutputReceived;
    bool Start();
    void BeginOutputRead();
    Task WaitForExitAsync(CancellationToken ct);
    bool HasExited { get; }
    int ExitCode { get; }
    void KillProcessTree();
}

internal sealed class SystemTerminalProcessFactory : ITerminalProcessFactory
{
    public ITerminalProcess Create(ProcessStartInfo startInfo) => new SystemTerminalProcess(startInfo);
}

internal sealed class SystemTerminalProcess : ITerminalProcess
{
    private readonly Process _process;
    public SystemTerminalProcess(ProcessStartInfo startInfo)
    {
        _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _process.OutputDataReceived += (_, args) => OutputReceived?.Invoke("stdout", args.Data);
        _process.ErrorDataReceived += (_, args) => OutputReceived?.Invoke("stderr", args.Data);
    }
    public event Action<string, string?>? OutputReceived;
    public bool HasExited => _process.HasExited;
    public int ExitCode => _process.ExitCode;
    public bool Start() => _process.Start();
    public void BeginOutputRead()
    {
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }
    // .NET 10 joins internal readers, not caller-created async callback work.
    public Task WaitForExitAsync(CancellationToken ct) => _process.WaitForExitAsync(ct);
    public void KillProcessTree() => _process.Kill(entireProcessTree: true);
    public void Dispose() => _process.Dispose();
}
