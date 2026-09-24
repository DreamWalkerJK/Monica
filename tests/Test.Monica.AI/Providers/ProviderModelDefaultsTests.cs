using AwesomeAssertions;
using Monica.AI.Configuration.Services;
using Monica.AI.Models;
using Monica.AI.Providers;
using Test.Monica.AI.Support;

namespace Test.Monica.AI.Providers;

public sealed class ProviderModelDefaultsTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("https://api.openai.com/v1", true)]
    [InlineData("http://api.openai.com/v1", false)]
    [InlineData("https://api.openai.com:8443/v1", false)]
    [InlineData("https://custom.example/v1", false)]
    [InlineData("https://api.openai.com.example/v1", false)]
    public void CodeModels_WhenNameMatchesBuiltIn_ShouldUseOnlyVerifiedEndpointTemplates(string? endpoint, bool expectedVision)
    {
        using var workspace = new ConfigurationTestWorkspace();
        var options = new OpenAIProviderOptions { ApiKey = "test", BaseUrl = endpoint, SupportedModels = ["gpt-4o"] };
        var baseline = new AIProviderDefinition(EAIProviderType.OpenAI, options).ToConfiguration(workspace.Catalog);
        var direct = AIProviderModelResolver.ResolveModels(workspace.Catalog, options);

        baseline.Models.Single().SupportsImage.Should().Be(expectedVision ? true : null);
        direct.Models.OfType<LLMModelInfo>().Single().SupportsImage.Should().Be(expectedVision ? true : null);
    }

    [Theory]
    [InlineData("https://api.anthropic.com", true)]
    [InlineData("https://api.openai.com/v1", false)]
    [InlineData("https://custom.example/v1", false)]
    public void AnthropicCodeModels_WhenNameMatchesBuiltIn_ShouldUseOnlyItsOfficialEndpoint(string endpoint, bool expectedVision)
    {
        using var workspace = new ConfigurationTestWorkspace();
        var options = new AnthropicProviderOptions
        {
            ApiKey = "test", BaseUrl = endpoint, SupportedModels = ["claude-sonnet-4-20250514"]
        };
        var baseline = new AIProviderDefinition(EAIProviderType.Anthropic, options).ToConfiguration(workspace.Catalog);
        var direct = AIProviderModelResolver.ResolveModels(workspace.Catalog, options);

        baseline.Models.Single().SupportsImage.Should().Be(expectedVision ? true : null);
        direct.Models.OfType<LLMModelInfo>().Single().SupportsImage.Should().Be(expectedVision ? true : null);
    }

    [Fact]
    public void CodeModels_WhenDeveloperReferencesExplicitTemplate_ShouldHonorItOnCustomEndpoints()
    {
        using var workspace = new ConfigurationTestWorkspace();
        workspace.Catalog.AddModel(new LLMModelInfo
        {
            ModelName = "gpt-4o", ContextWindow = 65536, SupportsImage = false, SupportsTools = true
        });
        workspace.Catalog.AddReservedModels([new LLMModelInfo { ModelName = "gpt-4o", SupportsImage = true }]);
        var options = new OpenAIProviderOptions
        {
            ApiKey = "test", BaseUrl = "https://custom.example/v1", SupportedModels = ["gpt-4o"]
        };
        var baseline = new AIProviderDefinition(EAIProviderType.OpenAI, options).ToConfiguration(workspace.Catalog);
        var direct = AIProviderModelResolver.ResolveModels(workspace.Catalog, options).Models.OfType<LLMModelInfo>().Single();

        baseline.Models.Single().ContextWindow.Should().Be(65536);
        baseline.Models.Single().SupportsImage.Should().BeFalse();
        baseline.Models.Single().SupportsTools.Should().BeTrue();
        direct.ContextWindow.Should().Be(65536);
        direct.SupportsImage.Should().BeFalse();
        direct.SupportsTools.Should().BeTrue();
    }

    [Fact]
    public void CodeModels_WhenProviderMetadataIsExplicit_ShouldPreferItOverGlobalTemplates()
    {
        using var workspace = new ConfigurationTestWorkspace();
        workspace.Catalog.AddModel(new LLMModelInfo { ModelName = "gpt-4o", ContextWindow = 65536, SupportsImage = true });
        var options = new OpenAIProviderOptions
        {
            ApiKey = "test", BaseUrl = "https://custom.example/v1", SupportedModels = ["gpt-4o"],
            Models = [new LLMModelInfo { ModelName = "gpt-4o", ContextWindow = 32768, SupportsImage = false }]
        };
        var baseline = new AIProviderDefinition(EAIProviderType.OpenAI, options).ToConfiguration(workspace.Catalog);
        var direct = AIProviderModelResolver.ResolveModels(workspace.Catalog, options).Models.OfType<LLMModelInfo>().Single();

        baseline.Models.Single().ContextWindow.Should().Be(32768);
        baseline.Models.Single().SupportsImage.Should().BeFalse();
        direct.ContextWindow.Should().Be(32768);
        direct.SupportsImage.Should().BeFalse();
    }

    [Fact]
    public void FakeModels_WhenUsingBuiltInEmbeddingShape_ShouldRetainSyntheticProviderSupport()
    {
        using var workspace = new ConfigurationTestWorkspace();
        var options = new FakeProviderOptions { ApiKey = "fake", SupportedModels = ["text-embedding-3-small"] };

        var resolved = AIProviderModelResolver.ResolveModels(workspace.Catalog, options);

        resolved.Models.OfType<EmbeddingModelInfo>().Single().Dimensions.Should().Be(1536);
    }
}
