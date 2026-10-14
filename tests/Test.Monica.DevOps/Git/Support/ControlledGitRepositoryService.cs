using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Monica.DevOps.Git.Abstractions;
using Monica.DevOps.Git.Facades;
using Monica.DevOps.Git.Models;
using Monica.DevOps.Git.State;
using Monica.Modules;

namespace Test.Monica.DevOps.Git.Support;

// Scenario-owned external boundary. Tests retain the real facade and controller orchestration.
internal sealed class ControlledGitRepositoryService : IGitRepositoryService
{
    public Func<Task<IReadOnlyList<GitRepositoryStatus>>> Read { get; set; } =
        () => Task.FromResult<IReadOnlyList<GitRepositoryStatus>>([Repository("alpha"), Repository("beta")]);
    public Func<Task<IReadOnlyList<GitCredentialInfo>>> ReadCredentials { get; set; } =
        () => Task.FromResult<IReadOnlyList<GitCredentialInfo>>([]);
    public Func<Task<IReadOnlyList<GitCredentialResolverInfo>>> ReadResolvers { get; set; } =
        () => Task.FromResult<IReadOnlyList<GitCredentialResolverInfo>>([]);
    public Func<string, Task<GitSyncResult>> Sync { get; set; } = id => Task.FromResult(SyncResult(id));
    public Func<Task<IReadOnlyList<GitSyncResult>>> SyncAll { get; set; } =
        () => Task.FromResult<IReadOnlyList<GitSyncResult>>([SyncResult("alpha"), SyncResult("beta")]);
    public Func<string, Task<GitRepositoryDeleteResult>> Delete { get; set; } = id => Task.FromResult(new GitRepositoryDeleteResult
    {
        RepositoryId = id, ResolvedLocalPath = $"/workspace/{id}", Success = true,
        PathExisted = true, Message = "Local copy removed."
    });
    public int RepositoryReads { get; private set; }
    public int CredentialReads { get; private set; }
    public int ResolverReads { get; private set; }
    public List<(string Id, GitSyncTrigger Trigger)> SyncCalls { get; } = [];
    public List<GitSyncTrigger> SyncAllCalls { get; } = [];
    public List<string> DeleteCalls { get; } = [];

    public GitWorkspaceController Create(bool manualEnabled = true)
    {
        var option = new ModuleGitOption();
        if (!manualEnabled) option.SetEnabledSyncTriggers(GitSyncTrigger.Startup);
        var options = Options.Create(option);
        return new(new GitFacade(this, options, NullLogger<GitFacade>.Instance), options);
    }

    public Task<IReadOnlyList<GitRepositoryStatus>> GetRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        RepositoryReads++;
        return Read();
    }

    public Task<IReadOnlyList<GitCredentialInfo>> GetCredentialsAsync(CancellationToken cancellationToken = default)
    {
        CredentialReads++;
        return ReadCredentials();
    }

    public Task<IReadOnlyList<GitCredentialResolverInfo>> GetCredentialResolversAsync(CancellationToken cancellationToken = default)
    {
        ResolverReads++;
        return ReadResolvers();
    }

    public Task<GitSyncResult> SyncRepositoryAsync(string repositoryId, GitSyncTrigger trigger = GitSyncTrigger.Manual,
        CancellationToken cancellationToken = default)
    {
        SyncCalls.Add((repositoryId, trigger));
        return Sync(repositoryId);
    }

    public Task<IReadOnlyList<GitSyncResult>> SyncAllAsync(GitSyncTrigger trigger = GitSyncTrigger.Manual,
        CancellationToken cancellationToken = default)
    {
        SyncAllCalls.Add(trigger);
        return SyncAll();
    }

    public Task<GitRepositoryDeleteResult> DeleteRepositoryAsync(string repositoryId, CancellationToken cancellationToken = default)
    {
        DeleteCalls.Add(repositoryId);
        return Delete(repositoryId);
    }

    public Task<GitRepositoryStatus> GetRepositoryAsync(string repositoryId, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("The workspace reads the inventory boundary.");

    public Task<GitRepositoryStatus?> FindRepositoryByRemoteUrlAsync(string remoteUrl, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("The workspace does not resolve credentials or remote repositories.");

    public static GitRepositoryStatus Repository(string id, GitRepositorySyncState state = GitRepositorySyncState.Ready,
        bool dirty = false) => new()
    {
        Id = id, Provider = "Test provider", RemoteUrl = $"https://example.invalid/{id}.git",
        LocalPath = id, ResolvedLocalPath = $"/workspace/{id}", ConfiguredBranch = "main", ResolvedBranch = "main",
        State = state, IsAvailable = true, IsDirty = dirty, WorkingDirectorySizeBytes = 1024,
        LastSyncAtUtc = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero)
    };

    public static GitSyncResult SyncResult(string id, bool success = true) => new()
    {
        RepositoryId = id, Trigger = GitSyncTrigger.Manual, Success = success,
        HasChanges = success, CurrentCommit = "accepted-commit", Message = success ? "Updated." : "Checkout failed."
    };
}
