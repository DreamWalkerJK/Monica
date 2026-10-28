using AwesomeAssertions;
using Monica.Core.ExceptionHandling.Exceptions;
using Monica.EventBus.Kafka.Providers.ConfluentKafka;
using Monica.EventBus.Models;
using Xunit;

namespace Test.Monica.EventBus.Kafka.Providers.ConfluentKafka;

public class KafkaEventBusSubscriptionHostedServiceTests
{
    [Fact]
    public async Task CommitAfterHandlingAsync_WaitsForHandlerBeforeAcknowledging()
    {
        var handled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var committed = false;
        var rewound = false;

        var pending = KafkaEventBusSubscriptionHostedService.CommitAfterHandlingAsync(
            () => handled.Task,
            () => committed = true,
            () => rewound = true,
            _ => Assert.Fail("A successful delivery must not be rejected."));

        committed.Should().BeFalse();
        handled.SetResult();
        (await pending).Should().BeTrue();
        committed.Should().BeTrue();
        rewound.Should().BeFalse();
    }

    [Fact]
    public async Task CommitAfterHandlingAsync_WhenHandlerFails_RewindsWithoutAcknowledging()
    {
        var committed = false;
        var rewound = false;
        var failure = new InvalidOperationException("handler failed");

        Func<Task> operation = () => KafkaEventBusSubscriptionHostedService.CommitAfterHandlingAsync(
            () => Task.FromException(failure),
            () => committed = true,
            () => rewound = true,
            _ => Assert.Fail("An unexpected failure must remain retryable."));

        (await Assert.ThrowsAsync<InvalidOperationException>(operation)).Should().BeSameAs(failure);
        committed.Should().BeFalse();
        rewound.Should().BeTrue();
    }

    public static TheoryData<Exception> PermanentFailures => new()
    {
        new BusinessException("Required matching time is missing."),
        new EventMessageDeserializationException("Invalid event body.")
    };

    [Theory]
    [MemberData(nameof(PermanentFailures))]
    public async Task CommitAfterHandlingAsync_WhenPermanentlyRejected_ShouldLogCommitAndContinue(Exception failure)
    {
        var operations = new List<string>();
        Exception? rejected = null;

        var processed = await KafkaEventBusSubscriptionHostedService.CommitAfterHandlingAsync(
            () => Task.FromException(failure),
            () => operations.Add("commit"),
            () => operations.Add("rewind"),
            exception =>
            {
                rejected = exception;
                operations.Add("reject");
            });
        var nextProcessed = await KafkaEventBusSubscriptionHostedService.CommitAfterHandlingAsync(
            () => Task.CompletedTask,
            () => operations.Add("next-commit"),
            () => operations.Add("next-rewind"),
            _ => Assert.Fail("The next delivery must not be rejected."));

        processed.Should().BeFalse();
        nextProcessed.Should().BeTrue();
        rejected.Should().BeSameAs(failure);
        operations.Should().Equal("reject", "commit", "next-commit");
    }

    [Fact]
    public async Task CommitAfterHandlingAsync_WhenCancelled_ShouldRewindWithoutAcknowledging()
    {
        var rewound = false;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            KafkaEventBusSubscriptionHostedService.CommitAfterHandlingAsync(
                () => Task.FromCanceled(new CancellationToken(canceled: true)),
                () => Assert.Fail("Cancellation must not acknowledge delivery."),
                () => rewound = true,
                _ => Assert.Fail("Cancellation must not reject delivery.")));

        rewound.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CommitAfterHandlingAsync_WhenCommitFails_ShouldRewindEvenAfterRejection(bool rejectHandler)
    {
        var rewound = false;
        var rejectionCount = 0;
        // Even a terminal-looking exception from commit is a failed acknowledgement, not a handler rejection.
        var commitFailure = new BusinessException("Commit failed.");

        var thrown = await Assert.ThrowsAsync<BusinessException>(() =>
            KafkaEventBusSubscriptionHostedService.CommitAfterHandlingAsync(
                () => rejectHandler ? Task.FromException(new BusinessException("Rejected.")) : Task.CompletedTask,
                () => throw commitFailure,
                () => rewound = true,
                _ => rejectionCount++));

        thrown.Should().BeSameAs(commitFailure);
        rewound.Should().BeTrue();
        rejectionCount.Should().Be(rejectHandler ? 1 : 0);
    }

