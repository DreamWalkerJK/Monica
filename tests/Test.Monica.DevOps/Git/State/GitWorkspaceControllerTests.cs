using Monica.DevOps.Git.Models;
using Monica.DevOps.Git.State;
using Test.Monica.DevOps.Git.Support;
using Xunit;

namespace Test.Monica.DevOps.Git.State;

public sealed class GitWorkspaceControllerTests
{
    private static readonly TimeSpan ObservationTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task TryBeginCommand_WhenConfirmationIsPending_ShouldReserveBeforeAnyProducerAndRejectOverlap()
    {
        var service = new ControlledGitRepositoryService();
        await using var state = service.Create();
        await ObserveAsync(state.RefreshAsync());

        var scope = Begin(state, GitCommandKind.Sync);
        Assert.Same(scope, state.Operation);
        Assert.True(state.IsBusy);
        Assert.False(scope.HasStarted);
        Assert.Empty(service.SyncCalls);
        Assert.False(state.TryBeginCommand(GitCommandKind.DeleteLocal, out var rejected));
        Assert.Null(rejected);
        await ObserveAsync(state.RefreshAsync());
        await ObserveAsync(state.RefreshMetadataAsync());
        Assert.Equal(1, service.RepositoryReads);
        Assert.Equal(1, service.CredentialReads);

        state.CancelCommand(scope);
        Assert.False(state.IsBusy);
        Assert.Null(state.Outcome);
        Assert.Empty(service.SyncCalls);
    }

