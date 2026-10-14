using AwesomeAssertions;
using Monica.EventBus.Abstractions;
using Monica.Framework.UI.UIEventBus.Models;
using Xunit;

namespace Test.Monica.EventBus.UIEventBus.Models;

public sealed class EventBusProviderInfoTests
{
    [Fact]
    public void UniqueId_WhenKeysHaveDifferentIdentity_ShouldDistinguishNullLiteralDefaultCaseAndDelimiters()
    {
        string?[] keys = [null, "", "default", "Default", "orders:dist", "orders", "订单", "\ud800", "\ufffd"];
        var providers = keys.Select(key => new EventBusProviderInfo
        {
            ServiceKey = key,
            ProviderType = EventBusProviderKind.Local
        }).ToList();

        providers.Select(provider => provider.UniqueId).Should().OnlyHaveUniqueItems();
        providers.Select(provider => provider.ServiceKey).Should().Equal(keys);
        providers.Select(provider => provider.UniqueId).Should().OnlyContain(identity => identity.All(character => character <= 127));
    }

    [Fact]
    public void UniqueId_WhenKindOrScopeChanges_ShouldRemainDifferentEvenWithAnIdenticalKey()
    {
        var local = new EventBusProviderInfo { ServiceKey = "orders", ProviderType = EventBusProviderKind.Local };
        var distributed = new EventBusProviderInfo
        {
            ServiceKey = "orders", ProviderType = EventBusProviderKind.Local, IsDistributed = true
        };
        var kafka = new EventBusProviderInfo
        {
            ServiceKey = "orders", ProviderType = EventBusProviderKind.Kafka, IsDistributed = true
        };

        new[] { local.UniqueId, distributed.UniqueId, kafka.UniqueId }.Should().OnlyHaveUniqueItems();
        new EventBusProviderInfo { ServiceKey = "orders", ProviderType = EventBusProviderKind.Local }
            .UniqueId.Should().Be(local.UniqueId);
    }
}
