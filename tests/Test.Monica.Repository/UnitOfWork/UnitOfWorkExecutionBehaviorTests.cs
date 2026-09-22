using Microsoft.Extensions.DependencyInjection;
using Monica.Repository.Persistence.Abstractions;
using Monica.Repository.UnitOfWork.Abstractions;
using Test.Monica.Repository.Hosting;
using Test.Monica.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Test.Monica.Repository.UnitOfWork;

public sealed class UnitOfWorkExecutionBehaviorTests
{
    [Fact]
    public async Task ExecuteAsync_WhenDomainEffectsQueueEvents_ShouldDrainInTheSameScope()
    {
        var seen = new List<TestRepositoryDbContext>();
        await using var app = await new RepositoryScenarioFactory(configure: services =>
        {
            services.AddSingleton(seen);
            services.AddScoped<IDomainEventHandler<First>, FirstHandler>();
            services.AddScoped<IDomainEventHandler<Second>, SecondHandler>();
        }).CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        TestRepositoryDbContext? operationContext = null;
        await app.ExecuteAsync(scope =>
        {
            operationContext = scope.Resolve<TestRepositoryDbContext>();
            scope.Resolve<IDomainEventQueue>().Enqueue(new First());
            return Task.CompletedTask;
        }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, seen.Count);
        Assert.All(seen, db => Assert.Same(operationContext, db));
        await app.VerifyAsync<TestRepositoryDbContext>(async (db, ct) =>
            Assert.Equal(2, await db.HardDeleteRows.CountAsync(ct)), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDomainEventsCycle_ShouldFailAndRollBack()
    {
        await using var app = await new RepositoryScenarioFactory(configure: services =>
            services.AddScoped<IDomainEventHandler<Loop>, LoopHandler>())
            .CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => app.ExecuteAsync(scope =>
        {
            scope.Resolve<IDomainEventQueue>().Enqueue(new Loop());
            return Task.CompletedTask;
        }, cancellationToken: TestContext.Current.CancellationToken));
        await app.VerifyAsync<TestRepositoryDbContext>(async (db, ct) =>
            Assert.Equal(0, await db.HardDeleteRows.CountAsync(ct)), TestContext.Current.CancellationToken);
    }

    public sealed record First;
    public sealed record Second;
    public sealed record Loop;
    private sealed class FirstHandler(TestRepositoryDbContext db, IDomainEventQueue queue, List<TestRepositoryDbContext> seen)
        : IDomainEventHandler<First>
    {
        public Task HandleAsync(First domainEvent, CancellationToken cancellationToken)
        {
            seen.Add(db);
            db.Add(new HardDeleteRow { Title = "first" });
            queue.Enqueue(new Second());
            return Task.CompletedTask;
        }
    }
    private sealed class SecondHandler(TestRepositoryDbContext db, List<TestRepositoryDbContext> seen) : IDomainEventHandler<Second>
    {
        public Task HandleAsync(Second domainEvent, CancellationToken cancellationToken)
        {
            seen.Add(db);
            db.Add(new HardDeleteRow { Title = "second" });
            return Task.CompletedTask;
        }
    }
    private sealed class LoopHandler(IDomainEventQueue queue) : IDomainEventHandler<Loop>
    {
        public Task HandleAsync(Loop domainEvent, CancellationToken cancellationToken)
        {
            queue.Enqueue(new Loop());
            return Task.CompletedTask;
        }
    }
}
