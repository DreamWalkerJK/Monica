using System.Reflection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Monica.Modules;
using Monica.Profiling.Pages;
using Monica.Profiling.UIMemoryAnalysis.State;
using Monica.Testing.Hosting;
using Monica.UI.Shell.Models;
using Monica.UI.Shell.Support;
using Test.Monica.Profiling.UIMemoryAnalysis.Support;
using Xunit;

namespace Test.Monica.Profiling.Modules;

public sealed class ModuleMemoryAnalysisUITests
{
    private static readonly IReadOnlyDictionary<string, Type> RouteComponents = new Dictionary<string, Type>
    {
        ["memory-analysis"] = typeof(UIMemoryAnalysisPage),
        ["memory-analysis/gc"] = typeof(UIMemoryGcPage),
        ["memory-analysis/history"] = typeof(UIMemoryHistoryPage),
        ["memory-analysis/allocations"] = typeof(UIMemoryAllocationsPage),
        ["memory-analysis/diagnostics"] = typeof(UIMemoryDiagnosticsPage)
    };

    [Fact]
    public async Task UseMonica_WhenAllocationSelectionDiffersBetweenHosts_ShouldPublishIndependentRouteAndNavigationCatalogs()
    {
        var cancellation = TestContext.Current.CancellationToken;
        // The shared factory applies UseMonica and MapMonica before returning each real host.
        // No rendered component or diagnostic operation is needed to prove startup registration.
        await using var disabled = await new MemoryAnalysisTestApplicationFactory(
            refreshIntervalMs: 0, sampleIntervalMs: int.MaxValue, enableAllocation: false)
            .CreateAsync(cancellationToken: cancellation);
        var disabledCatalog = disabled.Services.GetRequiredService<IPageCatalog>();

        await using var enabled = await new MemoryAnalysisTestApplicationFactory(
            refreshIntervalMs: 0, sampleIntervalMs: int.MaxValue, enableAllocation: true)
            .CreateAsync(cancellationToken: cancellation);
        var enabledCatalog = enabled.Services.GetRequiredService<IPageCatalog>();

        enabled.Application.Should().NotBeSameAs(disabled.Application);
        enabledCatalog.Should().NotBeSameAs(disabledCatalog);
        await AssertRegistrationAsync(disabled, disabledCatalog, allocationsEnabled: false);
        await AssertRegistrationAsync(enabled, enabledCatalog, allocationsEnabled: true);
    }

    private static async Task AssertRegistrationAsync(
        MonicaTestApplication application,
        IPageCatalog catalog,
        bool allocationsEnabled)
    {
        var pages = catalog.GetRegisteredPages();
        foreach (var (route, componentType) in RouteComponents)
        {
            var page = pages.Should().ContainSingle(page => page.Route == route).Subject;
            page.ComponentType.Should().Be(componentType);
            componentType.GetCustomAttributes<RouteAttribute>()
                .Should().ContainSingle(attribute => attribute.Template == $"/{route}");
            page.AccessPolicyType.Should().BeNull("the route split preserves the existing public page boundary");
        }
        catalog.GetAdditionalAssemblies().Should().Contain(typeof(UIMemoryAnalysisPage).Assembly,
            "direct route requests need the registered profiling page assembly");

        var categoryId = NavigationCategoryId.Create("Monica.Profiling.MemoryAnalysis");
        catalog.GetNavigationCategories().Should().ContainSingle(category => category.Id == categoryId)
            .Subject.ParentId.Should().Be(BuiltInNavigationCategoryIds.Monitor);
        var navigation = catalog.GetNavItems().Where(item => item.CategoryId == categoryId).ToArray();
        var expectedNavigation = RouteComponents.Keys
            .Where(route => allocationsEnabled || route != "memory-analysis/allocations");
        navigation.Select(item => item.Href).Should().BeEquivalentTo(expectedNavigation);
        foreach (var item in navigation)
        {
            item.Page.Should().BeSameAs(pages.Single(page => page.Route == item.Href));
        }

        application.Application.Dependencies.ModuleKeysByType.ContainsKey(typeof(ModuleTypeAllocation))
            .Should().Be(allocationsEnabled);
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);
        scope.Resolve<IOptions<ModuleMemoryAnalysisUIOption>>().Value.EnableTypeAllocationTab
            .Should().Be(allocationsEnabled);
        scope.Resolve<MemoryAnalysisPageStateFactory>().Should().NotBeNull();
        (scope.ServiceProvider.GetService<TypeAllocationPanelStateFactory>() is not null)
            .Should().Be(allocationsEnabled);
    }
}
