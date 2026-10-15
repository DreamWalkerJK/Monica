using Monica.Configuration.UI.Support;

namespace Test.Monica.Configuration.UI.Support;

public sealed class ConfigurationOperationLifetimeTests
{
    [Fact]
    public async Task Retirement_JoinsActualTokenlessWorkAndRepeatedCleanup()
    {
        var owner = new ConfigurationOperationLifetime();
        var produced = NewCompletion<int>();
        var cleanupCalls = 0;
        var operation = owner.RunAsync(_ => produced.Task);
        var retirement = owner.RetireAsync(() => { cleanupCalls++; return ValueTask.CompletedTask; }).AsTask();

        Assert.Same(retirement, owner.RetireAsync().AsTask());
        Assert.False(retirement.IsCompleted);
        Assert.Equal(0, cleanupCalls);
        produced.SetResult(42);
        Assert.Equal(42, await ObserveAsync(operation));
        await ObserveAsync(retirement);
        Assert.Equal(1, cleanupCalls);
        Assert.True(owner.IsRetired);
        var invoked = false;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ObserveAsync(owner.RunAsync(_ =>
        {
            invoked = true;
            return Task.CompletedTask;
        })));
        Assert.False(invoked);
    }

    [Fact]
    public async Task Producer_CanReserveRetirementBeforeReturningItsActualWork()
    {
        var owner = new ConfigurationOperationLifetime();
        var produced = NewCompletion<int>();
        Task? retirement = null;
        var operation = owner.RunAsync(_ =>
        {
            retirement = owner.RetireAsync().AsTask();
            return produced.Task;
        });
        Assert.NotNull(retirement);
        Assert.False(retirement.IsCompleted);
        produced.SetResult(7);
        Assert.Equal(7, await ObserveAsync(operation));
        await ObserveAsync(retirement);
    }

    [Fact]
    public async Task ExactOwnerCancellation_IsCancelledAndDoesNotFailRetirement()
    {
        var owner = new ConfigurationOperationLifetime();
        var proceed = NewCompletion<bool>();
        var operation = owner.RunAsync(async token =>
        {
            await proceed.Task;
            token.ThrowIfCancellationRequested();
        });
        var retirement = owner.RetireAsync().AsTask();
        proceed.SetResult(true);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ObserveAsync(operation));
        Assert.True(operation.IsCanceled);
        await ObserveAsync(retirement);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ForeignCancellation_RemainsAFaultEvenDuringRetirement(bool useForeignToken)
    {
        var owner = new ConfigurationOperationLifetime();
        var proceed = NewCompletion<bool>();
        using var foreign = new CancellationTokenSource();
        foreign.Cancel();
        var operation = owner.RunAsync(async _ =>
        {
            await proceed.Task;
            throw new OperationCanceledException(useForeignToken ? foreign.Token : default);
        });
        var retirement = owner.RetireAsync().AsTask();
        proceed.SetResult(true);
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => ObserveAsync(operation));
        Assert.IsType<OperationCanceledException>(failure.InnerException);
        Assert.True(operation.IsFaulted);
        var retired = await Assert.ThrowsAsync<AggregateException>(() => ObserveAsync(retirement));
        Assert.Contains(failure, retired.InnerExceptions);
    }

    [Fact]
    public async Task Retirement_StillRunsCleanupWhenProducerAndCancellationCallbackFail()
    {
        var owner = new ConfigurationOperationLifetime();
        var proceed = NewCompletion<bool>();
        var cleanupCalls = 0;
        var operation = owner.RunAsync(async token =>
        {
            using var registration = token.Register(() => throw new InvalidOperationException("cancel callback"));
            await proceed.Task;
            throw new ApplicationException("actual producer");
        });
        var retirement = owner.RetireAsync(() => { cleanupCalls++; return ValueTask.CompletedTask; }).AsTask();
        Assert.False(retirement.IsCompleted);
        proceed.SetResult(true);
        await Assert.ThrowsAsync<ApplicationException>(() => ObserveAsync(operation));
        var failure = await Assert.ThrowsAsync<AggregateException>(() => ObserveAsync(retirement));
        Assert.Equal(1, cleanupCalls);
        Assert.Contains(failure.Flatten().InnerExceptions, exception => exception.Message == "cancel callback");
        Assert.Contains(failure.Flatten().InnerExceptions, exception => exception.Message == "actual producer");
    }

    private static TaskCompletionSource<T> NewCompletion<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static Task ObserveAsync(Task task) => task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    private static Task<T> ObserveAsync<T>(Task<T> task) => task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
}
