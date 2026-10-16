using Monica.Configuration.Models;
using Monica.Configuration.UI.Support;

namespace Test.Monica.Configuration.UI.Support;

public sealed class ConfigurationHistoryValuePrivacyTests
{
    private const string DefinitionKey = " owner:上海/config?revision=1#opaque%25 ";
    private const string SchemaHash = "current-schema";
    private const string Redacted = "redacted";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IsSensitive_WhenScalarHasSensitiveObjectOrRootAncestor_ShouldRedactEveryComparison(bool sensitiveRoot)
    {
        var parent = Node("Credentials") with { IsSensitive = !sensitiveRoot, Children = [Node("Value")] };
        var root = Node("Root") with { IsSensitive = sensitiveRoot, Children = [parent] };

        AssertRedacted(Row(LogicalPath.FromProperties("credentials", "VALUE")), Definition(root));
    }

    [Theory]
    [InlineData("dictionary")]
    [InlineData("list-key")]
    [InlineData("list-index")]
    public void IsSensitive_WhenTemplateAncestorIsSensitive_ShouldRedactItsUnmarkedScalarChild(string templateKind)
    {
        var template = Node("Item") with { IsSensitive = true, Children = [Node("Value")] };
        var collection = Collection(templateKind, template);
        var root = Node("Root") with { Children = [collection] };
        var path = LogicalPath.FromProperties("Items").Append(ItemSegment(templateKind)).Append(new PropertySegment("Value"));

        AssertRedacted(Row(path), Definition(root));
    }

    [Theory]
    [InlineData("object")]
    [InlineData("dictionary")]
    [InlineData("list-key")]
    public void IsSensitive_WhenWholeTargetContainsSensitiveChildrenOrTemplates_ShouldPreserveSubtreeRedaction(string targetKind)
    {
        var sensitive = Node("Value") with { IsSensitive = true };
        var target = targetKind == "object"
            ? Node("Items") with { Children = [sensitive] }
            : Collection(targetKind, sensitive);
        var root = Node("Root") with { Children = [target] };

        AssertRedacted(Row(LogicalPath.FromProperties("Items")), Definition(root));
    }

    [Theory]
    [InlineData(2, SchemaHash)]
    [InlineData(1, "different-schema")]
    [InlineData(1, "CURRENT-SCHEMA")]
    [InlineData(1, null)]
    [InlineData(1, " ")]
    public void IsSensitive_WhenSchemaIdentityIsUnproven_ShouldRedactEvenOrdinaryNodes(int schemaVersion, string? schemaHash)
    {
        var root = Node("Root") with { Children = [Node("Value")] };
        var row = Row(LogicalPath.FromProperties("Value")) with { SchemaVersion = schemaVersion, SchemaHash = schemaHash };

        AssertRedacted(row, Definition(root));
    }

    [Fact]
    public void IsSensitive_WhenDefinitionIsMissing_ShouldRedactLegacyAndUnavailableMetadata()
    {
        AssertRedacted(Row(LogicalPath.FromProperties("Value")), null);
    }

    [Theory]
    [InlineData("property")]
    [InlineData("dictionary")]
    [InlineData("list")]
    public void IsSensitive_WhenPathOrTemplateCannotBeResolved_ShouldFailClosed(string missingKind)
    {
        var root = Node("Root") with { Children = [Node("Value")] };
        var path = LogicalPath.FromProperties("Value").Append(missingKind switch
        {
            "dictionary" => new DictionaryKeySegment("tenant:上海/?%#"),
            "list" => new ListIndexSegment(0),
            _ => new PropertySegment("Missing")
        });

        AssertRedacted(Row(path), Definition(root));
    }

