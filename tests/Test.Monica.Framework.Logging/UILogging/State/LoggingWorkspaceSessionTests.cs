using System.Text;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Monica.Framework.UI.UILogging.State;
using Monica.Testing.Results;
using Test.Monica.Framework.Logging.UILogging.Support;
using Xunit;

namespace Test.Monica.Framework.Logging.UILogging.State;

/// <summary>Exercises the registered circuit-scoped owner using actual readers and a deterministic navigation boundary.</summary>
public sealed class LoggingWorkspaceSessionTests
{
    [Fact]
    public async Task GetReader_WhenNavigatingBetweenReaderAndCatalog_ShouldPreserveSourceFiltersAndFollowingWithinTheScope()
    {
        using var factory = new LoggingUITestApplicationFactory();
        await WriteAsync(factory, "retained.log", "ordinary\nneedle 日志\n");
        await using var application = await factory.CreateAsync(
            scenario => scenario.With<NavigationManager>(static _ => new LoggingTestNavigationManager()),
            TestContext.Current.CancellationToken);
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);
        var owner = scope.Resolve<LoggingWorkspaceSession>();
        var navigation = scope.Resolve<NavigationManager>();
        scope.Resolve<LoggingWorkspaceSession>().Should().BeSameAs(owner);
        var reader = owner.GetReader();
        owner.IsInitialized.Should().BeFalse();
        (await reader.SwitchToFileAsync("retained.log", 100, scope.CancellationToken)).ShouldSucceed();
        var filtered = (await reader.UpdateFilterAsync("needle", true, cancellationToken: scope.CancellationToken)).ShouldSucceed()!;
        owner.MarkInitialized(reader).Should().BeTrue();
        (await reader.StartAsync(scope.CancellationToken)).ShouldSucceed();
        await using var subscription = reader.Subscribe();

        navigation.NavigateTo("logging-monitor/files?search=retained#catalog");

        owner.GetReader().Should().BeSameAs(reader);
        owner.IsInitialized.Should().BeTrue();
        var retained = reader.GetSnapshot();
        retained.Source.Should().Be(filtered.Source);
        retained.Filter.Keyword.Should().Be("needle");
        retained.Filter.OnlyCapture.Should().BeTrue();
        retained.Lines.Select(static line => line.Raw).Should().Equal("needle 日志");
        retained.IsRunning.Should().BeTrue();
        subscription.Reader.Completion.IsCompleted.Should().BeFalse();

        navigation.NavigateTo("logging-monitor?file=retained.log#line-2");

