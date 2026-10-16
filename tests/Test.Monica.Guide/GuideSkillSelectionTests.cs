using Monica.Guide;
using Xunit;

namespace Test.Monica.Guide;

public sealed class GuideSkillSelectionTests
{
    [Theory]
    [InlineData("microservice", "micro")]
    [InlineData("modular-monolith", "modular")]
    [InlineData("ui", "ui")]
    public void SelectProfileClosure_WhenCapabilitySelected_ShouldIncludeOnlyItsRequiredClosure(
        string capability, string selected)
    {
        var catalog = Catalog();
        var checks = new List<GuideCheck>();

        var closure = AgentGuideService.SelectProfileClosure(
            catalog, "application", checks, "guide", [capability]);

        Assert.Empty(checks);
        Assert.Equal(new[] { "base", "dependency", "recommended", selected }.Order(StringComparer.Ordinal),
            closure!.Select(static skill => skill.Name));
        Assert.DoesNotContain(closure!, skill => skill.Name == "guide");
    }

    [Fact]
    public void SelectProfileClosure_WhenCapabilitiesAbsent_ShouldKeepDefaultsWithoutConditionalRoots()
    {
        var closure = AgentGuideService.SelectProfileClosure(Catalog(), "application", [], "guide");

        Assert.Equal(["base", "dependency", "recommended"], closure!.Select(static skill => skill.Name));
    }

    [Fact]
    public void SelectProfileClosure_WhenDependencyMissing_ShouldRejectTheWholeSelection()
    {
        var catalog = Catalog();
        catalog = catalog with { Skills = catalog.Skills.Where(static skill => skill.Name != "dependency").ToArray() };
        var checks = new List<GuideCheck>();

        var closure = AgentGuideService.SelectProfileClosure(catalog, "application", checks, capabilities: ["microservice"]);

        Assert.Null(closure);
        Assert.Contains(checks, check => check.Status == GuideCheckStatus.Error && check.Message.Contains("dependency", StringComparison.Ordinal));
    }

    private static SkillCatalog Catalog()
        => new(1, 7, string.Empty,
            [
                Entry("base", ["dependency"]), Entry("dependency"), Entry("guide"),
                Entry("micro", ["base"]), Entry("modular", ["base"]), Entry("recommended"), Entry("ui", ["base"])
            ],
            ProfileSelections: new Dictionary<string, GuideProfileSkillSelection>
            {
                ["application"] = new(["base", "guide"], ["recommended"],
                    [new("microservice", ["micro"]), new("modular-monolith", ["modular"]), new("ui", ["ui"])])
            });

    private static SkillCatalogEntry Entry(string name, IReadOnlyList<string>? dependencies = null)
        => new(name, $"skills/{name}", "capability", [], dependencies ?? [], string.Empty);
}
