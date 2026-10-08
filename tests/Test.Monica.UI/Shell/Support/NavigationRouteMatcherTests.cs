using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Monica.UI.Shell.Support;
using Xunit;

namespace Test.Monica.UI.Shell.Support;

public sealed class NavigationRouteMatcherTests
{
    [Fact]
    public void FindActiveItem_WhenRoutesOverlap_ShouldSelectMostSpecificRegisteredDestination()
    {
        var registry = new PageRegistry();
        registry.RegisterPage<TestPage>("workbench", "Overview", addToNav: true);
        registry.RegisterPage<TestPage>("workbench/modules", "Modules", addToNav: true);
        registry.RegisterPage<TestPage>("workbench-preview", "Preview", addToNav: true);
        registry.Seal();

        var active = NavigationRouteMatcher.FindActiveItem("WORKBENCH/modules/details?module=one#summary", registry.GetNavItems());

        active.Should().NotBeNull();
        active!.Href.Should().Be("workbench/modules");
        NavigationRouteMatcher.FindActiveItem("unregistered", registry.GetNavItems()).Should().BeNull();
    }

    [Theory]
    [InlineData("gacha-pool", "gacha-pool")]
    [InlineData("GACHA-POOL", "/gacha-pool/")]
    [InlineData("gacha-pool/history", "gacha-pool")]
    [InlineData("gacha-pool?tab=overview", "gacha-pool")]
    [InlineData("gacha-pool#details", "gacha-pool")]
    [InlineData("", "")]
    public void IsActive_WhenDestinationOwnsCurrentRoute_ShouldReturnTrue(
        string currentRoute,
        string destination)
    {
        NavigationRouteMatcher.IsActive(currentRoute, destination).Should().BeTrue();
    }

    [Theory]
    [InlineData("gacha-pool-preview", "gacha-pool")]
    [InlineData("other/gacha-pool", "gacha-pool")]
    [InlineData("", "gacha-pool")]
    [InlineData("gacha-pool", "")]
    public void IsActive_WhenDestinationDoesNotOwnCurrentRoute_ShouldReturnFalse(
        string currentRoute,
        string destination)
    {
        NavigationRouteMatcher.IsActive(currentRoute, destination).Should().BeFalse();
    }

    private sealed class TestPage : ComponentBase;
}
