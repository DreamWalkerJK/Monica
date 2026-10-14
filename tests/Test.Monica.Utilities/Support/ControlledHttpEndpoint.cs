using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Test.Monica.Utilities.Support;

/// <summary>
/// Owns a loopback-only endpoint whose held request exposes admission, client cancellation and handler drain.
/// Every scenario gets an OS-assigned port; no external server, polling or timing sleep is involved.
/// </summary>
internal sealed class ControlledHttpEndpoint : IAsyncDisposable
{
    private readonly WebApplication _application;
    private readonly X509Certificate2? _certificate;
    private readonly TaskCompletionSource _release = NewGate();
    private readonly CancellationTokenSource _shutdown;
    private int _requestCount;

    private ControlledHttpEndpoint(WebApplication application, X509Certificate2? certificate)
    {
        _application = application;
        _certificate = certificate;
        _shutdown = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
    }

    public Uri Address { get; private set; } = null!;
    public int RequestCount => Volatile.Read(ref _requestCount);
    public TaskCompletionSource Admitted { get; } = NewGate();
    public TaskCompletionSource Aborted { get; } = NewGate();
    public TaskCompletionSource Drained { get; } = NewGate();

    public static async Task<ControlledHttpEndpoint> StartAsync(bool useHttps = false)
    {
        var certificate = useHttps ? CreateCertificate() : null;
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0, listener =>
        {
            if (certificate is not null)
                listener.UseHttps(certificate);
        }));
        var application = builder.Build();
        var endpoint = new ControlledHttpEndpoint(application, certificate);
        application.MapGet("/complete", () =>
        {
            Interlocked.Increment(ref endpoint._requestCount);
            return Results.NoContent();
        });
        application.MapGet("/held", endpoint.HandleHeldAsync);
        try
        {
            await application.StartAsync(TestContext.Current.CancellationToken);
            endpoint.Address = new Uri(application.Urls.Single());
            return endpoint;
        }
        catch
        {
            await endpoint.DisposeAsync();
            throw;
        }
    }

    public static Task ObserveAsync(Task signal) => signal.WaitAsync(
        TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

    public async Task ObserveCanceledConnectionDrainAsync()
    {
        await ObserveAsync(Aborted.Task);
        await ObserveAsync(Drained.Task);
    }

    private async Task HandleHeldAsync(HttpContext context)
    {
        Interlocked.Increment(ref _requestCount);
        var requestAborted = context.RequestAborted;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            requestAborted, _shutdown.Token);
        Admitted.TrySetResult();
        try
        {
            await _release.Task.WaitAsync(cancellation.Token);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // The client deliberately retires the held request; expose completion through Drained below.
        }
        finally
        {
            // WaitAsync can resume this handler inline from the latest cancellation callback. Disposing
            // an older observation registration here can remove it before the same Cancel call invokes it.
            // Record the captured transport token's state directly, independent of callback ordering.
            if (requestAborted.IsCancellationRequested)
                Aborted.TrySetResult();
            Drained.TrySetResult();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _shutdown.Cancel();
        _release.TrySetResult();
        try
        {
            await _application.StopAsync(CancellationToken.None);
        }
        finally
        {
            await _application.DisposeAsync();
            _certificate?.Dispose();
            _shutdown.Dispose();
        }
    }

    private static X509Certificate2 CreateCertificate()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=127.0.0.1", key, HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        names.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(names.Build());
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new("1.3.6.1.5.5.7.3.1") }, critical: false));
        using var generated = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
        var pfx = generated.Export(X509ContentType.Pfx);
        try
        {
            // Import the generated key for Windows Schannel instead of passing its in-memory key handle.
            // Default key-storage ownership removes the temporary imported key when this certificate is disposed.
            return X509CertificateLoader.LoadPkcs12(pfx, password: null);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pfx);
        }
    }

    private static TaskCompletionSource NewGate() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
