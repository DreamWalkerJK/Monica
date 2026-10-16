using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Monica.DevOps.K8S.Services;
using Monica.Modules;
using Test.Monica.DevOps.K8S.Support;
using Xunit;

namespace Test.Monica.DevOps.K8S.Services;

public sealed class K8SNodePortAddressResolverTests
{
    [Fact]
    public async Task ResolveAsync_WhenCustomInputsAndMultiplePortsAreProvided_ShouldPreserveServiceOrderAndCancellation()
    {
        using var environment = new NodePortAddressTestEnvironment();
        var reader = new ControlledInClusterServiceReader((_, _) => Task.FromResult<string?>(
            """{"spec":{"type":"NodePort","ports":[{"nodePort":31081},{"nodePort":31080}]}}"""));
        var resolver = CreateResolver(environment, reader);

        var addresses = await resolver.ResolveAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "http://192.0.2.8:31081", "http://192.0.2.8:31080" }, addresses);
        Assert.Equal("inventory-service", reader.LastServiceName);
        Assert.Equal(TestContext.Current.CancellationToken, reader.LastCancellationToken);
        Assert.Equal(1, reader.CallCount);
        Assert.Equal("previous-address", environment.Output);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResolveAsync_WhenAnInputVariableIsMissing_ShouldAvoidTheClusterRequest(bool missingServiceName)
    {
        using var environment = new NodePortAddressTestEnvironment();
        Environment.SetEnvironmentVariable(
            missingServiceName ? environment.ServiceNameVariable : environment.BaseAddressVariable,
            null);
        var reader = new ControlledInClusterServiceReader((_, _) =>
            throw new InvalidOperationException("A missing input must prevent the request."));

        var addresses = await CreateResolver(environment, reader)
            .ResolveAsync(TestContext.Current.CancellationToken);

        Assert.Null(addresses);
        Assert.Equal(0, reader.CallCount);
        Assert.Equal("previous-address", environment.Output);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{}")]
    [InlineData("{\"spec\":{}}")]
    [InlineData("{\"spec\":{\"type\":\"ClusterIP\",\"ports\":[{\"nodePort\":31080}]}}")]
    [InlineData("{\"spec\":{\"type\":\"LoadBalancer\",\"ports\":[{\"nodePort\":31080}]}}")]
    [InlineData("{\"spec\":{\"type\":\"NodePort\"}}")]
    public async Task ResolveAsync_WhenTheServiceHasNoPublishableNodePorts_ShouldReturnNoUpdate(string? json)
    {
        using var environment = new NodePortAddressTestEnvironment();
        var reader = new ControlledInClusterServiceReader((_, _) => Task.FromResult(json));

        var addresses = await CreateResolver(environment, reader)
            .ResolveAsync(TestContext.Current.CancellationToken);

        Assert.Null(addresses);
        Assert.Equal(1, reader.CallCount);
        Assert.Equal("previous-address", environment.Output);
    }

    [Fact]
    public async Task ResolveAsync_WhenTheNodePortArrayIsEmpty_ShouldReturnAnExplicitEmptyUpdate()
    {
        using var environment = new NodePortAddressTestEnvironment();
        var reader = new ControlledInClusterServiceReader((_, _) => Task.FromResult<string?>(
            """{"spec":{"type":"NodePort","ports":[]}}"""));

        var addresses = await CreateResolver(environment, reader)
            .ResolveAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(addresses);
        Assert.Empty(addresses);
    }

    private static K8SNodePortAddressResolver CreateResolver(
        NodePortAddressTestEnvironment environment,
        ControlledInClusterServiceReader reader) => new(
        Options.Create(new ModuleK8SOption { NodePortAddresses = environment.CreateOptions() }),
        reader,
        NullLogger<K8SNodePortAddressResolver>.Instance);
}
