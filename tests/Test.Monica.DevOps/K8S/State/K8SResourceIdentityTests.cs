using Monica.DevOps.K8S.Models;
using Monica.DevOps.K8S.State;
using Xunit;

namespace Test.Monica.DevOps.K8S.State;

public sealed class K8SResourceIdentityTests
{
    [Fact]
    public void Comparer_WhenEvidenceChanges_ShouldKeepIdentityAndHashAcrossRecreatedRows()
    {
        var original = Row("alpha", K8SResourceType.Deployment, "api");
        var refreshed = new K8SResourceSummary
        {
            Namespace = "alpha", ResourceType = K8SResourceType.Deployment, Name = "api",
            DesiredReplicas = 3, ReadyReplicas = 2, Images = ["app:v2"], LastRestartedAtUtc = DateTimeOffset.UnixEpoch
        };

        var comparer = K8SResourceIdentity.Comparer;

        Assert.True(comparer.Equals(original, refreshed));
        Assert.Equal(comparer.GetHashCode(original), comparer.GetHashCode(refreshed));
        var selected = new HashSet<K8SResourceSummary>(comparer) { original };
        Assert.Contains(refreshed, selected, comparer);
        Assert.False(selected.Add(refreshed));
    }

    [Theory]
    [InlineData("beta", K8SResourceType.Deployment, "api")]
    [InlineData("alpha", K8SResourceType.Service, "api")]
    [InlineData("alpha", K8SResourceType.Deployment, "worker")]
    [InlineData("alpha", K8SResourceType.Deployment, "API")]
    public void Comparer_WhenNamespaceKindOrExactNameDiffers_ShouldKeepResourcesDistinct(
        string namespaceName, K8SResourceType kind, string name)
    {
        var original = Row("alpha", K8SResourceType.Deployment, "api");
        var other = Row(namespaceName, kind, name);

        var comparer = K8SResourceIdentity.Comparer;

        Assert.False(comparer.Equals(original, other));
        var selected = new HashSet<K8SResourceSummary>(comparer) { original };
        Assert.True(selected.Add(other));
        Assert.Equal(2, selected.Count);
    }

    private static K8SResourceSummary Row(string namespaceName, K8SResourceType kind, string name) =>
        new() { Namespace = namespaceName, ResourceType = kind, Name = name };
}
