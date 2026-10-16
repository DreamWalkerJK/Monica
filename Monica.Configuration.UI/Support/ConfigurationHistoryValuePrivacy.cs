using Monica.Configuration.Models;

namespace Monica.Configuration.UI.Support;

/// <summary>Determines whether a history comparison can safely expose a stored value.</summary>
/// <remarks>
/// Missing definitions and unresolved paths remain hidden unless the host enables full disclosure. Sensitivity is inherited from every
/// ancestor and includes the complete target subtree, including dictionary and list templates.
/// </remarks>
internal static class ConfigurationHistoryValuePrivacy
{
    internal static bool IsSensitive(ConfigurationValueHistory row,
        IReadOnlyDictionary<string, ConfigurationDefinition> definitions, bool showSensitiveValues = false)
    {
        if (showSensitiveValues) return false;
        if (!definitions.TryGetValue(row.DefinitionKey, out var definition)) return true;

        // Preserve ConfigurationSchemaNavigator's ancestor semantics without broadening its internal core API.
        var current = definition.Root;
        if (current.IsSensitive) return true;
        foreach (var segment in row.LogicalPath.Segments)
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

    private static bool ContainsSensitiveNode(ConfigurationNodeDefinition node) =>
        node.IsSensitive || node.Children.Any(ContainsSensitiveNode)
        || node.DictionaryTemplate?.ValueTemplate is { } dictionaryValue && ContainsSensitiveNode(dictionaryValue)
        || node.ListTemplate?.ItemTemplate is { } listItem && ContainsSensitiveNode(listItem);
}
