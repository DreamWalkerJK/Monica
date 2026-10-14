using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Monica.DevOps.FileOps.Facades;
using Monica.DevOps.FileOps.Models;
using Monica.DevOps.FileOps.Services;
using Monica.DevOps.FileOps.Services.Support;
using Monica.DevOps.Localization;
using Monica.Modules;
using Monica.Testing.Localization;

namespace Test.Monica.DevOps.FileOps.Support;

internal sealed class FileOpsFilesystemFixture : IDisposable
{
    private readonly string _parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "Test.Monica.DevOps", "file-ops-services"));

    public FileOpsFilesystemFixture()
    {
        Root = Path.GetFullPath(Path.Combine(_parent, Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(Root);
        Store = new FileOpsRuntimeConfigStore(Options.Create(new ModuleFileOpsOption
        {
            RuntimeConfig = new FileOpsRuntimeConfig { AllowedRoots = [Root], StartupPath = Root }
        }));
        var policy = new FileOpsPathPolicy();
        Workspace = new FileOpsWorkspaceService(Store, policy, new FileOpsTextInspector(),
            NullLogger<FileOpsWorkspaceService>.Instance);
        Transfer = new FileOpsTransferService(Store, policy, NullLogger<FileOpsTransferService>.Instance);
        var localizer = new EchoStringLocalizer<FileOpsResource>();
        Facade = new FileOpsFacade(Workspace, Transfer, new FileOpsMessageLocalizer(localizer), localizer,
            NullLogger<FileOpsFacade>.Instance);
    }

    public string Root { get; }
    public FileOpsRuntimeConfigStore Store { get; }
    public FileOpsWorkspaceService Workspace { get; }
    public FileOpsTransferService Transfer { get; }
    public FileOpsFacade Facade { get; }

    public void Configure(Action<FileOpsRuntimeConfig> configure)
    {
        var config = Store.GetCurrent();
        configure(config);
        Store.Update(config);
    }

    public void Dispose()
    {
        var target = Path.GetFullPath(Root);
        var relative = Path.GetRelativePath(_parent, target);
        if (relative == "." || Path.IsPathRooted(relative) || relative.StartsWith("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The FileOps fixture escaped its owned temporary root.");
        }

        if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
    }
}
