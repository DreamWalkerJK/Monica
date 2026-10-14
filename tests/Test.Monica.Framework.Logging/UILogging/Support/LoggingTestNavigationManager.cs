using Microsoft.AspNetCore.Components;

namespace Test.Monica.Framework.Logging.UILogging.Support;

/// <summary>Provides one deterministic navigation boundary per actual scenario DI scope.</summary>
internal sealed class LoggingTestNavigationManager : NavigationManager
{
    public LoggingTestNavigationManager(string initialRelativeUri = "logging-monitor", string baseUri = "https://localhost/monica/")
    {
        Initialize(baseUri, new Uri(new Uri(baseUri), initialRelativeUri).AbsoluteUri);
    }

    protected override void NavigateToCore(string uri, bool forceLoad) => SetLocation(uri);

    protected override void NavigateToCore(string uri, NavigationOptions options) => SetLocation(uri);

    private void SetLocation(string uri)
    {
        Uri = ToAbsoluteUri(uri).AbsoluteUri;
        NotifyLocationChanged(isInterceptedLink: false);
    }
}