    [Fact]
    public async Task RefreshAsync_WhenProducerIsPending_ShouldAdmitBeforeInvocationAndRejectCommandsAndDuplicateReads()
    {
        var service = new ControlledGitRepositoryService();
        var result = Signal<IReadOnlyList<GitRepositoryStatus>>();
        service.Read = () => result.Task;
        var state = service.Create();
        try
        {
            var refresh = state.RefreshAsync();
            Assert.True(state.IsLoading);
            Assert.Equal(1, service.RepositoryReads);
            Assert.False(state.TryBeginCommand(GitCommandKind.SyncAll, out _));
            await ObserveAsync(state.RefreshAsync());
            Assert.Equal(1, service.RepositoryReads);
            result.SetResult([ControlledGitRepositoryService.Repository("alpha")]);
            await ObserveAsync(refresh);
            Assert.False(state.IsBusy);
        }
        finally
        {
            result.TrySetResult([]);
            await ObserveAsync(state.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task TryBeginCommand_WhenManualSyncIsDisabled_ShouldRejectSyncButAllowLocalDeletion()
    {
        var service = new ControlledGitRepositoryService();
        await using var state = service.Create(manualEnabled: false);
        await ObserveAsync(state.RefreshAsync());

        Assert.False(state.ManualSyncEnabled);
        Assert.False(state.TryBeginCommand(GitCommandKind.Sync, out _));
        Assert.False(state.TryBeginCommand(GitCommandKind.SyncAll, out _));
        var deletion = Begin(state, GitCommandKind.DeleteLocal);
        await ObserveAsync(state.ExecuteAsync(deletion));

        Assert.Empty(service.SyncCalls);
        Assert.Empty(service.SyncAllCalls);
        Assert.Equal("alpha", Assert.Single(service.DeleteCalls));
        Assert.True(Assert.IsType<GitRepositoryDeleteResult>(state.Outcome?.DeleteResult).Success);
        Assert.Null(state.Outcome?.FacadeError);
    }

    [Fact]
    public async Task RefreshMetadataAsync_WhenRequested_ShouldAcceptSafePairWithoutRepositoryRead()
    {
        var credential = new GitCredentialInfo { Id = "credential", AuthenticationType = GitAuthenticationType.Token, HasSecret = true };
        var later = Resolver("later", 10);
        var earlier = Resolver("earlier", 0);
        var service = new ControlledGitRepositoryService
        {
            ReadCredentials = () => Task.FromResult<IReadOnlyList<GitCredentialInfo>>([credential]),
            ReadResolvers = () => Task.FromResult<IReadOnlyList<GitCredentialResolverInfo>>([later, earlier])
        };
        await using var state = service.Create();

        await ObserveAsync(state.RefreshMetadataAsync());

        Assert.Equal(0, service.RepositoryReads);
        Assert.Same(credential, Assert.Single(state.Credentials));
        Assert.Equal(new[] { later, earlier }, state.Resolvers);
        Assert.NotNull(state.MetadataObservedAt);
        Assert.Null(state.RepositoriesObservedAt);
        Assert.Null(state.MetadataFailure);
    }

    [Fact]
    public async Task RefreshAsync_WhenRepositoryReadFails_ShouldRetainAcceptedEvidenceAndReceiptTime()
    {
        var service = new ControlledGitRepositoryService();
        await using var state = service.Create();
        await ObserveAsync(state.RefreshAsync());
        state.Select("beta");
        var accepted = state.Repositories;
        var observed = state.RepositoriesObservedAt;
        service.Read = () => Task.FromException<IReadOnlyList<GitRepositoryStatus>>(new IOException("Inspection unavailable"));

        await ObserveAsync(state.RefreshAsync(false));

        Assert.Same(accepted, state.Repositories);
        Assert.Equal(observed, state.RepositoriesObservedAt);
        Assert.Equal("beta", state.SelectedId);
        Assert.Equal(GitWorkspaceReadKind.Repositories, state.RepositoryFailure?.Kind);
        Assert.Contains("Inspection unavailable", state.RepositoryFailure?.Detail);
        Assert.Null(state.MetadataFailure);
    }

    [Fact]
    public async Task RefreshMetadataAsync_WhenResolverReadFails_ShouldRetainEntireAcceptedPair()
    {
        var service = new ControlledGitRepositoryService();
        await using var state = service.Create();
        await ObserveAsync(state.RefreshMetadataAsync());
        var credentials = state.Credentials;
        var resolvers = state.Resolvers;
        var observed = state.MetadataObservedAt;
        service.ReadCredentials = () => Task.FromResult<IReadOnlyList<GitCredentialInfo>>([
            new() { Id = "new", AuthenticationType = GitAuthenticationType.Token, HasSecret = true }]);
        service.ReadResolvers = () => Task.FromException<IReadOnlyList<GitCredentialResolverInfo>>(new IOException("Resolvers unavailable"));

        await ObserveAsync(state.RefreshMetadataAsync());

        Assert.Same(credentials, state.Credentials);
        Assert.Same(resolvers, state.Resolvers);
        Assert.Equal(observed, state.MetadataObservedAt);
        Assert.Equal(GitWorkspaceReadKind.Resolvers, state.MetadataFailure?.Kind);
        Assert.Contains("Resolvers unavailable", state.MetadataFailure?.Detail);
        Assert.Equal(0, service.RepositoryReads);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSelectionChangesAfterReservation_ShouldUseFrozenIdentityOnceAndRejectStaleScope()
    {
        var service = new ControlledGitRepositoryService();
        await using var state = service.Create();
        await ObserveAsync(state.RefreshAsync(false));
        var canceled = Begin(state, GitCommandKind.DeleteLocal);
        state.CancelCommand(canceled);
        var scope = Begin(state, GitCommandKind.Sync);
        state.Select("beta");

        await ObserveAsync(state.ExecuteAsync(canceled));
        Assert.Empty(service.DeleteCalls);
        Assert.Same(scope, state.Operation);
        Assert.Equal("alpha", Assert.Single(scope.Targets).Id);
        Assert.Equal("/workspace/alpha", scope.Targets[0].ResolvedLocalPath);
        await ObserveAsync(state.ExecuteAsync(scope));
        await ObserveAsync(state.ExecuteAsync(scope));

        Assert.Equal(("alpha", GitSyncTrigger.Manual), Assert.Single(service.SyncCalls));
        Assert.Equal("beta", state.SelectedId);
        Assert.Same(scope, state.Outcome?.Scope);
        Assert.Null(state.Outcome?.FacadeError);
        Assert.True(Assert.Single(state.Outcome!.SyncResults).Success);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProducerIsPending_ShouldRejectSecondStartAndPreventConfirmationCancelFromReleasingGate()
    {
        var service = new ControlledGitRepositoryService();
        var result = Signal<GitSyncResult>();
        service.Sync = _ => result.Task;
        var state = service.Create();
        try
        {
            await ObserveAsync(state.RefreshAsync(false));
            var scope = Begin(state, GitCommandKind.Sync);
            var execution = state.ExecuteAsync(scope);
            Assert.True(scope.HasStarted);
            Assert.False(execution.IsCompleted);

            state.CancelCommand(scope);
            await ObserveAsync(state.ExecuteAsync(scope));
            Assert.Same(scope, state.Operation);
            Assert.True(state.IsBusy);
            Assert.False(state.TryBeginCommand(GitCommandKind.SyncAll, out _));
            Assert.Single(service.SyncCalls);

            result.SetResult(ControlledGitRepositoryService.SyncResult("alpha"));
            await ObserveAsync(execution);
            Assert.False(state.IsBusy);
            Assert.Single(service.SyncCalls);
        }
        finally
        {
            result.TrySetResult(ControlledGitRepositoryService.SyncResult("alpha"));
            await ObserveAsync(state.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task TryBeginCommand_WhenSyncAllIsReserved_ShouldCaptureAndExecuteFullInventory()
    {
        var service = new ControlledGitRepositoryService();
        await using var state = service.Create();
        await ObserveAsync(state.RefreshAsync(false));
        state.Select("beta");
        var scope = Begin(state, GitCommandKind.SyncAll);

        Assert.Equal(new[] { "alpha", "beta" }, scope.Targets.Select(x => x.Id));
        await ObserveAsync(state.ExecuteAsync(scope));

        Assert.Equal(GitSyncTrigger.Manual, Assert.Single(service.SyncAllCalls));
        Assert.Equal(new[] { "alpha", "beta" }, state.Outcome!.SyncResults.Select(x => x.RepositoryId));
        Assert.Null(state.Outcome.FacadeError);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMutationFinishesButRefreshFails_ShouldKeepActualOutcomeAndPriorEvidence()
    {
        var service = new ControlledGitRepositoryService();
        await using var state = service.Create();
        await ObserveAsync(state.RefreshAsync(false));
        var accepted = state.Repositories;
        var failedResult = ControlledGitRepositoryService.SyncResult("alpha", success: false);
        service.Sync = _ => Task.FromResult(failedResult);
        service.Read = () => Task.FromException<IReadOnlyList<GitRepositoryStatus>>(new IOException("Read after command failed"));

        await ObserveAsync(state.ExecuteAsync(Begin(state, GitCommandKind.Sync)));

        Assert.Null(state.Outcome?.FacadeError);
        Assert.Same(failedResult, Assert.Single(state.Outcome!.SyncResults));
        Assert.False(failedResult.Success);
        Assert.Equal("Checkout failed.", failedResult.Message);
        Assert.Same(accepted, state.Repositories);
        Assert.Contains("Read after command failed", state.RepositoryFailure?.Detail);
        Assert.False(state.IsBusy);
    }

    [Fact]
    public async Task ExecuteAsync_WhenBackendThrows_ShouldKeepFacadeFailureDistinctFromIndividualResult()
    {
        var service = new ControlledGitRepositoryService
        {
            Sync = _ => Task.FromException<GitSyncResult>(new IOException("Backend unavailable"))
        };
        await using var state = service.Create();
        await ObserveAsync(state.RefreshAsync(false));

        await ObserveAsync(state.ExecuteAsync(Begin(state, GitCommandKind.Sync)));

        Assert.Contains("Backend unavailable", state.Outcome?.FacadeError);
        Assert.Empty(state.Outcome!.SyncResults);
        Assert.Null(state.RepositoryFailure);
        Assert.False(state.IsBusy);
    }

    [Fact]
    public async Task DisposeAsync_WhenMutationIsPending_ShouldStopAdmissionDrainActualProducerAndSuppressLatePublication()
    {
        var service = new ControlledGitRepositoryService();
        var result = Signal<GitSyncResult>();
        service.Sync = _ => result.Task;
        var state = service.Create();
        Task? retirement = null;
        try
        {
            await ObserveAsync(state.RefreshAsync(false));
            var accepted = state.Repositories;
            var notifications = 0;
            state.Changed += () => notifications++;
            var scope = Begin(state, GitCommandKind.Sync);
            var execution = state.ExecuteAsync(scope);
            Assert.Single(service.SyncCalls);
            Assert.False(execution.IsCompleted);
            retirement = state.DisposeAsync().AsTask();
            var notificationsAtRetirement = notifications;

            Assert.True(state.IsRetired);
            Assert.False(retirement.IsCompleted);
            Assert.Same(retirement, state.DisposeAsync().AsTask());
            Assert.False(state.TryBeginCommand(GitCommandKind.DeleteLocal, out _));
            await ObserveAsync(state.RefreshAsync());
            await ObserveAsync(state.RefreshMetadataAsync());
            await ObserveAsync(state.ExecuteAsync(scope));
            Assert.Equal(1, service.RepositoryReads);
            Assert.Equal(0, service.CredentialReads);

            result.SetResult(ControlledGitRepositoryService.SyncResult("alpha"));
            await ObserveAsync(Task.WhenAll(execution, retirement));

            Assert.Equal(notificationsAtRetirement, notifications);
            Assert.Null(state.Outcome);
            Assert.Same(accepted, state.Repositories);
            Assert.Equal(1, service.RepositoryReads);
        }
        finally
        {
            result.TrySetResult(ControlledGitRepositoryService.SyncResult("alpha"));
            await ObserveAsync(retirement ?? state.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenReadIsPending_ShouldDrainReadWithoutAcceptingLateEvidence()
    {
        var service = new ControlledGitRepositoryService();
        var result = Signal<IReadOnlyList<GitRepositoryStatus>>();
        service.Read = () => result.Task;
        var state = service.Create();
        try
        {
            var refresh = state.RefreshAsync();
            var retirement = state.DisposeAsync().AsTask();
            Assert.False(retirement.IsCompleted);
            result.SetResult([ControlledGitRepositoryService.Repository("late")]);
            await ObserveAsync(Task.WhenAll(refresh, retirement));

            Assert.Empty(state.Repositories);
            Assert.Null(state.RepositoriesObservedAt);
            Assert.Equal(0, service.CredentialReads);
        }
        finally
        {
            result.TrySetResult([]);
            await ObserveAsync(state.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task RefreshAsync_WhenLoadingSubscriberRetires_ShouldNotInvokeAProducerAfterRetirement()
    {
        var service = new ControlledGitRepositoryService();
        var state = service.Create();
        Task? retirement = null;
        state.Changed += () => retirement = state.DisposeAsync().AsTask();

        await ObserveAsync(state.RefreshAsync());
        await ObserveAsync(retirement ?? state.DisposeAsync().AsTask());

        Assert.True(state.IsRetired);
        Assert.Equal(0, service.RepositoryReads);
        Assert.Equal(0, service.CredentialReads);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSubscriberThrows_ShouldPreserveBackendOutcomeAndNotifyOtherSubscribers()
    {
        var service = new ControlledGitRepositoryService();
        await using var state = service.Create();
        await ObserveAsync(state.RefreshAsync(false));
        var expected = new InvalidOperationException("Presentation subscriber failed");
        var otherNotifications = 0;
        state.Changed += () => throw expected;
        state.Changed += () => otherNotifications++;

        await ObserveAsync(state.ExecuteAsync(Begin(state, GitCommandKind.Sync)));

        Assert.Same(expected, state.NotificationError);
        Assert.True(otherNotifications > 0);
        Assert.Null(state.Outcome?.FacadeError);
        Assert.Equal("Updated.", Assert.Single(state.Outcome!.SyncResults).Message);
        Assert.Single(service.SyncCalls);
        Assert.False(state.IsBusy);
    }

    private static GitCommandScope Begin(GitWorkspaceController state, GitCommandKind kind)
    {
        Assert.True(state.TryBeginCommand(kind, out var scope));
        return Assert.IsType<GitCommandScope>(scope);
    }

    private static GitCredentialResolverInfo Resolver(string id, int order) => new()
    {
        ResolverId = id, DisplayName = id, ImplementationType = $"Tests.{id}", Order = order
    };

    private static TaskCompletionSource<T> Signal<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static Task ObserveAsync(Task task) => task.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);
}
