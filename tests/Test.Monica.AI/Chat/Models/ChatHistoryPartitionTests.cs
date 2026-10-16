using System.Security.Claims;
using AwesomeAssertions;
using Monica.AI.Chat.Models;

namespace Test.Monica.AI.Chat.Models;

public sealed class ChatHistoryPartitionTests
{
    [Fact]
    public void ForIdentity_WhenUserWorkspaceOrIssuerDiffers_ShouldIsolatePartition()
    {
        static ClaimsPrincipal User(string subject, string issuer = "issuer") =>
            new(new ClaimsIdentity([new Claim("sub", subject, ClaimValueTypes.String, issuer)], "authenticated"));
        var first = ChatHistoryPartition.ForIdentity("workspace-one", User("alice"));

        first.Should().Be(ChatHistoryPartition.ForIdentity("workspace-one", User("alice")));
        first.Should().NotBe(ChatHistoryPartition.ForIdentity("workspace-one", User("bob")));
        first.Should().NotBe(ChatHistoryPartition.ForIdentity("workspace-two", User("alice")));
        first.Should().NotBe(ChatHistoryPartition.ForIdentity("workspace-one", User("alice", "another-issuer")));
        first.Should().NotBe(ChatHistoryPartition.ForIdentity("workspace-one", null));
    }

    [Fact]
    public void ForIdentity_WhenAuthenticatedSubjectIsMissing_ShouldRejectAnonymousFallback()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([], "authenticated"));
        Action resolve = () => ChatHistoryPartition.ForIdentity("workspace", principal);

        resolve.Should().Throw<InvalidOperationException>();
    }
}
