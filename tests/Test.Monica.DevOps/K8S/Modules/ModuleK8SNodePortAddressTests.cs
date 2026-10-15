using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Monica.Core.Modularity.Abstractions;
using Monica.DevOps.K8S.Providers.InCluster;
using Monica.DevOps.K8S.Services;
using Monica.Modules;
using Monica.Testing.Hosting;
using Test.Monica.DevOps.K8S.Support;
using Xunit;

namespace Test.Monica.DevOps.K8S.Modules;

public sealed class ModuleK8SNodePortAddressTests
{
    private const string NODE_PORT_SERVICE =
        """{"spec":{"type":"NodePort","ports":[{"nodePort":31081},{"nodePort":31080}]}}""";
    private const string EXPECTED_ADDRESSES = "http://192.0.2.8:31081;http://192.0.2.8:31080";

    [Fact]
    public async Task AddK8S_WhenPublishingIsNotEnabled_ShouldLeaveTheOutputAndClusterReaderUnregistered()
    {
        using var environment = new NodePortAddressTestEnvironment();
        await using var application = await new NodePortAddressApplicationFactory(environment).CreateAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(application.Services.GetRequiredService<IOptions<ModuleK8SOption>>()
            .Value.NodePortAddresses.Enabled);
        Assert.Null(application.Services.GetService<IK8SInClusterServiceReader>());
        Assert.Null(application.Services.GetService<K8SNodePortAddressResolver>());
        Assert.DoesNotContain(application.Services.GetServices<IHostedService>(),
            service => service is K8SNodePortAddressInitializer);
        Assert.Equal("previous-address", environment.Output);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AddK8S_WhenPublicationIsPending_ShouldPublishBeforeEveryHostedServiceStarts(
        bool startConcurrently)
    {
        using var environment = new NodePortAddressTestEnvironment();
        var requestStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var response = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reader = new ControlledInClusterServiceReader((_, cancellationToken) =>
        {
            requestStarted.TrySetResult(true);
            return response.Task.WaitAsync(cancellationToken);
        });
        var beforeModule = new AddressStartObserver(environment.OutputVariable);
        var afterModule = new AddressStartObserver(environment.OutputVariable);
        var factory = new NodePortAddressApplicationFactory(
            environment, enabled: true, startConcurrently, beforeModule, afterModule);
        var startup = factory.CreateAsync(
            scenario => scenario.With<IK8SInClusterServiceReader>(reader),
            TestContext.Current.CancellationToken);
        MonicaTestApplication? application = null;
        try
        {
            await requestStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

            Assert.False(beforeModule.Started);
            Assert.False(afterModule.Started);
            Assert.Equal("previous-address", environment.Output);

            response.SetResult(NODE_PORT_SERVICE);
            application = await startup;

            Assert.Equal(EXPECTED_ADDRESSES, environment.Output);
            Assert.Equal(EXPECTED_ADDRESSES, beforeModule.ValueWhenStarted);
            Assert.Equal(EXPECTED_ADDRESSES, afterModule.ValueWhenStarted);
            Assert.Equal("inventory-service", reader.LastServiceName);
            Assert.Equal(1, reader.CallCount);
            Assert.Single(application.Services.GetServices<IHostedService>()
                .OfType<K8SNodePortAddressInitializer>());
        }
        finally
        {
            response.TrySetResult(NODE_PORT_SERVICE);
            application ??= await startup;
            await application.DisposeAsync();
        }
    }

    [Theory]
    [InlineData("http-failure")]
    [InlineData("reader-exception")]
    [InlineData("reader-cancellation")]
    [InlineData("malformed-json")]
    [InlineData("missing-node-port")]
    [InlineData("partial-invalid-node-port")]
    [InlineData("non-node-port")]
    public async Task AddK8S_WhenPublicationCannotResolveAddresses_ShouldStartAndPreserveThePreviousValue(
        string failure)
    {
        using var environment = new NodePortAddressTestEnvironment();
        var reader = new ControlledInClusterServiceReader((_, _) => failure switch
        {
            "http-failure" => Task.FromResult<string?>(null),
            "reader-exception" => throw new IOException("The ServiceAccount could not be read."),
            "reader-cancellation" => throw new OperationCanceledException("The cluster request was canceled."),
            "malformed-json" => Task.FromResult<string?>("{invalid"),
            "missing-node-port" => Task.FromResult<string?>(
                """{"spec":{"type":"NodePort","ports":[{"port":80}]}}"""),
            "partial-invalid-node-port" => Task.FromResult<string?>(
                """{"spec":{"type":"NodePort","ports":[{"nodePort":31081},{"port":80}]}}"""),
            "non-node-port" => Task.FromResult<string?>(
                """{"spec":{"type":"ClusterIP","ports":[{"port":80}]}}"""),
            _ => throw new ArgumentOutOfRangeException(nameof(failure))
        });
        var observer = new AddressStartObserver(environment.OutputVariable);

        await using var application = await new NodePortAddressApplicationFactory(
                environment, enabled: true, afterModule: observer)
            .CreateAsync(scenario => scenario.With<IK8SInClusterServiceReader>(reader),
                TestContext.Current.CancellationToken);

        Assert.True(observer.Started);
        Assert.Equal("previous-address", observer.ValueWhenStarted);
        Assert.Equal("previous-address", environment.Output);
        Assert.Equal(1, reader.CallCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddK8S_WhenAnInputIsMissing_ShouldStartWithoutCallingTheClusterReader(bool missingServiceName)
    {
        using var environment = new NodePortAddressTestEnvironment();
        Environment.SetEnvironmentVariable(
            missingServiceName ? environment.ServiceNameVariable : environment.BaseAddressVariable,
            null);
        var reader = new ControlledInClusterServiceReader((_, _) =>
            throw new InvalidOperationException("The cluster reader must not run without both inputs."));

        await using var application = await new NodePortAddressApplicationFactory(environment, enabled: true)
            .CreateAsync(scenario => scenario.With<IK8SInClusterServiceReader>(reader),
                TestContext.Current.CancellationToken);

        Assert.Equal(0, reader.CallCount);
        Assert.Equal("previous-address", environment.Output);
    }

    [Fact]
    public async Task AddK8S_WhenTheNodePortArrayIsEmpty_ShouldClearThePreviousValue()
    {
        using var environment = new NodePortAddressTestEnvironment();
        var reader = new ControlledInClusterServiceReader((_, _) => Task.FromResult<string?>(
            """{"spec":{"type":"NodePort","ports":[]}}"""));

        await using var application = await new NodePortAddressApplicationFactory(environment, enabled: true)
            .CreateAsync(scenario => scenario.With<IK8SInClusterServiceReader>(reader),
                TestContext.Current.CancellationToken);

        Assert.True(string.IsNullOrEmpty(environment.Output));
        Assert.Equal(1, reader.CallCount);
    }

    [Fact]
    public async Task StartingAsync_WhenItsCancellationTokenIsCanceled_ShouldLeaveThePublishedValueUnchanged()
    {
        using var environment = new NodePortAddressTestEnvironment();
        using var canceled = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await canceled.CancelAsync();
        var reader = new ControlledInClusterServiceReader((_, cancellationToken) =>
            Task.FromCanceled<string?>(cancellationToken));
        var options = Options.Create(new ModuleK8SOption { NodePortAddresses = environment.CreateOptions() });
        var resolver = new K8SNodePortAddressResolver(
            options, reader, NullLogger<K8SNodePortAddressResolver>.Instance);
        var initializer = new K8SNodePortAddressInitializer(
            resolver, options, NullLogger<K8SNodePortAddressInitializer>.Instance);

        await initializer.StartingAsync(canceled.Token);

        Assert.Equal(canceled.Token, reader.LastCancellationToken);
        Assert.Equal("previous-address", environment.Output);
    }

    [Fact]
    public async Task AddK8S_WhenTwoHostsAreActive_ShouldKeepTheirOptionsAndPublishedValuesSeparate()
    {
        using var firstEnvironment = new NodePortAddressTestEnvironment();
        using var secondEnvironment = new NodePortAddressTestEnvironment();
        Environment.SetEnvironmentVariable(secondEnvironment.BaseAddressVariable, "192.0.2.9");
        var firstReader = new ControlledInClusterServiceReader((_, _) => Task.FromResult<string?>(NODE_PORT_SERVICE));
        var secondReader = new ControlledInClusterServiceReader((_, _) => Task.FromResult<string?>(
            """{"spec":{"type":"NodePort","ports":[{"nodePort":32080}]}}"""));
        var firstStartup = new NodePortAddressApplicationFactory(firstEnvironment, enabled: true).CreateAsync(
            scenario => scenario.With<IK8SInClusterServiceReader>(firstReader), TestContext.Current.CancellationToken);
        var secondStartup = new NodePortAddressApplicationFactory(secondEnvironment, enabled: true).CreateAsync(
            scenario => scenario.With<IK8SInClusterServiceReader>(secondReader), TestContext.Current.CancellationToken);

        await using var firstApplication = await firstStartup;
        await using var secondApplication = await secondStartup;

        Assert.Equal(EXPECTED_ADDRESSES, firstEnvironment.Output);
        Assert.Equal("http://192.0.2.9:32080", secondEnvironment.Output);
        Assert.NotSame(firstApplication.Services.GetRequiredService<K8SNodePortAddressResolver>(),
            secondApplication.Services.GetRequiredService<K8SNodePortAddressResolver>());
        Assert.Same(firstReader, firstApplication.Services.GetRequiredService<IK8SInClusterServiceReader>());
        Assert.Same(secondReader, secondApplication.Services.GetRequiredService<IK8SInClusterServiceReader>());
        Assert.Equal(1, firstReader.CallCount);
        Assert.Equal(1, secondReader.CallCount);
    }

    private sealed class NodePortAddressApplicationFactory(
        NodePortAddressTestEnvironment environment,
        bool? enabled = null,
        bool startConcurrently = false,
        AddressStartObserver? beforeModule = null,
        AddressStartObserver? afterModule = null)
        : MonicaTestApplicationFactory<ModuleK8SNodePortAddressTests>
    {
        protected override void ConfigureHost(WebApplicationBuilder builder)
        {
            builder.Services.Configure<HostOptions>(options => options.ServicesStartConcurrently = startConcurrently);
            if (beforeModule is not null)
            {
                builder.Services.AddSingleton<IHostedService>(beforeModule);
            }
        }

        protected override void ConfigureMonica(IMonicaBuilder builder)
        {
            builder.AddK8S(options =>
            {
                options.NodePortAddresses = environment.CreateOptions();
                if (enabled is { } enablePublication)
                {
                    options.NodePortAddresses.Enabled = enablePublication;
                }
            });
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            if (afterModule is not null)
            {
                services.AddSingleton<IHostedService>(afterModule);
            }
        }
    }

    private sealed class AddressStartObserver(string outputVariable) : IHostedService
    {
        public bool Started { get; private set; }

        public string? ValueWhenStarted { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            ValueWhenStarted = Environment.GetEnvironmentVariable(outputVariable);
            Started = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
