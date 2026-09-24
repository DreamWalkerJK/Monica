using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Monica.AI.Configuration.Models;
using Monica.AI.Models;
using Monica.AI.UI.Localization;
using Monica.AI.UI.UIChat.Components;
using Monica.Testing.Localization;
using MudBlazor;
using MudBlazor.Services;

namespace Test.Monica.AI.UI.UIChat.Components;

public sealed class ChatComposerControlsTests : BunitContext
{
    private readonly IRenderedComponent<MudPopoverProvider> _popovers;
    public ChatComposerControlsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<IStringLocalizer<AIResource>, EchoStringLocalizer<AIResource>>();
        _popovers = Render<MudPopoverProvider>();
    }

    [Fact]
    public void Selector_ShouldShowEffectiveDefaultEffortAndFullModelIdentity()
    {
        var model = new LLMModelInfo { ModelName = "long-exact-model-id", DisplayName = "Friendly model", SupportsReasoning = true, DefaultReasoningLevel = "high", ReasoningLevels = [new AIReasoningLevel { Id = "high", DisplayName = "High", ProviderValue = "high" }] };
        var view = Render<ChatModelSelector>(parameters => parameters.Add(component => component.ModelName, model.ModelName).Add(component => component.ProviderId, "provider").Add(component => component.CurrentModel, model));

        view.Find(".mud-menu-button-activator").TextContent.Should().Contain("Friendly model").And.Contain("High");
        view.Find(".model-selector").GetAttribute("title").Should().Contain("provider / long-exact-model-id");
    }

    [Fact]
    public void Selector_WhenReasoningIsExplicitlyUnsupported_ShouldHideEffortEvenWithStaleLevels()
    {
        var model = new LLMModelInfo { ModelName = "fast", SupportsReasoning = false, ReasoningLevels = [new AIReasoningLevel { Id = "high", ProviderValue = "high" }] };
        var view = Render<ChatModelSelector>(parameters => parameters.Add(component => component.CurrentModel, model));
        view.Find(".mud-menu-button-activator").TextContent.Should().NotContain(" · ");
        view.Find(".mud-menu-button-activator").Click();
        _popovers.FindAll(".menu-branch").Should().ContainSingle();
        _popovers.Markup.Should().NotContain("Workbench:Effort");
    }

    [Fact]
    public async Task Selector_ShouldSearchAcrossProvidersAndEmitOneCompleteSelection()
    {
        (string ProviderId, string ModelName)? selected = null;
        var models = new AIProviderInfo[]
        {
            new() { ProviderId = "first", DisplayName = "First provider", ProviderType = "OpenAI", SupportedModels = [new LLMModelInfo { ModelName = "alpha" }] },
            new() { ProviderId = "second", DisplayName = "Second provider", ProviderType = "OpenAI", SupportedModels = [new LLMModelInfo { ModelName = "beta", DisplayName = "Vision beta" }] }
        };
        var view = Render<ChatModelSelector>(parameters => parameters.Add(component => component.Providers, models).Add(component => component.ProviderId, "first").Add(component => component.ModelName, "alpha").Add(component => component.OnModelChanged, value => selected = value));
        view.Find(".mud-menu-button-activator").Click();
        _popovers.Find(".mud-menu-sub-menu-activator").Click();
        var search = _popovers.FindComponent<MudTextField<string>>();
        await view.InvokeAsync(() => search.Instance.ValueChanged.InvokeAsync("Vision"));
        var rows = _popovers.FindComponents<MudMenuItem>().Where(item => item.Instance.OnClick.HasDelegate && !item.Instance.Disabled).ToArray();
        var choice = rows.Single(item => item.Markup.Contains("Vision beta", StringComparison.Ordinal));
        await view.InvokeAsync(() => choice.Instance.OnClick.InvokeAsync());

        selected.Should().Be(("second", "beta"));
        _popovers.FindAll(".model-choice").Should().ContainSingle()
            .Which.TextContent.Should().Contain("Vision beta").And.NotContain("alpha");
    }
}
