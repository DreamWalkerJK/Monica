using Monica.DevOps.Git.State;
using Test.Monica.DevOps.Git.Support;
using Xunit;

namespace Test.Monica.DevOps.Git.State;

public sealed class GitWorkspaceCommandAdmissionTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public async Task TryBeginCommand_WhenKindIsUndefined_ShouldRejectWithoutReservationNotificationOrProducer(int value)
    {
        var service = new ControlledGitRepositoryService();
        await using var state = service.Create();
        await state.RefreshAsync(false).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var notifications = 0;
        state.Changed += () => notifications++;

        Assert.False(state.TryBeginCommand((GitCommandKind)value, out var scope));

        Assert.Null(scope);
        Assert.Null(state.Operation);
        Assert.Null(state.Outcome);
        Assert.False(state.IsBusy);
        Assert.Equal(0, notifications);
        Assert.Empty(service.SyncCalls);
        Assert.Empty(service.SyncAllCalls);
        Assert.Empty(service.DeleteCalls);
        Assert.Equal(1, service.RepositoryReads);
    }
}
