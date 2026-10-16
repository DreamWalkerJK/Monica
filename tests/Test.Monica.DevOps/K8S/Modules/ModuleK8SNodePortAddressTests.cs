using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

    [Fact]
    public async Task AddK8S_WhenPublicationIsPending_ShouldStartTheHostAndPublishWhenTheRequestCompletes()
    {
        using var environment = new NodePortAddressTestEnvironment();
        var requestStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var response = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reader = new ControlledInClusterServiceReader((_, cancellationToken) =>
        {
            requestStarted.TrySetResult(true);
            return response.Task.WaitAsync(cancellationToken);
        });
        var startup = new NodePortAddressApplicationFactory(environment, enabled: true).CreateAsync(
            scenario => scenario.With<IK8SInClusterServiceReader>(reader),
            TestContext.Current.CancellationToken);
        MonicaTestApplication? application = null;
        try
        {
            application = await startup.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            await requestStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
            var publication = GetInitializer(application).ExecuteTask!;

            Assert.False(response.Task.IsCompleted);
            Assert.False(publication.IsCompleted);
            Assert.Equal("previous-address", environment.Output);

            response.SetResult(NODE_PORT_SERVICE);
            await publication.WaitAsync(TestContext.Current.CancellationToken);

            Assert.Equal(EXPECTED_ADDRESSES, environment.Output);
            Assert.Equal("inventory-service", reader.LastServiceName);
            Assert.Equal(1, reader.CallCount);
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
        await using var application = await new NodePortAddressApplicationFactory(environment, enabled: true)
            .CreateAsync(scenario => scenario.With<IK8SInClusterServiceReader>(reader),
                TestContext.Current.CancellationToken);
        await GetInitializer(application).ExecuteTask!.WaitAsync(TestContext.Current.CancellationToken);

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
        await GetInitializer(application).ExecuteTask!.WaitAsync(TestContext.Current.CancellationToken);

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
        await GetInitializer(application).ExecuteTask!.WaitAsync(TestContext.Current.CancellationToken);

        Assert.True(string.IsNullOrEmpty(environment.Output));
        Assert.Equal(1, reader.CallCount);
    }

    [Fact]
    public async Task AddK8S_WhenTheHostStopsBeforeTheRequestCompletes_ShouldCancelAndPreserveThePreviousValue()
    {
        using var environment = new NodePortAddressTestEnvironment();
        var requestStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCanceled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var response = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reader = new ControlledInClusterServiceReader(async (_, cancellationToken) =>
        {
            using var registration = cancellationToken.Register(() => requestCanceled.TrySetResult(true));
            requestStarted.TrySetResult(true);
            return await response.Task;
        });
        await using var application = await new NodePortAddressApplicationFactory(environment, enabled: true)
            .CreateAsync(scenario => scenario.With<IK8SInClusterServiceReader>(reader),
                TestContext.Current.CancellationToken);
        var initializer = GetInitializer(application);
        await requestStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        var shutdown = application.DisposeAsync().AsTask();
        try
        {
            await requestCanceled.Task.WaitAsync(TestContext.Current.CancellationToken);
            Assert.True(reader.LastCancellationToken.IsCancellationRequested);
            response.SetResult(NODE_PORT_SERVICE);
            await shutdown.WaitAsync(TestContext.Current.CancellationToken);
            await initializer.ExecuteTask!.WaitAsync(TestContext.Current.CancellationToken);

            Assert.Equal("previous-address", environment.Output);
            Assert.Equal(1, reader.CallCount);
        }
        finally
        {
            response.TrySetResult(NODE_PORT_SERVICE);
            await shutdown;
        }
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
        await Task.WhenAll(GetInitializer(firstApplication).ExecuteTask!, GetInitializer(secondApplication).ExecuteTask!)
            .WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(EXPECTED_ADDRESSES, firstEnvironment.Output);
        Assert.Equal("http://192.0.2.9:32080", secondEnvironment.Output);
        Assert.NotSame(firstApplication.Services.GetRequiredService<K8SNodePortAddressResolver>(),
            secondApplication.Services.GetRequiredService<K8SNodePortAddressResolver>());
        Assert.Same(firstReader, firstApplication.Services.GetRequiredService<IK8SInClusterServiceReader>());
        Assert.Same(secondReader, secondApplication.Services.GetRequiredService<IK8SInClusterServiceReader>());
        Assert.Equal(1, firstReader.CallCount);
        Assert.Equal(1, secondReader.CallCount);
    }

    private static K8SNodePortAddressInitializer GetInitializer(MonicaTestApplication application) =>
        Assert.Single(application.Services.GetServices<IHostedService>().OfType<K8SNodePortAddressInitializer>());

    private sealed class NodePortAddressApplicationFactory(
        NodePortAddressTestEnvironment environment,
        bool? enabled = null)
        : MonicaTestApplicationFactory<ModuleK8SNodePortAddressTests>
    {
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
    }
}