    [Fact]
    public async Task CommitAfterHandlingAsync_WhenRewindFails_ShouldRequireConsumerToStop()
    {
        var handlerFailure = new InvalidOperationException("Handler failed.");
        var seekFailure = new InvalidOperationException("Seek failed.");

        var thrown = await Assert.ThrowsAnyAsync<Exception>(() =>
            KafkaEventBusSubscriptionHostedService.CommitAfterHandlingAsync(
                () => Task.FromException(handlerFailure),
                () => Assert.Fail("A failed delivery must not be acknowledged."),
                () => throw seekFailure,
                _ => Assert.Fail("An unexpected failure must remain retryable.")));

        thrown.GetType().Name.Should().Be("KafkaRewindFailedException");
        Assert.IsType<AggregateException>(thrown.InnerException).InnerExceptions
            .Should().Equal(handlerFailure, seekFailure);
    }

    [Fact]
    public async Task TopicConsumerTryStart_WhenStopWasRequestedFirst_ShouldNotRunConsumer()
    {
        var executionCount = 0;
        var consumer = CreateConsumer(_ =>
        {
            Interlocked.Increment(ref executionCount);
            return Task.CompletedTask;
        });

        await consumer.StopAsync().WaitAsync(TestContext.Current.CancellationToken);
        var started = consumer.TryStart();

        started.Should().BeFalse();
        Volatile.Read(ref executionCount).Should().Be(0);
    }

    [Fact]
    public async Task TopicConsumerStopAsync_WhenConsumerIsRunning_ShouldCancelAwaitAndShareShutdown()
    {
        var consumerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseConsumer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executionCount = 0;
        var consumer = CreateConsumer(async activeConsumer =>
        {
            using var registration = activeConsumer.CancellationToken.Register(
                () => cancellationObserved.TrySetResult());
            Interlocked.Increment(ref executionCount);
            consumerStarted.TrySetResult();
            await releaseConsumer.Task;
        });

        consumer.TryStart().Should().BeTrue();
        await consumerStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        var stopTask = consumer.StopAsync();
        var repeatedStopTask = consumer.StopAsync();
        await cancellationObserved.Task.WaitAsync(TestContext.Current.CancellationToken);
        var stoppedBeforeConsumerCompleted = stopTask.IsCompleted;

        releaseConsumer.TrySetResult();
        await stopTask.WaitAsync(TestContext.Current.CancellationToken);

        repeatedStopTask.Should().BeSameAs(stopTask);
        stoppedBeforeConsumerCompleted.Should().BeFalse();
        Volatile.Read(ref executionCount).Should().Be(1);
    }

    [Fact]
    public async Task TopicConsumerRegistryStopAccepting_WhenShutdownRacesWithCreation_ShouldOwnEveryConsumer()
    {
        var consumerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var registry = new KafkaEventBusSubscriptionHostedService.TopicConsumerRegistry();
        var activeConsumer = CreateConsumer(async consumer =>
        {
            consumerStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, consumer.CancellationToken);
        });

        registry.TryAddAndStart(activeConsumer).Should().BeTrue();
        await consumerStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        registry.StopAccepting();
        var drainedConsumers = registry.Drain();
        var lateConsumer = CreateConsumer(_ => Task.CompletedTask);
        registry.TryAddAndStart(lateConsumer).Should().BeFalse();

        await lateConsumer.StopAsync().WaitAsync(TestContext.Current.CancellationToken);
        foreach (var consumer in drainedConsumers)
        {
            await consumer.StopAsync().WaitAsync(TestContext.Current.CancellationToken);
        }

        drainedConsumers.Should().ContainSingle().Which.Should().BeSameAs(activeConsumer);
        registry.Drain().Should().BeEmpty();
    }

    private static KafkaEventBusSubscriptionHostedService.TopicConsumer CreateConsumer(
        Func<KafkaEventBusSubscriptionHostedService.TopicConsumer, Task> consumeAsync)
    {
        return new KafkaEventBusSubscriptionHostedService.TopicConsumer(
            "orders",
            typeof(object),
            CancellationToken.None,
            consumeAsync);
    }
}
