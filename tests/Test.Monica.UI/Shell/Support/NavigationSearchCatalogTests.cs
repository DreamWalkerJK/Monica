using System.Globalization;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Monica.Core.Localization.Abstractions;
using Monica.Testing.Localization;
using Monica.UI.Localization;
using Monica.UI.Shell.Models;
using Monica.UI.Shell.Support;
using Xunit;

namespace Test.Monica.UI.Shell.Support;

public sealed class NavigationSearchCatalogTests
{
    [Fact]
    public async Task CreateEntriesAsync_WhenPoliciesRestrictNavigation_ShouldResolveOnlyAuthorizedDestinations()
    {
        var registry = new PageRegistry();
        var root = registry.RegisterCategory("package.operations", "Operations");
        var deniedBranch = registry.RegisterLocalizedCategory<DeniedResource>(
            "package.denied", "Category:Title", parentId: root, descriptionKey: "Category:Description");
        registry.RegisterPage<TestPage>("public", "Public", categoryId: root, addToNav: true);
        registry.RegisterPage<TestPage>(
            "permitted", "Permitted", categoryId: root, addToNav: true, accessPolicyType: typeof(AllowPolicy));
        registry.RegisterLocalizedPage<TestPage, DeniedResource>(
            "denied", "Page:Title", categoryId: deniedBranch, addToNav: true,
            accessPolicyType: typeof(DenyPolicy), descriptionKey: "Page:Description");
        registry.RegisterLocalizedPage<TestPage, DeniedResource>(
            "missing-policy", "Page:Title", categoryId: deniedBranch, addToNav: true,
            accessPolicyType: typeof(MissingPolicy), descriptionKey: "Page:Description");
        registry.RegisterPage<TestPage>("unlisted", "Unlisted", categoryId: root, addToNav: false);
        registry.Seal();
        await using var services = new ServiceCollection()
            .AddSingleton(new AllowPolicy())
            .AddSingleton(new DenyPolicy())
            .BuildServiceProvider();
        var localization = new TestLocalizationCatalog();
        var search = CreateSearch(registry, services, localization);

        var entries = await search.CreateEntriesAsync(TestContext.Current.CancellationToken);

        entries.Select(static entry => entry.Item.Href).Should().BeEquivalentTo(new[] { "public", "permitted" });
        entries.Should().OnlyContain(entry => entry.CategoryBreadcrumb == "Operations");
        localization.RequestedResourceTypes.Should().BeEmpty();
    }

