using Monica.JobScheduler.UI.UIJobScheduler.Support;
using MudBlazor;

namespace Monica.JobScheduler.UI.UIJobScheduler.Executions.State;

/// <summary>Owns one recent-execution inspector using the shared native scheduler dialog lifetime.</summary>
internal sealed class ExecutionDialogSession(
    Func<string, Task<IDialogReference>> openDialog,
    Func<Task> refresh) : IAsyncDisposable
{
    private readonly SchedulerDialogSession<string> _session = new(openDialog, (_, _) => refresh());

    /// <summary>Admits an inspector using the exact clicked identity; duplicate or retired admission is ignored.</summary>
    internal Task OpenAsync(string instanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        return _session.OpenAsync(instanceId);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _session.DisposeAsync();
}
