using System.Text.Json;
using Monica.Guide;
using Xunit;

namespace Test.Monica.Guide;

public sealed class GuideSourceTests
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Bind_VerifiesLocalCheckoutAndWritesTheLedger()
    {
        using var fixture = new SourceFixture();
        var service = fixture.CreateService();
        var request = new GuideSourceBindRequest("Tairitsua/Monica", fixture.CheckoutPath, null);

        var preview = await service.BindAsync(request, cancellationToken: CancellationToken);
        Assert.Equal(GuideStatus.Ready, preview.Status);
        Assert.Contains(preview.Plan!.Actions, action =>
            action.Kind == GuidePlanActionKind.WriteFile
            && action.Target.EndsWith("source-bindings.json", StringComparison.OrdinalIgnoreCase));
        Assert.False(File.Exists(fixture.LedgerFile));

        var applied = await service.BindAsync(request, preview.Plan.PlanDigest, cancellationToken: CancellationToken);
        Assert.True(applied.Plan!.Applied);
        Assert.True(File.Exists(fixture.LedgerFile));

        var resolved = service.Resolve("monica");
        Assert.Equal(GuideStatus.Ready, resolved.Status);
        var binding = Assert.Single(resolved.Checks, check => check.Id == "source.binding.Tairitsua/Monica");
        Assert.Equal(GuideCheckStatus.Ok, binding.Status);
        Assert.Equal(SourceFixture.Commit, binding.Details!["commit"]);
        Assert.Equal(fixture.CheckoutPath, binding.Details!["path"]);

        // A second identical bind is a no-op.
        var repeat = await service.BindAsync(request, null, cancellationToken: CancellationToken);
        Assert.True(repeat.Plan!.IsNoOp);
    }

    [Fact]
    public async Task Bind_RejectsForeignCheckoutsAndStaleDigests()
    {
        using var fixture = new SourceFixture(remoteUrl: "https://github.com/Someone/Else.git");
        var service = fixture.CreateService();
        var request = new GuideSourceBindRequest("monica", fixture.CheckoutPath, null);

        var preview = await service.BindAsync(request, cancellationToken: CancellationToken);
        Assert.Equal(GuideStatus.Error, preview.Status);
        Assert.Contains(preview.Checks, check => check.Id == "source.binding.Tairitsua/Monica"
                                                  && check.Status == GuideCheckStatus.Error);
        Assert.Empty(preview.Plan!.Actions);
        Assert.False(File.Exists(fixture.LedgerFile));

        // A foreign repository selector fails closed as well.
        var unknown = await service.BindAsync(
            new GuideSourceBindRequest("unknown/Repo", fixture.CheckoutPath, null),
            cancellationToken: CancellationToken);
        Assert.Contains(unknown.Checks, check => check.Id == "source.repository");
    }

    [Fact]
    public async Task Bind_RequiresExactlyOneVerifiedSource()
    {
        using var fixture = new SourceFixture();
        var service = fixture.CreateService();

        var withoutSource = await service.BindAsync(
            new GuideSourceBindRequest("monica", null, null), cancellationToken: CancellationToken);
        Assert.Equal(GuideStatus.Error, withoutSource.Status);
        Assert.Contains(withoutSource.Checks, check => check.Status == GuideCheckStatus.Error);

        // A ref without a local checkout cannot be verified, so it fails closed as well.
        var refWithoutPath = await service.BindAsync(
            new GuideSourceBindRequest("monica", null, SourceFixture.Commit), cancellationToken: CancellationToken);
        Assert.Equal(GuideStatus.Error, refWithoutPath.Status);
        Assert.Contains(refWithoutPath.Checks, check => check.Status == GuideCheckStatus.Error);

        var missingPath = await service.BindAsync(
            new GuideSourceBindRequest("monica", Path.Combine(fixture.Root, "absent"), null),
            cancellationToken: CancellationToken);
        Assert.Equal(GuideStatus.Error, missingPath.Status);
    }

    [Fact]
    public async Task Bind_ResolvesTagRefsThroughTheGitProbe()
    {
        using var fixture = new SourceFixture(remoteUrl: "git@github.com:Tairitsua/Monica.git");
        fixture.Git.TagCommits["v1.4.0"] = SourceFixture.Commit;
        var service = fixture.CreateService();
        var request = new GuideSourceBindRequest("monica", fixture.CheckoutPath, "v1.4.0");

        var preview = await service.BindAsync(request, cancellationToken: CancellationToken);
        Assert.Equal(GuideStatus.Ready, preview.Status);

        var applied = await service.BindAsync(request, preview.Plan!.PlanDigest, cancellationToken: CancellationToken);
        Assert.True(applied.Plan!.Applied);
        var resolved = service.Resolve("monica");
        var binding = Assert.Single(resolved.Checks, check => check.Id == "source.binding.Tairitsua/Monica");
        Assert.Equal(GuideCheckStatus.Ok, binding.Status);
        Assert.Equal("v1.4.0", binding.Details!["ref"]);
    }

    [Fact]
    public async Task Unbind_RemovesTheBindingAndKeepsOthers()
    {
        using var fixture = new SourceFixture();
        var service = fixture.CreateService();
        var request = new GuideSourceBindRequest("Tairitsua/Monica", fixture.CheckoutPath, null);
        var preview = await service.BindAsync(request, cancellationToken: CancellationToken);
        await service.BindAsync(request, preview.Plan!.PlanDigest, cancellationToken: CancellationToken);

        var unbindPreview = await service.UnbindAsync("monica", cancellationToken: CancellationToken);
        Assert.Equal(GuideStatus.Ready, unbindPreview.Status);
        var unbound = await service.UnbindAsync("monica", unbindPreview.Plan!.PlanDigest, cancellationToken: CancellationToken);
        Assert.True(unbound.Plan!.Applied);

        var listing = service.List();
        Assert.Contains(listing.Checks, check =>
            check.Id == "source.binding.Tairitsua/Monica" && check.Message.Contains("not bound", StringComparison.Ordinal));

        // Unbinding again is a warning no-op.
        var again = await service.UnbindAsync("monica", null, cancellationToken: CancellationToken);
        Assert.True(again.Plan!.IsNoOp);
        Assert.Equal(GuideStatus.Warning, again.Status);
    }

    [Fact]
    public void Resolve_UnboundRepositoryIsAWarning()
    {
        using var fixture = new SourceFixture();
        var service = fixture.CreateService();

        var resolved = service.Resolve("monica");

        Assert.Equal(GuideStatus.Warning, resolved.Status);
        Assert.Contains(resolved.Checks, check =>
            check.Id == "source.binding.Tairitsua/Monica" && check.Status == GuideCheckStatus.Warning);
    }

    [Fact]
    public void Resolve_DocsAliasIsUnknownWithoutCatalogDeclaration()
    {
        using var fixture = new SourceFixture();
        var resolved = fixture.CreateService().Resolve("docs");

        Assert.Equal(GuideStatus.Error, resolved.Status);
        Assert.Contains(resolved.Checks, check =>
            check.Id == "source.repository" && check.Status == GuideCheckStatus.Error);
    }

    [Fact]
    public async Task Bind_UsesCatalogDeclaredRepositoryWithoutEngineSpecialCase()
    {
        using var fixture = new SourceFixture(remoteUrl: "https://github.com/Example/GuideDocs.git");
        var catalog = new SkillCatalog(
            SchemaVersion: 1,
            SkillCount: 0,
            TreeDigest: string.Empty,
            Skills: [],
            SourceRepositories: new Dictionary<string, GuideSourceRepositoryDefinition>
            {
                ["Example/GuideDocs"] = new("Example/GuideDocs", ["docs"])
            });
        var service = fixture.CreateService(catalog);
        var request = new GuideSourceBindRequest("docs", fixture.CheckoutPath, null);

        var preview = await service.BindAsync(request, cancellationToken: CancellationToken);
        Assert.Equal(GuideStatus.Ready, preview.Status);
        var applied = await service.BindAsync(request, preview.Plan!.PlanDigest, cancellationToken: CancellationToken);
        Assert.True(applied.Plan!.Applied);
        Assert.Contains(service.Resolve("docs").Checks, check =>
            check.Id == "source.binding.Example/GuideDocs" && check.Status == GuideCheckStatus.Ok);
    }

    [Fact]
    public async Task Bind_RejectsStalePlanDigest()
    {
        using var fixture = new SourceFixture();
        var service = fixture.CreateService();
        var request = new GuideSourceBindRequest("Tairitsua/Monica", fixture.CheckoutPath, null);

        var stale = await service.BindAsync(request, new string('c', 64), cancellationToken: CancellationToken);

        Assert.Equal(GuideStatus.Error, stale.Status);
        Assert.Contains(stale.Checks, check => check.Id == "plan.digest");
    }

    [Fact]
    public async Task Ledger_UnreadableStateIsAnErrorFinding()
    {
        using var fixture = new SourceFixture();
        Directory.CreateDirectory(Path.GetDirectoryName(fixture.LedgerFile)!);
        await File.WriteAllTextAsync(fixture.LedgerFile, "not json", CancellationToken);
        var service = fixture.CreateService();

        var listing = service.List();

        Assert.Equal(GuideStatus.Error, listing.Status);
        Assert.Contains(listing.Checks, check =>
            check.Id == "source.bindings.ledger" && check.Status == GuideCheckStatus.Error);
    }

    [Fact]
    public async Task HealthChecks_ReportsOnlyRecordedBindings()
    {
        using var fixture = new SourceFixture();
        var service = fixture.CreateService();
        Assert.Empty(service.HealthChecks());

        var request = new GuideSourceBindRequest("Tairitsua/Monica", fixture.CheckoutPath, null);
        var preview = await service.BindAsync(request, cancellationToken: CancellationToken);
        await service.BindAsync(request, preview.Plan!.PlanDigest, cancellationToken: CancellationToken);

        var checks = service.HealthChecks();

        var binding = Assert.Single(checks, check => check.Id == "source.binding.Tairitsua/Monica");
        Assert.Equal(GuideCheckStatus.Ok, binding.Status);
    }

    [Fact]
    public async Task HealthChecks_WarnWhenTheBoundCheckoutRunsAheadOfTheReleasePin()
    {
        using var fixture = new SourceFixture();
        var head = new string('a', 40);
        fixture.Git.HeadCommits[fixture.CheckoutPath] = head;
        fixture.Git.AheadCounts[(SourceFixture.Commit, head)] = 12;
        var service = fixture.CreateService();
        var request = new GuideSourceBindRequest("Tairitsua/Monica", fixture.CheckoutPath, null);
        var preview = await service.BindAsync(request, cancellationToken: CancellationToken);
        await service.BindAsync(request, preview.Plan!.PlanDigest, cancellationToken: CancellationToken);
        var release = new ReleaseManifest
        {
            ProductId = "Tairitsua.Monica",
            ProductVersion = "1.0.0-rc.13-local.4",
            SourceCommit = SourceFixture.Commit
        };

        var checks = service.HealthChecks(release);

        var lag = Assert.Single(checks, check => check.Id == "source.binding.Tairitsua/Monica.release-lag");
        Assert.Equal(GuideCheckStatus.Warning, lag.Status);
        Assert.Contains("12 commits ahead", lag.Message);
        Assert.Contains("1.0.0-rc.13-local.4", lag.Message);
    }

    [Fact]
    public async Task HealthChecks_StaySilentWhenTheReleasePinMatchesTheCheckout()
    {
        using var fixture = new SourceFixture();
        var service = fixture.CreateService();
        var request = new GuideSourceBindRequest("Tairitsua/Monica", fixture.CheckoutPath, null);
        var preview = await service.BindAsync(request, cancellationToken: CancellationToken);
        await service.BindAsync(request, preview.Plan!.PlanDigest, cancellationToken: CancellationToken);
        var release = new ReleaseManifest
        {
            ProductId = "Tairitsua.Monica",
            ProductVersion = "1.0.0-rc.13-local.4",
            SourceCommit = SourceFixture.Commit
        };

        var checks = service.HealthChecks(release);

        Assert.Contains(checks, check =>
            check.Id == "source.binding.Tairitsua/Monica" && check.Status == GuideCheckStatus.Ok);
        Assert.DoesNotContain(checks, check => check.Id.EndsWith("release-lag", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Observe_MovedOrDirtyCheckoutsProduceWarnings()
    {
        using var fixture = new SourceFixture();
        var service = fixture.CreateService();
        var request = new GuideSourceBindRequest("Tairitsua/Monica", fixture.CheckoutPath, null);
        var preview = await service.BindAsync(request, cancellationToken: CancellationToken);
        await service.BindAsync(request, preview.Plan!.PlanDigest, cancellationToken: CancellationToken);
        fixture.Git.Dirty = true;

        var resolved = service.Resolve("monica");

        Assert.Equal(GuideStatus.Warning, resolved.Status);
        Assert.Contains(resolved.Checks, check =>
            check.Id == "source.binding.Tairitsua/Monica.dirty" && check.Status == GuideCheckStatus.Warning);
    }

    [Fact]
    public void CanonicalRepository_AcceptsHttpsAndSshGitHubRemotes()
    {
        Assert.Equal("Tairitsua/Monica", GuideGitProbe.CanonicalRepository("https://github.com/Tairitsua/Monica.git"));
        Assert.Equal("Tairitsua/Monica", GuideGitProbe.CanonicalRepository("git@github.com:Tairitsua/Monica.git"));
        // The remote's own casing is preserved; every engine comparison is case-insensitive.
        Assert.Equal("someone/other", GuideGitProbe.CanonicalRepository("https://github.com/someone/other"));
        Assert.Null(GuideGitProbe.CanonicalRepository("https://gitlab.com/Tairitsua/Monica"));
        Assert.Null(GuideGitProbe.CanonicalRepository(null));
    }

    [Fact]
    public async Task Describe_ProjectsDeclaredRepositoriesWithLiveObservation()
    {
        using var fixture = new SourceFixture();
        var service = fixture.CreateService();

        // Unbound repositories carry null binding fields instead of a warning.
        var unbound = Assert.Single(service.Describe(), status => status.Repository == "Tairitsua/Monica");
        Assert.Null(unbound.Binding);
        Assert.Null(unbound.Observation);
        Assert.Null(unbound.LedgerIssue);
        Assert.Contains("monica", unbound.Aliases);

        var request = new GuideSourceBindRequest("Tairitsua/Monica", fixture.CheckoutPath, null);
        var preview = await service.BindAsync(request, cancellationToken: CancellationToken);
        await service.BindAsync(request, preview.Plan!.PlanDigest, cancellationToken: CancellationToken);

        var bound = Assert.Single(service.Describe(), status => status.Repository == "Tairitsua/Monica");
        Assert.Equal(fixture.CheckoutPath, bound.Binding!.SourcePath);
        Assert.Equal(SourceFixture.Commit, bound.Binding.Commit);
        Assert.Equal("available", bound.Observation!.PathHealth);
        Assert.Equal(SourceFixture.Commit, bound.Observation.ObservedCommit);
    }

    [Fact]
    public void Describe_SurfacesAnUnreadableLedgerOnEveryRepository()
    {
        using var fixture = new SourceFixture();
        Directory.CreateDirectory(Path.GetDirectoryName(fixture.LedgerFile)!);
        File.WriteAllText(fixture.LedgerFile, "not json");
        var service = fixture.CreateService();

        var statuses = service.Describe();

        Assert.All(statuses, status =>
        {
            Assert.Null(status.Binding);
            Assert.NotNull(status.LedgerIssue);
            Assert.Equal("source.bindings.ledger", status.LedgerIssue!.Id);
        });
    }

    private sealed class SourceFixture : IDisposable
    {
        internal const string Commit = "0123456789abcdef0123456789abcdef01234567";
        private readonly string _root = Path.Combine(Path.GetTempPath(), $"guide-source-{Guid.NewGuid():N}");

        internal SourceFixture(string remoteUrl = "https://github.com/Tairitsua/Monica.git")
        {
            EnginePaths = new GuidePaths(Path.Combine(_root, "engine-data"));
            CheckoutPath = Path.Combine(_root, "checkout");
            Directory.CreateDirectory(CheckoutPath);
            Git = new FakeGitProbe
            {
                Remotes = { [CheckoutPath] = remoteUrl }
            };
        }

        internal GuidePaths EnginePaths { get; }
        internal FakeGitProbe Git { get; }
        internal string CheckoutPath { get; }
        internal string Root => _root;
        internal string LedgerFile => Path.Combine(EnginePaths.StateDirectory, "source-bindings.json");

        internal GuideSourceService CreateService(SkillCatalog? catalog = null)
            => new(EnginePaths, catalog, Git);

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
    }

    private sealed class FakeGitProbe : IGuideGitProbe
    {
        internal Dictionary<string, string> Remotes { get; } = new(StringComparer.OrdinalIgnoreCase);
        internal Dictionary<string, string> TagCommits { get; } = new(StringComparer.OrdinalIgnoreCase);
        internal Dictionary<string, string> HeadCommits { get; } = new(StringComparer.OrdinalIgnoreCase);
        internal Dictionary<(string From, string To), int> AheadCounts { get; } = new();
        internal bool Dirty { get; set; }

        public GuideGitInfo? Describe(string path)
        {
            if (!Remotes.TryGetValue(path, out var remote))
            {
                return null;
            }

            var head = HeadCommits.GetValueOrDefault(path, SourceFixture.Commit);
            return new GuideGitInfo(head, path, GuideGitProbe.CanonicalRepository(remote), Dirty);
        }

        public GuideGitIdentity? FindIdentity(string path)
            => Remotes.TryGetValue(path, out var remote)
                ? new GuideGitIdentity(path, GuideGitProbe.CanonicalRepository(remote))
                : null;

        public string? ResolveTagCommit(string repositoryRoot, string tag)
            => TagCommits.TryGetValue(tag, out var commit) ? commit : null;

        public int? CountCommitsAhead(string repositoryRoot, string fromCommit, string toCommit)
            => AheadCounts.TryGetValue((fromCommit, toCommit), out var count) ? count : null;
    }
}
