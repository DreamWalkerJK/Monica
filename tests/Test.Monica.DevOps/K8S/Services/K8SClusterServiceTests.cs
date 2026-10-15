using Monica.DevOps.K8S.Models;
using Test.Monica.DevOps.K8S.Support;
using Xunit;

namespace Test.Monica.DevOps.K8S.Services;

public sealed class K8SClusterServiceTests
{
    [Theory]
    [InlineData(K8SResourceType.Service)]
    [InlineData(K8SResourceType.Deployment)]
    public async Task DescribeResource_WhenConfigurationChangesDuringDiscovery_ShouldReturnDetailsFromOneSnapshot(K8SResourceType resourceType)
    {
        var operations = new ControlledK8SOperations();
        var gate = new ControlledK8SCallGate();
        var firstDiscovery = true;
        var cancellationToken = TestContext.Current.CancellationToken;
        operations.Store.AfterRead = count =>
        {
            if (count == 1)
            {
                operations.Store.Update(ControlledK8SOperations.Configuration("node-b"));
            }
        };
        operations.BeforeResponse = async (_, token) =>
        {
            if (firstDiscovery)
            {
                firstDiscovery = false;
                await gate.PauseAsync(token);
            }
        };

        var resourceName = resourceType == K8SResourceType.Service ? "allowed-main" : "api-node-a";
        var execution = operations.Cluster.DescribeResourceAsync(
            ControlledK8SOperations.NAMESPACE, resourceType, resourceName, cancellationToken);
        try
        {
            await gate.Entered.WaitAsync(cancellationToken);
            operations.Store.Update(ControlledK8SOperations.Configuration("node-c"));
            gate.Release();

            var details = await execution;
            Assert.Equal(1, operations.Store.ReadCount);
            Assert.All(operations.Calls, call =>
            {
                Assert.Equal("node-a", call.Node);
                Assert.Equal("user-node-a", call.UserName);
                Assert.Equal(new[] { ControlledK8SOperations.NAMESPACE }, call.NamespaceScope);
                Assert.Equal(new[] { "allowed", "api" }, call.Restrictions);
                Assert.Equal(cancellationToken, call.CancellationToken);
            });
            Assert.Equal(resourceName, details.Summary.Name);
            Assert.Equal(ControlledK8SOperations.NAMESPACE, details.Summary.Namespace);
            Assert.Equal(resourceType, details.Summary.ResourceType);
            Assert.Equal("description from node-a", details.DescriptionText);
            Assert.Equal("Deployment/api-node-a", Assert.Single(details.RelatedWorkloads).DisplayName);
            var pod = Assert.Single(details.RelatedPods);
            Assert.Equal("api-node-a-pod", pod.Name);
            Assert.Equal("node-a", pod.NodeName);
            Assert.Equal("api-node-a", pod.WorkloadName);
            Assert.Equal("Deployment", pod.WorkloadKind);
            Assert.True(pod.IsReady);
            Assert.Equal(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), details.Summary.LastRestartedAtUtc);
            if (resourceType == K8SResourceType.Service)
            {
                Assert.Equal("node-a", details.Summary.ClusterIp);
            }

            Assert.Single(operations.Calls, call => call.Command.StartsWith("describe ", StringComparison.Ordinal));
            Assert.Contains(operations.Calls, call => call.Command.StartsWith("get pod ", StringComparison.Ordinal));
            Assert.DoesNotContain(operations.Calls, call => call.IsMutation);
        }
        finally
        {
            gate.Release();
        }
    }
}