    [Fact]
    public void IsSensitive_WhenExactSchemaAndPublicScalarAreValid_ShouldKeepRecordedAndCurrentValuesReadable()
    {
        var root = Node("Root") with { Children = [Node("Value")] };
        var row = Row(LogicalPath.FromProperties("VALUE"));
        var definitions = Definitions(Definition(root));
        var current = new Dictionary<string, ConfigurationStoredValue?> { [row.HistoryId] = ConfigurationStoredValue.FromJson("\"current-marker\"") };

        Assert.False(ConfigurationHistoryValuePrivacy.IsSensitive(row, definitions));
        bool Sensitive(ConfigurationValueHistory history) => ConfigurationHistoryValuePrivacy.IsSensitive(history, definitions);
        var origin = ConfigurationHistoryDiffSnapshotBuilder.BuildOriginSnapshot([row], false, current, Sensitive, key => key, _ => "target", Redacted);
        var target = ConfigurationHistoryDiffSnapshotBuilder.BuildTargetSnapshot([row], false, Sensitive, key => key, _ => "target", Redacted);
        var rollbackOrigin = ConfigurationHistoryDiffSnapshotBuilder.BuildOriginSnapshot([row], true, current, Sensitive, key => key, _ => "target", Redacted);
        Assert.Contains("before-marker", origin);
        Assert.Contains("after-marker", target);
        Assert.Contains("current-marker", rollbackOrigin);
        Assert.DoesNotContain(Redacted, origin + target + rollbackOrigin);
    }

    public static IEnumerable<object[]> UnprovenCollapsedSchemaCases()
    {
        foreach (var rootPath in new[] { false, true })
        foreach (var unprovenIndex in new[] { 0, 1, 2 })
        foreach (var unprovenKind in new[] { "missing-hash", "different-hash", "different-version" })
            yield return [rootPath, unprovenIndex, unprovenKind];
    }

