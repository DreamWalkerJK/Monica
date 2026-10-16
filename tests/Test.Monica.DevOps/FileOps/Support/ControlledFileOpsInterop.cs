using Microsoft.JSInterop;

namespace Test.Monica.DevOps.FileOps.Support;

internal sealed class ControlledFileOpsJsRuntime : IJSRuntime
{
    public Func<Task<IJSObjectReference>> Import { get; set; } = () => Task.FromResult<IJSObjectReference>(new ControlledFileOpsJsModule());
    public int Imports { get; private set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, default, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        if (identifier != "import") throw new InvalidOperationException($"Unexpected runtime invocation: {identifier}");
        Imports++;
        return new(ImportAsync<TValue>());
    }

    private async Task<TValue> ImportAsync<TValue>() => (TValue)(object)await Import();
}

internal sealed class ControlledFileOpsJsModule : IJSObjectReference
{
    public Func<Task<bool>> Invoke { get; set; } = () => Task.FromResult(true);
    public Func<Task> Release { get; set; } = () => Task.CompletedTask;
    public int Invocations { get; private set; }
    public int Releases { get; private set; }
    public DotNetStreamReference? StreamReference { get; private set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, default, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        if (identifier != "downloadStream") throw new InvalidOperationException($"Unexpected module invocation: {identifier}");
        Invocations++;
        StreamReference = args?.OfType<DotNetStreamReference>().Single();
        return new(InvokeCoreAsync<TValue>());
    }

    private async Task<TValue> InvokeCoreAsync<TValue>() => (TValue)(object)await Invoke();

    public ValueTask DisposeAsync()
    {
        Releases++;
        return new(Release());
    }
}
