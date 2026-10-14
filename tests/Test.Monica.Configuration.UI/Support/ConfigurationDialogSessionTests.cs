using Monica.Configuration.UI.Support;
using MudBlazor;
using NSubstitute;

namespace Test.Monica.Configuration.UI.Support;

public sealed class ConfigurationDialogSessionTests
{
    [Fact]
    public async Task DuplicateOpens_JoinTheActualNativeResult()
    {
        var boundary = new DialogBoundary();
        var calls = 0;
        var session = new ConfigurationDialogSession(() => { calls++; return Task.FromResult(boundary.Reference); });
        var operation = session.OpenAsync();
        Assert.Same(operation, session.OpenAsync());
        Assert.Equal(1, calls);
        var actual = DialogResult.Ok("persisted");
        boundary.Result.SetResult(actual);
        Assert.Same(actual, await ObserveAsync(operation));
        await ObserveAsync(session.DisposeAsync().AsTask());
        Assert.Equal(0, boundary.CloseCalls);
    }

    [Fact]
    public async Task Retirement_ClosesLateReferenceAndWaitsForActualResult()
    {
        var produced = NewCompletion<IDialogReference>();
        var closed = NewCompletion<bool>();
        var boundary = new DialogBoundary(() => closed.TrySetResult(true));
        var session = new ConfigurationDialogSession(() => produced.Task);
        var operation = session.OpenAsync();
        var retirement = session.DisposeAsync().AsTask();
        Assert.Same(retirement, session.DisposeAsync().AsTask());
        produced.SetResult(boundary.Reference);
        await ObserveAsync(closed.Task);
        Assert.True(boundary.CloseResult!.Canceled);
        Assert.False(operation.IsCompleted);
        Assert.False(retirement.IsCompleted);
        var actual = DialogResult.Cancel();
        boundary.Result.SetResult(actual);
        Assert.Same(actual, await ObserveAsync(operation));
        await ObserveAsync(retirement);
        Assert.Equal(1, boundary.CloseCalls);
    }

    [Fact]
    public async Task CloseFailure_DoesNotSkipActualResultDrain()
    {
        var boundary = new DialogBoundary(() => throw new InvalidOperationException("native close"));
        var session = new ConfigurationDialogSession(() => Task.FromResult(boundary.Reference));
        var operation = session.OpenAsync();
        var retirement = session.DisposeAsync().AsTask();
        Assert.False(retirement.IsCompleted);
        boundary.Result.SetResult(DialogResult.Cancel());
        var operationFailure = await Assert.ThrowsAsync<InvalidOperationException>(() => ObserveAsync(operation));
        Assert.Equal("native close", operationFailure.Message);
        var failure = await Assert.ThrowsAsync<AggregateException>(() => ObserveAsync(retirement));
        Assert.Contains(failure.InnerExceptions, exception => exception.Message == "native close");
        Assert.Same(retirement, session.DisposeAsync().AsTask());
    }

    [Fact]
    public async Task NativeClose_CanReenterSharedRetirement()
    {
        ConfigurationDialogSession? session = null;
        Task? callbackRetirement = null;
        var boundary = new DialogBoundary(() => callbackRetirement = session!.DisposeAsync().AsTask());
        session = new ConfigurationDialogSession(() => Task.FromResult(boundary.Reference));
        var operation = session.OpenAsync();
        var retirement = session.DisposeAsync().AsTask();
        Assert.Same(retirement, callbackRetirement);
        Assert.Equal(1, boundary.CloseCalls);
        boundary.Result.SetResult(DialogResult.Cancel());
        await ObserveAsync(Task.WhenAll(operation, retirement));
    }

    [Fact]
    public async Task ForeignNativeResultCancellation_IsNotHiddenByOwnerRetirement()
    {
        using var ownerCancellation = new CancellationTokenSource();
        var boundary = new DialogBoundary();
        var session = new ConfigurationDialogSession(() => Task.FromResult(boundary.Reference), ownerCancellation.Token);
        var operation = session.OpenAsync();
        ownerCancellation.Cancel();
        var retirement = session.DisposeAsync().AsTask();
        boundary.Result.SetException(new OperationCanceledException());
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => ObserveAsync(operation));
        Assert.IsType<OperationCanceledException>(failure.InnerException);
        Assert.True(operation.IsFaulted);
        var retired = await Assert.ThrowsAsync<AggregateException>(() => ObserveAsync(retirement));
        Assert.Contains(failure, retired.InnerExceptions);
    }

    private static TaskCompletionSource<T> NewCompletion<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static Task ObserveAsync(Task task) => task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    private static Task<T> ObserveAsync<T>(Task<T> task) => task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

    private sealed class DialogBoundary
    {
        internal DialogBoundary(Action? onClose = null)
        {
            Reference.Result.Returns(Result.Task);
            Reference.When(reference => reference.Close(Arg.Any<DialogResult?>())).Do(call =>
            {
                CloseCalls++;
                CloseResult = call.Arg<DialogResult?>();
                onClose?.Invoke();
            });
        }
        internal IDialogReference Reference { get; } = Substitute.For<IDialogReference>();
        internal TaskCompletionSource<DialogResult?> Result { get; } = NewCompletion<DialogResult?>();
        internal int CloseCalls { get; private set; }
        internal DialogResult? CloseResult { get; private set; }
    }
}
