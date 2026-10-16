using AwesomeAssertions;
using Monica.AI.Models;
using Monica.AI.Providers;
using Monica.AI.Providers.OpenAI;
using Monica.AI.Services;

namespace Test.Monica.AI.Providers.OpenAI;

public sealed class OpenAIProviderCompatibilityTests
{
    [Theory]
    [InlineData(OpenAIProviderApiMode.Chat)]
    [InlineData(OpenAIProviderApiMode.Responses)]
    public void GetChatClient_WhenUsingInstalledSdk_ShouldConstructBothProtocolAdapters(OpenAIProviderApiMode mode)
    {
        // Exercise the production switch: a Responses SDK type mismatch can also break its Chat branch at JIT time.
        using var provider = new OpenAIProvider(new OpenAIProviderOptions
        {
            ApiKey = "compatibility-test-key", BaseUrl = "https://model.example/v1", ApiMode = mode,
            DefaultModel = "test-model", Models = [new LLMModelInfo { ModelName = "test-model" }]
        }, new AIModelCatalog());

        var client = provider.GetChatClient();

        client.Should().NotBeNull();
        provider.GetChatClient().Should().BeSameAs(client);
    }
}
