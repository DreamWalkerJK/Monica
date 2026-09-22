using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Test.Monica.Repository.Hosting;
using Test.Monica.Repository.Persistence;
using Xunit;

namespace Test.Monica.Repository.UnitOfWork;

public sealed class UnitOfWorkManagerTests
{
    [Fact]
    public async Task RunAsync_WhenCancellationAndRollbackFail_ShouldPreservePrimaryException()
    {
        using var cancellation = new CancellationTokenSource();
        var rollbackFailure = new InvalidOperationException("rollback failed");
        var interceptor = new RollbackFailure(rollbackFailure);
        await using var app = await new RepositoryScenarioFactory(interceptor: interceptor)
            .CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        var operationFailure = new OperationCanceledException(cancellation.Token);
        var thrown = await Assert.ThrowsAsync<OperationCanceledException>(() => app.ExecuteAsync(async scope =>
        {
            scope.Resolve<TestRepositoryDbContext>().Add(new SoftDeleteAuditRow { Title = "rolled back on dispose" });
            await scope.Resolve<TestRepositoryDbContext>().SaveChangesAsync(cancellation.Token);
            await cancellation.CancelAsync();
            await ThrowOperationAsync(operationFailure);
        }, cancellationToken: cancellation.Token));
        Assert.Same(operationFailure, thrown);
        Assert.Contains(nameof(ThrowOperationAsync), thrown.StackTrace);
        Assert.Same(rollbackFailure, thrown.Data["Monica.Repository.UnitOfWork.RollbackException"]);
        Assert.False(interceptor.RollbackToken.CanBeCanceled);
        await app.VerifyAsync<TestRepositoryDbContext>(async (db, ct) =>
            Assert.Equal(0, await db.SoftDeleteRows.CountAsync(ct)), TestContext.Current.CancellationToken);
    }

    private static async Task ThrowOperationAsync(Exception failure)
    {
        await Task.Yield();
        throw failure;
    }

    private sealed class RollbackFailure(Exception failure) : DbTransactionInterceptor
    {
        public CancellationToken RollbackToken { get; private set; }
        public override ValueTask<InterceptionResult> TransactionRollingBackAsync(
            DbTransaction transaction, TransactionEventData eventData, InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            RollbackToken = cancellationToken;
            throw failure;
        }
    }
}
