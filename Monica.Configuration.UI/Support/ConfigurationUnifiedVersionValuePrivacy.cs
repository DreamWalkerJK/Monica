using Monica.Configuration.Models;

namespace Monica.Configuration.UI.Support;

/// <summary>Requires current schema authority before exposing raw unified-version snapshot values or paths.</summary>
/// <remarks>
/// A facade snapshot is not API-redacted. Missing or blank schema identity, a version/hash mismatch, an
/// unresolved path, or a payload without its matching captured document fails closed. Sensitivity includes
/// every ancestor and the complete selected subtree, including dictionary and list templates.
/// </remarks>
internal static class ConfigurationUnifiedVersionValuePrivacy
{
    internal static bool HasSchemaDrift(ConfigurationUnifiedVersionComparison comparison,
        ConfigurationUnifiedVersionDefinitionChange change, ConfigurationDefinition? definition) =>
        definition is null
        || !HasProvenSide(comparison.Origin, change.DefinitionKey, change.OriginJson, definition)
        || !HasProvenSide(comparison.Target, change.DefinitionKey, change.TargetJson, definition);

    internal static bool HasSchemaDrift(ConfigurationUnifiedVersionApplyTarget target, ConfigurationDefinition? definition) =>
        definition is null || target.CurrentSchemaVersion != definition.SchemaVersion
        || !SameSchemaHash(target.CapturedSchemaHash, target.CurrentSchemaHash)
        || !SameSchemaHash(target.CurrentSchemaHash, definition.SchemaHash);

    internal static bool ShouldHidePath(ConfigurationDefinition? definition, string logicalPath, bool schemaUnproven)
    {
        if (schemaUnproven || definition is null) return true;
        try
        {
            var current = definition.Root;
            if (current.IsSensitive) return true;
            foreach (var segment in LogicalPath.Parse(logicalPath).Segments)
            {
                current = segment switch
                {
                    PropertySegment property => current.Children.FirstOrDefault(child =>
                        string.Equals(child.Name, property.Name, StringComparison.OrdinalIgnoreCase)),
                    DictionaryKeySegment => current.DictionaryTemplate?.ValueTemplate,
                    ListItemKeySegment or ListIndexSegment => current.ListTemplate?.ItemTemplate,
                    _ => null
                };
                if (current is null || current.IsSensitive) return true;
            }
            return ContainsSensitiveNode(current);
        }
        catch (FormatException) { return true; }
        catch (ArgumentException) { return true; }
    }

    private static bool HasProvenSide(ConfigurationUnifiedVersionSnapshot snapshot, string definitionKey,
        string? payload, ConfigurationDefinition definition)
    {
        var documents = snapshot.Definitions.Where(document =>
            string.Equals(document.DefinitionKey, definitionKey, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (payload is null) return documents.Length == 0;
        return documents.Length == 1 && documents[0].SchemaVersion == definition.SchemaVersion
            && SameSchemaHash(documents[0].SchemaHash, definition.SchemaHash)
            && string.Equals(documents[0].Json, payload, StringComparison.Ordinal);
    }

    private static bool SameSchemaHash(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right)
        && string.Equals(left, right, StringComparison.Ordinal);

    private static bool ContainsSensitiveNode(ConfigurationNodeDefinition node) => node.IsSensitive
        || node.Children.Any(ContainsSensitiveNode)
        || node.DictionaryTemplate?.ValueTemplate is { } dictionary && ContainsSensitiveNode(dictionary)
        || node.ListTemplate?.ItemTemplate is { } item && ContainsSensitiveNode(item);
}
