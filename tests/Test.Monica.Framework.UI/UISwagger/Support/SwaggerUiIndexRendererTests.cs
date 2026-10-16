using System.Globalization;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Monica.Framework.UI.Localization;
using Monica.Framework.UI.UISwagger.Models;
using Monica.Framework.UI.UISwagger.Support;
using Monica.UI.Theming;
using Swashbuckle.AspNetCore.SwaggerUI;
using Xunit;

namespace Test.Monica.Framework.UI.UISwagger.Support;

public sealed class SwaggerUiIndexRendererTests
{
    [Fact]
    public void Render_WhenHostOwnsTheFactory_ShouldEvaluateItPerRequestAndDisposeEachProducedStreamOnce()
    {
        const string template = "<html><head>%(HeadContent)</head><body>%(ConfigObject)|%(Interceptors)|%(OAuthConfigObject)</body></html>";
        var context = new DefaultHttpContext();
        context.Response.Headers.Vary = "Accept-Encoding";
        var accessor = new TestContextAccessor { HttpContext = context };
        var renderer = CreateRenderer(accessor);
        var originals = new List<TrackedStream>();
        var options = new SwaggerUIOptions
        {
            HeadContent = "<meta name=host-content />",
            IndexStream = () =>
            {
                var stream = new TrackedStream(template);
                originals.Add(stream);
                return stream;
            }
        };
        renderer.Configure(options);

        var first = Read(options.IndexStream());
        context.Request.PathBase = "/another-mount";
        var second = Read(options.IndexStream());

        originals.Should().HaveCount(2).And.AllSatisfy(static stream => stream.DisposalCount.Should().Be(1));
        options.HeadContent.Should().Be("<meta name=host-content />");
        first.Should().Contain("%(HeadContent)").And.Contain("%(ConfigObject)")
            .And.Contain("%(Interceptors)").And.Contain("%(OAuthConfigObject)");
        using var configuration = ReadConfiguration(second);
        configuration.RootElement.GetProperty("storage").GetProperty("moduleUrl").GetString()
            .Should().Be("/another-mount/_content/Monica.UI/js/mo-browser-storage.js");
        context.Response.Headers.Vary.ToString().Split(',').Select(static value => value.Trim())
            .Should().Equal("Accept-Encoding", "Cookie");
    }

    [Fact]
    public void Render_WhenCallerTextContainsMarkup_ShouldPreserveLiteralDataAndKeepTheConfigurationElementSafe()
    {
        const string name = "</script><script>alert('literal')</script> & 中文 %(HeadContent) %(DocumentTitle)";
        var context = new DefaultHttpContext();
        context.Request.PathBase = "/mounted";
        var renderer = CreateRenderer(
            new TestContextAccessor { HttpContext = context },
            [new SwaggerNavigationButton { Name = name, Path = "~/details?x=1&y=2", Description = name }]);

        var html = Read(renderer.Render(static () => new MemoryStream(Encoding.UTF8.GetBytes("<head>%(HeadContent)</head>"))));
        var expanded = html.Replace("%(DocumentTitle)", "Host title", StringComparison.Ordinal)
            .Replace("%(HeadContent)", "<meta name=host-content />", StringComparison.Ordinal);
        using var configuration = ReadConfiguration(expanded);

        html.Should().NotContain(name);
        var link = configuration.RootElement.GetProperty("buttons")[0];
        link.GetProperty("name").GetString().Should().Be(name);
        link.GetProperty("description").GetString().Should().Be(name);
        link.GetProperty("href").GetString().Should().Be("/mounted/details?x=1&y=2");
    }

    [Fact]
    public void Render_WhenNavigationIsEmpty_ShouldRetainNativeTemplateAndProvideSharedThemeAssets()
    {
        var renderer = CreateRenderer(new TestContextAccessor());
        var html = Read(renderer.Render(static () => new MemoryStream(Encoding.UTF8.GetBytes(
            "<html><HEAD><title>%(DocumentTitle)</title></HEAD><body id=host-native-root></body></html>"))));
        using var configuration = ReadConfiguration(html);

        html.Should().Contain("<title>%(DocumentTitle)</title>").And.Contain("id=host-native-root")
            .And.Contain("/_content/Monica.Framework.UI/UISwagger/custom.js")
            .And.Contain("/_content/Monica.Framework.UI/UISwagger/custom.css")
            .And.Contain("/_content/Monica.UI/css/mo-fonts.css");
        configuration.RootElement.GetProperty("buttons").GetArrayLength().Should().Be(0);
        configuration.RootElement.GetProperty("themes").GetArrayLength().Should().Be(ThemeCatalog.GetAvailableThemes().Length);
        configuration.RootElement.GetProperty("storage").GetProperty("key").GetString().Should().Be(ThemePreference.BrowserStorageKey);
    }

    internal static JsonDocument ReadConfiguration(string html)
    {
        var marker = $"<script id=\"{SwaggerUiIndexRenderer.CONFIGURATION_ELEMENT_ID}\" type=\"application/json\">";
        var start = html.IndexOf(marker, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0);
        start += marker.Length;
        var end = html.IndexOf("</script>", start, StringComparison.Ordinal);
        return JsonDocument.Parse(html[start..end]);
    }

    private static SwaggerUiIndexRenderer CreateRenderer(
        IHttpContextAccessor accessor,
        IReadOnlyList<SwaggerNavigationButton>? buttons = null) => new(
        accessor,
        new RequestCultureLocalizer(),
        new ThemePreference(MonicaThemeKind.Default, false),
        buttons ?? []);

    private static string Read(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private sealed class TestContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }

    private sealed class TrackedStream(string text) : MemoryStream(Encoding.UTF8.GetBytes(text))
    {
        internal int DisposalCount { get; private set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposalCount++;
            }

            base.Dispose(disposing);
        }
    }

    private sealed class RequestCultureLocalizer : IStringLocalizer<SwaggerUiResource>
    {
        public LocalizedString this[string name] => new(name, $"{CultureInfo.CurrentUICulture.Name}:{name}", false);
        public LocalizedString this[string name, params object[] arguments] => this[name];
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
