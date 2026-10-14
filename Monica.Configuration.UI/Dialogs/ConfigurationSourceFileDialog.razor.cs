using Microsoft.AspNetCore.Components;
using Monica.Configuration.Models;
using Monica.Configuration.UI.State;
using Monica.Configuration.UI.Support;
using MudBlazor;

namespace Monica.Configuration.UI.Dialogs;

/// <summary>Displays the facade's known-schema-redacted JSON file view for an exact source key.</summary>
/// <remarks>Retirement and changed parameters reject late adoption and drain actual tokenless reads.</remarks>
public partial class ConfigurationSourceFileDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    /// <summary>Gets or sets the exact runtime source identity without trimming or normalization.</summary>
    [Parameter, EditorRequired] public string SourceKey { get; set; } = string.Empty;
    private readonly ConfigurationOperationLifetime _operations = new();
    private ConfigurationSourceFileState? _state;
    private string? _requestedSourceKey;
    private ConfigurationSourceFileView? _fileView => _state?.Read.Value;
    private string? _error => _state?.Read.Error;
    private bool _loading => _state?.Read.Loading ?? true;

    protected override void OnInitialized() => _state = new(Facade, L["Common:Errors:LoadFailed"], _operations);

    protected override async Task OnParametersSetAsync()
    {
        if (_operations.IsRetired || string.Equals(_requestedSourceKey, SourceKey, StringComparison.Ordinal)) return;
        var sourceKey = SourceKey;
        _requestedSourceKey = sourceKey;
        try { await _state!.LoadAsync(sourceKey); }
        catch (OperationCanceledException) when (_operations.IsRetired) { }
    }

    private void Close()
    {
        if (!_operations.IsRetired) MudDialog.Close();
    }

    /// <summary>Joins actual file-read completion without disposing borrowed services.</summary>
    public ValueTask DisposeAsync() => _operations.RetireAsync();
}
