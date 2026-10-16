using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Test.Monica.AI.Support;

/// <summary>An owned local HTTP endpoint for SDK discovery tests; never contacts an external provider.</summary>
internal sealed class LoopbackModelEndpoint : IAsyncDisposable
{
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Func<HttpListenerRequest, CancellationToken, Task<(int Status, string Body)>> _respond;
    private readonly Task _server;
    public string BaseUrl { get; }
    public TaskCompletionSource<string?> Credential { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public LoopbackModelEndpoint(Func<HttpListenerRequest, CancellationToken, Task<(int Status, string Body)>> respond)
    {
        _respond = respond;
        using var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        BaseUrl = $"http://127.0.0.1:{port}/v1";
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        _listener.Start();
        _server = RunAsync();
    }

    private async Task RunAsync()
    {
        try
        {
            while (!_lifetime.IsCancellationRequested)
            {
                var context = await _listener.GetContextAsync().WaitAsync(_lifetime.Token);
                Credential.TrySetResult(context.Request.Headers["Authorization"]);
                var (status, body) = await _respond(context.Request, _lifetime.Token);
                var bytes = Encoding.UTF8.GetBytes(body);
                context.Response.StatusCode = status;
                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes, _lifetime.Token);
                context.Response.Close();
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (HttpListenerException) when (_lifetime.IsCancellationRequested) { }
    }

    public async ValueTask DisposeAsync()
    {
        await _lifetime.CancelAsync();
        _listener.Stop();
        await _server;
        _listener.Close();
        _lifetime.Dispose();
    }
}
