using AwesomeAssertions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Monica.Core.Localization.Models;
using Monica.Markdown.Abstractions;
using Monica.Markdown.Facades;
using Monica.Markdown.Localization;
using Monica.Markdown.Models;
using Monica.Markdown.UIMarkdown.State;
using Monica.Markdown.UIMarkdown.Dialogs;
using Monica.UI.Shared.Components.Markdown;
using Monica.Testing.Localization;
using Monica.Tool.Algorithms.Trees;
using MudBlazor;
using NSubstitute;
using Test.Monica.Markdown.Support;

namespace Test.Monica.Markdown.UIMarkdown.State;

public sealed class MarkdownViewerPageStateTests
{
    private const string MARKDOWN_LAYOUT_MODULE_PATH =
        "./_content/Monica.Markdown/js/markdown-layout.js";

    [Fact]
    public async Task InitializeAsync_WhenDisposedDuringGroupLoad_ShouldIgnoreLateResultAndRender()
    {
        var groupsStarted = NewSignal();
        var groupsCompletion = new TaskCompletionSource<List<MarkdownDocumentGroup>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var catalog = Substitute.For<IMarkdownDocumentCatalog>();
        catalog.GetAllDocumentGroupsAsync().Returns(_ =>
        {
            groupsStarted.TrySetResult();
            return groupsCompletion.Task;
        });
        var fixture = CreateFixture(catalog);

        var initialization = fixture.State.InitializeAsync();
        await groupsStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        var renderCountAtDisposal = fixture.RenderCount;

        var firstDisposal = fixture.State.DisposeAsync().AsTask();
        var secondDisposal = fixture.State.DisposeAsync().AsTask();
        firstDisposal.IsCompleted.Should().BeFalse();
        secondDisposal.IsCompleted.Should().BeFalse();
        groupsCompletion.TrySetResult([CreateGroup()]);

        await Task.WhenAll(initialization, firstDisposal, secondDisposal);

        fixture.State.Groups.Should().BeNull();
        fixture.State.IsLoading.Should().BeTrue();
        fixture.RenderCount.Should().Be(renderCountAtDisposal);
        fixture.Runtime.InvocationCount.Should().Be(0);

        fixture.State.ToggleSidebar();
        await fixture.State.OnHashChangedAsync("late-anchor");
        await fixture.State.InitializeAsync();

        fixture.State.ShowSidebar.Should().BeFalse();
        fixture.State.CurrentAnchorId.Should().BeNull();
        fixture.RenderCount.Should().Be(renderCountAtDisposal);
    }

    [Fact]
    public async Task LocationLoad_WhenDisposedDuringContentRead_ShouldNotPublishLateContentOrRender()
    {
        var document = CreateDocument();
        var group = CreateGroup(document);
        var contentStarted = NewSignal();
        var contentCompletion = new TaskCompletionSource<string>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var catalog = Substitute.For<IMarkdownDocumentCatalog>();
        catalog.GetAllDocumentGroupsAsync().Returns(Task.FromResult(new List<MarkdownDocumentGroup> { group }));
        catalog.GetDocumentTreeAsync(group.Key, Arg.Any<string?>()).Returns(Task.FromResult(group.RootNode));
        catalog.GetDocumentContentAsync(document).Returns(_ =>
        {
            contentStarted.TrySetResult();
            return contentCompletion.Task;
        });
        var fixture = CreateFixture(
            catalog,
            "/markdown-docs?group=docs&document=guide.md");

        await fixture.State.InitializeAsync();
        await contentStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        fixture.State.SelectedDocument.Should().BeSameAs(document);
        fixture.State.IsContentLoading.Should().BeTrue();
        var renderCountAtDisposal = fixture.RenderCount;

        var disposal = fixture.State.DisposeAsync().AsTask();
        disposal.IsCompleted.Should().BeFalse();
        contentCompletion.TrySetResult("late content");
        await disposal;

        fixture.State.DocumentContent.Should().BeNull();
        fixture.State.IsContentLoading.Should().BeTrue();
        fixture.State.SelectedDocument.Should().BeSameAs(document);
        fixture.RenderCount.Should().Be(renderCountAtDisposal);

        fixture.Navigation.NavigateTo(
            "/markdown-docs?group=docs&document=another.md");
        await Task.Yield();

        _ = catalog.Received(1).GetDocumentTreeAsync(group.Key, Arg.Any<string?>());
        _ = catalog.Received(1).GetDocumentContentAsync(document);
        fixture.RenderCount.Should().Be(renderCountAtDisposal);
    }

