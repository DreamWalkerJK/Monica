using Microsoft.AspNetCore.Components;
using Monica.AI.Chat.Models;
using Monica.AI.Configuration.Models;
using Monica.AI.Providers;
using Monica.Core.Results;
using MudBlazor;

namespace Monica.AI.UI.UIProvider.Components;

/// <summary>Edits one provider-scoped model draft, preserving unknown capabilities and explicit protocol mappings.</summary>
public partial class ModelConfigurationDialog
{
    /// <summary>The containing dialog; a successful save closes it.</summary>
    [CascadingParameter] public IMudDialogInstance Dialog { get; set; } = null!;
    /// <summary>Initial saved or discovered values; null creates an empty draft.</summary>
    [Parameter] public AIModelConfiguration? Model { get; set; }
    /// <summary>Controls provider-specific reasoning options.</summary>
    [Parameter] public EAIProviderType ProviderType { get; set; }
    /// <summary>Saves the draft; a safe diagnostic keeps the dialog open on failure.</summary>
    [Parameter, EditorRequired] public required Func<AIModelConfiguration, Task<string?>> OnSave { get; set; }
    /// <summary>Optional cancellable discovery using the owning provider's current connection. Does not persist edits.</summary>
    [Parameter] public Func<CancellationToken, Task<Res<IReadOnlyList<AIModelConfiguration>>>>? OnDiscover { get; set; }