    [Theory]
    [MemberData(nameof(UnprovenCollapsedSchemaCases))]
    public void BuildSnapshots_WhenCollapsedPathContainsUnprovenSchema_ShouldRedactEveryComparison(
        bool rootPath, int unprovenIndex, string unprovenKind)
    {
        var path = rootPath ? LogicalPath.Root : LogicalPath.FromProperties("Branch", "Value");
        var definition = Definition(Node("Root") with
        {
            Children = [Node("Branch") with { Children = [Node("Value")] }]
        });
        var rows = CollapsedRows(path);
        rows[unprovenIndex] = unprovenKind switch
        {
            "missing-hash" => rows[unprovenIndex] with { SchemaHash = null },
            "different-hash" => rows[unprovenIndex] with { SchemaHash = "drifted-schema" },
            _ => rows[unprovenIndex] with { SchemaVersion = 2 }
        };
        var definitions = Definitions(definition);
        for (var index = 0; index < rows.Length; index++)
            Assert.Equal(index == unprovenIndex, ConfigurationHistoryValuePrivacy.IsSensitive(rows[index], definitions));

        AssertCollapsedRedacted(rows, definition);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BuildSnapshots_WhenProvenCollapsedPathIsSensitive_ShouldRedactEveryComparison(bool rootPath)
    {
        var path = rootPath ? LogicalPath.Root : LogicalPath.FromProperties("Branch", "Value");
        var definition = Definition(Node("Root") with
        {
            IsSensitive = rootPath,
            Children = [Node("Branch") with { IsSensitive = !rootPath, Children = [Node("Value")] }]
        });

        AssertCollapsedRedacted(CollapsedRows(path), definition);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BuildSnapshots_WhenEveryCollapsedMemberIsProven_ShouldKeepChronologicalValuesReadable(bool rootPath)
    {
        var path = rootPath ? LogicalPath.Root : LogicalPath.FromProperties("Branch", "Value");
        var definition = Definition(Node("Root") with
        {
            Children = [Node("Branch") with { Children = [Node("Value")] }]
        });
        var rows = CollapsedRows(path);
        var snapshots = ComparisonSnapshots([rows[2], rows[0], rows[1]], definition);

        Assert.Contains("before-marker-0", snapshots[0]);
        Assert.Contains("after-marker-2", snapshots[1]);
        Assert.Contains("current-grouped-2", snapshots[2]);
        Assert.Contains("before-marker-0", snapshots[3]);
        foreach (var snapshot in snapshots) Assert.DoesNotContain(Redacted, snapshot);
    }

    [Fact]
    public void BuildSnapshots_WhenOnlyOneCollapsedPathIsUnproven_ShouldKeepOtherPathsAndTargetsReadable()
    {
        var definition = Definition(Node("Root") with
        {
            Children = [Node("Branch") with { Children = [Node("Value")] }, Node("OtherValue")]
        });
        var rows = CollapsedRows(LogicalPath.FromProperties("Branch", "Value"));
        rows[1] = rows[1] with { SchemaHash = null };
        var safePath = Row(LogicalPath.FromProperties("OtherValue")) with
        {
            HistoryId = "safe-path",
            OldValue = ConfigurationStoredValue.FromJson("\"safe-path-before\""),
            NewValue = ConfigurationStoredValue.FromJson("\"safe-path-after\"")
        };
        var safeTarget = Row(LogicalPath.FromProperties("Branch", "Value")) with
        {
            HistoryId = "safe-target",
            TargetKind = ConfigurationMutationTargetKind.ExternalConfigurationSource,
            SourcePhysicalPath = "review:source/上海?revision=1#%25",
            OldValue = ConfigurationStoredValue.FromJson("\"safe-target-before\""),
            NewValue = ConfigurationStoredValue.FromJson("\"safe-target-after\"")
        };
        var snapshots = ComparisonSnapshots([.. rows, safePath, safeTarget], definition);
        foreach (var snapshot in snapshots)
        {
            Assert.Contains(Redacted, snapshot);
            foreach (var index in new[] { 0, 1, 2 })
            {
                Assert.DoesNotContain($"before-marker-{index}", snapshot);
                Assert.DoesNotContain($"after-marker-{index}", snapshot);
                Assert.DoesNotContain($"current-grouped-{index}", snapshot);
            }
        }
        Assert.Contains("safe-path-before", snapshots[0]);
        Assert.Contains("safe-target-before", snapshots[0]);
        Assert.Contains("safe-path-after", snapshots[1]);
        Assert.Contains("safe-target-after", snapshots[1]);
        Assert.Contains("current-safe-path", snapshots[2]);
        Assert.Contains("current-safe-target", snapshots[2]);
        Assert.Contains("safe-path-before", snapshots[3]);
        Assert.Contains("safe-target-before", snapshots[3]);
    }

    private static void AssertCollapsedRedacted(ConfigurationValueHistory[] rows, ConfigurationDefinition definition)
    {
        // Input ordering must not change which member controls the collapsed comparison's privacy.
        foreach (var snapshot in ComparisonSnapshots([rows[2], rows[0], rows[1]], definition))
        {
            Assert.Contains(Redacted, snapshot);
            foreach (var index in new[] { 0, 1, 2 })
            {
                Assert.DoesNotContain($"before-marker-{index}", snapshot);
                Assert.DoesNotContain($"after-marker-{index}", snapshot);
                Assert.DoesNotContain($"current-grouped-{index}", snapshot);
            }
        }
    }

    private static string[] ComparisonSnapshots(IReadOnlyList<ConfigurationValueHistory> rows, ConfigurationDefinition definition)
    {
        var definitions = Definitions(definition);
        bool Sensitive(ConfigurationValueHistory history) => ConfigurationHistoryValuePrivacy.IsSensitive(history, definitions);
        var current = rows.ToDictionary(row => row.HistoryId,
            row => (ConfigurationStoredValue?)ConfigurationStoredValue.FromJson($"\"current-{row.HistoryId}\""));
        return
        [
            ConfigurationHistoryDiffSnapshotBuilder.BuildOriginSnapshot(rows, false, current, Sensitive, key => key, row => row.TargetKind.ToString(), Redacted),
            ConfigurationHistoryDiffSnapshotBuilder.BuildTargetSnapshot(rows, false, Sensitive, key => key, row => row.TargetKind.ToString(), Redacted),
            ConfigurationHistoryDiffSnapshotBuilder.BuildOriginSnapshot(rows, true, current, Sensitive, key => key, row => row.TargetKind.ToString(), Redacted),
            ConfigurationHistoryDiffSnapshotBuilder.BuildTargetSnapshot(rows, true, Sensitive, key => key, row => row.TargetKind.ToString(), Redacted)
        ];
    }

    private static ConfigurationValueHistory[] CollapsedRows(LogicalPath path) => Enumerable.Range(0, 3)
        .Select(index => Row(path) with
        {
            HistoryId = $"grouped-{index}", ModifiedTime = DateTimeOffset.UnixEpoch.AddTicks(index), Version = index + 1,
            OldValue = ConfigurationStoredValue.FromJson($"\"before-marker-{index}\""),
            NewValue = ConfigurationStoredValue.FromJson($"\"after-marker-{index}\"")
        }).ToArray();

    private static void AssertRedacted(ConfigurationValueHistory row, ConfigurationDefinition? definition)
    {
        var definitions = Definitions(definition);
        Assert.True(ConfigurationHistoryValuePrivacy.IsSensitive(row, definitions));
        bool Sensitive(ConfigurationValueHistory history) => ConfigurationHistoryValuePrivacy.IsSensitive(history, definitions);
        var current = new Dictionary<string, ConfigurationStoredValue?> { [row.HistoryId] = ConfigurationStoredValue.FromJson("\"current-marker\"") };
        var snapshots = new[]
        {
            ConfigurationHistoryDiffSnapshotBuilder.BuildOriginSnapshot([row], false, current, Sensitive, key => key, _ => "target", Redacted),
            ConfigurationHistoryDiffSnapshotBuilder.BuildTargetSnapshot([row], false, Sensitive, key => key, _ => "target", Redacted),
            ConfigurationHistoryDiffSnapshotBuilder.BuildOriginSnapshot([row], true, current, Sensitive, key => key, _ => "target", Redacted),
            ConfigurationHistoryDiffSnapshotBuilder.BuildTargetSnapshot([row], true, Sensitive, key => key, _ => "target", Redacted)
        };
        foreach (var snapshot in snapshots)
        {
            Assert.Contains(Redacted, snapshot);
            Assert.DoesNotContain("before-marker", snapshot);
            Assert.DoesNotContain("after-marker", snapshot);
            Assert.DoesNotContain("current-marker", snapshot);
        }
    }

    private static Dictionary<string, ConfigurationDefinition> Definitions(ConfigurationDefinition? definition) => definition is null
        ? new(StringComparer.OrdinalIgnoreCase)
        : new(StringComparer.OrdinalIgnoreCase) { [definition.DefinitionKey] = definition };

    private static ConfigurationDefinition Definition(ConfigurationNodeDefinition root) => new()
    {
        DefinitionKey = DefinitionKey, SectionPath = "Review", DisplayName = "Review", ClrTypeName = "ReviewOptions",
        FromProject = "Review", SchemaHash = SchemaHash, SchemaVersion = 1, Root = root
    };

    private static ConfigurationNodeDefinition Node(string name) => new()
    {
        NodeKey = name, Name = name, RelativePath = LogicalPath.Root, ClrTypeName = "System.String"
    };

    private static ConfigurationNodeDefinition Collection(string kind, ConfigurationNodeDefinition template) => kind == "dictionary"
        ? Node("Items") with
        {
            NodeKind = ConfigurationNodeKind.Dictionary,
            DictionaryTemplate = new() { KeyClrTypeName = "System.String", ValueTemplate = template }
        }
        : Node("Items") with { NodeKind = ConfigurationNodeKind.List, ListTemplate = new() { ItemTemplate = template } };

    private static ConfigurationPathSegment ItemSegment(string kind) => kind switch
    {
        "dictionary" => new DictionaryKeySegment(" tenant:上海/?%# "),
        "list-key" => new ListItemKeySegment(" item:上海/?%# "),
        _ => new ListIndexSegment(0)
    };

    private static ConfigurationValueHistory Row(LogicalPath path) => new()
    {
        HistoryId = " history:上海/?revision=1#%25 ", DefinitionKey = DefinitionKey, LogicalPath = path,
        SchemaVersion = 1, SchemaHash = SchemaHash, OldValue = ConfigurationStoredValue.FromJson("\"before-marker\""),
        NewValue = ConfigurationStoredValue.FromJson("\"after-marker\"")
    };
}