    [Theory]
    [InlineData("items/{id}")]
    [InlineData("items/{id:int}")]
    [InlineData("files/{*path}")]
    [InlineData("reports/{year?}")]
    public async Task CreateEntriesAsync_WhenRouteRequiresParameters_ShouldExcludeItBeforeAccessAndLocalization(string route)
    {
        var registry = new PageRegistry();
        var category = registry.RegisterCategory("package.records", "Records", description: "Inspect stored records.");
        registry.RegisterPage<TestPage>("records", "Record list", categoryId: category, addToNav: true);
        registry.RegisterLocalizedPage<TestPage, TemplateResource>(
            route, "Page:Title", categoryId: category, addToNav: true,
            accessPolicyType: typeof(CountingPolicy), descriptionKey: "Page:Description");
        registry.Seal();
        var policy = new CountingPolicy();
        await using var services = new ServiceCollection().AddSingleton(policy).BuildServiceProvider();
        var localization = new TestLocalizationCatalog();
        var search = CreateSearch(registry, services, localization);

        var entries = await search.CreateEntriesAsync(TestContext.Current.CancellationToken);

        entries.Should().ContainSingle().Which.Item.Href.Should().Be("records");
        policy.Evaluations.Should().Be(0);
        localization.RequestedResourceTypes.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Operations", "Diagnostics", "Memory", "Heap snapshot", "Inspect managed memory.")]
    [InlineData("运行", "诊断", "内存", "堆快照", "检查托管内存。")]
    public async Task CreateEntriesAsync_WhenCategoriesAreNested_ShouldResolveBreadcrumbsFromTheirOwningResources(
        string rootTitle,
        string branchTitle,
        string leafTitle,
        string pageTitle,
        string pageDescription)
    {
        var registry = new PageRegistry();
        var root = registry.RegisterLocalizedCategory<RootResource>("package.operations", "Root:Title");
        var branch = registry.RegisterLocalizedCategory<BranchResource>(
            "package.diagnostics", "Branch:Title", parentId: root);
        var leaf = registry.RegisterLocalizedCategory<BranchResource>(
            "package.memory", "Leaf:Title", parentId: branch);
        registry.RegisterLocalizedPage<TestPage, PageResource>(
            "heap", "Page:Title", categoryId: leaf, addToNav: true, descriptionKey: "Page:Description");
        registry.Seal();
        using var services = new ServiceCollection().BuildServiceProvider();
        var localization = new TestLocalizationCatalog(new Dictionary<Type, IStringLocalizer>
        {
            [typeof(RootResource)] = new TranslationLocalizer(new Dictionary<string, string>
            {
                ["Root:Title"] = rootTitle
            }),
            [typeof(BranchResource)] = new TranslationLocalizer(new Dictionary<string, string>
            {
                ["Branch:Title"] = branchTitle,
                ["Leaf:Title"] = leafTitle
            }),
            [typeof(PageResource)] = new TranslationLocalizer(new Dictionary<string, string>
            {
                ["Page:Title"] = pageTitle,
                ["Page:Description"] = pageDescription
            })
        });
        var fallback = new FallbackLocalizer();
        var search = CreateSearch(registry, services, localization, fallback);

        var entry = (await search.CreateEntriesAsync(TestContext.Current.CancellationToken))
            .Should().ContainSingle().Subject;

        entry.Title.Should().Be(pageTitle);
        entry.Description.Should().Be(pageDescription);
        entry.CategoryBreadcrumb.Should().Be($"{rootTitle} / {branchTitle} / {leafTitle}");
        entry.RootCategory.Should().Be(rootTitle);
        entry.Item.Should().BeSameAs(registry.GetNavItems().Single());
        localization.RequestedResourceTypes.Should().BeEquivalentTo(
            new[] { typeof(RootResource), typeof(BranchResource), typeof(PageResource) });
        fallback.Evaluations.Should().Be(0);
    }

    [Theory]
    [InlineData("Page purpose", "Branch capabilities", "Root capabilities", "Page purpose", 0)]
    [InlineData(null, "Branch capabilities", "Root capabilities", "Branch capabilities", 0)]
    [InlineData(null, null, "Root capabilities", "Root capabilities", 0)]
    [InlineData(null, null, null, "Open Destination via Root / Branch.", 1)]
    public async Task CreateEntriesAsync_WhenDescriptionsAreOptional_ShouldPreferPageThenCategoryThenRootThenLocalizedFallback(
        string? pageDescription,
        string? branchDescription,
        string? rootDescription,
        string expectedDescription,
        int fallbackEvaluations)
    {
        var registry = new PageRegistry();
        var root = registry.RegisterCategory("package.root", "Root", description: rootDescription);
        var branch = registry.RegisterCategory(
            "package.branch", "Branch", parentId: root, description: branchDescription);
        registry.RegisterPage<TestPage>(
            "destination", "Destination", categoryId: branch, addToNav: true, description: pageDescription);
        registry.Seal();
        using var services = new ServiceCollection().BuildServiceProvider();
        var fallback = new FallbackLocalizer();
        var search = CreateSearch(registry, services, new TestLocalizationCatalog(), fallback);

        var entry = (await search.CreateEntriesAsync(TestContext.Current.CancellationToken))
            .Should().ContainSingle().Subject;

        entry.Description.Should().Be(expectedDescription);
        fallback.Evaluations.Should().Be(fallbackEvaluations);
    }

    private static NavigationSearchCatalog CreateSearch(
        PageRegistry registry,
        IServiceProvider services,
        ILocalizationCatalog localization,
        FallbackLocalizer? fallback = null)
    {
        return new NavigationSearchCatalog(
            registry,
            new PageAccessEvaluator(registry, services),
            localization,
            fallback ?? new FallbackLocalizer());
    }

    private sealed class TestPage : ComponentBase;

    private sealed class DeniedResource : ILocalizationResource;

    private sealed class TemplateResource : ILocalizationResource;

    private sealed class RootResource : ILocalizationResource;

    private sealed class BranchResource : ILocalizationResource;

    private sealed class PageResource : ILocalizationResource;

    private sealed class AllowPolicy : IPageAccessPolicy
    {
        public Task<bool> IsAuthorizedAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class DenyPolicy : IPageAccessPolicy
    {
        public Task<bool> IsAuthorizedAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class MissingPolicy : IPageAccessPolicy
    {
        public Task<bool> IsAuthorizedAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class CountingPolicy : IPageAccessPolicy
    {
        public int Evaluations { get; private set; }

        public Task<bool> IsAuthorizedAsync(CancellationToken cancellationToken = default)
        {
            Evaluations++;
            return Task.FromResult(true);
        }
    }

    private sealed class TestLocalizationCatalog(IReadOnlyDictionary<Type, IStringLocalizer>? resources = null)
        : ILocalizationCatalog
    {
        public HashSet<Type> RequestedResourceTypes { get; } = [];

        public IStringLocalizer For<TResource>() where TResource : class, ILocalizationResource => For(typeof(TResource));

        public IStringLocalizer For(Type resourceType)
        {
            RequestedResourceTypes.Add(resourceType);
            return resources is not null && resources.TryGetValue(resourceType, out var localizer)
                ? localizer
                : throw new InvalidOperationException($"Unexpected localization lookup: {resourceType.Name}.");
        }

        public string Get<TResource>(string key) where TResource : class, ILocalizationResource => For<TResource>()[key].Value;

        public string Get<TResource>(string key, params object[] arguments) where TResource : class, ILocalizationResource
            => For<TResource>()[key, arguments].Value;
    }

    private sealed class TranslationLocalizer(IReadOnlyDictionary<string, string> translations)
        : EchoStringLocalizer<PageResource>
    {
        public override LocalizedString this[string name] => translations.TryGetValue(name, out var value)
            ? new LocalizedString(name, value, resourceNotFound: false)
            : base[name];
    }

    private sealed class FallbackLocalizer : EchoStringLocalizer<NavigationSearchResource>
    {
        public int Evaluations { get; private set; }

        public override LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                Evaluations++;
                return new LocalizedString(
                    name,
                    string.Format(CultureInfo.InvariantCulture, "Open {0} via {1}.", arguments),
                    resourceNotFound: false);
            }
        }
    }
}
