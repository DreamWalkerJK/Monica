using Microsoft.AspNetCore.Components;
using Monica.AI.Chat.Models;
using Monica.AI.Configuration.Models;
using Monica.AI.Providers;
using Monica.Core.Results;
using MudBlazor;

namespace Monica.AI.UI.UIProvider.Components;

/// <summary>Owns an unsaved provider draft, cancellable model discovery, and local model edits.</summary>
public partial class ProviderConfigurationDialog
{
    /// <summary>The containing MudBlazor dialog.</summary>
    [CascadingParameter] public IMudDialogInstance Dialog { get; set; } = null!;
    /// <summary>Saved values to edit, or null for a new provider.</summary>
    [Parameter] public AIProviderSettings? Provider { get; set; }
    /// <summary>Persists the complete draft; return a safe diagnostic to keep the dialog open on failure.</summary>
    [Parameter, EditorRequired] public required Func<ProviderConfigurationEdit, CancellationToken, Task<string?>> OnSave { get; set; }
    /// <summary>Fetches endpoint model metadata without saving the supplied draft.</summary>
    [Parameter, EditorRequired] public required Func<ProviderConfigurationEdit, CancellationToken, Task<Res<IReadOnlyList<AIModelConfiguration>>>> OnDiscover { get; set; }
    private bool _saving, _disposed;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _discoveryCancellation;
    private bool _discovering, _hasDiscovered;
    private string? _discoveryError, _defaultModel;
    private string _query = string.Empty;
    private List<AIModelConfiguration> _models = [];
    private IReadOnlyList<AIModelConfiguration> _discovered = [];
    private readonly HashSet<string> _selectedModels = new(StringComparer.OrdinalIgnoreCase);
    private (string? Url, string? Key, EAIProviderType Type, OpenAIProviderApiMode Mode, OpenAIProtocolProfile Profile, int Timeout, bool Clear)? _discoveryConnection;
    private (string? Url, string? Key, EAIProviderType Type, OpenAIProviderApiMode Mode, OpenAIProtocolProfile Profile, int Timeout, bool Clear) CurrentConnection =>
        (_url, _key, _type, _apiMode, _profile, _timeout, _clearKey);
    private bool DiscoveryMatches => _discoveryConnection == CurrentConnection;
    private ICollection<AIModelConfiguration> FilteredDiscovery => _discovered.Where(model => model.ModelName.Contains(_query, StringComparison.OrdinalIgnoreCase)).ToArray();
    private string _id = string.Empty;
    private string? _name, _url, _key, _prompt, _error;
    private EAIProviderType _type;
    private OpenAIProviderApiMode _apiMode = OpenAIProviderApiMode.Chat;
    private OpenAIProtocolProfile _profile;
    private bool _enabled = true, _default, _clearKey;
    private int _timeout = 120;
    protected override void OnInitialized()
    {
        if (Provider?.Configuration is not { } value) return;
        _id = value.ProviderId; _name = value.DisplayName; _url = value.BaseUrl; _prompt = value.SystemPrompt;
        _type = value.ProviderType; _apiMode = value.OpenAIApiMode; _profile = value.OpenAIProtocolProfile; _enabled = value.Enabled;
        _default = value.IsDefault; _timeout = value.TimeoutSeconds;
        _models = value.Models.ToList(); _defaultModel = value.DefaultModel;
    }
    private void Cancel() => Dialog.Cancel();
    private async Task SaveAsync()
    {
        if (_saving || _discovering || _disposed) return;
        if (string.IsNullOrWhiteSpace(_id)) { _error = L["Settings:IdRequired"]; return; }
        if (!string.IsNullOrWhiteSpace(_url) && (!Uri.TryCreate(_url, UriKind.Absolute, out var endpoint) || endpoint.Scheme is not ("http" or "https")))
        { _error = L["Settings:InvalidUrl"]; return; }
        var configuration = BuildConfiguration();
        _saving = true;
        try
        {
            var error = await OnSave(new ProviderConfigurationEdit(configuration, _key, _clearKey), _lifetime.Token);
            if (_disposed) return;
            _error = error;
            if (error is null) { _key = null; Dialog.Close(DialogResult.Ok(true)); }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!_disposed) _error = L["Error:Generic"]; }
        finally { if (!_disposed) _saving = false; }
    }
    private AIProviderConfiguration BuildConfiguration() => (Provider?.Configuration ?? new AIProviderConfiguration { ProviderId = _id.Trim() }) with
    {
        DisplayName = _name, BaseUrl = string.IsNullOrWhiteSpace(_url) ? null : _url.Trim(),
        ProviderType = _type, OpenAIApiMode = _apiMode, OpenAIProtocolProfile = _profile, SystemPrompt = _prompt, Enabled = _enabled,
        IsDefault = _default, TimeoutSeconds = _timeout, Models = _models.ToArray(), DefaultModel = _defaultModel
    };

    private async Task FetchModelsAsync()
    {
        if (_discovering || _saving || _disposed) return;
        _discovering = true; _discoveryError = null;
        var connection = CurrentConnection;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _discoveryCancellation = cancellation;
        try
        {
            var result = await OnDiscover(new ProviderConfigurationEdit(BuildConfiguration(), _key, _clearKey), cancellation.Token);
            if (_disposed || cancellation.IsCancellationRequested) return;
            if (connection != CurrentConnection)
            { _discoveryError = L["Settings:DraftConnectionChanged"]; return; }
            if (result.IsFailed(out var error, out var models)) { _discoveryError = error.Message; return; }
            _discovered = models; _hasDiscovered = true; _discoveryConnection = connection; _selectedModels.Clear();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception) { if (!_disposed) _discoveryError = L["Error:Generic"]; }
        finally { _discoveryCancellation = null; if (!_disposed) _discovering = false; }
    }

    private void CancelDiscovery() => _discoveryCancellation?.Cancel();
    private void SelectModel(string id, bool selected) { if (selected) _selectedModels.Add(id); else _selectedModels.Remove(id); }
    private void SelectVisible() { foreach (var model in FilteredDiscovery) _selectedModels.Add(model.ModelName); }
    private void ImportSelected()
    {
        if (!DiscoveryMatches) { _discoveryError = L["Settings:DraftConnectionChanged"]; return; }
        foreach (var model in _discovered.Where(model => _selectedModels.Contains(model.ModelName)))
        {
            var index = _models.FindIndex(existing => string.Equals(existing.ModelName, model.ModelName, StringComparison.OrdinalIgnoreCase));
            if (index < 0) _models.Add(model); else _models[index] = _models[index].WithDiscoveredMetadata(model);
        }
        _selectedModels.Clear();
        _defaultModel ??= _models.FirstOrDefault(model => model.Kind == AIModelKind.Chat)?.ModelName;
    }
    private void RemoveModel(AIModelConfiguration model)
    {
        _models.Remove(model);
        if (_defaultModel == model.ModelName) _defaultModel = null;
    }
    private async Task EditModelAsync(AIModelConfiguration? model)
    {
        await DialogService.ShowAsync<ModelConfigurationDialog>(model is null ? L["Settings:AddModel"] : L["Settings:EditModel"],
            new DialogParameters
            {
                [nameof(ModelConfigurationDialog.Model)] = model,
                [nameof(ModelConfigurationDialog.ProviderType)] = _type,
                [nameof(ModelConfigurationDialog.OnSave)] = (Func<AIModelConfiguration, Task<string?>>)(async saved =>
                {
                    if (_disposed) return L["Error:Generic"];
                    if (_models.Any(existing => existing != model && string.Equals(existing.ModelName, saved.ModelName, StringComparison.OrdinalIgnoreCase)))
                        return L["Settings:DuplicateModelId"];
                    var index = model is null ? -1 : _models.IndexOf(model);
                    if (index < 0) _models.Add(saved); else _models[index] = saved;
                    if (_defaultModel == model?.ModelName) _defaultModel = saved.Kind == AIModelKind.Chat ? saved.ModelName : null;
                    await InvokeAsync(StateHasChanged);
                    return null;
                })
            }, new DialogOptions { FullWidth = true, MaxWidth = MaxWidth.Medium, CloseButton = true });
    }
    private string ModelSummary(AIModelConfiguration model)
    {
        var capacity = model.ContextWindow is { } tokens ? $"{tokens:N0}" : model.Kind == AIModelKind.Chat
            ? L["Settings:DefaultContext", ChatContextCapacity.DefaultTokens] : L["Settings:Unknown"];
        var capabilities = new List<string>();
        if (model.SupportsImage == true) capabilities.Add(L["Settings:ImageInput"]);
        if (model.SupportsDocuments == true) capabilities.Add(L["Settings:NativeDocuments"]);
        if (model.SupportsTools == true) capabilities.Add(L["Settings:FunctionTools"]);
        if (model.SupportsReasoning == true) capabilities.Add(L["Workbench:Reasoning"]);
        return capacity + " · " + (capabilities.Count > 0 ? string.Join(" · ", capabilities)
            : L[model.SupportsImage == false && model.SupportsDocuments == false && model.SupportsTools == false && model.SupportsReasoning == false
                ? "Settings:TextOnly" : "Settings:CapabilitiesUnknown"]);
    }
    public void Dispose() { if (_disposed) return; _disposed = true; _lifetime.Cancel(); _lifetime.Dispose(); _key = null; _discoveryConnection = null; }
}
