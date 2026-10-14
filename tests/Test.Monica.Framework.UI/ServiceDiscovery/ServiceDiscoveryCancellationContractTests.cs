using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Monica.Modules;
using Monica.ServiceDiscovery.Abstractions;
using Monica.ServiceDiscovery.Models;
using Monica.StateStore.Abstractions;
using Xunit;

namespace Test.Monica.Framework.UI.ServiceDiscovery;

public sealed class ServiceDiscoveryCancellationContractTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetAllInstancesAsync_WhenAlreadyCanceled_ShouldRejectEmptyAndPopulatedReadsBeforeProviderAccess(
        bool hasRegistrations)
    {
        await using var application = await new ServiceDiscoveryTestApplicationFactory().CreateAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await ServiceDiscoveryCaptureContractTests.StopLocalRegistrationAsync(application);
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);
        var store = scope.ServiceProvider.GetRequiredKeyedService<IStateStore>(nameof(ModuleServiceDiscovery));
        if (hasRegistrations)
        {
            var instance = ServiceDiscoveryCaptureContractTests.CreateInstance();
            await store.SaveStateAsync($"reg:{instance.AppId}:{instance.InstanceId}", instance,
                scope.CancellationToken, TimeSpan.FromHours(1));
        }

        scope.Resolve<ReadFailureMemoryStore>().FailRegistryRead = true;
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Func<Task> read = async () => await scope.Resolve<IRegistrationStateManager>()
            .GetAllInstancesAsync(cancellation.Token);
        await read.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetAllInstancesAsync_WhenCancellationArrivesAfterScan_ShouldRejectEmptyAndPopulatedEvidence(
        bool hasRegistrations)
    {
        await using var application = await new ServiceDiscoveryTestApplicationFactory().CreateAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await ServiceDiscoveryCaptureContractTests.StopLocalRegistrationAsync(application);
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);
        var store = scope.ServiceProvider.GetRequiredKeyedService<IStateStore>(nameof(ModuleServiceDiscovery));
        if (hasRegistrations)
        {
            var instance = ServiceDiscoveryCaptureContractTests.CreateInstance();
            await store.SaveStateAsync($"reg:{instance.AppId}:{instance.InstanceId}", instance,
                scope.CancellationToken, TimeSpan.FromHours(1));
        }

        using var cancellation = new CancellationTokenSource();
        var controlledStore = scope.Resolve<ReadFailureMemoryStore>();
        var completed = false;
        controlledStore.RegistryScanCompleted = () =>
        {
            completed = true;
            cancellation.Cancel();
        };

        try
        {
            Func<Task> read = async () => await scope.Resolve<IRegistrationStateManager>()
                .GetAllInstancesAsync(cancellation.Token);
            await read.Should().ThrowAsync<OperationCanceledException>();
            completed.Should().BeTrue();
        }
        finally
        {
            controlledStore.RegistryScanCompleted = null;
        }
    }

    [Fact]
    public async Task GetAllInstancesAsync_WhenCancellationArrivesAfterBulkRead_ShouldRejectCompletedRegistrations()
    {
        await using var application = await new ServiceDiscoveryTestApplicationFactory().CreateAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await ServiceDiscoveryCaptureContractTests.StopLocalRegistrationAsync(application);
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);
        var store = scope.ServiceProvider.GetRequiredKeyedService<IStateStore>(nameof(ModuleServiceDiscovery));
        var instance = ServiceDiscoveryCaptureContractTests.CreateInstance();
        await store.SaveStateAsync($"reg:{instance.AppId}:{instance.InstanceId}", instance,
            scope.CancellationToken, TimeSpan.FromHours(1));
        using var cancellation = new CancellationTokenSource();
        var controlledStore = scope.Resolve<ReadFailureMemoryStore>();
        var completed = false;
        controlledStore.RegistryBulkReadCompleted = () =>
        {
            completed = true;
            cancellation.Cancel();
        };

        try
        {
            Func<Task> read = async () => await scope.Resolve<IRegistrationStateManager>()
                .GetAllInstancesAsync(cancellation.Token);
            await read.Should().ThrowAsync<OperationCanceledException>();
            completed.Should().BeTrue();
        }
        finally
        {
            controlledStore.RegistryBulkReadCompleted = null;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetLeaderStateAsync_WhenAlreadyCanceled_ShouldRejectAbsentAndPresentLeaseBeforeProviderAccess(
        bool hasLease)
    {
        await using var application = await new ServiceDiscoveryTestApplicationFactory().CreateAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await ServiceDiscoveryCaptureContractTests.StopLocalRegistrationAsync(application);
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);
        var store = scope.ServiceProvider.GetRequiredKeyedService<IStateStore>(nameof(ModuleServiceDiscovery));
        if (hasLease)
        {
            var lease = CreateLease();
            await store.SaveStateAsync($"leader:{lease.AppId}", lease,
                scope.CancellationToken, TimeSpan.FromHours(1));
        }

        scope.Resolve<ReadFailureMemoryStore>().FailLeaseRead = true;
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Func<Task> read = async () => await scope.Resolve<IRegistrationStateManager>()
            .GetLeaderStateAsync(cancellation.Token);
        await read.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetLeaderStateAsync_WhenCancellationArrivesAfterRead_ShouldRejectAbsentAndPresentLease(
        bool hasLease)
    {
        await using var application = await new ServiceDiscoveryTestApplicationFactory().CreateAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await ServiceDiscoveryCaptureContractTests.StopLocalRegistrationAsync(application);
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);
        var store = scope.ServiceProvider.GetRequiredKeyedService<IStateStore>(nameof(ModuleServiceDiscovery));
        if (hasLease)
        {
            var lease = CreateLease();
            await store.SaveStateAsync($"leader:{lease.AppId}", lease,
                scope.CancellationToken, TimeSpan.FromHours(1));
        }

        using var cancellation = new CancellationTokenSource();
        var controlledStore = scope.Resolve<ReadFailureMemoryStore>();
        var completed = false;
        controlledStore.LeaseReadCompleted = () =>
        {
            completed = true;
            cancellation.Cancel();
        };

        try
        {
            Func<Task> read = async () => await scope.Resolve<IRegistrationStateManager>()
                .GetLeaderStateAsync(cancellation.Token);
            await read.Should().ThrowAsync<OperationCanceledException>();
            completed.Should().BeTrue();
        }
        finally
        {
            controlledStore.LeaseReadCompleted = null;
        }
    }

    private static LeaderState CreateLease() => new()
    {
        AppId = ServiceDiscoveryTestApplicationFactory.AppId,
        InstanceId = "stored-leader",
        BecomeLeaderTime = new DateTime(2026, 10, 9, 1, 0, 0, DateTimeKind.Utc)
    };
}
