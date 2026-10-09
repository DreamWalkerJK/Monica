using AwesomeAssertions;
using Monica.Framework.UI.UIEventBus.Models;
using Monica.Framework.UI.UIEventBus.State;
using Xunit;

namespace Test.Monica.EventBus.UIEventBus.Models;

public sealed class SubscriptionFilterTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("default")]
    [InlineData("Default")]
    public void CloneAndClear_WhenServiceKeyIsExact_ShouldRetainTheKeyUntilExplicitlyCleared(string key)
    {
        var filter = new SubscriptionFilter { ServiceKey = key };
        var matching = new SubscriptionViewModel { ServiceKey = key };
        var other = new SubscriptionViewModel { ServiceKey = key + " " };
        var defaultBus = new SubscriptionViewModel { ServiceKey = null };

        filter.HasAnyFilter.Should().BeTrue();
        var clone = filter.Clone();
        clone.Should().NotBeSameAs(filter);
        clone.HasAnyFilter.Should().BeTrue();
        clone.ServiceKey.Should().Be(key);
        EventBusMonitorService.ApplyFilter([matching, other, defaultBus], clone).Should().Equal(matching);

        filter.Clear();

        filter.HasAnyFilter.Should().BeFalse();
        filter.ServiceKey.Should().BeNull();
        clone.ServiceKey.Should().Be(key, "clearing one filter does not retire its independent clone");
        EventBusMonitorService.ApplyFilter([matching, other, defaultBus], filter).Should().HaveCount(3);
    }
}
