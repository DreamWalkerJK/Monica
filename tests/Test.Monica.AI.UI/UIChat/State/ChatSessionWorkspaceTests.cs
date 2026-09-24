using AwesomeAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Monica.AI.Chat.Abstractions;
using Monica.AI.Chat.Facades;
using Monica.AI.Chat.Models;
using Monica.AI.Models;
using Monica.AI.UI.UIChat.Models;
using Monica.AI.UI.UIChat.State;
using Monica.Core.Modularity.Abstractions;
using Monica.Core.Results;
using Monica.Modules;
using Monica.Testing.Hosting;

namespace Test.Monica.AI.UI.UIChat.State;

public sealed class ChatSessionWorkspaceTests
{
    private static readonly ChatHistoryPartition PARTITION = new("workspace-test");

    [Fact]
    public async Task InitializeAsync_WhenCurrentSnapshotExists_ShouldRestoreTranscriptWithoutRuntime()
    {
        await using var fixture = await WorkspaceFixture.CreateAsync();

        await fixture.Workspace.InitializeAsync(TestContext.Current.CancellationToken);

        fixture.Workspace.IsLoading.Should().BeFalse();
        fixture.Workspace.Sessions.Should().ContainSingle();
        fixture.Workspace.CurrentSessionId.Should().Be("session-1");
        fixture.Workspace.CurrentSession.Should().NotBeNull();
        fixture.Workspace.CurrentSession!.Messages.Should().ContainSingle(message => message.Content == "persisted message");
        fixture.Workspace.CurrentSession.IsRuntimeActive.Should().BeFalse();
    }

    [Fact]
    public async Task ClearAsync_WhenRevisionWasAdvancedElsewhere_ShouldKeepLocalHistoryAndWarn()
    {
        await using var fixture = await WorkspaceFixture.CreateAsync();
        await fixture.Workspace.InitializeAsync(TestContext.Current.CancellationToken);
        var warnings = new List<ChatHistoryWorkspaceWarning>();
        fixture.Workspace.WarningRaised += warnings.Add;
        _ = await fixture.Provider.SetCurrentSessionAsync(
            PARTITION,
            "session-1",
            fixture.Workspace.Revision,
            TestContext.Current.CancellationToken);

        var cleared = await fixture.Workspace.ClearAsync(TestContext.Current.CancellationToken);

        cleared.Should().BeFalse();
        fixture.Workspace.Sessions.Should().ContainSingle();
        warnings.Should().ContainSingle(item => item.Kind == ChatHistoryWorkspaceWarningKind.RevisionConflict);
    }

    [Fact]
    public async Task SelectRemoveAndClearAsync_WhenRevisionsMatch_ShouldPersistWorkspaceLifecycle()
    {
        await using var fixture = await WorkspaceFixture.CreateAsync(includeSecondSession: true);
        var ct = TestContext.Current.CancellationToken;
        await fixture.Workspace.InitializeAsync(ct);

        var selected = await fixture.Workspace.SelectSessionAsync("session-2", ct);
        var removed = await fixture.Workspace.RemoveSessionAsync("session-2", ct);
        var cleared = await fixture.Workspace.ClearAsync(ct);
        var catalog = await fixture.Provider.GetCatalogAsync(PARTITION, ct);

        selected.Should().NotBeNull();
        selected!.IsRuntimeActive.Should().BeFalse();
        removed.Should().BeTrue();
        cleared.Should().BeTrue();
        fixture.Workspace.Sessions.Should().BeEmpty();
        fixture.Workspace.CurrentSession.Should().BeNull();
        catalog.Sessions.Should().BeEmpty();
        catalog.CurrentSessionId.Should().BeNull();
    }

