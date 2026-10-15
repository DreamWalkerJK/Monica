using AwesomeAssertions;
using Monica.AI.UI.Shared.Support;

namespace Test.Monica.AI.UI.Shared.Support;

public sealed class AIAdministrationOperationLifetimeTests
{
    [Fact]
    public async Task RunAsync_WhenSynchronousPrefixStartsRetirement_ShouldDrainReservedOperation()
    {
        var lifetime = new AIAdministrationOperationLifetime();
        var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? retirement = null;
        var cleanupCount = 0;

        var operation = lifetime.RunAsync(_ =>
        {
            retirement = lifetime.RetireAsync(() =>
            {
                cleanupCount++;
                return ValueTask.CompletedTask;
            }).AsTask();
            return release.Task;
        });

        lifetime.IsRetired.Should().BeTrue();
        retirement.Should().NotBeNull();
        retirement!.IsCompleted.Should().BeFalse();
        cleanupCount.Should().Be(0);
        release.SetResult(17);

        (await operation).Should().Be(17);
        await retirement;
        cleanupCount.Should().Be(1);
    }

    [Fact]
    public async Task RetireAsync_WhenProducerIgnoresCancellation_ShouldCancelThenDrainBeforeCleanup()
    {
        var lifetime = new AIAdministrationOperationLifetime();
        var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = lifetime.Token.Register(() => cancellationObserved.SetResult());
        var cleanupCount = 0;
        var operation = lifetime.RunAsync(_ => release.Task);

        var retirement = lifetime.RetireAsync(() =>
        {
            cleanupCount++;
            return ValueTask.CompletedTask;
        }).AsTask();
        await cancellationObserved.Task;

        lifetime.Token.IsCancellationRequested.Should().BeTrue();
        retirement.IsCompleted.Should().BeFalse();
        cleanupCount.Should().Be(0);
        release.SetResult(23);

        (await operation).Should().Be(23);
        await retirement;
        cleanupCount.Should().Be(1);
    }

    [Fact]
    public async Task RetireAsync_WhenCalledRepeatedly_ShouldShareDrainAndRunFirstCleanupOnce()
    {
        var lifetime = new AIAdministrationOperationLifetime();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupCount = 0;
        var otherCleanupCount = 0;
        var operation = lifetime.RunAsync(_ => release.Task);
        var first = lifetime.RetireAsync(async () =>
        {
            cleanupCount++;
            cleanupStarted.SetResult();
            await cleanupRelease.Task;
        }).AsTask();
        var second = lifetime.RetireAsync(() =>
        {
            otherCleanupCount++;
            return ValueTask.CompletedTask;
        }).AsTask();

        second.Should().BeSameAs(first);
        release.SetResult();
        await operation;
        await cleanupStarted.Task;
        first.IsCompleted.Should().BeFalse();
        cleanupRelease.SetResult();
        await Task.WhenAll(first, second);
        await lifetime.RetireAsync();

        cleanupCount.Should().Be(1);
        otherCleanupCount.Should().Be(0);
    }

    [Fact]
    public async Task RetireAsync_WhenCancellationProducerAndCleanupFail_ShouldReportAllFailuresAndCleanOnce()
    {
        var lifetime = new AIAdministrationOperationLifetime();
        var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationFailure = new InvalidOperationException("Cancellation callback failed.");
        var producerFailure = new InvalidOperationException("Tokenless producer failed.");
        var cleanupFailure = new InvalidOperationException("Cleanup failed.");
        using var registration = lifetime.Token.Register(() => throw cancellationFailure);
        var cleanupCount = 0;
        var operation = lifetime.RunAsync(_ => release.Task);
        var retirement = lifetime.RetireAsync(() =>
        {
            cleanupCount++;
            return ValueTask.FromException(cleanupFailure);
        }).AsTask();
        release.SetException(producerFailure);

        (await Assert.ThrowsAsync<InvalidOperationException>(() => operation)).Should().BeSameAs(producerFailure);
        var failure = await Assert.ThrowsAsync<AggregateException>(() => retirement);
        failure.Flatten().InnerExceptions.Should().HaveCount(3);
        failure.Flatten().InnerExceptions.Should().Contain(cancellationFailure);
        failure.Flatten().InnerExceptions.Should().Contain(producerFailure);
        failure.Flatten().InnerExceptions.Should().Contain(cleanupFailure);
        (await Assert.ThrowsAsync<AggregateException>(() => lifetime.RetireAsync().AsTask())).Should().BeSameAs(failure);
        cleanupCount.Should().Be(1);
    }

    [Fact]
    public async Task RetireAsync_WhenProducerHonorsCancellation_ShouldDrainWithoutReportingCancellationFailure()
    {
        var lifetime = new AIAdministrationOperationLifetime();
        var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = lifetime.RunAsync(async token =>
        {
            using var cancellation = token.Register(() => release.TrySetCanceled(token));
            return await release.Task;
        });

        await lifetime.RetireAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        operation.IsCanceled.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_WhenRetired_ShouldRejectProducerAndStateUpdates()
    {
        var lifetime = new AIAdministrationOperationLifetime();
        var producerCount = 0;
        var stateUpdateCount = 0;
        await lifetime.RetireAsync();

        var operation = lifetime.RunAsync(_ =>
        {
            producerCount++;
            return Task.FromResult(42);
        });
        var applied = lifetime.TryApply(() => stateUpdateCount++);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        applied.Should().BeFalse();
        producerCount.Should().Be(0);
        stateUpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task RunAsync_WhenProducerThrowsBeforeReturningTask_ShouldSurfaceFailureAndStillRetire()
    {
        var lifetime = new AIAdministrationOperationLifetime();
        var failure = new InvalidOperationException("Synchronous producer failed.");
        var cleanupCount = 0;

        var operation = lifetime.RunAsync<int>(_ => throw failure);

        (await Assert.ThrowsAsync<InvalidOperationException>(() => operation)).Should().BeSameAs(failure);
        await lifetime.RetireAsync(() =>
        {
            cleanupCount++;
            return ValueTask.CompletedTask;
        });
        cleanupCount.Should().Be(1);
    }
}
