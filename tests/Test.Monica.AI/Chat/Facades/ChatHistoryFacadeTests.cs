using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Monica.AI.Abstractions;
using Monica.AI.AgentCapabilities.Abstractions;
using Monica.AI.Chat.Abstractions;
using Monica.AI.Chat.Facades;
using Monica.AI.Chat.Models;
using Monica.AI.Models;
using Monica.AI.Services;
using Monica.AI.Services.Support;
using Monica.Core.Results;
using Monica.Modules;
using NSubstitute;
using Test.Monica.AI.Support;

namespace Test.Monica.AI.Chat.Facades;

public sealed class ChatHistoryFacadeTests
{
    [Fact]
    public async Task LoadThenSave_WhenIdentityChanges_ShouldRejectCrossPartitionWrite()
    {
        var provider = Substitute.For<IChatHistoryProvider>();
        var resolver = Substitute.For<IChatHistoryPartitionResolver>();
        resolver.ResolveAsync(Arg.Any<CancellationToken>()).Returns(new ChatHistoryPartition("owner"));
        provider.LoadSessionAsync(Arg.Any<ChatHistoryPartition>(), "session", Arg.Any<CancellationToken>()).Returns(CreateSnapshot());
        var facade = new ChatHistoryFacade(provider, resolver, CreateService());
        var loaded = await facade.LoadSessionAsync("session", TestContext.Current.CancellationToken);
        loaded.Status.Should().Be(ResStatus.Ok);
        loaded.Message.Should().BeNull();
        loaded.Data.Should().NotBeNull();
        await using var session = loaded.Data!;
        session.Partition!.Key.Should().Be("owner");
        session.IsRuntimeActive.Should().BeFalse();
        resolver.ResolveAsync(Arg.Any<CancellationToken>()).Returns(new ChatHistoryPartition("other-user"));

        var result = await facade.SaveSessionAsync(session, 0, TestContext.Current.CancellationToken);

        result.IsFailed(out _).Should().BeTrue();
        result.Message.Should().Contain("different user or workspace");
        await provider.DidNotReceive().SaveSessionAsync(Arg.Any<ChatHistoryPartition>(), Arg.Any<ChatSessionSnapshot>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCatalog_WhenCancelled_ShouldPropagateCancellationWithoutFailureEnvelope()
    {
        var resolver = Substitute.For<IChatHistoryPartitionResolver>();
        resolver.ResolveAsync(Arg.Any<CancellationToken>()).Returns(_ => ValueTask.FromException<ChatHistoryPartition>(new OperationCanceledException()));
        var facade = new ChatHistoryFacade(Substitute.For<IChatHistoryProvider>(), resolver, CreateService());
        var action = async () => await facade.GetCatalogAsync(TestContext.Current.CancellationToken);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    private static ChatSessionSnapshot CreateSnapshot()
    {
        var now = DateTimeOffset.UtcNow;
        return new ChatSessionSnapshot { SessionId = "session", Title = "Saved", CreatedAt = now, UpdatedAt = now,
            Settings = new ChatSessionSettings("provider"), Turns = [] };
    }

    private static AIChatService CreateService() => new(Substitute.For<IAIProviderFactory>(), Options.Create(new ModuleAIOption()),
        new TestAIChatAgentFactory(static (_, _, _) => throw new InvalidOperationException("History must not activate an agent.")),
        Substitute.For<IAgentCapabilityStateStore>(), new AgentStreamingCoordinator(new AIChatRuntimeContextAccessor(), new AgentResponseUpdateChannelContext()),
        Substitute.For<IChatAttachmentStore>());
}
