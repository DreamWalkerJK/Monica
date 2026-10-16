using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Monica.Core.Modularity.Abstractions;
using Monica.DataChannel.Abstractions;
using Monica.DataChannel.Abstractions.Pipeline;
using Monica.DataChannel.Pipeline;
using Monica.DataChannel.Providers.Default;
using Monica.Modules;
using Monica.Testing.Hosting;

namespace Test.Monica.DataChannel.Support;

internal sealed class DataChannelTestApplicationFactory(DataChannelScenario scenario, int historyCapacity = 3,
    bool enableMinimalApi = false) : MonicaTestApplicationFactory<DataChannelTestApplicationFactory>
{
    protected override void ConfigureMonica(IMonicaBuilder builder)
    {
        builder.ConfigureModuleSystem(options =>
        {
            options.AutoAddMonicaHttpListener = false;
            options.EnableMinimalApiByDefault = false;
        });
        builder.AddDataChannel(options =>
        {
            options.RecentExceptionToKeep = historyCapacity;
            options.InitThreadCount = 1;
            options.EnableMinimalApi = enableMinimalApi;
        }).UseSetup<DataChannelScenarioSetup>();
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddSingleton(scenario);
    }
}

internal sealed class DataChannelScenario
{
    public List<ScenarioChannel> Channels { get; } = [new()];
    public ScenarioChannel Primary => Channels[0];
    public ScenarioChannel Find(string id) => Channels.Single(channel => channel.Id == id);
}

internal sealed class ScenarioChannel
{
    public string Id { get; init; } = "orders:α/outer";
    public string? GroupId { get; init; } = "operations/東京";
    public EndpointBehavior Inner { get; } = new();
    public EndpointBehavior Outer { get; } = new();
    public bool TransientOuter { get; init; }
    public List<IPipelineMiddleware> Middlewares { get; } = [];
    public bool TransientDebugger { get; init; }
}

internal sealed class EndpointBehavior
{
    private int _initializationCalls;
    public int InitializationCalls => Volatile.Read(ref _initializationCalls);
    public Func<CancellationToken, Task> Initialization { get; set; } = _ => Task.CompletedTask;
    public bool FailReceive { get; set; }
    public ConcurrentQueue<object?> Receipts { get; } = new();

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _initializationCalls);
        return Initialization(cancellationToken);
    }

    public Task ReceiveAsync(ChannelDataContext context)
    {
        if (FailReceive) throw new InvalidOperationException($"Local receive failed for '{context.Data}'.");
        Receipts.Enqueue(context.Data);
        return Task.CompletedTask;
    }
}

internal sealed class DataChannelScenarioSetup(DataChannelScenario scenario) : IDataChannelSetup
{
    public void Setup(IDataChannelRegistrar registrar)
    {
        foreach (var channel in scenario.Channels)
            registrar.Add(channel.Id, pipeline =>
            {
                pipeline.SetInnerEndpoint<ScenarioInnerEndpoint>();
                if (channel.TransientOuter) pipeline.SetOuterEndpoint<TransientScenarioOuterEndpoint>();
                else pipeline.SetOuterEndpoint<ScenarioOuterEndpoint>();
                pipeline.AddPipeMiddleware(channel.Middlewares.ToArray());
                if (channel.TransientDebugger) pipeline.AddPipeMiddleware<TransientScenarioDebugger>();
            }, channel.GroupId);
    }
}

internal sealed class ScenarioInnerEndpoint(DataChannelScenario scenario) : DefaultChannelEndpoint
{
    public override Task InitAsync(CancellationToken cancellationToken = default) =>
        scenario.Find(Pipe.Id).Inner.InitializeAsync(cancellationToken);
    public override Task ReceiveDataAsync(ChannelDataContext data) => scenario.Find(Pipe.Id).Inner.ReceiveAsync(data);
}

internal sealed class ScenarioOuterEndpoint(DataChannelScenario scenario) : DefaultChannelEndpoint
{
    public override Task InitAsync(CancellationToken cancellationToken = default) =>
        scenario.Find(Pipe.Id).Outer.InitializeAsync(cancellationToken);
    public override Task ReceiveDataAsync(ChannelDataContext data) => scenario.Find(Pipe.Id).Outer.ReceiveAsync(data);
}

internal sealed class TransientScenarioOuterEndpoint(DataChannelScenario scenario) : DefaultChannelEndpoint, ITransientChannelComponent
{
    public override dynamic GetMetadata() => new { ChannelId = Pipe.Id, Source = EntranceType };
    public override Task InitAsync(CancellationToken cancellationToken = default) =>
        scenario.Find(Pipe.Id).Outer.InitializeAsync(cancellationToken);
    public override Task ReceiveDataAsync(ChannelDataContext data) => scenario.Find(Pipe.Id).Outer.ReceiveAsync(data);
}

internal sealed class TransientScenarioDebugger : global::Monica.DataChannel.Middlewares.MessageDebuggerMiddleware, ITransientChannelComponent;
