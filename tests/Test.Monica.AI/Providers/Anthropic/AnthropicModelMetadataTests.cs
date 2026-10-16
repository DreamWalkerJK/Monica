using System.Text.Json;
using Anthropic.Models.Models;
using AwesomeAssertions;
using Monica.AI.Providers.Anthropic;

namespace Test.Monica.AI.Providers.Anthropic;

public sealed class AnthropicModelMetadataTests
{
    [Fact]
    public void Read_WhenEndpointReportsCapabilities_ShouldPreserveEvidenceAndReasoningChoices()
    {
        var model = ModelInfo.FromRawUnchecked(JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""
            {
                "id": "custom-claude", "display_name": "Configured Claude",
                "max_input_tokens": 200000, "max_tokens": 64000,
                "capabilities": {
                    "image_input": { "supported": true }, "pdf_input": { "supported": false },
                    "thinking": { "supported": true },
                    "effort": { "low": { "supported": true }, "high": { "supported": true }, "max": { "supported": false } }
                }
            }
            """)!);

        var configuration = AnthropicModelMetadata.Read(model);

        configuration.ContextWindow.Should().Be(200000);
        configuration.MaxOutputTokens.Should().Be(64000);
        configuration.SupportsImage.Should().BeTrue();
        configuration.SupportsDocuments.Should().BeFalse();
        configuration.SupportsTools.Should().BeNull();
        configuration.ReasoningLevels.Select(static level => level.ProviderValue).Should().Equal("low", "high");
    }

    [Fact]
    public void Read_WhenEndpointOnlyListsModelNames_ShouldLeaveCapabilitiesUnknown()
    {
        var model = ModelInfo.FromRawUnchecked(JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""
            { "id": "claude-opus-alias", "display_name": "Claude Alias" }
            """)!);

        var configuration = AnthropicModelMetadata.Read(model);

        configuration.ContextWindow.Should().BeNull();
        configuration.SupportsImage.Should().BeNull();
        configuration.SupportsDocuments.Should().BeNull();
        configuration.SupportsReasoning.Should().BeNull();
        configuration.ReasoningLevels.Should().BeEmpty();
    }

    [Fact]
    public void Read_WhenThinkingIsExplicitlyUnsupported_ShouldOmitAdvertisedEfforts()
    {
        var model = ModelInfo.FromRawUnchecked(JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""
            {"id":"text-only","capabilities":{"thinking":{"supported":false},"effort":{"high":{"supported":true}}}}
            """)!);

        var configuration = AnthropicModelMetadata.Read(model);

        configuration.SupportsReasoning.Should().BeFalse();
        configuration.ReasoningLevels.Should().BeEmpty();
    }
}