    private readonly CancellationTokenSource _lifetime = new();
    private bool _saving, _fetching, _disposed;
    private string _name = string.Empty, _contextText = string.Empty, _outputText = string.Empty;
    private string? _display, _defaultReasoning, _error, _metadataMessage;
    private AIModelKind _kind;
    private int? _dimensions;
    private bool? _images, _documents, _tools, _reasoning;
    private List<ReasoningForm> _levels = [];
    private bool Busy => _saving || _fetching;
    private bool InvalidContext => !TokenQuantity.TryParse(_contextText, out _);
    private bool InvalidOutput => !TokenQuantity.TryParse(_outputText, out _);
    private IReadOnlyList<string> SelectedLevelIds => _levels
        .Where(level => !string.IsNullOrWhiteSpace(level.Id))
        .Select(level => level.Id.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    protected override void OnInitialized() => Reset();

    private void Reset()
    {
        Load(Model ?? new AIModelConfiguration { ModelName = string.Empty });
        _error = null;
        _metadataMessage = null;
    }

    private void Load(AIModelConfiguration model)
    {
        _name = model.ModelName; _display = model.DisplayName; _kind = model.Kind;
        _contextText = TokenQuantity.Format(model.ContextWindow); _outputText = TokenQuantity.Format(model.MaxOutputTokens);
        _dimensions = model.EmbeddingDimensions;
        _images = model.SupportsImage; _documents = model.SupportsDocuments; _tools = model.SupportsTools; _reasoning = model.SupportsReasoning;
        _defaultReasoning = model.DefaultReasoningLevel;
        _levels = model.ReasoningLevels.Select(ReasoningForm.From).ToList();
    }

    private string TokenPreview(string text, bool useDefault)
    {
        if (!TokenQuantity.TryParse(text, out var tokens)) return string.Empty;
        return tokens is { } count ? L["Settings:ExactTokens", count]
            : useDefault ? L["Settings:DefaultContext", ChatContextCapacity.DefaultTokens] : L["Settings:UnknownNumber"];
    }

    private void SetReasoning(bool? value)
    {
        _reasoning = value;
        if (value == false) { _levels.Clear(); _defaultReasoning = null; }
    }

    private void SetLevels(IReadOnlyList<string> ids)
    {
        // Retain custom mappings when the supported-level picker changes its selection.
        _levels = ids.Select(id => _levels.FirstOrDefault(level => level.Id.Trim().Equals(id, StringComparison.OrdinalIgnoreCase))
            ?? new ReasoningForm { Id = id, ProviderValue = id }).ToList();
        if (_levels.Count > 0) _reasoning = true;
        if (!_levels.Any(level => level.Id.Trim().Equals(_defaultReasoning, StringComparison.OrdinalIgnoreCase))) _defaultReasoning = null;
    }

    private void AddLevel() { _levels.Add(new ReasoningForm()); _reasoning = true; }
    private void RemoveLevel(ReasoningForm level)
    {
        _levels.Remove(level);
        if (_defaultReasoning == level.Id) _defaultReasoning = null;
    }

    private AIModelConfiguration Build(int? context, int? output) => (Model ?? new AIModelConfiguration { ModelName = _name.Trim() }) with
    {
        ModelName = _name.Trim(), DisplayName = string.IsNullOrWhiteSpace(_display) ? null : _display.Trim(), Kind = _kind,
        ContextWindow = context, MaxOutputTokens = output, EmbeddingDimensions = _kind == AIModelKind.Embedding ? _dimensions : null,
        SupportsImage = _kind == AIModelKind.Chat ? _images : null, SupportsDocuments = _kind == AIModelKind.Chat ? _documents : null,
        SupportsTools = _kind == AIModelKind.Chat ? _tools : null, SupportsReasoning = _kind == AIModelKind.Chat ? _reasoning : null,
        ReasoningLevels = _kind == AIModelKind.Chat ? _levels.Select(level => new AIReasoningLevel
        {
            Id = level.Id.Trim(), DisplayName = level.DisplayName,
            ProviderValue = string.IsNullOrWhiteSpace(level.ProviderValue) ? null : level.ProviderValue.Trim(),
            BudgetTokens = ProviderType == EAIProviderType.Anthropic ? level.Budget : null
        }).ToArray() : [],
        DefaultReasoningLevel = _kind == AIModelKind.Chat
            ? _levels.FirstOrDefault(level => level.Id.Trim().Equals(_defaultReasoning, StringComparison.OrdinalIgnoreCase))?.Id.Trim()
            : null
    };

    private async Task FetchMetadataAsync()
    {
        if (Busy || _disposed || OnDiscover is null) return;
        if (!TokenQuantity.TryParse(_contextText, out var context) || !TokenQuantity.TryParse(_outputText, out var output))
        { _error = L["Settings:InvalidTokenQuantity"]; return; }
        var draft = Build(context, output);
        _fetching = true; _error = null; _metadataMessage = null;
        try
        {
            var result = await OnDiscover(_lifetime.Token);
            if (_disposed) return;
            if (result.IsFailed(out var error, out var models)) { _error = error.Message; return; }
            var evidence = models.FirstOrDefault(model => model.ModelName.Equals(draft.ModelName, StringComparison.OrdinalIgnoreCase));
            if (evidence is null) { _metadataMessage = L["Settings:MetadataNotFound"]; return; }
            Load(draft.WithDiscoveredMetadata(evidence));
            _metadataMessage = L["Settings:MetadataApplied"];
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception) { if (!_disposed) _error = L["Error:Generic"]; }
        finally { if (!_disposed) _fetching = false; }
    }

    private async Task SaveAsync()
    {
        if (Busy || _disposed) return;
        if (string.IsNullOrWhiteSpace(_name)) { _error = L["Settings:IdRequired"]; return; }
        if (!TokenQuantity.TryParse(_contextText, out var context) || !TokenQuantity.TryParse(_outputText, out var output))
        { _error = L["Settings:InvalidTokenQuantity"]; return; }
        var capacity = _kind == AIModelKind.Chat ? ChatContextCapacity.Resolve(null, context).Tokens : context;
        if ((_kind == AIModelKind.Embedding && _dimensions is <= 0) || (capacity is { } limit && output >= limit))
        { _error = L["Settings:InvalidModelLimits"]; return; }
        if (_kind == AIModelKind.Chat)
        {
            if (_levels.Any(level => string.IsNullOrWhiteSpace(level.Id))
                || _levels.Select(level => level.Id.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != _levels.Count)
            { _error = L["Settings:InvalidLevels"]; return; }
            var invalidBudget = ProviderType == EAIProviderType.Anthropic && _levels.Any(level => level.Budget is { } budget
                && (budget < 1024 || output is { } maximum && budget >= maximum || string.Equals(level.ProviderValue, "none", StringComparison.OrdinalIgnoreCase)));
            if ((_reasoning == false && _levels.Count > 0) || invalidBudget)
            { _error = L["Settings:InvalidReasoningConfiguration"]; return; }
        }
        _saving = true;
        try
        {
            var error = await OnSave(Build(context, output));
            if (_disposed) return;
            _error = error;
            if (error is null) Dialog.Close(DialogResult.Ok(true));
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception exception) { if (!_disposed) _error = exception.Message; }
        finally { if (!_disposed) _saving = false; }
    }

    /// <summary>Cancels discovery and ignores callbacks after the dialog closes.</summary>
    public void Dispose() { if (_disposed) return; _disposed = true; _lifetime.Cancel(); _lifetime.Dispose(); }

    private sealed class ReasoningForm
    {
        public string Id { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? ProviderValue { get; set; }
        public int? Budget { get; set; }
        public static ReasoningForm From(AIReasoningLevel level) => new() { Id = level.Id, DisplayName = level.DisplayName, ProviderValue = level.ProviderValue, Budget = level.BudgetTokens };
    }
}
