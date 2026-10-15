using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Monica.Core.Modularity.Abstractions;
using Monica.DevOps.K8S.Pages;
using Monica.Modules;
using Monica.Testing.Hosting;
using Monica.UI.Shell.Support;
using Xunit;

namespace Test.Monica.DevOps.K8S.Modules;

public sealed class ModuleK8SUITests
{
    [Fact]
    public async Task AddK8SUI_WhenRoutesAreComposed_ShouldPublishOneRoutedOwnerForBothDestinations()
    {
        await using var application = await new K8SUIApplicationFactory().CreateAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var catalog = application.Services.GetRequiredService<IPageCatalog>();

        foreach (var route in new[] { UIK8SPage.PAGE_URL, UIK8SRuntimeConfigPage.PAGE_URL })
        {
            var page = Assert.Single(catalog.GetRegisteredPages(), definition => definition.Route == route.Trim('/'));
            Assert.Equal(typeof(UIK8SPage), page.ComponentType);
            Assert.Null(page.AccessPolicyType);

            var routedOwner = Assert.Single(typeof(UIK8SPage).Assembly.GetTypes(), type =>
                type.GetCustomAttributes<RouteAttribute>(inherit: false)
                    .Any(attribute => attribute.Template == route));
            Assert.Equal(page.ComponentType, routedOwner);
        }

        Assert.Empty(typeof(UIK8SRuntimeConfigPage).GetCustomAttributes<RouteAttribute>(inherit: false));
    }

    [Fact]
    public async Task AddK8SUI_WhenInventoryComponentPolicyDenies_ShouldDenyBothDestinationOwners()
    {
        await using var application = await new K8SUIApplicationFactory(denyInventoryComponent: true).CreateAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);
        var catalog = scope.Resolve<IPageCatalog>();
        var evaluator = scope.Resolve<PageAccessEvaluator>();

        foreach (var route in new[] { UIK8SPage.PAGE_URL, UIK8SRuntimeConfigPage.PAGE_URL })
        {
            var page = Assert.Single(catalog.GetRegisteredPages(), definition => definition.Route == route.Trim('/'));
            Assert.Null(page.AccessPolicyType);
            Assert.False(await evaluator.IsRouteAuthorizedAsync(
                page.ComponentType,
                TestContext.Current.CancellationToken));
        }
    }

    [Theory]
    [InlineData("https://localhost/", "https://localhost/ui/k8s/runtime-config", true)]
    [InlineData("https://localhost/host/", "https://localhost/host/UI/K8S/RUNTIME-CONFIG/?ns=alpha&kind=deployment&q=api#editor", true)]
    [InlineData("https://localhost/host/", "https://localhost/host/ui/k8s?ns=alpha#runtime-config", false)]
    [InlineData("https://localhost/host/", "https://localhost/host/ui/k8s?q=runtime-config", false)]
    public void IsRuntimeConfigurationRoute_WhenHostPathAndReturnHintsVary_ShouldSelectOnlyTheConfigPath(
        string baseUri,
        string uri,
        bool expected)
    {
        Assert.Equal(expected, UIK8SPage.IsRuntimeConfigurationRoute(new TestNavigationManager(baseUri, uri)));
    }

    [Fact]
    public void IsRuntimeConfigurationRoute_WhenHistoryReturnsWithUnchangedHints_ShouldFollowEachDestination()
    {
        var navigation = new TestNavigationManager(
            "https://localhost/host/",
            "https://localhost/host/ui/k8s?ns=alpha&kind=deployment&q=api");

        Assert.False(UIK8SPage.IsRuntimeConfigurationRoute(navigation));
        navigation.Adopt("ui/k8s/runtime-config?ns=alpha&kind=deployment&q=api");
        Assert.True(UIK8SPage.IsRuntimeConfigurationRoute(navigation));
        navigation.Adopt("ui/k8s?ns=alpha&kind=deployment&q=api");
        Assert.False(UIK8SPage.IsRuntimeConfigurationRoute(navigation));
        navigation.Adopt("ui/k8s/runtime-config?ns=alpha&kind=deployment&q=api");
        Assert.True(UIK8SPage.IsRuntimeConfigurationRoute(navigation));
    }

    private sealed class K8SUIApplicationFactory(bool denyInventoryComponent = false)
        : MonicaTestApplicationFactory<ModuleK8SUITests>
    {
        protected override void ConfigureHost(WebApplicationBuilder builder)
        {
            // Compose the shell with the UI module's real static-asset manifest in TestServer.
            builder.Environment.ApplicationName = typeof(ModuleK8SUI).Assembly.GetName().Name!;
        }

        protected override void ConfigureMonica(IMonicaBuilder builder)
        {
            builder.AddK8SUI();
            if (denyInventoryComponent)
            {
                // This policy metadata seam exercises the existing component-wide alias gate without rendering.
                builder.AddUIShell(options => options.ConfigureNavigation(registry =>
                    registry.RegisterPage<UIK8SPage>(
                        "/test/k8s-policy",
                        "K8S component policy",
                        accessPolicyType: typeof(DenyPolicy))));
            }
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddScoped(static _ => new DenyPolicy());
        }
    }

    private sealed class DenyPolicy : IPageAccessPolicy
    {
        public Task<bool> IsAuthorizedAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(false);
        }
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string baseUri, string uri) => Initialize(baseUri, uri);

        internal void Adopt(string uri) => Uri = ToAbsoluteUri(uri).AbsoluteUri;
    }
}
