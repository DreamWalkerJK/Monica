using AwesomeAssertions;
using Monica.AI.UI.Shared.Support;

namespace Test.Monica.AI.UI.Shared.Support;

/// <summary>Controlled non-rendering checks for deferred native focus admission and retirement.</summary>
public sealed class KnowledgeDossierFocusTests
{
    [Fact]
    public async Task AfterRenderAsync_WhenSelectionReturnsToSameId_ShouldRejectOldGeneration()
    {
        await using var focus = new KnowledgeDossierFocus();
        var selectedId = "first";
        var generation = 1L;
        var focusCount = 0;
        focus.Request(selectedId, generation);

        selectedId = "other";
        generation++;
        selectedId = "first";
        generation++;
        await focus.AfterRenderAsync(IsCurrent, ApplyFocus);

        focusCount.Should().Be(0);
        focus.Request(selectedId, generation);
        await focus.AfterRenderAsync(IsCurrent, ApplyFocus);
        focusCount.Should().Be(1);
        await focus.AfterRenderAsync(IsCurrent, ApplyFocus);
        focusCount.Should().Be(1);

        bool IsCurrent(string id, long requestGeneration)
            => id == selectedId && requestGeneration == generation;
        Task ApplyFocus() { focusCount++; return Task.CompletedTask; }
    }

    [Fact]
    public async Task DisposeAsync_WhenFocusIsPending_ShouldRejectPendingAndLaterRequests()
    {
        var focus = new KnowledgeDossierFocus();
        var validationCount = 0;
        var focusCount = 0;
        focus.Request("first", 1);

        await focus.DisposeAsync();
        await focus.AfterRenderAsync(IsCurrent, ApplyFocus);
        focus.Request("first", 2);
        await focus.AfterRenderAsync(IsCurrent, ApplyFocus);

        validationCount.Should().Be(0);
        focusCount.Should().Be(0);
        await focus.DisposeAsync();

        bool IsCurrent(string _, long __) { validationCount++; return true; }
        Task ApplyFocus() { focusCount++; return Task.CompletedTask; }
    }

    [Fact]
    public async Task DisposeAsync_WhenAdmittedFocusCannotBeCancelled_ShouldDrainActualTask()
    {
        var focus = new KnowledgeDossierFocus();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var focusCompleted = false;
        focus.Request("first", 1);
        var publication = focus.AfterRenderAsync((id, generation) => id == "first" && generation == 1, async () =>
        {
            entered.SetResult();
            await release.Task;
            focusCompleted = true;
        });
        await entered.Task;

        var retirement = focus.DisposeAsync().AsTask();
        var repeated = focus.DisposeAsync().AsTask();
        try
        {
            repeated.Should().BeSameAs(retirement);
            retirement.IsCompleted.Should().BeFalse();
            publication.IsCompleted.Should().BeFalse();
            focusCompleted.Should().BeFalse();
        }
        finally
        {
            release.TrySetResult();
            await Task.WhenAll(publication, retirement);
        }

        focusCompleted.Should().BeTrue();
    }
}
