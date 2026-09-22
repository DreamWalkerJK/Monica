using System.Net;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Monica.Core.Modularity.Models;
using Monica.Framework.UI.UILogging.Models;
using Monica.Testing.Hosting;
using Xunit;

namespace Test.Monica.Framework.UI.Logging;

/// <summary>
/// Verifies the UI download contract through the real module graph and in-memory HTTP server.
/// </summary>
public sealed class ModuleLoggingUITests
{
    private const string DOWNLOAD_NAME = "日志 %20 #.log";
    private const string RELATIVE_LOG_PATH = "archive/" + DOWNLOAD_NAME;
    private static readonly string[] BufferLines = ["[INF] 第一行 — café", "[ERR] literal %20 and # preserved"];
    private static readonly byte[] FileBytes = Encoding.UTF8.GetBytes("\uFEFF日志文件\r\n100% complete\nlast line");

    [Theory]
    [InlineData(false, null, false)]
    [InlineData(true, null, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    public async Task Downloads_WhenMinimalApiConfigurationChanges_ShouldRemainAvailableAsUiEndpoints(
        bool hostDefault,
        bool? moduleOverride,
        bool expectListApi)
    {
        using var factory = new LoggingUITestApplicationFactory(hostDefault, moduleOverride);
        await WriteLogFileAsync(factory.LogDirectory);
        await using var application = await factory.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        application.Services.GetRequiredService<ScreenLogBuffer>().Reset(BufferLines);
        using var client = CreateClient(application);

        using var exportResponse = await client.GetAsync(
            "/logging-ui/current/export", TestContext.Current.CancellationToken);
        exportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        exportResponse.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");
        var exportDisposition = exportResponse.Content.Headers.ContentDisposition;
        exportDisposition.Should().NotBeNull();
        exportDisposition!.DispositionType.Should().Be("attachment");
        exportDisposition.FileNameStar.Should().MatchRegex(@"^temp-logs-\d{14}\.log$");
        var exportBytes = await exportResponse.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        var timestampLineEnd = Array.IndexOf(exportBytes, (byte)'\n') + 1;
        timestampLineEnd.Should().BeGreaterThan(0);
        Encoding.UTF8.GetString(exportBytes, 0, timestampLineEnd).Should().StartWith("# Exported at ");
        // The first line contains wall-clock time; the remaining exported bytes are deterministic.
        var expectedExportBody = Encoding.UTF8.GetBytes(
            $"# Total Stored Lines: {BufferLines.Length}{Environment.NewLine}{Environment.NewLine}" +
            string.Join(Environment.NewLine, BufferLines) + Environment.NewLine);
        exportBytes[timestampLineEnd..].Should().Equal(expectedExportBody);

        using var fileResponse = await client.GetAsync(
            DownloadPath(), TestContext.Current.CancellationToken);
        fileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        fileResponse.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");
        var fileDisposition = fileResponse.Content.Headers.ContentDisposition;
        fileDisposition.Should().NotBeNull();
        fileDisposition!.DispositionType.Should().Be("attachment");
        fileDisposition.FileNameStar.Should().Be(DOWNLOAD_NAME);
        (await fileResponse.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken))
            .Should().Equal(FileBytes);

        var endpoints = GetLoggingEndpoints(application);
        var downloadEndpoints = endpoints.Where(endpoint =>
            endpoint.RoutePattern.RawText is "/logging-ui/current/export" or "/logging-ui/files/{*filePath}")
            .ToArray();
        downloadEndpoints.Should().HaveCount(2);
        foreach (var endpoint in downloadEndpoints)
        {
            endpoint.Metadata.GetMetadata<MonicaEndpointMetadata>()?.Kind.Should().Be(MonicaEndpointKind.Ui);
            endpoint.Metadata.GetMetadata<MonicaMinimalApiMetadata>().Should().BeNull();
        }

        var listEndpoint = endpoints.SingleOrDefault(endpoint => endpoint.RoutePattern.RawText == "/logging-ui/files");
        using var listResponse = await client.GetAsync("/logging-ui/files", TestContext.Current.CancellationToken);
        if (expectListApi)
        {
            listEndpoint.Should().NotBeNull();
            listEndpoint!.Metadata.GetMetadata<MonicaMinimalApiMetadata>().Should().NotBeNull();
            listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        else
        {
            listEndpoint.Should().BeNull();
            listResponse.IsSuccessStatusCode.Should().BeFalse();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Downloads_WhenRequestArrivesOnAnotherPort_ShouldReturnNotFound(bool downloadFile)
    {
        using var factory = new LoggingUITestApplicationFactory(endpointPort: 31447);
        await WriteLogFileAsync(factory.LogDirectory);
        await using var application = await factory.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        using var client = CreateClient(application);

        // TestServer uses local port zero, so this request cannot pass the configured Monica port guard.
        using var response = await client.GetAsync(
            downloadFile ? DownloadPath() : "/logging-ui/current/export",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentDisposition.Should().BeNull();
        (await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();
    }

    [Fact]
    public async Task Download_WhenFileDoesNotExist_ShouldReturnJsonFailureWithoutAnAttachment()
    {
        using var factory = new LoggingUITestApplicationFactory();
        await using var application = await factory.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        using var client = CreateClient(application);

        using var response = await client.GetAsync(
            "/logging-ui/files/missing.log", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        response.Content.Headers.ContentDisposition.Should().BeNull();
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        body.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    private static HttpClient CreateClient(MonicaTestApplication application) =>
        ((TestServer)application.Services.GetRequiredService<IServer>()).CreateClient();

    private static RouteEndpoint[] GetLoggingEndpoints(MonicaTestApplication application) =>
        application.Services.GetServices<EndpointDataSource>()
            .SelectMany(static source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(static endpoint => endpoint.RoutePattern.RawText?.StartsWith("/logging-ui/", StringComparison.Ordinal) == true)
            .ToArray();

    private static string DownloadPath() =>
        "/logging-ui/files/" + string.Join('/', RELATIVE_LOG_PATH.Split('/').Select(Uri.EscapeDataString));

    private static Task WriteLogFileAsync(string logDirectory)
    {
        var path = Path.Combine(logDirectory, RELATIVE_LOG_PATH);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return File.WriteAllBytesAsync(path, FileBytes, TestContext.Current.CancellationToken);
    }
}
