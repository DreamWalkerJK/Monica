using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using ModelContextProtocol.Client;
using Monica.AI.AgentCapabilities.Models;
using Monica.AI.Facades;
using Monica.AI.Mcp.Facades;
using Monica.AI.Mcp.Models;
using Monica.AI.UI.Localization;
using Monica.AI.UI.Shared.Support;
using Monica.AI.UI.UIAgentCapabilities.State;
using Monica.AI.UI.UIMcp.Components;
using Monica.UI.Icons;
using MudBlazor;

namespace Monica.AI.UI.Pages;

/// <summary>Inspects and configures the host's registered skill and MCP catalogs.</summary>
public partial class AgentCapabilityManagePage : IAsyncDisposable
{
    /// <summary>The routed destination for skill administration.</summary>
    public const string PAGE_URL = "/ai/capabilities";

    /// <summary>
    /// Gets or sets the catalog shown by this page. The default is skills.
    /// The value is captured before the initial refresh; route owners create a new page
    /// for each catalog so its work is canceled and drained when that route retires.
    /// </summary>
    [Parameter] public AgentCapabilityKind CatalogKind { get; set; } = AgentCapabilityKind.Skill;

    [Inject] private AgentCapabilityFacade CapabilityFacade { get; set; } = null!;
    [Inject] private McpFacade McpFacade { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IStringLocalizer<AIResource> L { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private readonly AIAdministrationOperationLifetime _ui = new();
    private AgentCapabilityAdministrationState _state = null!;
    private AIAdministrationDialogSession _dialogs = null!;
    private ElementReference _pageElement;
    private readonly string _focusVisit = "capability-" + Guid.NewGuid().ToString("N");
    private long _focusVersion;
    private FocusIntent? _focusIntent;
    private Task<IJSObjectReference>? _focusModuleImport;
    private bool _catalogOpen;
    private bool _clipboardBusy;

    private bool Busy => _state?.IsBusy == true;
    private string BusyAttribute => Busy ? "true" : "false";
    private string PageTitleText => L[CatalogKind == AgentCapabilityKind.Mcp ? "Capabilities:Mcp:PageTitle" : "Capabilities:Skill:PageTitle"];
    private string PageSubtitleText => L[CatalogKind == AgentCapabilityKind.Mcp ? "Capabilities:Mcp:PageSubtitle" : "Capabilities:Skill:PageSubtitle"];
    private string ResultCount => L["Capabilities:Administration:ResultCount", _state.VisibleEntries.Count, _state.CurrentEntries.Count];
    private string EffectiveFilter => _state.EnabledFilter switch { true => "available", false => "unavailable", _ => "all" };
    private string SourceFilter => _state.ActiveKind == AgentCapabilityKind.Skill
        ? _state.SkillSourceFilter?.ToString() ?? "all" : _state.McpSourceFilter?.ToString() ?? "all";
    private bool HasFilters => !string.IsNullOrWhiteSpace(_state.Query) || _state.EnabledFilter is not null
        || (_state.ActiveKind == AgentCapabilityKind.Skill ? _state.SkillSourceFilter is not null : _state.McpSourceFilter is not null);

    protected override async Task OnInitializedAsync()
    {
        _state = new(CapabilityFacade, McpFacade, L);
        _state.SetActiveKind(CatalogKind);
        _dialogs = new(DialogService, action => InvokeAsync(action));
        await _state.RefreshAsync();
        _catalogOpen = _state.SelectedEntry is null;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusIntent is not { } intent || _ui.IsRetired) return;
        _focusIntent = null;
        try
        {
            await _ui.RunAsync(async token =>
            {
                token.ThrowIfCancellationRequested();
                // Observe the actual resource-producing task even if this visit retires during import.
                _focusModuleImport ??= JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Monica.AI.UI/UIAgentCapabilities/capability-picker.js").AsTask();
                var module = await _focusModuleImport;
                if (_ui.IsRetired || intent.Version != _focusVersion || intent.Key != (_state.SelectedEntry?.Key ?? string.Empty)) return;
                await module.InvokeVoidAsync("reveal", _pageElement, _focusVisit, intent.Version, intent.Kind, intent.Key);
            });
        }
        catch (OperationCanceledException) when (_ui.IsRetired) { }
        catch (JSDisconnectedException) { }
    }

    private void CancelFocusIntent() { _focusVersion++; _focusIntent = null; }
    private void SetQuery(string? value) { CancelFocusIntent(); _state.SetQuery(value); }
    private void ClearFilters() { CancelFocusIntent(); _state.ClearFilters(); }

    private async Task RefreshAsync()
    {
        if (Busy || _ui.IsRetired) return;
        CancelFocusIntent();
        await _state.RefreshAsync();
        if (_ui.IsRetired) return;
        if (_state.SelectedEntry is null) _catalogOpen = true;
        await InvokeAsync(StateHasChanged);
    }

    private void SetEffectiveFilter(string value)
    {
        CancelFocusIntent();
        _state.SetEnabledFilter(value switch { "available" => true, "unavailable" => false, _ => null });
    }

    private void SetSourceFilter(string value)
    {
        CancelFocusIntent();
        if (_state.ActiveKind == AgentCapabilityKind.Skill)
            _state.SetSkillSourceFilter(Enum.TryParse<AgentCapabilitySourceKind>(value, out var skillSource) ? skillSource : null);
        else
            _state.SetMcpSourceFilter(Enum.TryParse<McpCatalogSourceKind>(value, out var mcpSource) ? mcpSource : null);
    }

    private void ToggleCatalog()
    {
        if (_ui.IsRetired) return;
        CancelFocusIntent();
        _catalogOpen = !_catalogOpen;
        if (_catalogOpen) _focusIntent = new(_focusVersion, "catalog", _state.SelectedEntry?.Key ?? string.Empty);
    }

    private void SelectEntry(AgentCapabilityEntryInfo entry)
    {
        if (_ui.IsRetired) return;
        _state.SelectEntry(entry);
        if (_state.SelectedEntry?.Key != entry.Key) return;
        CancelFocusIntent();
        _catalogOpen = false;
        // Reveal the selected dossier in either layout without changing the inventory's local scroll.
        _focusIntent = new(_focusVersion, "detail", entry.Key);
    }

    private Task SetCatalogEnabledAsync(bool enabled) => RunMutationAsync(() => _state.SetCatalogEnabledAsync(_state.ActiveKind, enabled));
    private Task SetEntryEnabledAsync(AgentCapabilityEntryInfo entry, bool enabled) => RunMutationAsync(() => _state.SetEntryEnabledAsync(entry, enabled));
    private Task SetSkillMcpServerEnabledAsync(AgentCapabilityEntryInfo entry, bool enabled) => RunMutationAsync(() => _state.SetSkillMcpServerEnabledAsync(entry, enabled));

    private async Task RunMutationAsync(Func<Task<string?>> mutate)
    {
        if (Busy || _ui.IsRetired) return;
        var diagnostic = await mutate();
        if (_ui.IsRetired || diagnostic is not null) return;
        Snackbar.Add(L["Common:Success"], Severity.Success);
    }

    private Task ShowAddMcpProfileDialogAsync() => ShowMcpProfileDialogAsync(null);
    private Task ShowEditMcpProfileDialogAsync(AgentCapabilityEntryInfo entry) => entry is { IsUserManaged: true, McpExternalProfile: not null }
        ? ShowMcpProfileDialogAsync(entry) : Task.CompletedTask;

    private async Task ShowMcpProfileDialogAsync(AgentCapabilityEntryInfo? entry)
    {
        if (Busy || _ui.IsRetired) return;
        var capture = _state.CaptureExternalProfile(entry);
        var parameters = new DialogParameters
        {
            [nameof(ExternalMcpProfileDialog.Profile)] = capture.Profile,
            [nameof(ExternalMcpProfileDialog.IsEdit)] = entry is not null,
            [nameof(ExternalMcpProfileDialog.SubmitText)] = L["Common:Actions:Save"].Value,
            [nameof(ExternalMcpProfileDialog.OnSave)] = (Func<ExternalMcpClientProfile, CancellationToken, Task<string?>>)(async (profile, token) =>
            {
                var save = _state.SaveExternalProfileAsync(capture, profile, token);
                if (!_ui.IsRetired) await InvokeAsync(StateHasChanged);
                var diagnostic = await save;
                if (!_ui.IsRetired) await InvokeAsync(StateHasChanged);
                return diagnostic;
            })
        };
        var result = await _dialogs.ShowAsync<ExternalMcpProfileDialog>(
            L[entry is null ? "Capabilities:Mcp:Dialog:AddTitle" : "Capabilities:Mcp:Dialog:EditTitle"], parameters);
        if (_ui.IsRetired || result is not { Canceled: false }) return;
        Snackbar.Add(L["Common:Success"], Severity.Success);
    }

    private async Task DeleteMcpProfileAsync(AgentCapabilityEntryInfo entry)
    {
        if (Busy || !entry.IsUserManaged || _ui.IsRetired) return;
        if (await _dialogs.ConfirmAsync(L["Common:Confirm"], L["Capabilities:Mcp:DeleteConfirm", entry.Name], L["Common:Delete"])
            && !_ui.IsRetired) await RunMutationAsync(() => _state.DeleteExternalProfileAsync(entry));
    }

    private async Task TestMcpAsync(AgentCapabilityEntryInfo entry)
    {
        if (Busy || _ui.IsRetired) return;
        await _state.TestMcpAsync(entry);
        if (_ui.IsRetired || _state.LastProbe is not { } probe || probe.EntryKey != entry.Key) return;
        if (probe.Result is { } result)
            Snackbar.Add(FormatConnectivityMessage(result), result.Success ? Severity.Success : Severity.Error);
    }

    private static string GetEntryIcon(AgentCapabilityEntryInfo entry) => entry.Kind == AgentCapabilityKind.Skill
        ? MoIcons.Sparkle : MoIcons.Workflow;

    private static Color GetStatusColor(AgentCapabilityEntryInfo entry)
    {
        if (entry.IsEnabled) return Color.Success;
        if (!entry.IsCatalogEnabled || !entry.IsEntryEnabled || !entry.IsBuiltInEnabled
            || entry.DisabledReason is McpCapabilityMessageCode.DisabledReason.SkillMcpExposureDisabled
                or McpCapabilityMessageCode.DisabledReason.NotAgentTool or McpCapabilityMessageCode.DisabledReason.SkillServerNotAgentTool)
            return Color.Default;
        return Color.Warning;
    }

    private string GetStatusText(AgentCapabilityEntryInfo entry) => L[entry.IsEnabled
        ? "Capabilities:Administration:Available" : "Capabilities:Administration:Unavailable"];
    private string BooleanText(bool value) => L[value ? "Capabilities:Status:Enabled" : "Capabilities:Status:Disabled"];
    private string GetSourceText(AgentCapabilityEntryInfo entry) => entry.McpSourceKind is { } mcpSource
        ? GetMcpSourceKindText(mcpSource) : L[entry.SourceKind == AgentCapabilitySourceKind.ExternalFile
            ? "Capabilities:Skill:External" : "Capabilities:Administration:CodeDefined"];

    private string GetMcpSourceKindText(McpCatalogSourceKind source) => L[source switch
    {
        McpCatalogSourceKind.LocalServer => "Capabilities:Mcp:Source:LocalServer",
        McpCatalogSourceKind.SkillServer => "Capabilities:Mcp:Source:SkillServer",
        McpCatalogSourceKind.ExternalClient => "Capabilities:Mcp:Source:ExternalClient",
        _ => "Capabilities:Administration:CodeDefined"
    }];

    private string FormatDisabledReason(string reason)
    {
        if (SkillCapabilityMessageCode.DisabledReason.TryParseRequiredModulesMissing(reason, out var missingModules))
            return L["Capabilities:Skill:DisabledReason:RequiredModulesMissing", missingModules];
        return reason switch
        {
            SkillCapabilityMessageCode.DisabledReason.ImplementationDisabled => L["Capabilities:Skill:DisabledReason:ImplementationDisabled"],
            SkillCapabilityMessageCode.DisabledReason.ReadOnlyFileAccessNoRoots => L["Capabilities:Skill:DisabledReason:ReadOnlyFileAccessNoRoots"],
            SkillCapabilityMessageCode.DisabledReason.CatalogDisabled => L["Capabilities:Skill:DisabledReason:CatalogDisabled"],
            SkillCapabilityMessageCode.DisabledReason.EntryDisabled => L["Capabilities:Skill:DisabledReason:EntryDisabled"],
            McpCapabilityMessageCode.DisabledReason.SkillMcpExposureDisabled => L["Capabilities:Mcp:DisabledReason:SkillMcpExposureDisabled"],
            McpCapabilityMessageCode.DisabledReason.SkillServerNotAgentTool => L["Capabilities:Mcp:DisabledReason:SkillServerNotAgentTool"],
            McpCapabilityMessageCode.DisabledReason.NotAgentTool => L["Capabilities:Mcp:DisabledReason:NotAgentTool"],
            McpCapabilityMessageCode.DisabledReason.CatalogDisabled => L["Capabilities:Mcp:DisabledReason:CatalogDisabled"],
            McpCapabilityMessageCode.DisabledReason.EntryDisabled => L["Capabilities:Mcp:DisabledReason:EntryDisabled"],
            _ => reason
        };
    }

    private string FormatConnectivityMessage(McpConnectivityTestResult result) => result.Message switch
    {
        McpCapabilityMessageCode.Connectivity.ConnectedToEndpoint => L["Capabilities:Mcp:Connectivity:ConnectedToEndpoint", result.ToolCount],
        McpCapabilityMessageCode.Connectivity.ConnectedAndListedTools => L["Capabilities:Mcp:Connectivity:ConnectedAndListedTools", result.ToolCount],
        McpCapabilityMessageCode.Connectivity.LocalHttpDisplayUrlMissing => L["Capabilities:Mcp:Connectivity:LocalHttpDisplayUrlMissing", result.ToolCount],
        McpCapabilityMessageCode.Connectivity.LocalStdioInProcessReady => L["Capabilities:Mcp:Connectivity:LocalStdioInProcessReady", result.ToolCount],
        _ => result.Message
    };

    private static string SanitizeDiagnostic(AgentCapabilityEntryInfo entry, string diagnostic)
    {
        foreach (var value in (entry.McpExternalProfile?.Headers.Values.AsEnumerable() ?? []).Where(value => !string.IsNullOrEmpty(value)).OrderByDescending(value => value.Length))
            diagnostic = diagnostic.Replace(value, "[redacted]", StringComparison.Ordinal);
        return diagnostic;
    }

    private static bool CanCopyMcpConfig(AgentCapabilityEntryInfo entry) => entry.Kind == AgentCapabilityKind.Mcp
        && (entry.McpExternalProfile is not null || entry.McpTransportKind == McpServerTransportKind.Http
            && (!string.IsNullOrWhiteSpace(entry.McpDisplayUrl) || !string.IsNullOrWhiteSpace(entry.McpEndpointPath)));
    private static bool CanExportSkillPack(AgentCapabilityEntryInfo entry) => entry.Kind == AgentCapabilityKind.Mcp
        && entry.McpExternalProfile is null && entry.McpTransportKind == McpServerTransportKind.Http
        && !string.IsNullOrWhiteSpace(entry.McpEndpointPath);

    private Task ShowSkillPackExportDialogAsync(AgentCapabilityEntryInfo entry) => !CanExportSkillPack(entry) || Busy || _ui.IsRetired
        ? Task.CompletedTask : _dialogs.ShowAsync<SkillPackExportDialog>(L["Capabilities:Mcp:ExportSkill:Title"], new()
        {
            [nameof(SkillPackExportDialog.ServerName)] = entry.Name,
            [nameof(SkillPackExportDialog.DefaultEndpoint)] = ResolveMcpEndpointUrl(entry),
            [nameof(SkillPackExportDialog.ToolCount)] = entry.Tools.Count
        });

    private string ResolveMcpEndpointUrl(AgentCapabilityEntryInfo entry) => entry.McpExternalProfile?.Endpoint
        ?? (!string.IsNullOrWhiteSpace(entry.McpDisplayUrl) ? entry.McpDisplayUrl
            : new Uri(NavigationManager.BaseUri).GetLeftPart(UriPartial.Authority).TrimEnd('/')
              + "/" + entry.McpEndpointPath!.TrimStart('/'));

    private async Task CopyMcpConfigAsync(AgentCapabilityEntryInfo entry)
    {
        if (Busy || !CanCopyMcpConfig(entry) || _clipboardBusy || _ui.IsRetired) return;
        _clipboardBusy = true;
        try
        {
            await _ui.RunAsync(async token =>
            {
                var endpoint = ResolveMcpEndpointUrl(entry);
                if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
                {
                    _ui.TryApply(() => Snackbar.Add(L["Capabilities:Mcp:CopyConfigFailed"], Severity.Error));
                    return;
                }
                if (entry.McpExternalProfile?.Headers.Count > 0 && !await _dialogs.ConfirmAsync(L["Common:Confirm"],
                        L["Capabilities:Administration:CopyHeadersConfirm", entry.Name], L["Capabilities:Administration:CopyIncludingHeaders"])) return;
                token.ThrowIfCancellationRequested();
                var server = new Dictionary<string, object?>
                {
                    ["type"] = entry.McpExternalProfile?.TransportMode == HttpTransportMode.Sse ? "sse" : "http",
                    ["url"] = endpoint
                };
                if (entry.McpExternalProfile?.Headers.Count > 0) server["headers"] = entry.McpExternalProfile.Headers;
                var payload = new Dictionary<string, object?> { ["mcpServers"] = new Dictionary<string, object?> { [entry.Name] = server } };
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
                var copied = await JSRuntime.InvokeAsync<bool>("MoClipboard.copyText", token, json);
                token.ThrowIfCancellationRequested();
                _ui.TryApply(() => Snackbar.Add(L[copied ? "Capabilities:Mcp:CopyConfigSuccess" : "Capabilities:Mcp:CopyConfigFailed"], copied ? Severity.Success : Severity.Error));
            });
        }
        catch (OperationCanceledException) when (_ui.IsRetired) { }
        catch (Exception) { _ui.TryApply(() => Snackbar.Add(L["Capabilities:Mcp:CopyConfigFailed"], Severity.Error)); }
        finally { _ui.TryApply(() => _clipboardBusy = false); }
    }

    /// <summary>Cancels and drains component-owned consumers without disposing borrowed services.</summary>
    public async ValueTask DisposeAsync() => await Task.WhenAll(
        _ui.RetireAsync(DisposeFocusModuleAsync).AsTask(), _dialogs.DisposeAsync().AsTask(), _state.DisposeAsync().AsTask());

    private async ValueTask DisposeFocusModuleAsync()
    {
        if (_focusModuleImport is null) return;
        try { await (await _focusModuleImport).DisposeAsync(); }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) when (_ui.IsRetired) { }
    }

    private sealed record FocusIntent(long Version, string Kind, string Key);
}
