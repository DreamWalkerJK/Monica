using AwesomeAssertions;
using Monica.AI.Providers.OpenAI;

namespace Test.Monica.AI.Providers.OpenAI;

public sealed class OpenAIModelMetadataTests
{
    [Fact]
    public void IdentifierOnlyModels_ShouldKeepCapabilitiesUnknownRegardlessOfAlias()
    {
        var models = OpenAIModelMetadata.Read(BinaryData.FromString("""
            {"data":[{"id":"deepseek-v4-flash-0731-fast-max","supported_endpoint_types":["openai"]},{"id":"qwen3.8-flash"}]}
            """));

        models.Should().HaveCount(2);
        models.Values.Should().OnlyContain(model => model.ContextWindow == null && model.MaxOutputTokens == null
            && model.SupportsImage == null && model.SupportsDocuments == null && model.SupportsReasoning == null && model.SupportsTools == null);
    }

    [Fact]
    public void EnrichedModel_ShouldPreserveExplicitFalseAndReasoningMappings()
    {
        var models = OpenAIModelMetadata.Read(BinaryData.FromString("""
            {"data":[{"id":"custom","context_window":524288,"max_completion_tokens":16384,"capabilities":{"image_input":{"supported":false},"pdf_input":{"supported":true},"tools":true,"reasoning":{"supported":true,"efforts":["none","high","max"]}},"architecture":{"input_modalities":["text","image"]}}]}
            """));

        var model = models["custom"];
        model.ContextWindow.Should().Be(524288);
        model.MaxOutputTokens.Should().Be(16384);
        model.SupportsImage.Should().BeFalse();
        model.SupportsDocuments.Should().BeTrue();
        model.SupportsTools.Should().BeTrue();
        model.SupportsReasoning.Should().BeTrue();
        model.ReasoningLevels.Select(level => level.ProviderValue).Should().Equal("none", "high", "max");
    }

    [Fact]
    public void Read_WhenReasoningIsExplicitlyUnsupported_ShouldIgnoreACommonEffortList()
    {
        var models = OpenAIModelMetadata.Read(BinaryData.FromString("""
            {"data":[{"id":"text-only","supports_reasoning":false,"reasoning_efforts":["none","high"]}]}
            """));

        models["text-only"].SupportsReasoning.Should().BeFalse();
        models["text-only"].ReasoningLevels.Should().BeEmpty();
    }
}
