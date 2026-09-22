using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Monica.AI.Chat.Models;
using Monica.AI.Models;
using Monica.AI.UI.Localization;
using Monica.AI.UI.UIChat.Components;
using Monica.Testing.Localization;
using MudBlazor;
using MudBlazor.Services;

namespace Test.Monica.AI.UI.UIChat.Components;

public sealed class SessionListTests : BunitContext
{
    public SessionListTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<IStringLocalizer<AIResource>, EchoStringLocalizer<AIResource>>();
        Render<MudPopoverProvider>();
    }

    [Fact]
    public void Render_WhenHistoryIsLoading_ShouldExposeBusyStatusWithoutEmptyState()
    {
        var component = Render<SessionList>(parameters => parameters
            .Add(item => item.IsLoading, true));

        component.Find(".session-list-content").GetAttribute("aria-busy").Should().Be("true");
        component.Find("[role='status']").TextContent.Should().Contain("Chat:History:Loading");
        component.Markup.Should().NotContain("Session:NoConversations");
    }

    [Fact]
    public void Render_WhenSessionExists_ShouldExposeNamedDeleteAndClearActions()
    {
        var summary = new ChatSessionSummary
        {
            SessionId = "session-1",
            Title = "Conversation",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Settings = new ChatSessionSettings("provider", "model", null, null)
        };

        var component = Render<SessionList>(parameters => parameters
            .Add(item => item.Sessions, [summary])
            .Add(item => item.CurrentSessionId, summary.SessionId));

        component.Find("button[aria-label='Session:Delete']").Should().NotBeNull();
        component.Find("button[aria-label='Chat:History:Clear:Action']").Should().NotBeNull();
    }

    [Fact]
    public void SelectedConversation_ShouldStaySelectedWhenSavingMovesItToTheTop()
    {
        var first = new ChatSessionSummary { SessionId = "first", Title = "First conversation", Settings = new("provider") };
        var selected = new ChatSessionSummary { SessionId = "selected", Title = "Selected conversation", Settings = new("provider") };
        var view = Render<SessionList>(parameters => parameters
            .Add(component => component.Sessions, [first, selected])
            .Add(component => component.CurrentSessionId, selected.SessionId));

        view.Find(".mud-selected-item .session-title").TextContent.Should().Be(selected.Title);

        view.Render(parameters => parameters.Add(component => component.Sessions, [selected, first]));

        view.FindAll(".mud-selected-item").Should().ContainSingle()
            .Which.QuerySelector(".session-title")!.TextContent.Should().Be(selected.Title);
    }
}
