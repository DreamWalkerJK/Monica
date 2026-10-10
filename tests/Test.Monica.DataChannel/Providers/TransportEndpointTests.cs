using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Monica.Core.Modularity.Abstractions;
using Monica.DataChannel.Abstractions;
using Monica.DataChannel.Abstractions.Communication;
using Monica.DataChannel.Middlewares;
using Monica.DataChannel.Pipeline;
using Monica.DataChannel.Providers.Default;
using Monica.DataChannel.Providers.Serial;
using Monica.DataChannel.Providers.TCP;
using Monica.DataChannel.Providers.TCP.Utils;
using Monica.DataChannel.Providers.UDP;
using Monica.Modules;
using Monica.Testing.Hosting;
using Monica.Tool.Extensions;
using Xunit;

namespace Test.Monica.DataChannel.Providers;

public sealed class TransportEndpointTests
{
    [Fact]
    public async Task Udp_WhenDuplex_ShouldPreserveBytesAndReleaseBoundSocket()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var peer = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var peerAddress = (IPEndPoint)peer.Client.LocalEndPoint!;
        var factory = new ChannelFactory(channels => channels.Add("udp", pipe => pipe
            .SetOuterEndpoint(new UdpOptions(ConnectionDirection.InputAndOutput)
            {
                Address = "127.0.0.1", Port = 0,
                RemoteAddress = "127.0.0.1", RemotePort = peerAddress.Port
            }).SetInnerEndpoint<RecordingEndpoint>()));
        IPEndPoint address;
        await using (var application = await factory.CreateAsync(cancellationToken: deadline.Token))
        {
            var channel = application.Services.GetRequiredService<IDataChannelManager>().Fetch("udp")!;
            byte[] expected = [0, 1, 3, 127, 128, 255];
            await channel.SendDataFromInnerAsync(expected, deadline.Token);
            var sent = await peer.ReceiveAsync(deadline.Token);
            Assert.Equal(expected, sent.Buffer);
            address = sent.RemoteEndPoint;
            await peer.SendAsync(expected, address, deadline.Token);
            var received = await factory.Probe.Messages.Reader.ReadAsync(deadline.Token);
            Assert.Equal(expected, Assert.IsType<byte[]>(received.Data));
            Assert.True(received.CancellationToken.CanBeCanceled);
        }
        using var rebound = new UdpClient(address);
        Assert.Equal(address.Port, ((IPEndPoint)rebound.Client.LocalEndPoint!).Port);
    }

    [Fact]
    public async Task Udp_WhenBindFailsThenReinitialized_ShouldReportFailureAndRecover()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var occupied = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var address = (IPEndPoint)occupied.Client.LocalEndPoint!;
        var factory = new ChannelFactory(channels => channels.Add("udp", pipe => pipe
            .SetOuterEndpoint(new UdpOptions { Address = "127.0.0.1", Port = address.Port })));
        await using var application = await factory.CreateAsync(cancellationToken: deadline.Token);
        var channel = application.Services.GetRequiredService<IDataChannelManager>().Fetch("udp")!;
        Assert.True(channel.Pipe.IsNotAvailable);
        Assert.False(channel.Pipe.IsInitialized);
        Assert.True(channel.Pipe.HasExceptions);
        occupied.Dispose();
        await channel.ReInitialize(deadline.Token);
        Assert.False(channel.Pipe.IsNotAvailable);
        Assert.True(channel.Pipe.IsInitialized);
    }

    [Fact]
    public async Task TcpClient_WhenConnected_ShouldPreserveBinaryPayloadAndAwaitInboundPipeline()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var address = (IPEndPoint)listener.LocalEndpoint;
        var factory = new ChannelFactory(channels => channels.Add("tcp", pipe => pipe
            .SetOuterEndpoint(new TcpClientOptions
            {
                IsClient = true,
                ClientAddress = new("tcp", new ConnectedExtend
                {
                    Address = Tuple.Create("127.0.0.1", address.Port), IsMainConnected = true
                })
            }).SetInnerEndpoint<RecordingEndpoint>()));
        await using var application = await factory.CreateAsync(cancellationToken: deadline.Token);
        using var peer = await listener.AcceptTcpClientAsync(deadline.Token);
        byte[] expected = [1, 0, 128, 255, 3];
        await peer.GetStream().WriteAsync(expected, deadline.Token);
        var input = await factory.Probe.Messages.Reader.ReadAsync(deadline.Token);
        Assert.Equal(expected, Assert.IsType<byte[]>(input.Data));
        var channel = application.Services.GetRequiredService<IDataChannelManager>().Fetch("tcp")!;
        await channel.SendDataFromInnerAsync(expected.AsMemory(), deadline.Token);
        var buffer = new byte[expected.Length];
        await peer.GetStream().ReadExactlyAsync(buffer, deadline.Token);
        Assert.Equal(expected, buffer);

        var originalEpoch = Assert.IsType<Guid>(input.Metadata.GetOrDefault("ConnectionEpoch"));
        await channel.ReInitialize(deadline.Token);
        using var reconnectedPeer = await listener.AcceptTcpClientAsync(deadline.Token);
        await reconnectedPeer.GetStream().WriteAsync(expected, deadline.Token);
        var nextInput = await factory.Probe.Messages.Reader.ReadAsync(deadline.Token);
        Assert.NotEqual(originalEpoch, Assert.IsType<Guid>(nextInput.Metadata.GetOrDefault("ConnectionEpoch")));
    }

    [Fact]
    public async Task TcpClient_WhenDisconnected_ShouldFailInsteadOfAcknowledgingUnsentBytes()
    {
        var factory = new ChannelFactory(channels => channels.Add("tcp", pipe => pipe
            .SetOuterEndpoint(new TcpClientOptions
            {
                IsClient = false,
                ClientAddress = new("tcp", new ConnectedExtend { Address = Tuple.Create("127.0.0.1", 1) })
            })));
        await using var application = await factory.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        var channel = application.Services.GetRequiredService<IDataChannelManager>().Fetch("tcp")!;
        await Assert.ThrowsAsync<IOException>(() => channel.SendDataFromInnerAsync(new byte[] { 1 }, TestContext.Current.CancellationToken));
        Assert.True(channel.Pipe.HasExceptions);
    }

    [Fact]
    public async Task TcpServer_WhenTwoListenersShareHost_ShouldSendOnlyToOwnConnections()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var factory = new ChannelFactory(channels =>
        {
            foreach (var id in new[] { "first", "second" })
                channels.Add(id, pipe => pipe.SetOuterEndpoint(new TcpServerOptions
                {
                    IsServer = true,
                    ServerAddress = new(id, new ConnectedExtend { Address = Tuple.Create("127.0.0.1", 0) })
                }).SetInnerEndpoint<RecordingEndpoint>());
        });
        await using var application = await factory.CreateAsync(cancellationToken: deadline.Token);
        var manager = application.Services.GetRequiredService<IDataChannelManager>();
        var first = manager.Fetch("first")!;
        var second = manager.Fetch("second")!;
        using var peerA = new TcpClient();
        using var peerB = new TcpClient();
        await peerA.ConnectAsync(Assert.IsType<TcpServerEndpoint>(first.Pipe.OuterEndpoint).LocalEndpoint!, deadline.Token);
        await peerB.ConnectAsync(Assert.IsType<TcpServerEndpoint>(second.Pipe.OuterEndpoint).LocalEndpoint!, deadline.Token);
        await peerA.GetStream().WriteAsync(new byte[] { 10 }, deadline.Token);
        await peerB.GetStream().WriteAsync(new byte[] { 20 }, deadline.Token);
        await factory.Probe.Messages.Reader.ReadAsync(deadline.Token);
        await factory.Probe.Messages.Reader.ReadAsync(deadline.Token);
        await first.SendDataFromInnerAsync(new byte[] { 11 }, deadline.Token);
        await second.SendDataFromInnerAsync(new byte[] { 22 }, deadline.Token);
        var fromA = new byte[1];
        var fromB = new byte[1];
        await peerA.GetStream().ReadExactlyAsync(fromA, deadline.Token);
        await peerB.GetStream().ReadExactlyAsync(fromB, deadline.Token);
        Assert.Equal((byte)11, fromA[0]);
        Assert.Equal((byte)22, fromB[0]);
    }

    [Fact]
    public async Task Pipeline_WhenReceiverFailsAfterAwait_ShouldPropagateAndRecordFailure()
    {
        var factory = new ChannelFactory(channels => channels.Add("test", pipe => pipe
            .SetOuterEndpoint(new DefaultEndpointOptions()).SetInnerEndpoint<RecordingEndpoint>()));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Probe.OnReceive = async _ =>
        {
            entered.SetResult();
            await release.Task;
            throw new InvalidOperationException("delivery failed");
        };
        await using var application = await factory.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        var channel = application.Services.GetRequiredService<IDataChannelManager>().Fetch("test")!;
        var delivery = channel.SendDataFromOuterAsync("input", TestContext.Current.CancellationToken);
        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.False(delivery.IsCompleted);
        release.SetResult();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => delivery);
        Assert.Equal("delivery failed", exception.Message);
        Assert.True(channel.Pipe.HasExceptions);
    }

    [Fact]
    public async Task Pipeline_WhenAlreadyCanceled_ShouldNotDeliverPayload()
    {
        var factory = new ChannelFactory(channels => channels.Add("test", pipe => pipe
            .SetOuterEndpoint(new DefaultEndpointOptions()).SetInnerEndpoint<RecordingEndpoint>()));
        await using var application = await factory.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        var channel = application.Services.GetRequiredService<IDataChannelManager>().Fetch("test")!;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => channel.SendDataFromOuterAsync("input", new CancellationToken(true)));
        Assert.False(factory.Probe.Messages.Reader.TryRead(out _));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Pipeline_WhenTransformReplacesContext_ShouldPreserveDeliveryCancellation(bool cancelDuringTransform)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var deliveryCancellation = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
        var transformEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseTransform = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var receiverEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var middleware = new ContextReplacementMiddleware(async context =>
        {
            var replacement = new ChannelDataContext(context.Source, context.Data).CopyMetadata(context);
            context.CancellationToken = CancellationToken.None;
            transformEntered.SetResult();
            await releaseTransform.Task.WaitAsync(deadline.Token);
            return replacement;
        });
        var factory = new ChannelFactory(channels => channels.Add("test", pipe => pipe
            .SetOuterEndpoint(new DefaultEndpointOptions())
            .SetInnerEndpoint<RecordingEndpoint>()
            .AddPipeMiddleware(middleware)));
        factory.Probe.OnReceive = async data =>
        {
            Assert.Equal(deliveryCancellation.Token, data.CancellationToken);
            receiverEntered.SetResult();
            await Task.Delay(Timeout.Infinite, data.CancellationToken);
        };
        await using var application = await factory.CreateAsync(cancellationToken: deadline.Token);
        var channel = application.Services.GetRequiredService<IDataChannelManager>().Fetch("test")!;

        var delivery = channel.SendDataFromOuterAsync("input", deliveryCancellation.Token);
        await transformEntered.Task.WaitAsync(deadline.Token);
        if (cancelDuringTransform) deliveryCancellation.Cancel();
        releaseTransform.SetResult();
        if (!cancelDuringTransform)
        {
            await receiverEntered.Task.WaitAsync(deadline.Token);
            deliveryCancellation.Cancel();
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => delivery.WaitAsync(deadline.Token));
        Assert.Equal(!cancelDuringTransform, receiverEntered.Task.IsCompleted);
        Assert.False(factory.Probe.Messages.Reader.TryRead(out _));
    }

    [Theory]
    [InlineData(0, 8, StopBits.One)]
    [InlineData(9600, 4, StopBits.One)]
    [InlineData(9600, 8, StopBits.None)]
    public void SerialOptions_WhenInvalid_ShouldRejectBeforeOpeningHardware(int baud, int bits, StopBits stopBits)
    {
        var options = new SerialPortOptions { PortName = "COM1", BaudRate = baud, DataBits = bits, StopBits = stopBits };
        Assert.ThrowsAny<ArgumentException>(options.EnrichOrValidate);
    }

    [Fact]
    public async Task TcpClient_WhenAnotherCircuitDisconnects_ShouldKeepReceivingOnIndependentCircuit()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var listenerA = new TcpListener(IPAddress.Loopback, 0);
        using var listenerB = new TcpListener(IPAddress.Loopback, 0);
        listenerA.Start();
        listenerB.Start();
        var factory = new ChannelFactory(channels =>
        {
            foreach (var (id, listener) in new[] { ("first", listenerA), ("second", listenerB) })
                channels.Add(id, pipe => pipe.SetOuterEndpoint(new TcpClientOptions
                {
                    IsClient = true,
                    ClientAddress = new(id, new ConnectedExtend
                    {
                        Address = Tuple.Create("127.0.0.1", ((IPEndPoint)listener.LocalEndpoint).Port),
                        IsMainConnected = true
                    })
                }).SetInnerEndpoint<RecordingEndpoint>());
        });
        await using var application = await factory.CreateAsync(cancellationToken: deadline.Token);
        using var peerA = await listenerA.AcceptTcpClientAsync(deadline.Token);
        using var peerB = await listenerB.AcceptTcpClientAsync(deadline.Token);
        await peerA.GetStream().WriteAsync(new byte[] { 1 }, deadline.Token);
        await peerB.GetStream().WriteAsync(new byte[] { 2 }, deadline.Token);
        await factory.Probe.Messages.Reader.ReadAsync(deadline.Token);
        await factory.Probe.Messages.Reader.ReadAsync(deadline.Token);
        peerA.Dispose();
        var endpointA = Assert.IsType<TcpClientEndpoint>(application.Services.GetRequiredService<IDataChannelManager>().Fetch("first")!.Pipe.OuterEndpoint);
        while (endpointA.IsConnected) await Task.Delay(10, deadline.Token);
        await peerB.GetStream().WriteAsync(new byte[] { 3 }, deadline.Token);
        var received = await factory.Probe.Messages.Reader.ReadAsync(deadline.Token);
        Assert.Equal(new byte[] { 3 }, Assert.IsType<byte[]>(received.Data));
    }

    [Fact]
    public async Task Shutdown_WhenInnerReceiverIsBlocked_ShouldCancelIngressBeforeDisposingInner()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var factory = new ChannelFactory(channels => channels.Add("udp", pipe => pipe
            .SetOuterEndpoint(new UdpOptions { Address = "127.0.0.1", Port = 0 })
            .SetInnerEndpoint<RecordingEndpoint>()));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Probe.OnReceive = async data =>
        {
            entered.SetResult();
            try { await Task.Delay(Timeout.Infinite, data.CancellationToken); }
            finally { finished.SetResult(); }
        };
        factory.Probe.OnDispose = () => finished.Task;
        var application = await factory.CreateAsync(cancellationToken: deadline.Token);
        var channel = application.Services.GetRequiredService<IDataChannelManager>().Fetch("udp")!;
        var endpoint = Assert.IsType<UdpEndpoint>(channel.Pipe.OuterEndpoint).LocalEndpoint!;
        using var peer = new UdpClient();
        await peer.SendAsync(new byte[] { 1 }, endpoint, deadline.Token);
        await entered.Task.WaitAsync(deadline.Token);
        await application.DisposeAsync().AsTask().WaitAsync(deadline.Token);
        Assert.True(finished.Task.IsCompleted);
    }

    public sealed record ChannelPlan(Action<IDataChannelRegistrar> Configure);

    public sealed class ChannelSetup(ChannelPlan plan) : IDataChannelSetup
    {
        public void Setup(IDataChannelRegistrar channels) => plan.Configure(channels);
    }

    public sealed class ReceiveProbe
    {
        public Channel<ChannelDataContext> Messages { get; } = Channel.CreateUnbounded<ChannelDataContext>();
        public Func<ChannelDataContext, Task>? OnReceive { get; set; }
        public Func<Task>? OnDispose { get; set; }
    }

    public sealed class RecordingEndpoint(ReceiveProbe probe) : DefaultChannelEndpoint
    {
        public override Task DisposeAsync(CancellationToken cancellationToken = default) => probe.OnDispose?.Invoke() ?? Task.CompletedTask;
        public override async Task ReceiveDataAsync(ChannelDataContext data)
        {
            if (probe.OnReceive is { } callback) await callback(data);
            await probe.Messages.Writer.WriteAsync(data, data.CancellationToken);
        }
    }

    private sealed class ContextReplacementMiddleware(Func<ChannelDataContext, Task<ChannelDataContext>> transform)
        : PipelineTransformMiddlewareBase
    {
        public override Task<ChannelDataContext> PassAsync(ChannelDataContext context) => transform(context);
    }

    private sealed class ChannelFactory(Action<IDataChannelRegistrar> configure) : MonicaTestApplicationFactory<RecordingEndpoint>
    {
        public ReceiveProbe Probe { get; } = new();
        protected override void ConfigureMonica(IMonicaBuilder builder) => builder.AddDataChannel().UseSetup<ChannelSetup>();
        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddSingleton(new ChannelPlan(configure));
            services.AddSingleton(Probe);
        }
    }
}
