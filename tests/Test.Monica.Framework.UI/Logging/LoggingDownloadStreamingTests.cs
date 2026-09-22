using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Test.Monica.Framework.UI.Logging;

/// <summary>
/// Verifies download framing and file ownership while an active log changes during its response.
/// </summary>
public sealed class LoggingDownloadStreamingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Download_WhenActiveFileChangesAfterHeaders_ShouldCompleteWithoutAnObsoleteContentLength(
        bool truncateFile)
    {
        var initialBytes = Enumerable.Range(0, 512 * 1024).Select(static index => (byte)(index % 251)).ToArray();
        var appendedBytes = new byte[] { 251, 252, 253, 254, 255 };
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var responseStarted = false;
        long? contentLengthAtStart = null;
        var logPath = string.Empty;
        using var factory = new LoggingUITestApplicationFactory(configureRequest: context =>
        {
            // The endpoint has opened the log and selected its response before headers start.
            // Mutating here reproduces live appends and rotation without scheduler timing or sleeps.
            context.Response.OnStarting(async () =>
            {
                responseStarted = true;
                contentLengthAtStart = context.Response.ContentLength;
                await using var writer = new FileStream(
                    logPath,
                    FileMode.Open,
                    FileAccess.Write,
                    FileShare.ReadWrite | FileShare.Delete);
                if (truncateFile)
                {
                    writer.SetLength(0);
                }
                else
                {
                    writer.Seek(0, SeekOrigin.End);
                    await writer.WriteAsync(appendedBytes, TestContext.Current.CancellationToken);
                }
            });
            // Registered before the endpoint's disposal callback, this runs after owned streams are released.
            context.Response.OnCompleted(() =>
            {
                completed.TrySetResult();
                return Task.CompletedTask;
            });
        });
        logPath = Path.Combine(factory.LogDirectory, "active.log");
        await File.WriteAllBytesAsync(logPath, initialBytes, TestContext.Current.CancellationToken);
        await using var application = await factory.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        using var client = ((TestServer)application.Services.GetRequiredService<IServer>()).CreateClient();

        using var response = await client.GetAsync(
            "/logging-ui/files/active.log",
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);
        // HttpContent may synthesize Content-Length after buffering, so observe the actual response headers first.
        var receivedContentLength = response.Content.Headers.ContentLength;
        var downloadedBytes = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseStarted.Should().BeTrue();
        contentLengthAtStart.Should().BeNull();
        receivedContentLength.Should().BeNull();
        response.Content.Headers.ContentDisposition?.FileNameStar.Should().Be("active.log");
        if (truncateFile)
        {
            downloadedBytes.Should().NotBeEmpty();
            downloadedBytes.Length.Should().BeLessThan(initialBytes.Length);
            downloadedBytes.Should().Equal(initialBytes[..downloadedBytes.Length]);
        }
        else
        {
            downloadedBytes.Should().Equal(initialBytes);
        }

        // An exclusive reopen verifies response completion released the download stream's file handle.
        using var exclusive = new FileStream(logPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        exclusive.Length.Should().Be(truncateFile ? 0 : initialBytes.Length + appendedBytes.Length);
    }
}
