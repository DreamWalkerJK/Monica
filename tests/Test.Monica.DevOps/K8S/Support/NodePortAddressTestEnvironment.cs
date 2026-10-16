using Monica.DevOps.K8S.Models;

namespace Test.Monica.DevOps.K8S.Support;

internal sealed class NodePortAddressTestEnvironment : IDisposable
{
    private readonly Dictionary<string, string?> _originalValues;

    public NodePortAddressTestEnvironment()
    {
        var prefix = $"MONICA_NODEPORT_TEST_{Guid.NewGuid():N}";
        ServiceNameVariable = $"{prefix}_SERVICE";
        BaseAddressVariable = $"{prefix}_HOST";
        OutputVariable = $"{prefix}_ADDRESSES";
        _originalValues = new[] { ServiceNameVariable, BaseAddressVariable, OutputVariable }
            .ToDictionary(name => name, Environment.GetEnvironmentVariable);

        Environment.SetEnvironmentVariable(ServiceNameVariable, "inventory-service");
        Environment.SetEnvironmentVariable(BaseAddressVariable, "192.0.2.8");
        Output = "previous-address";
    }

    public string ServiceNameVariable { get; }

    public string BaseAddressVariable { get; }

    public string OutputVariable { get; }

    public string? Output
    {
        get => Environment.GetEnvironmentVariable(OutputVariable);
        set => Environment.SetEnvironmentVariable(OutputVariable, value);
    }

    public K8SNodePortAddressOptions CreateOptions() => new()
    {
        ServiceNameEnvironmentVariable = ServiceNameVariable,
        BaseAddressEnvironmentVariable = BaseAddressVariable,
        ListeningAddressesEnvironmentVariable = OutputVariable
    };

    public void Dispose()
    {
        foreach (var (name, value) in _originalValues)
        {
            Environment.SetEnvironmentVariable(name, value);
        }
    }
}
