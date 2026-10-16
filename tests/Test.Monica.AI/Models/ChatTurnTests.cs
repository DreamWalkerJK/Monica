using AwesomeAssertions;
using Monica.AI.Models;

namespace Test.Monica.AI.Models;

public sealed class ChatTurnTests
{
    [Fact]
    public void Messages_WhenGenerationFailsAfterPartialOutput_ShouldKeepOutputBeforeError()
    {
        var turn = new ChatTurn(
            new AIChatMessage { Role = AIChatRole.User, Content = "question" })
        {
            AssistantMessage = new AIChatMessage
            {
                Role = AIChatRole.Assistant,
                Content = "partial answer"
            }
        };

        turn.AddError("provider failed");
        turn.AddError("provider failed");
        turn.AddError("retry failed");

        turn.Messages.Select(static message => (message.Kind, message.Content))
            .Should().Equal(
                (AIChatMessageKind.Message, "question"),
                (AIChatMessageKind.Message, "partial answer"),
                (AIChatMessageKind.Error, "provider failed"),
                (AIChatMessageKind.Error, "retry failed"));
    }
}
