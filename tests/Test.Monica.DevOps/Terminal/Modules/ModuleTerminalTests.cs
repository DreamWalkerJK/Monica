using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monica.Core.Modularity.Abstractions;
using Monica.DevOps.Localization;
using Monica.DevOps.Terminal.Facades;
using Monica.DevOps.Terminal.Services;
using Monica.DevOps.Terminal.Services.Support;
using Monica.Modules;
using Monica.Testing.Hosting;
using Monica.Testing.Localization;
using Test.Monica.DevOps.Terminal.Support;
using Xunit;

namespace Test.Monica.DevOps.Terminal.Modules;

public sealed class ModuleTerminalTests
{
    [Fact]
    public async Task AddTerminal_WhenHostsAndScopesDiffer_ShouldShareOnlyHostOwnedSessionsAndScopeFacades()
    {
        var factory = new TerminalApplicationFactory();
        await using var first = await factory.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        await using var second = await factory.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        await using var firstScope = first.CreateScope(TestContext.Current.CancellationToken);
        await using var anotherScope = first.CreateScope(TestContext.Current.CancellationToken);
        await using var secondScope = second.CreateScope(TestContext.Current.CancellationToken);
        var sessions = firstScope.Resolve<TerminalSessionService>();
        var created = sessions.CreateSession("same-owner");
        Assert.Same(sessions, anotherScope.Resolve<TerminalSessionService>());
        Assert.Same(firstScope.Resolve<TerminalCommandRunner>(), anotherScope.Resolve<TerminalCommandRunner>());
        Assert.NotSame(firstScope.Resolve<TerminalFacade>(), anotherScope.Resolve<TerminalFacade>());
        Assert.Equal(created.Id, Assert.Single(anotherScope.Resolve<TerminalSessionService>().GetSessions("same-owner")).Id);
        Assert.Empty(secondScope.Resolve<TerminalSessionService>().GetSessions("same-owner"));
        Assert.Empty(sessions.GetSessions("SAME-OWNER"));
        Assert.Single(sessions.GetSessions(" same-owner "));
    }

    [Fact]
    public async Task DisposeAsync_WhenClosedSessionStillRuns_ShouldJoinActualRemovedProducerThroughRealHostLifetime()
    {
        await using var fixture = new TerminalRuntimeFixture();
        var application = await new TerminalApplicationFactory(fixture.Factory).CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        Task? disposal = null;
        var process = fixture.Factory.NextProcess;
        try
        {
            await using (var scope = application.CreateScope(TestContext.Current.CancellationToken))
            {
                var sessions = scope.Resolve<TerminalSessionService>();
                var session = sessions.CreateSession("owner", new() { WorkingDirectory = fixture.Root });
                sessions.StartCommand("owner", session.Id, new() { Command = "harmless" });
                await sessions.CloseSessionAsync("owner", session.Id);
                Assert.Empty(sessions.GetSessions("owner"));
            }
            await process.KillRequested.WaitAsync(TestContext.Current.CancellationToken);
            disposal = application.DisposeAsync().AsTask();
            Assert.False(disposal.IsCompleted);
            process.CompleteRootExit();
            process.CompleteOutput();
            Assert.False(disposal.IsCompleted);
            Assert.Equal(0, process.DisposeCount);
            process.CompleteError();
            await disposal;
            Assert.Equal(1, process.DisposeCount);
        }
        finally
        {
            process.Complete();
            if (disposal is not null) await disposal;
            else await application.DisposeAsync();
        }
    }

    private sealed class TerminalApplicationFactory(ControlledTerminalProcessFactory? processFactory = null)
        : MonicaTestApplicationFactory<ModuleTerminalTests>
    {
        protected override void ConfigureMonica(IMonicaBuilder builder) => builder.AddTerminal();
        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddSingleton<IStringLocalizer<TerminalResource>>(new EchoStringLocalizer<TerminalResource>());
            if (processFactory is not null)
                services.AddSingleton(provider => new TerminalCommandRunner(
                    provider.GetRequiredService<TerminalEnvironmentDetector>(),
                    provider.GetRequiredService<IOptions<ModuleTerminalOption>>(),
                    provider.GetRequiredService<ILogger<TerminalCommandRunner>>(), processFactory));
        }
    }
}
