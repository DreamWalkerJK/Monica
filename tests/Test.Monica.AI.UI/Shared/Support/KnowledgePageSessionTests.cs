using AwesomeAssertions;
using Monica.AI.UI.Shared.Support;
using MudBlazor;
using NSubstitute;

namespace Test.Monica.AI.UI.Shared.Support;

/// <summary>Controlled non-rendering interleavings for page-owned producers and renderer publication.</summary>
public sealed class KnowledgePageSessionTests
{
    [Fact]
    public async Task RunAsync_WhenSelectionReturnsToSameId_ShouldRejectOldGeneration()
    {
        var id = "first";
        var session = Create(() => id);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var applied = false;
        var read = session.RunAsync(async () =>
        {
            await release.Task;
            session.CheckCurrent();
            applied = true;
        });

        id = "other";
        session.AdvanceSelection();
        id = "first";
        session.AdvanceSelection();
        release.SetResult();
        await read;
        applied.Should().BeFalse();
        await session.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_WhenProducerIgnoresCancellation_ShouldDrainActualReadAndSuppressAdoption()
    {
        var session = Create(() => "first");
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var applied = false;
        var read = session.RunAsync(async () =>
        {
            await release.Task;
            session.CheckCurrent();
            applied = true;
        });

        var retirement = session.DisposeAsync().AsTask();
        var repeated = session.DisposeAsync().AsTask();
        repeated.Should().BeSameAs(retirement);
        session.IsRetired.Should().BeTrue();
        retirement.IsCompleted.Should().BeFalse();
        release.SetResult();
        await Task.WhenAll(read, retirement);
        applied.Should().BeFalse();
        var admitted = false;
        await session.RunAsync(() => { admitted = true; return Task.CompletedTask; });
        admitted.Should().BeFalse();
    }

    [Fact]
    public async Task DisposeAsync_WhenRendererDispatchIsPending_ShouldDrainItBeforeReturning()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var applied = false;
        var session = new KnowledgePageSession(() => "first", Substitute.For<IDialogService>(), async action =>
        {
            entered.SetResult();
            await release.Task;
            action();
        });
        var publication = session.PublishAsync(() => applied = true);
        await entered.Task;
        var retirement = session.DisposeAsync().AsTask();

        retirement.IsCompleted.Should().BeFalse();
        release.SetResult();
        await Task.WhenAll(publication, retirement);
        applied.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_WhenLiveProducerCancels_ShouldKeepUnexpectedCancellationObservable()
    {
        var session = Create(() => "first");
        Func<Task> operation = () => session.RunAsync(() => Task.FromCanceled(new CancellationToken(true)));

        await operation.Should().ThrowAsync<OperationCanceledException>();
        session.IsRetired.Should().BeFalse();
        await session.DisposeAsync();
    }

    private static KnowledgePageSession Create(Func<string?> selectedId)
        => new(selectedId, Substitute.For<IDialogService>(), action => { action(); return Task.CompletedTask; });
}
