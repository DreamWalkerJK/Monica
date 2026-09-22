using System.Collections.Concurrent;
using AwesomeAssertions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Monica.AI.Abstractions;
using Monica.AI.AgentCapabilities.Models;
using Monica.AI.Services;
using Monica.AI.Services.Support;

namespace Test.Monica.AI.Services;

public sealed class AIChatAgentFactoryTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RunAsync_WhenToolsHaveMixedApprovalRequirements_ShouldHonorRuleAndResumeBothTools(
        bool autoApprove)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var executedTools = new ConcurrentQueue<string>();
        var approvalContexts = new List<ToolAutoApprovalRuleContext>();
        var readTool = AIFunctionFactory.Create(() =>
        {
            executedTools.Enqueue("read");
            return "read result";
        }, "read");
        var writeTool = new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() =>
        {
            executedTools.Enqueue("write");
            return "write result";
        }, "write"));
        var contributor = new ToolContributor([readTool, writeTool], context =>
        {
            approvalContexts.Add(context);
            return ValueTask.FromResult(autoApprove);
        });
        using var services = new ServiceCollection().BuildServiceProvider();
        using var chatClient = new ToolCallingChatClient();
        var factory = new AIChatAgentFactory([contributor], [], NullLoggerFactory.Instance, services);
        await using var runtime = await factory.CreateAsync(chatClient, new AIChatAgentCreateContext
        {
            CapabilityState = new AgentCapabilityState()
        }, cancellationToken);
        var session = await runtime.Agent.CreateSessionAsync(cancellationToken);

        var response = await runtime.Agent.RunAsync("use tools", session, cancellationToken: cancellationToken);

        if (!autoApprove)
        {
            var request = response.Messages.SelectMany(message => message.Contents)
                .OfType<ToolApprovalRequestContent>().Should().ContainSingle().Which;
            executedTools.Should().BeEmpty();
            response = await runtime.Agent.RunAsync(
                new ChatMessage(ChatRole.User, [request.CreateResponse(approved: true)]),
                session,
                cancellationToken: cancellationToken);
        }

        response.Text.Should().Be("finished");
        executedTools.Should().BeEquivalentTo(["read", "write"]);
        approvalContexts.Should().ContainSingle();
        var approvalContext = approvalContexts.Single();
        approvalContext.FunctionCallContent.Name.Should().Be("write");
        approvalContext.Session.Should().BeSameAs(session);
        approvalContext.RequestMessages.Should().Contain(message => message.Text == "use tools");
        chatClient.FinalRequest.SelectMany(message => message.Contents)
            .OfType<FunctionResultContent>().Select(result => result.CallId)
            .Should().BeEquivalentTo(["read-call", "write-call"]);
    }

    private sealed class ToolContributor(
        IReadOnlyList<AITool> tools,
        Func<ToolAutoApprovalRuleContext, ValueTask<bool>> approvalRule) : IAIChatAgentContributor
    {
        public ValueTask ContributeAsync(
            AIChatAgentContributionContext context,
            CancellationToken cancellationToken = default)
        {
            context.AddTools(tools);
            context.AddToolAutoApprovalRule(approvalRule);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ToolCallingChatClient : IChatClient
    {
        private int _requestCount;

        public IReadOnlyList<ChatMessage> FinalRequest { get; private set; } = [];

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            FinalRequest = messages.ToList();
            var message = _requestCount++ == 0
                ? new ChatMessage(ChatRole.Assistant,
                [
                    new FunctionCallContent("read-call", "read", new Dictionary<string, object?>()),
                    new FunctionCallContent("write-call", "write", new Dictionary<string, object?>())
                ])
                : new ChatMessage(ChatRole.Assistant, "finished");
            return Task.FromResult(new ChatResponse(message));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("This scenario exercises non-streaming tool approval.");

        public object? GetService(Type serviceType, object? serviceKey = null)
            => serviceType == typeof(IChatClient) ? this : null;

        public void Dispose()
        {
        }
    }
}