    [Fact]
    public async Task SaveSessionAsync_WhenConversationWasDeletedElsewhere_ShouldKeepLocalDraftUntilReload()
    {
        await using var fixture = await WorkspaceFixture.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Workspace.InitializeAsync(ct);
        var local = fixture.Workspace.CurrentSession!;
        var catalog = await fixture.Provider.GetCatalogAsync(PARTITION, ct);
        _ = await fixture.Provider.ClearAsync(PARTITION, catalog.Revision, ct);

        var saved = await fixture.Workspace.SaveSessionAsync(local, ct);
        var persistedCatalog = await fixture.Provider.GetCatalogAsync(PARTITION, ct);

        saved.Should().BeFalse();
        fixture.Workspace.IsConflicted.Should().BeTrue();
        fixture.Workspace.CurrentSession.Should().BeSameAs(local);
        persistedCatalog.Sessions.Should().BeEmpty();
        await fixture.Workspace.ReloadAsync(ct);
        fixture.Workspace.IsConflicted.Should().BeFalse();
        fixture.Workspace.CurrentSession.Should().BeNull();
        fixture.Workspace.Sessions.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveSessionAsync_WhenOnlySelectionChangedElsewhere_ShouldRebaseUnchangedConversation()
    {
        await using var fixture = await WorkspaceFixture.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Workspace.InitializeAsync(ct);
        await fixture.Provider.SetCurrentSessionAsync(PARTITION, null, fixture.Workspace.Revision, ct);

        var saved = await fixture.Workspace.SaveSessionAsync(fixture.Workspace.CurrentSession!, ct);

        saved.Should().BeTrue();
        fixture.Workspace.IsConflicted.Should().BeFalse();
        (await fixture.Provider.GetCatalogAsync(PARTITION, ct)).Revision.Should().Be(fixture.Workspace.Revision);
    }

    [Fact]
    public async Task SaveSessionAsync_WhenSameConversationChangedElsewhere_ShouldRequireExplicitReload()
    {
        await using var fixture = await WorkspaceFixture.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Workspace.InitializeAsync(ct);
        var local = fixture.Workspace.CurrentSession!;
        var stored = (await fixture.Provider.LoadSessionAsync(PARTITION, "session-1", ct))!;
        await fixture.Provider.SaveSessionAsync(PARTITION, stored with { Title = "Changed in another tab" }, fixture.Workspace.Revision, ct);

        (await fixture.Workspace.SaveSessionAsync(local, ct)).Should().BeFalse();
        fixture.Workspace.IsConflicted.Should().BeTrue();
        local.Title.Should().Be("Persisted session-1");
        await fixture.Workspace.ReloadAsync(ct);
        fixture.Workspace.CurrentSession!.Title.Should().Be("Changed in another tab");
        fixture.Workspace.IsConflicted.Should().BeFalse();
    }

    [Fact]
    public async Task PinThenSave_ShouldRetainOrganizationAcrossCheckpointAndReload()
    {
        await using var fixture = await WorkspaceFixture.CreateAsync(includeSecondSession: true);
        var ct = TestContext.Current.CancellationToken;
        await fixture.Workspace.InitializeAsync(ct);

        (await fixture.Workspace.SetPinnedAsync("session-1", true, ct)).Should().BeTrue();
        (await fixture.Workspace.SaveSessionAsync(fixture.Workspace.CurrentSession!, ct)).Should().BeTrue();
        await fixture.Workspace.ReloadAsync(ct);

        fixture.Workspace.Sessions[0].SessionId.Should().Be("session-1");
        fixture.Workspace.Sessions[0].IsPinned.Should().BeTrue();
    }

    [Fact]
    public async Task ArchiveCurrent_ShouldKeepTranscriptAndSelectNextActiveConversationAfterReload()
    {
        await using var fixture = await WorkspaceFixture.CreateAsync(includeSecondSession: true);
        var ct = TestContext.Current.CancellationToken;
        await fixture.Workspace.InitializeAsync(ct);
        await fixture.Workspace.SetPinnedAsync("session-1", true, ct);

        (await fixture.Workspace.SetArchivedAsync(["session-1"], true, ct)).Should().BeTrue();

        fixture.Workspace.CurrentSessionId.Should().Be("session-2");
        fixture.Workspace.ArchivedSessions.Should().ContainSingle().Which.IsPinned.Should().BeFalse();
        fixture.Workspace.GetLoadedSession("session-1").Should().BeNull();
        (await fixture.Provider.LoadSessionAsync(PARTITION, "session-1", ct))!.Turns.Should().ContainSingle();
        await fixture.Workspace.ReloadAsync(ct);
        fixture.Workspace.CurrentSessionId.Should().Be("session-2");
        fixture.Workspace.Sessions.Should().ContainSingle().Which.SessionId.Should().Be("session-2");
        (await fixture.Workspace.SelectSessionAsync("session-1", ct)).Should().BeNull();
    }

    [Fact]
    public async Task RestoreAndDeleteArchived_ShouldApplyOnlyTheSelectedConversations()
    {
        await using var fixture = await WorkspaceFixture.CreateAsync(includeSecondSession: true);
        var ct = TestContext.Current.CancellationToken;
        await fixture.Workspace.InitializeAsync(ct);
        await fixture.Workspace.SetArchivedAsync(["session-1", "session-2"], true, ct);
        fixture.Workspace.CurrentSession.Should().BeNull();

        (await fixture.Workspace.SetArchivedAsync(["session-1"], false, ct)).Should().BeTrue();
        (await fixture.Workspace.DeleteArchivedSessionsAsync(["session-2"], ct)).Should().BeTrue();

        fixture.Workspace.Sessions.Should().ContainSingle().Which.SessionId.Should().Be("session-1");
        fixture.Workspace.ArchivedSessions.Should().BeEmpty();
        fixture.Workspace.CurrentSession.Should().BeNull();
        (await fixture.Provider.LoadSessionAsync(PARTITION, "session-1", ct)).Should().NotBeNull();
        (await fixture.Provider.LoadSessionAsync(PARTITION, "session-2", ct)).Should().BeNull();
    }

    [Fact]
    public async Task Save_WhenAnotherTabArchivesConversation_ShouldRequireReloadWithoutRestoringIt()
    {
        await using var fixture = await WorkspaceFixture.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Workspace.InitializeAsync(ct);
        var local = fixture.Workspace.CurrentSession!;
        await fixture.Provider.SetArchivedAsync(PARTITION, ["session-1"], true, fixture.Workspace.Revision, ct);

        (await fixture.Workspace.SaveSessionAsync(local, ct)).Should().BeFalse();
        fixture.Workspace.IsConflicted.Should().BeTrue();
        fixture.Workspace.CurrentSession.Should().BeSameAs(local);
        (await fixture.Provider.GetCatalogAsync(PARTITION, ct)).Sessions.Single().ArchivedAt.Should().NotBeNull();
        await fixture.Workspace.ReloadAsync(ct);
        fixture.Workspace.CurrentSession.Should().BeNull();
        fixture.Workspace.Sessions.Should().BeEmpty();
        fixture.Workspace.ArchivedSessions.Should().ContainSingle();
    }

    [Fact]
    public async Task Save_WhenAnotherTabPinsConversation_ShouldSafelyRebaseAndRetainPin()
    {
        await using var fixture = await WorkspaceFixture.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Workspace.InitializeAsync(ct);
        await fixture.Provider.SetPinnedAsync(PARTITION, "session-1", true, fixture.Workspace.Revision, ct);

        (await fixture.Workspace.SaveSessionAsync(fixture.Workspace.CurrentSession!, ct)).Should().BeTrue();

        fixture.Workspace.IsConflicted.Should().BeFalse();
        fixture.Workspace.Sessions.Single().IsPinned.Should().BeTrue();
    }

    [Fact]
    public async Task Archive_WhenCatalogChangedElsewhere_ShouldLeaveSelectionAndLocalRowsUntouched()
    {
        await using var fixture = await WorkspaceFixture.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Workspace.InitializeAsync(ct);
        await fixture.Provider.SetPinnedAsync(PARTITION, "session-1", true, fixture.Workspace.Revision, ct);

        (await fixture.Workspace.SetArchivedAsync(["session-1"], true, ct)).Should().BeFalse();

        fixture.Workspace.IsConflicted.Should().BeTrue();
        fixture.Workspace.CurrentSessionId.Should().Be("session-1");
        fixture.Workspace.Sessions.Should().ContainSingle();
        fixture.Workspace.ArchivedSessions.Should().BeEmpty();
    }

    private sealed class WorkspaceFixture : IAsyncDisposable
    {
        private readonly MonicaTestApplication _application;
        private readonly MonicaTestScope _scope;
        private string? _directory;

        private WorkspaceFixture(
            MonicaTestApplication application,
            MonicaTestScope scope,
            IChatHistoryProvider provider,
            ChatSessionWorkspace workspace)
        {
            _application = application;
            _scope = scope;
            Provider = provider;
            Workspace = workspace;
            HistoryFacade = scope.Resolve<ChatHistoryFacade>();
        }

        internal IChatHistoryProvider Provider { get; }

        internal ChatSessionWorkspace Workspace { get; }

        internal ChatHistoryFacade HistoryFacade { get; }

        internal static async Task<WorkspaceFixture> CreateAsync(bool includeSecondSession = false)
        {
            var directory = Path.Combine(Path.GetTempPath(), "monica-ai-ui-tests", Guid.NewGuid().ToString("N"));
            var ct = TestContext.Current.CancellationToken;
            var application = await new WorkspaceApplicationFactory(directory).CreateAsync(cancellationToken: ct);
            var scope = application.CreateScope(ct);
            var provider = scope.Resolve<IChatHistoryProvider>();
            var saved = await provider.SaveSessionAsync(PARTITION, CreateSnapshot(), 0, ct);
            if (includeSecondSession)
            {
                saved = await provider.SaveSessionAsync(
                    PARTITION,
                    CreateSnapshot("session-2", "second persisted message"),
                    saved.Revision,
                    ct);
            }

            _ = await provider.SetCurrentSessionAsync(PARTITION, "session-1", saved.Revision, ct);

            var workspace = new ChatSessionWorkspace(scope.Resolve<ChatHistoryFacade>());
            return new WorkspaceFixture(application, scope, provider, workspace) { _directory = directory };
        }

        public async ValueTask DisposeAsync()
        {
            await Workspace.DisposeAsync();
            await _scope.DisposeAsync();
            await _application.DisposeAsync();
            if (_directory is { } directory)
            {
                var parent = Path.Combine(Path.GetTempPath(), "monica-ai-ui-tests");
                var relative = Path.GetRelativePath(parent, Path.GetFullPath(directory));
                if (relative.StartsWith("..", StringComparison.Ordinal) || relative == "." || Path.IsPathRooted(relative))
                    throw new InvalidOperationException("The owned test directory escaped its temporary root.");
                if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            }
        }
    }

    private sealed class WorkspaceApplicationFactory(string directory) : MonicaTestApplicationFactory<ChatSessionWorkspaceTests>
    {
        protected override void ConfigureMonica(IMonicaBuilder builder)
            => builder.AddAI(options => options.StorageRootPath = directory);

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.RemoveAll<IChatHistoryPartitionResolver>();
            services.AddScoped<IChatHistoryPartitionResolver, FixedPartitionResolver>();
        }
    }

    private sealed class FixedPartitionResolver : IChatHistoryPartitionResolver
    {
        public ValueTask<ChatHistoryPartition> ResolveAsync(CancellationToken ct = default)
            => ValueTask.FromResult(PARTITION);
    }

    private static ChatSessionSnapshot CreateSnapshot(
        string sessionId = "session-1",
        string message = "persisted message")
    {
        var now = DateTimeOffset.UtcNow;
        return new ChatSessionSnapshot
        {
            SessionId = sessionId,
            Title = $"Persisted {sessionId}",
            CreatedAt = now,
            UpdatedAt = now,
            Settings = new ChatSessionSettings("missing-provider", "missing-model", null, null),
            Turns =
            [
                new ChatTurnSnapshot
                {
                    Status = ChatExecutionStatus.Completed,
                    StartedAt = now,
                    CompletedAt = now,
                    UserMessage = new ChatMessageSnapshot
                    {
                        Id = $"message-{sessionId}",
                        Role = AIChatRole.User,
                        Parts = [ChatContentPart.FromText(message)],
                        CreatedAt = now
                    }
                }
            ]
        };
    }
}