        owner.GetReader().Should().BeSameAs(reader);
        owner.IsInitialized.Should().BeTrue();
        reader.GetSnapshot().IsRunning.Should().BeTrue();
        (await reader.PauseAsync(scope.CancellationToken)).ShouldSucceed();
    }

    [Theory]
    [InlineData("logging-monitor")]
    [InlineData("logging-monitor/")]
    [InlineData("logging-monitor?file=a.log#L2")]
    [InlineData("logging-monitor/files")]
    [InlineData("logging-monitor/files/?search=x#catalog")]
    [InlineData("LOGGING-MONITOR/FILES")]
    public async Task GetReader_WhenAnExactWorkspaceRouteUsesABasePath_ShouldAcceptItsQueryHashAndSupportedPathVariants(string relativeUri)
    {
        using var factory = new LoggingUITestApplicationFactory();
        await using var application = await factory.CreateAsync(
            scenario => scenario.With<NavigationManager>(_ => new LoggingTestNavigationManager(relativeUri)),
            TestContext.Current.CancellationToken);
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);
        var owner = scope.Resolve<LoggingWorkspaceSession>();

        var reader = owner.GetReader();

        reader.Should().NotBeNull();
        owner.GetReader().Should().BeSameAs(reader);
        owner.IsInitialized.Should().BeFalse();
    }

    [Theory]
    [InlineData("logging-monitoring")]
    [InlineData("logging-monitor/files-old")]
    [InlineData("logging-monitor/files/item")]
    [InlineData("health-checks?returnUrl=logging-monitor")]
    [InlineData("?page=logging-monitor#logging-monitor/files")]
    public async Task GetReader_WhenTheLocationOnlyResemblesAWorkspaceRoute_ShouldRejectAcquisition(string relativeUri)
    {
        using var factory = new LoggingUITestApplicationFactory();
        await using var application = await factory.CreateAsync(
            scenario => scenario.With<NavigationManager>(_ => new LoggingTestNavigationManager(relativeUri)),
            TestContext.Current.CancellationToken);
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);
        var owner = scope.Resolve<LoggingWorkspaceSession>();

        var acquire = () => owner.GetReader();

        acquire.Should().Throw<InvalidOperationException>();
        owner.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public async Task LocationChanged_WhenTheUserLeavesAndReenters_ShouldRetireTheOldReaderAndRejectItsLateInitialization()
    {
        using var factory = new LoggingUITestApplicationFactory();
        await WriteAsync(factory, "old.log", "previous reader\n");
        await using var application = await factory.CreateAsync(
            scenario => scenario.With<NavigationManager>(static _ => new LoggingTestNavigationManager()),
            TestContext.Current.CancellationToken);
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);
        var owner = scope.Resolve<LoggingWorkspaceSession>();
        var navigation = scope.Resolve<NavigationManager>();
        var previous = owner.GetReader();
        (await previous.SwitchToFileAsync("old.log", 100, scope.CancellationToken)).ShouldSucceed();
        (await previous.UpdateFilterAsync("previous", true, cancellationToken: scope.CancellationToken)).ShouldSucceed();
        (await previous.StartAsync(scope.CancellationToken)).ShouldSucceed();
        owner.MarkInitialized(previous).Should().BeTrue();
        await using var subscription = previous.Subscribe();

        navigation.NavigateTo("health-checks");

        owner.IsInitialized.Should().BeFalse();
        owner.MarkInitialized(previous).Should().BeFalse();
        var stale = () => previous.GetSnapshot();
        stale.Should().Throw<ObjectDisposedException>();
        var outside = () => owner.GetReader();
        outside.Should().Throw<InvalidOperationException>();

        navigation.NavigateTo("logging-monitor/files");
        var fresh = owner.GetReader();

        fresh.Should().NotBeSameAs(previous);
        fresh.GetSnapshot().TotalStoredLines.Should().Be(0);
        fresh.GetSnapshot().Filter.IsActive.Should().BeFalse();
        fresh.GetSnapshot().IsRunning.Should().BeFalse();
        owner.IsInitialized.Should().BeFalse();
        owner.MarkInitialized(previous).Should().BeFalse();
        owner.MarkInitialized(fresh).Should().BeTrue();
        await DrainSubscriptionAsync(subscription);
    }

    [Fact]
    public async Task GetReader_WhenTwoActualDiScopesShareTheHost_ShouldKeepTheirOwnersAndNavigationIndependent()
    {
        using var factory = new LoggingUITestApplicationFactory();
        await WriteAsync(factory, "a.log", "alpha\nneedle alpha\n");
        await WriteAsync(factory, "b.log", "beta\n");
        await using var application = await factory.CreateAsync(
            scenario => scenario.With<NavigationManager>(static _ => new LoggingTestNavigationManager()),
            TestContext.Current.CancellationToken);
        await using var firstScope = application.CreateScope(TestContext.Current.CancellationToken);
        await using var secondScope = application.CreateScope(TestContext.Current.CancellationToken);
        var firstOwner = firstScope.Resolve<LoggingWorkspaceSession>();
        var secondOwner = secondScope.Resolve<LoggingWorkspaceSession>();
        var firstNavigation = firstScope.Resolve<NavigationManager>();
        var secondNavigation = secondScope.Resolve<NavigationManager>();
        firstOwner.Should().NotBeSameAs(secondOwner);
        firstNavigation.Should().NotBeSameAs(secondNavigation);
        var first = firstOwner.GetReader();
        var second = secondOwner.GetReader();
        (await first.SwitchToFileAsync("a.log", 100, firstScope.CancellationToken)).ShouldSucceed();
        (await second.SwitchToFileAsync("b.log", 100, secondScope.CancellationToken)).ShouldSucceed();
        (await first.UpdateFilterAsync("needle", true, cancellationToken: firstScope.CancellationToken)).ShouldSucceed();
        (await second.StartAsync(secondScope.CancellationToken)).ShouldSucceed();
        firstOwner.MarkInitialized(first).Should().BeTrue();
        secondOwner.MarkInitialized(second).Should().BeTrue();

        firstNavigation.NavigateTo("health-checks");
        await firstScope.DisposeAsync();

        secondOwner.GetReader().Should().BeSameAs(second);
        secondOwner.IsInitialized.Should().BeTrue();
        second.GetSnapshot().Source!.RelativePath.Should().Be("b.log");
        second.GetSnapshot().Filter.IsActive.Should().BeFalse();
        second.GetSnapshot().IsRunning.Should().BeTrue();
        secondNavigation.NavigateTo("logging-monitor/files");
        secondOwner.GetReader().Should().BeSameAs(second);
        (await second.PauseAsync(secondScope.CancellationToken)).ShouldSucceed();
    }

    [Fact]
    public async Task DisposeAsync_WhenTheOwnerHasDetachedAndCurrentReaders_ShouldCacheAndDrainEveryOwnedRetirement()
    {
        using var factory = new LoggingUITestApplicationFactory();
        await WriteAsync(factory, "drain.log", "retained\n");
        await using var application = await factory.CreateAsync(
            scenario => scenario.With<NavigationManager>(static _ => new LoggingTestNavigationManager()),
            TestContext.Current.CancellationToken);
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);
        var owner = scope.Resolve<LoggingWorkspaceSession>();
        var navigation = scope.Resolve<NavigationManager>();
        var previous = owner.GetReader();
        (await previous.SwitchToFileAsync("drain.log", 100, scope.CancellationToken)).ShouldSucceed();
        (await previous.StartAsync(scope.CancellationToken)).ShouldSucceed();
        await using var previousSubscription = previous.Subscribe();
        navigation.NavigateTo("health-checks");
        navigation.NavigateTo("logging-monitor");
        var current = owner.GetReader();
        (await current.SwitchToFileAsync("drain.log", 100, scope.CancellationToken)).ShouldSucceed();
        (await current.StartAsync(scope.CancellationToken)).ShouldSucceed();
        await using var currentSubscription = current.Subscribe();
        owner.MarkInitialized(current).Should().BeTrue();

        var firstDispose = owner.DisposeAsync().AsTask();
        var secondDispose = owner.DisposeAsync().AsTask();
        firstDispose.Should().BeSameAs(secondDispose);
        await Task.WhenAll(firstDispose, secondDispose);

        owner.IsInitialized.Should().BeFalse();
        owner.MarkInitialized(previous).Should().BeFalse();
        owner.MarkInitialized(current).Should().BeFalse();
        var acquire = () => owner.GetReader();
        acquire.Should().Throw<ObjectDisposedException>();
        var previousSnapshot = () => previous.GetSnapshot();
        var currentSnapshot = () => current.GetSnapshot();
        previousSnapshot.Should().Throw<ObjectDisposedException>();
        currentSnapshot.Should().Throw<ObjectDisposedException>();
        await DrainSubscriptionAsync(previousSubscription);
        await DrainSubscriptionAsync(currentSubscription);
        navigation.NavigateTo("logging-monitor/files");
        acquire.Should().Throw<ObjectDisposedException>();
    }

    private static Task WriteAsync(LoggingUITestApplicationFactory factory, string file, string content) =>
        File.WriteAllTextAsync(Path.Combine(factory.LogDirectory, file), content, new UTF8Encoding(false), TestContext.Current.CancellationToken);

    private static async Task DrainSubscriptionAsync(LoggingSubscription subscription)
    {
        while (subscription.Reader.TryRead(out _)) { }
        await subscription.Reader.Completion.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    }
}
