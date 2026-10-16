using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Monica.Configuration.Models;
using Monica.Configuration.UI.Localization;
using Monica.Configuration.UI.Support;
using Monica.Modules;
using Monica.Testing.Localization;

namespace Test.Monica.Configuration.UI.Support;

public sealed class ConfigurationValueVisibilityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BuildEditorDocument_WhenHostSelectsDisclosure_ShouldKeepEditorBaselineAndExportConsistent(bool showSensitiveValues)
    {
        var visibility = new ConfigurationValueVisibility(Options.Create(new ModuleConfigurationUIOption
        {
            ShowSensitiveValues = showSensitiveValues
        }));
        var localizer = new EchoStringLocalizer<ConfigurationUIResource>();
        var drafts = new ConfigurationJsonDraftService(null!, localizer, new(localizer), visibility);
        var schema = UnifiedVersionPrivacyTestData.Node("Root") with
        {
            NodeKind = ConfigurationNodeKind.Object,
            Children =
            [
                UnifiedVersionPrivacyTestData.Node("ConsumerOriginator"),
                UnifiedVersionPrivacyTestData.Node("Token") with { IsSensitive = true }
            ]
        };
        var definition = UnifiedVersionPrivacyTestData.Definition() with { Root = schema };
        const string json = """{"ConsumerOriginator":"ordinary-value","Token":"synthetic-secret"}""";
        var value = new ConfigurationEffectiveValue
        {
            DefinitionKey = definition.DefinitionKey,
            LogicalPath = LogicalPath.Root,
            DisplayValue = json,
            IsSensitive = true
        };

        var editor = drafts.BuildEditorDocument(definition, schema, value,
            new Dictionary<LogicalPath, ConfigurationEffectiveValue>(), []);
        var exported = drafts.RedactForExport(JsonNode.Parse(json), schema, out var redactedPaths);
        var preview = ConfigurationJsonDisplayFormatter.Format(json, schema, "redacted", visibility.ShowSensitiveValues);

        Assert.True(editor.CanAnalyze);
        Assert.Contains("ordinary-value", editor.Json);
        Assert.Equal(showSensitiveValues, editor.Json.Contains("synthetic-secret", StringComparison.Ordinal));
        Assert.Equal(showSensitiveValues, editor.BaselineJson!.Contains("synthetic-secret", StringComparison.Ordinal));
        Assert.Equal(showSensitiveValues, exported!.ToJsonString().Contains("synthetic-secret", StringComparison.Ordinal));
        Assert.Equal(showSensitiveValues, preview.Contains("synthetic-secret", StringComparison.Ordinal));
        Assert.Equal(showSensitiveValues, redactedPaths.Count == 0);
        Assert.Equal(showSensitiveValues, editor.RedactedPaths.Count == 0);
        Assert.True(schema.Children[1].IsSensitive);
        Assert.Equal(json, value.DisplayValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HistoryComparison_WhenDisclosureEnabled_ShouldRevealSensitiveAndUnavailableSchemaValues(bool definitionAvailable)
    {
        var definition = UnifiedVersionPrivacyTestData.Definition() with
        {
            Root = UnifiedVersionPrivacyTestData.Node("Root") with { IsSensitive = true }
        };
        var definitions = new Dictionary<string, ConfigurationDefinition>(StringComparer.OrdinalIgnoreCase);
        if (definitionAvailable) definitions.Add(definition.DefinitionKey, definition);
        var row = new ConfigurationValueHistory
        {
            HistoryId = "old-record",
            DefinitionKey = definition.DefinitionKey,
            LogicalPath = LogicalPath.Root,
            SchemaVersion = 30,
            OldValue = ConfigurationStoredValue.FromJson("\"synthetic-before\""),
            NewValue = ConfigurationStoredValue.FromJson("\"synthetic-after\"")
        };

        Assert.True(ConfigurationHistoryValuePrivacy.IsSensitive(row, definitions));
        Assert.False(ConfigurationHistoryValuePrivacy.IsSensitive(row, definitions, showSensitiveValues: true));
        var pair = ConfigurationUnifiedVersionJsonFormatter.FormatComparisonForDisplay(
            row.OldValue.Json, row.NewValue.Json, definitionAvailable ? definition : null,
            "sensitive", "hidden", "before", "after", showSensitiveValues: true);

        Assert.Contains("synthetic-before", pair.OriginText);
        Assert.Contains("synthetic-after", pair.TargetText);
        Assert.False(pair.HasHiddenChanges);
        Assert.False(ConfigurationUnifiedVersionValuePrivacy.ShouldHidePath(null, "Unknown.Path", showSensitiveValues: true));
    }
}
