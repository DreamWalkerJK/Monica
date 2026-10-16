using Monica.DevOps.K8S.Providers.InCluster;

namespace Test.Monica.DevOps.K8S.Support;

internal sealed class ControlledInClusterServiceReader(
    Func<string, CancellationToken, Task<string?>> read) : IK8SInClusterServiceReader
{
    private int _callCount;

    public int CallCount => Volatile.Read(ref _callCount);

    public string? LastServiceName { get; private set; }

    public CancellationToken LastCancellationToken { get; private set; }

    public Task<string?> ReadServiceAsync(string serviceName, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _callCount);
        LastServiceName = serviceName;
        LastCancellationToken = cancellationToken;
        return read(serviceName, cancellationToken);
    }
}
