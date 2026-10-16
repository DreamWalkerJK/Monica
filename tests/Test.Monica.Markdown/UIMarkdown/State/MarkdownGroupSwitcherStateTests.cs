using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Monica.Markdown.Abstractions;
using Monica.Markdown.Facades;
using Monica.Markdown.Models;
using Monica.Markdown.UIMarkdown.State;
using NSubstitute;

namespace Test.Monica.Markdown.UIMarkdown.State;

public sealed class MarkdownGroupSwitcherStateTests
{
    [Fact]
    public async Task DisposeAsync_WhenRefreshIgnoresCancellation_ShouldDrainAndNotPublishOrStartCatalogRead()
    {
        var catalog = Substitute.For<IMarkdownDocumentCatalog>();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        catalog.RefreshAllAsync().Returns(_ => { started.TrySetResult(); return completion.Task; });
        var original = new List<MarkdownDocumentGroup>();
        var state = new MarkdownGroupSwitcherState(CreateFacade(catalog), original);
        var refresh = state.RefreshAsync();
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);
        var first = state.DisposeAsync().AsTask();
        var second = state.DisposeAsync().AsTask();
        first.IsCompleted.Should().BeFalse();
        second.IsCompleted.Should().BeFalse();
        completion.TrySetResult();
        await Task.WhenAll(refresh, first, second);

        state.Groups.Should().BeSameAs(original);
        state.ErrorMessage.Should().BeNull();
        _ = catalog.DidNotReceive().GetAllDocumentGroupsAsync();
        await state.RefreshAsync();
        _ = catalog.Received(1).RefreshAllAsync();
    }

    [Fact]
    public async Task RefreshAsync_WhenCatalogReadFails_ShouldRetainOriginalAndExposeActualFailure()
    {
        var catalog = Substitute.For<IMarkdownDocumentCatalog>();
        catalog.RefreshAllAsync().Returns(Task.CompletedTask);
        catalog.GetAllDocumentGroupsAsync().Returns(_ =>
            Task.FromException<List<MarkdownDocumentGroup>>(new InvalidOperationException("catalog unavailable")));
        var original = new List<MarkdownDocumentGroup>();
        await using var state = new MarkdownGroupSwitcherState(CreateFacade(catalog), original);

        await state.RefreshAsync();

        state.Groups.Should().BeSameAs(original);
        state.ErrorMessage.Should().Contain("catalog unavailable");
        state.IsRefreshing.Should().BeFalse();
    }

    private static MarkdownFacade CreateFacade(IMarkdownDocumentCatalog catalog) =>
        new(catalog, Substitute.For<IMarkdownDocumentSearcher>(), NullLogger<MarkdownFacade>.Instance);
}
