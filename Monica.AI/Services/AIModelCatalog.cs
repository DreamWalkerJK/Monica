using Monica.AI.Models;

namespace Monica.AI.Services;

/// <summary>
/// Code-defined model metadata templates. Runtime identity and overrides are provider-scoped;
/// this catalog supplies explicit registration defaults and exact metadata for official endpoint discovery.
/// </summary>
internal sealed class AIModelCatalog
{
    private readonly Dictionary<string, ModelTemplate> _models = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers an explicit developer template, replacing any built-in template with the same name.
    /// Code-defined providers may reference this template on any endpoint through SupportedModels.
    /// </summary>
    public void AddModel(AIModelInfo model)
    {
        _models[model.ModelName] = new ModelTemplate(model, IsExplicit: true);
    }

    /// <summary>
    /// Adds built-in templates without replacing explicit developer registrations.
    /// </summary>
    public void AddReservedModels(IEnumerable<AIModelInfo> models)
    {
        foreach (var model in models)
        {
            _models.TryAdd(model.ModelName, new ModelTemplate(model, IsExplicit: false));
        }
    }

    /// <summary>
    /// Get all models in the catalog
    /// </summary>
    public IReadOnlyList<AIModelInfo> GetModels()
    {
        return _models.Values.Select(static template => template.Model).ToList().AsReadOnly();
    }

    /// <summary>
    /// Get all model names in the catalog
    /// </summary>
    public IReadOnlyList<string> GetModelNames()
    {
        return _models.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets an exact template by name (case-insensitive). Explicit code registrations are always eligible;
    /// callers must disable built-in templates when a custom endpoint may use the same name for another model.
    /// </summary>
    public AIModelInfo? GetModel(string modelName, bool allowBuiltInTemplates = true)
    {
        return _models.TryGetValue(modelName, out var template) && (template.IsExplicit || allowBuiltInTemplates)
            ? template.Model : null;
    }

    private sealed record ModelTemplate(AIModelInfo Model, bool IsExplicit);
}
