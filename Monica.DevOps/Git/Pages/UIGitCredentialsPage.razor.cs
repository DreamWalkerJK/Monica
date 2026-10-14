using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Monica.DevOps.Git.Facades;
using Monica.DevOps.Git.Models;
using Monica.DevOps.Git.State;
using Monica.DevOps.Git.Support;
using Monica.DevOps.Localization;
using Monica.Modules;

namespace Monica.DevOps.Git.Pages;

/// <summary>Inspects safe credential registrations and resolver precedence without reading repository disk state.</summary>
public partial class UIGitCredentialsPage : IAsyncDisposable
{
    /// <summary>The stable peer route; an optional credential query names an exact accepted registration.</summary>
    public const string PAGE_URL = "/git-credentials";

    [Inject] private GitFacade Facade { get; set; } = default!;
    [Inject] private IOptions<ModuleGitOption> Options { get; set; } = default!;
    [Inject] private IStringLocalizer<GitResource> L { get; set; } = default!;

    /// <summary>Gets or sets a case-insensitive exact registration reference, never an authentication request.</summary>
    [Parameter, SupplyParameterFromQuery(Name = "credential")] public string? Credential { get; set; }

    private GitWorkspaceController? _controller;
    private GitPresentationPrivacy? _privacy;
    private string _query = string.Empty;
    private string? _lastParameter;
    private bool _parametersSeen;
    private bool _focusPending;
    private bool _retired;
    private long _parameterVersion;
    private int _currentPage;
    private ElementReference _targetElement;
    private readonly HashSet<Task> _focusTasks = [];
    private Task? _retirement;

    private GitCredentialInfo? TargetCredential => string.IsNullOrWhiteSpace(Credential) ? null :
        _controller?.Credentials.FirstOrDefault(value => string.Equals(value.Id, Credential, StringComparison.OrdinalIgnoreCase));
    private List<GitCredentialInfo> VisibleCredentials => _controller?.Credentials.Where(value =>
        string.IsNullOrWhiteSpace(_query) || new[] { Safe(value.Id), Safe(value.UserName),
            GitWorkspacePresentation.CredentialTypeText(value.AuthenticationType, L) }
            .Any(text => text.Contains(_query.Trim(), StringComparison.OrdinalIgnoreCase))).ToList() ?? [];

    protected override async Task OnInitializedAsync()
    {
        _privacy = new(Options.Value.CredentialRegistrations);
        _controller = new(Facade, Options);
        _controller.Changed += OnChanged;
        await _controller.RefreshMetadataAsync();
    }

    protected override void OnParametersSet()
    {
        if (_retired || _parametersSeen && string.Equals(_lastParameter, Credential, StringComparison.Ordinal)) return;
        _parametersSeen = true;
        _lastParameter = Credential;
        _parameterVersion++;
        _query = string.Empty;
        _currentPage = 0;
        _focusPending = !string.IsNullOrWhiteSpace(Credential);
        ReconcileTargetPage();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_retired || !_focusPending || TargetCredential is null) return;
        var rows = VisibleCredentials;
        var index = rows.FindIndex(IsTarget);
        if (index < 0 || index / 12 != _currentPage) return;
        _focusPending = false;
        var version = _parameterVersion;
        await ReserveFocusAsync(version);
    }

    private Task ReserveFocusAsync(long version)
    {
        if (_retired || version != _parameterVersion) return Task.CompletedTask;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _focusTasks.Add(completion.Task);
        _ = CompleteFocusAsync();
        return completion.Task;

        async Task CompleteFocusAsync()
        {
            Exception? failure = null;
            try { await FocusTargetAsync(version); }
            catch (Exception exception) { failure = exception; }
            finally { _focusTasks.Remove(completion.Task); }
            if (failure is null) completion.TrySetResult();
            else completion.TrySetException(failure);
        }
    }

    private async Task FocusTargetAsync(long version)
    {
        if (_retired || version != _parameterVersion) return;
        try { await _targetElement.FocusAsync(preventScroll: false); }
        catch (JSDisconnectedException) when (_retired) { }
        catch (ObjectDisposedException) when (_retired) { }
    }

    private async Task RefreshAsync()
    {
        if (_retired || _controller is null) return;
        await _controller.RefreshMetadataAsync();
        if (_retired) return;
        ReconcileTargetPage();
    }

    private void OnChanged()
    {
        if (_retired) return;
        ReconcileTargetPage();
        StateHasChanged();
    }

    private void ReconcileTargetPage()
    {
        var rows = VisibleCredentials;
        if (_focusPending)
        {
            var target = rows.FindIndex(IsTarget);
            if (target >= 0) { _currentPage = target / 12; return; }
        }
        _currentPage = Math.Min(_currentPage, Math.Max(0, (rows.Count - 1) / 12));
    }

    private void SetQuery(string? value)
    {
        if (_retired) return;
        _query = value ?? string.Empty;
        _currentPage = 0;
        // Search is an operator action; it must not revive a prior query focus request.
        _focusPending = false;
    }

    private bool IsTarget(GitCredentialInfo value) =>
        !string.IsNullOrWhiteSpace(Credential) && string.Equals(value.Id, Credential, StringComparison.OrdinalIgnoreCase);
    private string CredentialRowClass(GitCredentialInfo value, int index) => IsTarget(value) ? "credential-query-target" : string.Empty;
    private string Safe(string? value) => _privacy?.Redact(value) ?? string.Empty;

    /// <summary>Stops the owned visit, then drains its admitted metadata reads and native focus work.</summary>
    public ValueTask DisposeAsync()
    {
        if (_retirement is not null) return new(_retirement);
        _retired = true;
        _parameterVersion++;
        if (_controller is not null) _controller.Changed -= OnChanged;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _retirement = completion.Task;
        _ = DrainAsync(completion);
        return new(_retirement);
    }

    private async Task DrainAsync(TaskCompletionSource completion)
    {
        try
        {
            var controller = _controller?.DisposeAsync().AsTask() ?? Task.CompletedTask;
            await Task.WhenAll(_focusTasks.Append(controller).ToArray());
            completion.TrySetResult();
        }
        catch (Exception exception) { completion.TrySetException(exception); }
    }
}
