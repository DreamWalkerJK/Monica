using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Monica.Markdown.Abstractions;
using Monica.Markdown.Facades;
using Monica.Markdown.Models;
using Monica.Markdown.UIMarkdown.State;
using Monica.Modules;
using NSubstitute;

namespace Test.Monica.Markdown.UIMarkdown.State;

public sealed class MarkdownDocumentSearchStateTests
{
    [Fact]
    public async Task SearchAsync_WhenDisposedDuringProviderCall_ShouldIgnoreLateResultAndRemainIdempotent()
    {
        var searchStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var searchCompletion = new TaskCompletionSource<IReadOnlyList<MarkdownDocumentSearchResult>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var searcher = Substitute.For<IMarkdownDocumentSearcher>();
        searcher.SearchAsync(
                Arg.Any<MarkdownDocumentSearchRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                searchStarted.TrySetResult();
                return searchCompletion.Task;
            });
        var state = new MarkdownDocumentSearchState(
            new MarkdownFacade(
                Substitute.For<IMarkdownDocumentCatalog>(),
                searcher,
                NullLogger<MarkdownFacade>.Instance),
            Options.Create(new ModuleMarkdownOption()),
            Options.Create(new ModuleMarkdownUIOption()),
            currentGroup: null,
            currentCulture: null);

        var search = state.OnQueryDebouncedAsync("lifetime");
        await searchStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        state.IsSearching.Should().BeTrue();

        var firstDisposal = state.DisposeAsync().AsTask();
        var secondDisposal = state.DisposeAsync().AsTask();
        firstDisposal.IsCompleted.Should().BeFalse();
        secondDisposal.IsCompleted.Should().BeFalse();
        state.IsSearching.Should().BeFalse();
        searchCompletion.TrySetResult([CreateResult()]);

        await Task.WhenAll(search, firstDisposal, secondDisposal);

        state.Query.Should().Be("lifetime");
        state.Results.Should().BeEmpty();
        state.IsSearching.Should().BeFalse();

        state.OnQueryChanged("late query");
        await state.OnIncludeAllKnowledgeBasesChangedAsync(true);

        state.Query.Should().Be("lifetime");
        state.IncludeAllKnowledgeBases.Should().BeFalse();
        state.Results.Should().BeEmpty();
    }

    private static MarkdownDocumentSearchResult CreateResult()
    {
        return new MarkdownDocumentSearchResult(
            "docs",
            "Documentation",
            "guide.md",
            "Guide",
            null,
            "Lifecycle",
            "lifetime result",
            [],
            "lifecycle",
            new MarkdownSearchLocator(
                "lifecycle",
                "lifetime",
                string.Empty,
                string.Empty,
                HeadingLevel: 2),
            Score: 1);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("zh-CN")]
    public async Task UpdateContext_WhenOriginalProviderFinishesLate_ShouldDiscardPreviousSnapshotAndRetainOwnership(string nextCulture)
    {
        var catalog = Substitute.For<IMarkdownDocumentCatalog>();
        var searcher = Substitute.For<IMarkdownDocumentSearcher>();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completion = new TaskCompletionSource<IReadOnlyList<MarkdownDocumentSearchResult>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken originalToken = default;
        searcher.SearchAsync(Arg.Any<MarkdownDocumentSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                originalToken = call.Arg<CancellationToken>();
                started.TrySetResult();
                return completion.Task;
            });
        var originalGroup = new MarkdownDocumentGroup
        {
            Key = "original", Title = "Original", BasePath = "/original", IsValid = true, DocumentCount = 0,
            RootNode = new(new MarkdownDocumentNodeData(string.Empty, IsDocument: false))
        };
        var state = new MarkdownDocumentSearchState(
            new MarkdownFacade(catalog, searcher, NullLogger<MarkdownFacade>.Instance),
            Options.Create(new ModuleMarkdownOption()), Options.Create(new ModuleMarkdownUIOption()), originalGroup, "zh-CN");
        var search = state.OnQueryDebouncedAsync("lifetime");
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);
        // A new group snapshot invalidates results even when the requested culture remains the same.
        var nextGroup = nextCulture == "en-US" ? originalGroup : new MarkdownDocumentGroup
        {
            Key = "other", Title = "Other group", BasePath = "/other", IsValid = true, DocumentCount = 0,
            RootNode = new(new MarkdownDocumentNodeData(string.Empty, IsDocument: false))
        };
        state.UpdateContext(nextGroup, nextCulture);

        originalToken.IsCancellationRequested.Should().BeTrue();
        state.IsSearching.Should().BeFalse();
        var retirement = state.DisposeAsync().AsTask();
        retirement.IsCompleted.Should().BeFalse();
        completion.TrySetResult([CreateResult()]);
        await Task.WhenAll(search, retirement);
        state.CurrentGroup.Should().BeSameAs(nextGroup);
        state.CurrentCulture.Should().Be(nextCulture);
        state.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WhenQueriesCompleteInReverseOrder_ShouldKeepOnlyLatestResults()
    {
        var searcher = Substitute.For<IMarkdownDocumentSearcher>();
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstCompletion = new TaskCompletionSource<IReadOnlyList<MarkdownDocumentSearchResult>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var latest = CreateResult() with { DocumentTitle = "Latest result" };
        searcher.SearchAsync(Arg.Any<MarkdownDocumentSearchRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            if (call.Arg<MarkdownDocumentSearchRequest>().Query == "first")
            {
                firstStarted.TrySetResult();
                return firstCompletion.Task;
            }
            return Task.FromResult<IReadOnlyList<MarkdownDocumentSearchResult>>([latest]);
        });
        await using var state = new MarkdownDocumentSearchState(
            new MarkdownFacade(Substitute.For<IMarkdownDocumentCatalog>(), searcher, NullLogger<MarkdownFacade>.Instance),
            Options.Create(new ModuleMarkdownOption()), Options.Create(new ModuleMarkdownUIOption()), null, "zh-CN");
        var first = state.OnQueryDebouncedAsync("first");
        await firstStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        state.OnQueryChanged("latest");
        await state.OnQueryDebouncedAsync("latest");
        firstCompletion.TrySetResult([CreateResult()]);
        await first;

        state.Results.Should().ContainSingle().Which.Should().BeSameAs(latest);
        state.Query.Should().Be("latest");
        state.IsSearching.Should().BeFalse();
    }
}