    [Fact]
    public async Task ReloadAsync_WhenGroupSnapshotChanges_ShouldReloadSameSourceAndResolveNewDocument()
    {
        var originalDocument = CreateDocument();
        var updatedDocument = CreateDocument();
        var addedDocument = CreateDocument(path: "new.md");
        var originalGroup = CreateGroup(originalDocument);
        var updatedGroup = CreateGroup(updatedDocument, additionalDocument: addedDocument);
        var catalog = Substitute.For<IMarkdownDocumentCatalog>();
        catalog.GetAllDocumentGroupsAsync().Returns(
            Task.FromResult(new List<MarkdownDocumentGroup> { originalGroup }),
            Task.FromResult(new List<MarkdownDocumentGroup> { updatedGroup }));
        catalog.GetDocumentTreeAsync("docs", Arg.Any<string?>()).Returns(
            Task.FromResult(originalGroup.RootNode), Task.FromResult(updatedGroup.RootNode));
        catalog.GetDocumentContentAsync(Arg.Any<MarkdownDocument>()).Returns(call =>
            Task.FromResult(ReferenceEquals(call.Arg<MarkdownDocument>(), originalDocument) ? "before refresh" : "fresh source"));
        var fixture = CreateFixture(catalog, "/markdown-docs?group=docs&document=guide.md");
        await using var state = fixture.State;
        await state.InitializeAsync();
        await fixture.WaitForStateAsync(() => state.DocumentContent == "before refresh");

        await state.ReloadAsync();

        state.CurrentGroup.Should().BeSameAs(updatedGroup);
        state.CurrentTree.Should().BeSameAs(updatedGroup.RootNode);
        state.SelectedDocument.Should().BeSameAs(updatedDocument);
        state.DocumentContent.Should().Be("fresh source");
        fixture.Navigation.Uri.Should().EndWith("group=docs&document=guide.md");
        await state.OnDocumentSelectedAsync(addedDocument);
        await fixture.WaitForStateAsync(() => state.SelectedDocument == addedDocument && !state.IsContentLoading);
        state.DocumentContent.Should().Be("fresh source");
        _ = catalog.Received(1).GetDocumentContentAsync(addedDocument);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OpenGroupSwitcherAsync_WhenRefreshEndsWithCancelOrSameSelection_ShouldAdoptFreshSnapshot(bool canceled)
    {
        var document = CreateDocument();
        var oldGroup = CreateGroup(document);
        var newGroup = CreateGroup(CreateDocument());
        var catalog = Substitute.For<IMarkdownDocumentCatalog>();
        catalog.GetAllDocumentGroupsAsync().Returns(
            Task.FromResult(new List<MarkdownDocumentGroup> { oldGroup }),
            Task.FromResult(new List<MarkdownDocumentGroup> { newGroup }));
        catalog.GetDocumentTreeAsync("docs", Arg.Any<string?>()).Returns(Task.FromResult(oldGroup.RootNode));
        catalog.GetDocumentContentAsync(Arg.Any<MarkdownDocument>()).Returns(Task.FromResult("content"));
        var dialogs = Substitute.For<IDialogService>();
        var reference = Substitute.For<IDialogReference>();
        reference.Result.Returns(Task.FromResult<DialogResult?>(canceled ? DialogResult.Cancel() : DialogResult.Ok("docs")));
        dialogs.ShowAsync<DocumentGroupSwitcherDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult(reference));
        var fixture = CreateFixture(catalog, "/markdown-docs?group=docs&document=guide.md", dialogs);
        await using var state = fixture.State;
        await state.InitializeAsync();
        await fixture.WaitForStateAsync(() => state.DocumentContent == "content");

        await state.OpenGroupSwitcherAsync();

        state.CurrentGroup.Should().BeSameAs(newGroup);
        state.SelectedDocument.Should().BeSameAs(newGroup.Documents[0]);
        _ = catalog.Received(2).GetAllDocumentGroupsAsync();
        _ = catalog.Received(2).GetDocumentTreeAsync("docs", Arg.Any<string?>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OpenDialogAsync_WhenTemporaryPaneIsOpen_ShouldRenderClosedPaneBeforeNativeAcquisition(bool switchGroups)
    {
        var document = CreateDocument();
        var group = CreateGroup(document);
        var catalog = Substitute.For<IMarkdownDocumentCatalog>();
        catalog.GetAllDocumentGroupsAsync().Returns(Task.FromResult(new List<MarkdownDocumentGroup> { group }));
        catalog.GetDocumentTreeAsync("docs", Arg.Any<string?>()).Returns(Task.FromResult(group.RootNode));
        catalog.GetDocumentContentAsync(document).Returns(Task.FromResult("content"));
        var dialogs = Substitute.For<IDialogService>();
        var reference = Substitute.For<IDialogReference>();
        reference.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Cancel()));
        var fixture = CreateFixture(catalog, "/markdown-docs?group=docs&document=guide.md", dialogs);
        await using var state = fixture.State;
        await state.InitializeAsync();
        await fixture.WaitForStateAsync(() => state.DocumentContent == "content");
        await state.OnDocumentHeadingsChangedAsync([new MoMarkdownHeading("first", "First", 2)]);
        await state.OnViewportChangedAsync(889, 600);
        if (switchGroups) state.SetSidebarOpen(true);
        else state.SetOutlineOpen(true);
        (state.ShowSidebar || state.ShowTableOfContents).Should().BeTrue();

        var closedPaneRendered = false;
        fixture.RenderOperation = () =>
        {
            if (!state.ShowSidebar && !state.ShowTableOfContents) closedPaneRendered = true;
            return Task.CompletedTask;
        };
        Task<IDialogReference> AcquireNativeDialog()
        {
            closedPaneRendered.Should().BeTrue();
            state.ShowSidebar.Should().BeFalse();
            state.ShowTableOfContents.Should().BeFalse();
            return Task.FromResult(reference);
        }
        dialogs.ShowAsync<DocumentGroupSwitcherDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(_ => AcquireNativeDialog());
        dialogs.ShowAsync<DocumentSearchDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(_ => AcquireNativeDialog());

        if (switchGroups) await state.OpenGroupSwitcherAsync();
        else await state.OpenDocumentSearchAsync();

        closedPaneRendered.Should().BeTrue();
        if (switchGroups)
            _ = dialogs.Received(1).ShowAsync<DocumentGroupSwitcherDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>());
        else
            _ = dialogs.Received(1).ShowAsync<DocumentSearchDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>());
    }

    [Theory]
    [InlineData("catalog")]
    [InlineData("tree")]
    [InlineData("content")]
    public async Task InitializeAsync_WhenSourceReadFails_ShouldExposeOwningFailureWithoutEmptySuccess(string stage)
    {
        var document = CreateDocument();
        var group = CreateGroup(document);
        var catalog = Substitute.For<IMarkdownDocumentCatalog>();
        catalog.GetAllDocumentGroupsAsync().Returns(_ => stage == "catalog"
            ? Task.FromException<List<MarkdownDocumentGroup>>(new InvalidOperationException("catalog failure"))
            : Task.FromResult(new List<MarkdownDocumentGroup> { group }));
        catalog.GetDocumentTreeAsync("docs", Arg.Any<string?>()).Returns(_ => stage == "tree"
            ? Task.FromException<TreeNode<MarkdownDocumentNodeData>>(new InvalidOperationException("tree failure"))
            : Task.FromResult(group.RootNode));
        catalog.GetDocumentContentAsync(document).Returns(_ =>
            Task.FromException<string>(new InvalidOperationException("content failure")));
        var fixture = CreateFixture(catalog, "/markdown-docs?group=docs&document=guide.md");
        await using var state = fixture.State;
        await state.InitializeAsync();
        await fixture.WaitForStateAsync(() => (stage switch
        {
            "catalog" => state.CatalogError,
            "tree" => state.TreeError,
            _ => state.ContentError
        }) is not null);

        state.DocumentContent.Should().BeNull();
        if (stage == "catalog")
        {
            state.Groups.Should().BeNull();
            state.CatalogError.Should().Contain("catalog failure");
        }
        else if (stage == "tree")
        {
            state.CurrentTree.Should().BeNull();
            state.TreeError.Should().Contain("tree failure");
            _ = catalog.DidNotReceive().GetDocumentContentAsync(Arg.Any<MarkdownDocument>());
        }
        else
        {
            state.SelectedDocument.Should().BeSameAs(document);
            state.ContentError.Should().Contain("content failure");
        }
    }

    [Fact]
    public async Task ReloadAsync_WhenCatalogIsSuccessfullyEmpty_ShouldClearPreviousSelectionWithoutFailure()
    {
        var document = CreateDocument();
        var group = CreateGroup(document);
        var catalog = Substitute.For<IMarkdownDocumentCatalog>();
        catalog.GetAllDocumentGroupsAsync().Returns(
            Task.FromResult(new List<MarkdownDocumentGroup> { group }), Task.FromResult(new List<MarkdownDocumentGroup>()));
        catalog.GetDocumentTreeAsync("docs", Arg.Any<string?>()).Returns(Task.FromResult(group.RootNode));
        catalog.GetDocumentContentAsync(document).Returns(Task.FromResult("content"));
        var fixture = CreateFixture(catalog, "/markdown-docs?group=docs&document=guide.md");
        await using var state = fixture.State;
        await state.InitializeAsync();
        await fixture.WaitForStateAsync(() => state.DocumentContent == "content");

        await state.ReloadAsync();

        state.Groups.Should().BeEmpty();
        state.CurrentGroup.Should().BeNull();
        state.SelectedDocument.Should().BeNull();
        state.CatalogError.Should().BeNull();
        state.TreeError.Should().BeNull();
        state.ContentError.Should().BeNull();
    }

    [Fact]
    public async Task LocationLoad_WhenEarlierContentCompletesLast_ShouldKeepLatestExactGroupAndCulture()
    {
        var first = CreateDocument(culture: "zh-CN");
        var second = CreateDocument(culture: "en-US");
        var group = CreateGroup(first, additionalDocument: second, multilingual: true);
        var firstStarted = NewSignal();
        var firstContent = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var catalog = Substitute.For<IMarkdownDocumentCatalog>();
        catalog.GetAllDocumentGroupsAsync().Returns(Task.FromResult(new List<MarkdownDocumentGroup> { group }));
        catalog.GetDocumentTreeAsync("docs", Arg.Any<string?>()).Returns(Task.FromResult(group.RootNode));
        catalog.GetDocumentContentAsync(first).Returns(_ => { firstStarted.TrySetResult(); return firstContent.Task; });
        catalog.GetDocumentContentAsync(second).Returns(Task.FromResult("English content"));
        var fixture = CreateFixture(catalog, "/markdown-docs?group=docs&document=guide.md&culture=zh-CN");
        await using var state = fixture.State;
        await state.InitializeAsync();
        await firstStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        fixture.Navigation.NavigateTo("/markdown-docs?group=docs&document=guide.md&culture=en-US");
        await fixture.WaitForStateAsync(() => state.DocumentContent == "English content");
        firstContent.TrySetResult("late Chinese content");
        await state.DisposeAsync();

        state.SelectedDocument.Should().BeSameAs(second);
        state.SelectedCulture.Should().Be("en-US");
        state.DocumentContent.Should().Be("English content");
        state.ContentError.Should().BeNull();
    }

    [Fact]
    public async Task DisposeAsync_WhenRendererDispatchIsIncomplete_ShouldDrainActualRender()
    {
        var group = CreateGroup();
        var catalog = Substitute.For<IMarkdownDocumentCatalog>();
        catalog.GetAllDocumentGroupsAsync().Returns(Task.FromResult(new List<MarkdownDocumentGroup> { group }));
        var fixture = CreateFixture(catalog);
        var renderStarted = NewSignal();
        var renderCompletion = NewSignal();
        fixture.RenderOperation = () =>
        {
            renderStarted.TrySetResult();
            return renderCompletion.Task;
        };
        var initialization = fixture.State.InitializeAsync();
        await renderStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        var retirement = fixture.State.DisposeAsync().AsTask();
        retirement.IsCompleted.Should().BeFalse();
        renderCompletion.TrySetResult();
        await Task.WhenAll(initialization, retirement);
        fixture.State.Groups.Should().BeNull();
        _ = catalog.DidNotReceive().GetAllDocumentGroupsAsync();
    }

    [Fact]
    public async Task OnViewportChangedAsync_WhenOwnerChanges_ShouldKeepSeparateWidePreferencesAndOneTemporaryPane()
    {
        var document = CreateDocument();
        var group = CreateGroup(document);
        var catalog = Substitute.For<IMarkdownDocumentCatalog>();
        catalog.GetAllDocumentGroupsAsync().Returns(Task.FromResult(new List<MarkdownDocumentGroup> { group }));
        catalog.GetDocumentTreeAsync("docs", Arg.Any<string?>()).Returns(Task.FromResult(group.RootNode));
        catalog.GetDocumentContentAsync(document).Returns(Task.FromResult("content"));
        var fixture = CreateFixture(catalog, "/markdown-docs?group=docs&document=guide.md");
        await using var state = fixture.State;
        await state.InitializeAsync();
        await fixture.WaitForStateAsync(() => state.DocumentContent == "content");
        await state.OnDocumentHeadingsChangedAsync([new MoMarkdownHeading("first", "First", 2)]);
        await state.OnViewportChangedAsync(1300, 600);
        state.CanPinSidebar.Should().BeTrue();
        state.CanPinOutline.Should().BeTrue();
        state.ShowSidebar.Should().BeTrue();
        state.ShowTableOfContents.Should().BeTrue();
        state.SetSidebarOpen(false);
        await state.OnViewportChangedAsync(889, 600);
        state.ShowSidebar.Should().BeFalse();
        state.ShowTableOfContents.Should().BeFalse();
        state.SetSidebarOpen(true);
        state.SetOutlineOpen(true);
        state.ShowSidebar.Should().BeFalse();
        state.ShowTableOfContents.Should().BeTrue();
        await state.OnViewportChangedAsync(1300, 600);
        state.ShowSidebar.Should().BeFalse();
        state.ShowTableOfContents.Should().BeTrue();
        await state.OnViewportChangedAsync(1300, 540);
        state.CanPinSidebar.Should().BeFalse();
        state.CanPinOutline.Should().BeFalse();
        state.ShowTableOfContents.Should().BeFalse();
        await state.DisposeAsync();
        await state.OnViewportChangedAsync(1300, 600);
        state.ShowSidebar.Should().BeFalse();
        state.ShowTableOfContents.Should().BeFalse();
    }

    [Fact]
    public async Task OnDocumentSelectedAsync_WhenSourceAndUrlAreUnchanged_ShouldRetainAcceptedHeadings()
    {
        var document = CreateDocument();
        var group = CreateGroup(document);
        var catalog = Substitute.For<IMarkdownDocumentCatalog>();
        catalog.GetAllDocumentGroupsAsync().Returns(Task.FromResult(new List<MarkdownDocumentGroup> { group }));
        catalog.GetDocumentTreeAsync("docs", Arg.Any<string?>()).Returns(Task.FromResult(group.RootNode));
        catalog.GetDocumentContentAsync(document).Returns(Task.FromResult("content"));
        const string canonicalLocation = "/markdown-docs?group=docs&document=guide.md&culture=zh-CN";
        var fixture = CreateFixture(catalog, canonicalLocation);
        await using var state = fixture.State;
        await state.InitializeAsync();
        await fixture.WaitForStateAsync(() => state.DocumentContent == "content" && !state.IsContentLoading);
        state.SelectedCulture.Should().Be("zh-CN");
        _ = catalog.Received(1).GetDocumentContentAsync(document);
        var heading = new MoMarkdownHeading("first", "First", 2);
        await state.OnDocumentHeadingsChangedAsync([heading]);
        await state.OnViewportChangedAsync(889, 600);
        state.SetSidebarOpen(true);

        await state.OnDocumentSelectedAsync(document);

        state.ShowSidebar.Should().BeFalse();
        state.DocumentHeadings.Should().ContainSingle().Which.Should().BeSameAs(heading);
        state.DocumentContent.Should().Be("content");
        fixture.Navigation.Uri.Should().Be("https://localhost" + canonicalLocation);
        _ = catalog.Received(1).GetDocumentContentAsync(document);
    }

    [Fact]
    public async Task OnViewportChangedAsync_WhenCatalogIsEmpty_ShouldNotOpenPanesFromLateHeadingsOrToggle()
    {
        var catalog = Substitute.For<IMarkdownDocumentCatalog>();
        catalog.GetAllDocumentGroupsAsync().Returns(Task.FromResult(new List<MarkdownDocumentGroup>()));
        var fixture = CreateFixture(catalog);
        await using var state = fixture.State;
        await state.InitializeAsync();
        await state.OnViewportChangedAsync(1300, 700);
        await state.OnDocumentHeadingsChangedAsync([new MoMarkdownHeading("late", "Late", 2)]);
        state.ToggleSidebar();
        state.ToggleTableOfContents();

        state.CanPinSidebar.Should().BeFalse();
        state.CanPinOutline.Should().BeFalse();
        state.ShowSidebar.Should().BeFalse();
        state.ShowTableOfContents.Should().BeFalse();
        state.DocumentHeadings.Should().BeEmpty();
    }

    [Fact]
    public async Task ReloadAsync_WhenOlderCatalogReadCompletesLast_ShouldKeepLatestSnapshotAndRouteContent()
    {
        var original = CreateGroup(CreateDocument());
        var obsolete = CreateGroup(CreateDocument());
        var latest = CreateGroup(CreateDocument());
        var oldReadStarted = NewSignal();
        var oldRead = new TaskCompletionSource<List<MarkdownDocumentGroup>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var catalog = Substitute.For<IMarkdownDocumentCatalog>();
        catalog.GetAllDocumentGroupsAsync().Returns(_ =>
        {
            var call = Interlocked.Increment(ref calls);
            if (call == 2) { oldReadStarted.TrySetResult(); return oldRead.Task; }
            return Task.FromResult(new List<MarkdownDocumentGroup> { call == 1 ? original : latest });
        });
        catalog.GetDocumentTreeAsync("docs", Arg.Any<string?>()).Returns(Task.FromResult(original.RootNode));
        catalog.GetDocumentContentAsync(Arg.Any<MarkdownDocument>()).Returns(Task.FromResult("content"));
        var fixture = CreateFixture(catalog, "/markdown-docs?group=docs&document=guide.md");
        await using var state = fixture.State;
        await state.InitializeAsync();
        await fixture.WaitForStateAsync(() => state.DocumentContent == "content");

        var older = state.ReloadAsync();
        await oldReadStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        await state.ReloadAsync();
        oldRead.TrySetResult([obsolete]);
        await older;

        state.CurrentGroup.Should().BeSameAs(latest);
        state.SelectedDocument.Should().BeSameAs(latest.Documents[0]);
        state.DocumentContent.Should().Be("content");
        state.IsLoading.Should().BeFalse();
        state.CatalogError.Should().BeNull();
    }

    private static ViewerStateFixture CreateFixture(
        IMarkdownDocumentCatalog catalog,
        string initialRelativeUri = "/markdown-docs",
        IDialogService? dialogs = null)
    {
        var eventLog = new JsInteropEventLog();
        var import = new ControlledJsOperation(eventLog, "runtime.import");
        var runtime = new ControlledJsRuntime(
            eventLog,
            import,
            MARKDOWN_LAYOUT_MODULE_PATH);
        var navigation = new TestNavigationManager(initialRelativeUri);
        var facade = new MarkdownFacade(
            catalog,
            Substitute.For<IMarkdownDocumentSearcher>(),
            NullLogger<MarkdownFacade>.Instance);
        var fixture = new ViewerStateFixture(runtime, navigation);
        fixture.State = new MarkdownViewerPageState(
            facade,
            dialogs ?? Substitute.For<IDialogService>(),
            navigation,
            Substitute.For<ISnackbar>(),
            runtime,
            new EchoStringLocalizer<MarkdownResource>(),
            LocalizationProfile.Create(
                "zh-CN",
                ["zh-CN", "en-US"],
                new Dictionary<string, string>
                {
                    ["zh-CN"] = "简体中文",
                    ["en-US"] = "English"
                },
                ".AspNetCore.Culture"));
        fixture.State.Attach(fixture.RequestRenderAsync);
        return fixture;
    }

    private static MarkdownDocumentGroup CreateGroup(
        MarkdownDocument? document = null,
        MarkdownDocument? additionalDocument = null,
        bool multilingual = false)
    {
        var root = new TreeNode<MarkdownDocumentNodeData>(
            new MarkdownDocumentNodeData(string.Empty, IsDocument: false));

        return new MarkdownDocumentGroup
        {
            Key = "docs",
            Title = "Documentation",
            BasePath = "/docs",
            IsValid = true,
            DocumentCount = document is null ? 0 : additionalDocument is null ? 1 : 2,
            RootNode = root,
            Documents = document is null ? [] : additionalDocument is null ? [document] : [document, additionalDocument],
            IsMultilingual = multilingual,
            Languages = multilingual ? [new("zh-CN", "Chinese", 1), new("en-US", "English", 1)] : [],
            LanguageDocuments = multilingual ? new Dictionary<string, IReadOnlyList<MarkdownDocument>>
            {
                ["zh-CN"] = [document!], ["en-US"] = [additionalDocument!]
            } : new Dictionary<string, IReadOnlyList<MarkdownDocument>>()
        };
    }

    private static MarkdownDocument CreateDocument(string path = "guide.md", string? culture = null)
    {
        return new MarkdownDocument
        {
            GroupKey = "docs",
            Title = "Guide",
            NavigationTitle = "Guide",
            FilePath = "/docs/" + path,
            RelativePath = culture is null ? path : culture + "/" + path,
            NavigationRelativePath = path,
            Culture = culture,
            Level = 0,
            FileSize = 42,
            LastModifiedUtc = DateTime.UnixEpoch
        };
    }

    private static TaskCompletionSource NewSignal()
    {
        return new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class ViewerStateFixture(
        ControlledJsRuntime runtime,
        TestNavigationManager navigation)
    {
        private int _renderCount;

        internal MarkdownViewerPageState State { get; set; } = null!;

        internal ControlledJsRuntime Runtime { get; } = runtime;

        internal TestNavigationManager Navigation { get; } = navigation;

        internal int RenderCount => Volatile.Read(ref _renderCount);
        internal Func<Task>? RenderOperation { get; set; }
        private event Action? StateChanged;

        internal async Task WaitForStateAsync(Func<bool> predicate)
        {
            var completion = NewSignal();
            void Changed() { if (predicate()) completion.TrySetResult(); }
            StateChanged += Changed;
            try
            {
                Changed();
                await completion.Task.WaitAsync(TestContext.Current.CancellationToken);
            }
            finally { StateChanged -= Changed; }
        }

        internal Task RequestRenderAsync()
        {
            Interlocked.Increment(ref _renderCount);
            StateChanged?.Invoke();
            return RenderOperation?.Invoke() ?? Task.CompletedTask;
        }
    }
}
