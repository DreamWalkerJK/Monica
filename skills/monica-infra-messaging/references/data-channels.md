# DataChannel

Use DataChannel for a named pipeline whose two ends and middleware are owned by one running host. The Web host must call `app.UseMonica()` and `app.MapMonica()` after building the app.

`monica.AddDataChannel().UseSetup<TSetup>()` needs an ASP.NET Core web host and a singleton `IDataChannelSetup`. The setup calls `IDataChannelRegistrar.Add` once per pipeline. Every pipeline has a unique ID and an outer endpoint; the inner endpoint defaults to `DefaultChannelEndpoint`. Registration closes after startup materialization. Runtime callers inject `IDataChannelManager`, fetch a host-owned channel, and call its send method.

```csharp
public sealed class OrderChannelSetup : IDataChannelSetup
{
    public void Setup(IDataChannelRegistrar channels) => channels.Add(
        "orders", pipeline => pipeline.SetOuterEndpoint(new KafkaOptions(ConnectionDirection.Output)
        {
            BootstrapServers = "localhost:9092",
            Topic = "orders",
            SecurityProtocol = SecurityProtocol.Plaintext
        }));
}

builder.AddMonica(monica => monica.AddDataChannel().UseSetup<OrderChannelSetup>());

var channel = channels.Fetch("orders")
    ?? throw new InvalidOperationException("orders channel is not registered");
await channel.SendDataFromInnerAsync(message);
```

The example needs `Monica.DataChannel.Providers.Kafka`, `Monica.DataChannel.Abstractions.Communication`, and `Confluent.Kafka`. The broker address and plaintext transport are illustrative for a local broker: `KafkaOptions` otherwise defaults to SASL plaintext with SCRAM-SHA-512, which requires credentials and broker support. `SendDataFromInnerAsync` enters the pipeline at its inner end and reaches the configured outer endpoint; `SendDataFromOuterAsync` travels the opposite direction. If no inner endpoint is set, it defaults to `DefaultChannelEndpoint`. Add DI-resolved middleware with `AddPipeMiddleware<T>()`, or use `groupId` on `IDataChannelRegistrar.Add(...)` and `IDataChannelManager.FetchGroup(...)` to organize related pipelines. After startup, inject `IDataChannelManager` and fetch an existing channel; the setup registrar is no longer mutable.

Duplicate IDs, late registration, or missing outer endpoints throw during setup/materialization. If sending fails, first check the channel ID and endpoint direction, then the provider's connectivity and the DataChannel exception diagnostics for that host. `ModuleDataChannelOption.RecentExceptionToKeep` defaults to 10 per channel. Initialization failures mark the channel unavailable; a successful `ReInitialize` clears that state. TCP, UDP, and serial endpoints close their previous resources before reinitializing and await owned receivers during shutdown.

## Raw transport pipelines

TCP, UDP, and serial providers accept `byte[]`, `Memory<byte>`, `ReadOnlyMemory<byte>`, or a UTF-8 `string`. They preserve byte payloads and reject other payload types. Their input is a raw stream chunk or UDP datagram: framing, circuit routing, persistence, retries, and protocol acknowledgements belong to the application. A successful send means the local transport write completed; it does not prove that the remote application received the message. Failed or canceled writes may have transmitted some bytes, so retry policy must account for duplicates. Disconnected TCP sends and TCP server sends with no matching client fail instead of reporting success. The framework never recursively retries an application send.

| Provider options | Required configuration and behavior |
| --- | --- |
| `TcpClientOptions` | Set `IsClient = true`, a unique `ClientAddress.Key`, and `ConnectedExtend.Address` containing host and port. `IsMainConnected = true` enables input from a primary connection. Each client reconnects its own configured peer with a two-second delay; one circuit disconnecting does not change another circuit's receive role. The application handles durable delivery retry. |
| `TcpServerOptions` | Set `IsServer = true` and `ServerAddress` containing a unique listener name plus local IP address and port. `LocalEndpoint` exposes the bound address, including an ephemeral port. A send targets only this listener's accepted connections; optional `ConnectionName` context metadata narrows it to one connection. Existing primary/standby selection groups accepted peers by network prefix. |
| `UdpOptions` | `Address` and `Port` are the local IP bind address and port. Output directions additionally require `RemoteAddress` and `RemotePort`. `LocalEndpoint` exposes the bound address. One pipeline owns each listening socket; dispatch multiple logical circuits after receipt. |
| `SerialPortOptions` | `PortName` selects hardware. Defaults are 9600 baud, 8 data bits, no parity, one stop bit, no handshake, and DTR/RTS disabled. Set hardware properties explicitly for the device. A port failure remains observable until reinitialization. |

Pass cancellation with `await channel.SendDataFromInnerAsync(bytes, cancellationToken)`. Custom inner endpoints override `Task ReceiveDataAsync(ChannelDataContext data)` and pass `data.CancellationToken` to their asynchronous work. Do not use `async void ReceiveData`: that returns before delivery completes and loses pipeline failure propagation. Raw transport receivers await each pipeline callback, making failures observable before handling the next chunk. Shutdown stops the outer receiver before disposing the inner endpoint, so awaited inner work can finish through its canceled receive token. Implement `ITransientChannelComponent` on a stateless inner endpoint when its collaborators need a per-delivery DI scope; keep stream-framing state in a host-owned service keyed by connection. TCP input metadata includes a `ConnectionEpoch` GUID that changes with every socket connection; reset any incomplete frame when it changes so a new stream cannot complete a previous session's partial frame.

Serial setup uses `Monica.DataChannel.Providers.Serial`:

```csharp
channels.Add("messages", pipeline => pipeline
    .SetOuterEndpoint(new SerialPortOptions { PortName = "COM1", BaudRate = 9600 })
    .SetInnerEndpoint<MessageReceiveEndpoint>());
```

The package marks DataChannel as Labs. These transports do not provide a durable queue or a receipt protocol. Source and checks: `Monica.DataChannel/Modules/ModuleDataChannel.cs`, `Monica.DataChannel/Abstractions/IDataChannelRegistrar.cs` and `IDataChannelManager.cs`, `Monica.DataChannel/DataChannel.cs`, the `Providers/TCP`, `Providers/UDP`, and `Providers/Serial` implementations, and `tests/Test.Monica.DataChannel/Providers/TransportEndpointTests.cs`.
