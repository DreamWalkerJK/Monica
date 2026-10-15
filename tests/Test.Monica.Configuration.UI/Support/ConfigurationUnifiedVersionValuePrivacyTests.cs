using Monica.Configuration.Models;
using Monica.Configuration.UI.Support;

namespace Test.Monica.Configuration.UI.Support;

public sealed class ConfigurationUnifiedVersionValuePrivacyTests
{
    [Theory]
    [InlineData("missing", false)] [InlineData("missing", true)]
    [InlineData("hash", false)] [InlineData("hash", true)]
    [InlineData("blank", false)] [InlineData("blank", true)]
    [InlineData("version", false)] [InlineData("version", true)]
    [InlineData("payload", false)] [InlineData("payload", true)]
    [InlineData("duplicate", false)] [InlineData("duplicate", true)]
    public void Comparison_WhenEitherSideIsUnproven_ShouldRedactBothValues(string failure, bool targetSide)
    {
        var comparison = UnifiedVersionPrivacyTestData.Comparison();
        var side = targetSide ? comparison.Target : comparison.Origin;
        var document = Assert.Single(side.Definitions);
        side = failure switch
        {
            "missing" => side with { Definitions = [] },
            "hash" => side with { Definitions = [document with { SchemaHash = "different" }] },
            "blank" => side with { Definitions = [document with { SchemaHash = " " }] },
            "version" => side with { Definitions = [document with { SchemaVersion = 2 }] },
            "payload" => side with { Definitions = [document with { Json = "\"other payload\"" }] },
            _ => side with { Definitions = [document, document] }
        };
        comparison = targetSide ? comparison with { Target = side } : comparison with { Origin = side };
        var pair = Display(comparison, UnifiedVersionPrivacyTestData.Definition());
        Assert.DoesNotContain("before-marker", pair.OriginText);
        Assert.DoesNotContain("after-marker", pair.TargetText);
        Assert.True(pair.HasHiddenChanges);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Comparison_WhenSideIsLegitimatelyAbsent_ShouldPreserveAddedOrRemovedValue(bool removed)
    {
        var comparison = UnifiedVersionPrivacyTestData.Comparison();
        var change = Assert.Single(comparison.Changes);
        comparison = removed
            ? comparison with { Target = comparison.Target with { Definitions = [] },
                Changes = [change with { TargetJson = null, ChangeKind = ConfigurationUnifiedVersionDefinitionChangeKind.Removed }] }
            : comparison with { Origin = comparison.Origin with { Definitions = [] },
                Changes = [change with { OriginJson = null, ChangeKind = ConfigurationUnifiedVersionDefinitionChangeKind.Added }] };
        Assert.False(ConfigurationUnifiedVersionValuePrivacy.HasSchemaDrift(comparison, Assert.Single(comparison.Changes),
            UnifiedVersionPrivacyTestData.Definition()));
        var pair = Display(comparison, UnifiedVersionPrivacyTestData.Definition());
        Assert.Equal("null", removed ? pair.TargetText : pair.OriginText);
        Assert.Contains(removed ? "before-marker" : "after-marker", removed ? pair.OriginText : pair.TargetText);
    }

    [Fact]
    public void Comparison_WhenAuthorityMissingOrBlank_ShouldRedactBothValues()
    {
        foreach (var definition in new ConfigurationDefinition?[] { null, UnifiedVersionPrivacyTestData.Definition() with { SchemaHash = " " } })
        {
            var pair = Display(UnifiedVersionPrivacyTestData.Comparison(), definition);
            Assert.DoesNotContain("before-marker", pair.OriginText);
            Assert.DoesNotContain("after-marker", pair.TargetText);
        }
    }

    [Fact]
    public void Comparison_WhenProven_ShouldPreservePublicValuesAndOpaqueIdentity()
    {
        var comparison = UnifiedVersionPrivacyTestData.Comparison();
        Assert.Equal(UnifiedVersionPrivacyTestData.DefinitionKey, Assert.Single(comparison.Changes).DefinitionKey);
        var pair = Display(comparison, UnifiedVersionPrivacyTestData.Definition());
        Assert.Contains("before-marker", pair.OriginText);
        Assert.Contains("after-marker", pair.TargetText);
        Assert.False(pair.HasHiddenChanges);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Comparison_WhenRootOrParentSensitive_ShouldRedactDescendants(bool rootSensitive)
    {
        var root = UnifiedVersionPrivacyTestData.Node("Root") with { NodeKind = ConfigurationNodeKind.Object, IsSensitive = rootSensitive,
            Children = [UnifiedVersionPrivacyTestData.Node("Branch") with { NodeKind = ConfigurationNodeKind.Object,
                IsSensitive = !rootSensitive, Children = [UnifiedVersionPrivacyTestData.Node("Value")] }] };
        var definition = UnifiedVersionPrivacyTestData.Definition() with { Root = root };
        var pair = Display(UnifiedVersionPrivacyTestData.Comparison("{\"Branch\":{\"Value\":\"before-marker\"}}",
            "{\"Branch\":{\"Value\":\"after-marker\"}}"), definition);
        Assert.DoesNotContain("before-marker", pair.OriginText);
        Assert.DoesNotContain("after-marker", pair.TargetText);
        Assert.True(ConfigurationUnifiedVersionValuePrivacy.ShouldHidePath(definition,
            LogicalPath.FromProperties("Branch", "Value").ToCanonicalString(), false));
    }

    [Theory]
    [InlineData("object")] [InlineData("dictionary")] [InlineData("list")]
    public void Path_WhenSubtreeOrTemplateSensitive_ShouldHideAggregatePath(string kind)
    {
        var value = UnifiedVersionPrivacyTestData.Node("Value") with { IsSensitive = true };
        var branch = UnifiedVersionPrivacyTestData.Node("Branch");
        branch = kind switch
        {
            "dictionary" => branch with { NodeKind = ConfigurationNodeKind.Dictionary,
                DictionaryTemplate = new() { KeyClrTypeName = "System.String", ValueTemplate = value } },
            "list" => branch with { NodeKind = ConfigurationNodeKind.List, ListTemplate = new() { ItemTemplate = value } },
            _ => branch with { NodeKind = ConfigurationNodeKind.Object, Children = [value] }
        };
        var definition = UnifiedVersionPrivacyTestData.Definition() with { Root = UnifiedVersionPrivacyTestData.Node("Root") with
            { NodeKind = ConfigurationNodeKind.Object, Children = [branch] } };
        Assert.True(ConfigurationUnifiedVersionValuePrivacy.ShouldHidePath(definition, "Branch", false));
    }

    [Theory]
    [InlineData("unknown")] [InlineData("Branch[unterminated")]
    public void Path_WhenUnresolvedOrMalformed_ShouldHideIt(string path) =>
        Assert.True(ConfigurationUnifiedVersionValuePrivacy.ShouldHidePath(UnifiedVersionPrivacyTestData.Definition(), path, false));

    [Fact]
    public void Path_WhenProvenPublicScalar_ShouldPreserveVisiblePath()
    {
        var definition = UnifiedVersionPrivacyTestData.Definition();
        Assert.False(ConfigurationUnifiedVersionValuePrivacy.ShouldHidePath(definition, "Value", false));
        Assert.True(ConfigurationUnifiedVersionValuePrivacy.ShouldHidePath(definition, "Value", true));
        Assert.True(ConfigurationUnifiedVersionValuePrivacy.ShouldHidePath(null, "Value", false));
    }

    [Theory]
    [InlineData("captured-blank")] [InlineData("captured-drift")]
    [InlineData("current-blank")] [InlineData("current-drift")]
    [InlineData("current-version")] [InlineData("missing-version")]
    [InlineData("definition-blank")] [InlineData("missing-definition")]
    public void Rollback_WhenSchemaAuthorityUnproven_ShouldRequireRedaction(string failure)
    {
        ConfigurationDefinition? definition = UnifiedVersionPrivacyTestData.Definition();
        var target = new ConfigurationUnifiedVersionApplyTarget { DefinitionKey = definition.DefinitionKey, DisplayName = "Review",
            TargetJson = "\"after-marker\"", CapturedSchemaHash = "schema", CurrentSchemaHash = "schema", CurrentSchemaVersion = 1 };
        target = failure switch
        {
            "captured-blank" => target with { CapturedSchemaHash = " " },
            "captured-drift" => target with { CapturedSchemaHash = "other" },
            "current-blank" => target with { CurrentSchemaHash = " " },
            "current-drift" => target with { CurrentSchemaHash = "other" },
            "current-version" => target with { CurrentSchemaVersion = 2 },
            "missing-version" => target with { CurrentSchemaVersion = null },
            _ => target
        };
        if (failure == "definition-blank") definition = definition with { SchemaHash = " " };
        if (failure == "missing-definition") definition = null;
        Assert.True(ConfigurationUnifiedVersionValuePrivacy.HasSchemaDrift(target, definition));
    }

    private static ConfigurationUnifiedVersionJsonDisplayPair Display(ConfigurationUnifiedVersionComparison comparison,
        ConfigurationDefinition? definition)
    {
        var change = Assert.Single(comparison.Changes);
        return ConfigurationUnifiedVersionJsonFormatter.FormatComparisonForDisplay(change.OriginJson, change.TargetJson,
            definition, "sensitive", "hidden", "hidden before change", "hidden after change",
            ConfigurationUnifiedVersionValuePrivacy.HasSchemaDrift(comparison, change, definition));
    }
}

internal static class UnifiedVersionPrivacyTestData
{
    internal const string DefinitionKey = " owner:上海/config?revision=1#opaque%25 ";
    internal static ConfigurationUnifiedVersionComparison Comparison(string origin = "{\"Value\":\"before-marker\"}",
        string target = "{\"Value\":\"after-marker\"}", long originVersion = 1, long targetVersion = 2) => new()
    {
        Origin = new() { Summary = new() { Version = originVersion }, Definitions = [Document(origin)] },
        Target = new() { Summary = new() { Version = targetVersion }, Definitions = [Document(target)] },
        Changes = [new() { DefinitionKey = DefinitionKey, DisplayName = "Review", OriginJson = origin, TargetJson = target,
            ChangeKind = ConfigurationUnifiedVersionDefinitionChangeKind.Modified }]
    };
    internal static ConfigurationUnifiedVersionDefinitionSnapshot Document(string json) => new()
    {
        DefinitionKey = DefinitionKey, DisplayName = "Review", FromProject = "Review", SchemaHash = "schema", SchemaVersion = 1, Json = json
    };
    internal static ConfigurationDefinition Definition() => new()
    {
        DefinitionKey = DefinitionKey, DisplayName = "Review", SectionPath = "Review", ClrTypeName = "ReviewOptions", FromProject = "Review",
        SchemaHash = "schema", SchemaVersion = 1,
        Root = Node("Root") with { NodeKind = ConfigurationNodeKind.Object, Children = [Node("Value")] }
    };
    internal static ConfigurationNodeDefinition Node(string name) => new()
    {
        Name = name, NodeKey = name, RelativePath = LogicalPath.Root, ClrTypeName = "System.String", NodeKind = ConfigurationNodeKind.Scalar
    };
}
